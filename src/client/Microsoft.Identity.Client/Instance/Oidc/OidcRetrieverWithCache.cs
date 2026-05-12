// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance.Oidc
{
    internal static class OidcRetrieverWithCache
    {
        private static readonly ConcurrentDictionary<string, OidcMetadata> s_cache = new();
        private static readonly SemaphoreSlim s_lockOidcRetrieval = new SemaphoreSlim(1);

        public static async Task<OidcMetadata> GetOidcAsync(
            string authority,
            RequestContext requestContext)
        {
            if (s_cache.TryGetValue(authority, out OidcMetadata configuration))
            {
                requestContext.Logger.Verbose(() => $"[OIDC Discovery] OIDC discovery found a cached entry for {authority}");
                return configuration;
            }

            await s_lockOidcRetrieval.WaitAsync().ConfigureAwait(false);

            Uri oidcMetadataEndpoint = null;
            try
            {
                // try again in critical section
                if (s_cache.TryGetValue(authority, out configuration))
                {
                    requestContext.Logger.Verbose(() => $"[OIDC Discovery] OIDC discovery found a cached entry for {authority}");
                    return configuration;
                }

                // preserve any query parameters in the authority
                UriBuilder builder = new UriBuilder(authority);
                string existingPath = builder.Path;
                builder.Path = existingPath.TrimEnd('/') + "/" + Constants.WellKnownOpenIdConfigurationPath;

                oidcMetadataEndpoint = builder.Uri;
                var client = new OAuth2Client(requestContext.Logger, requestContext.ServiceBundle.HttpManager, null);
                configuration = await client.DiscoverOidcMetadataAsync(oidcMetadataEndpoint, requestContext).ConfigureAwait(false);

                // Validate the issuer before caching the configuration
                requestContext.Logger.Verbose(() => $"[OIDC Discovery] Validating issuer: {configuration.Issuer} against authority: {authority}");
                Uri authorityUri = new Uri(authority);
                ValidateIssuer(authorityUri, configuration.Issuer);

                // Endpoint same-cloud check: when the configured authority is a known Microsoft
                // host, both token_endpoint and authorization_endpoint MUST resolve to the SAME
                // sovereign cloud. Catches a tampered discovery doc that swaps the endpoint to
                // a different MS cloud OR to an unrelated host. Runs BEFORE the cache write so
                // a bad doc never gets cached.
                EnsureKnownAuthorityEndpointSameCloud(authorityUri, configuration.TokenEndpoint, "token_endpoint", requestContext.Logger);
                EnsureKnownAuthorityEndpointSameCloud(authorityUri, configuration.AuthorizationEndpoint, "authorization_endpoint", requestContext.Logger);

                s_cache[authority] = configuration;
                requestContext.Logger.Verbose(() => $"[OIDC Discovery] OIDC discovery retrieved metadata from the network for {authority}");

                return configuration;
            }
            catch (Exception ex)
            {
                requestContext.Logger.Error($"[OIDC Discovery] Failed to retrieve OpenID configuration from the OpenID endpoint {authority + Constants.WellKnownOpenIdConfigurationPath} due to {ex}");

                if (ex is MsalServiceException)
                    throw;

                throw new MsalServiceException(
                    "oidc_failure",
                    $"Failed to retrieve OIDC configuration from {oidcMetadataEndpoint}. See inner exception. ",
                    ex);
            }
            finally
            {
                s_lockOidcRetrieval.Release();
            }
        }

        /// <summary>
        /// Accepts an issuer if any of:
        ///   1. Scheme + host match the authority (path may differ).
        ///      e.g. authority <c>login.microsoftonline.com/contoso</c>, issuer <c>login.microsoftonline.com/contoso/v2.0</c>.
        ///   2. HTTPS issuer hosted on a known Microsoft cloud, AND either:
        ///      2a. Authority host is a custom domain (federation case, issue #5927).
        ///          e.g. authority <c>clientlogin.test.parentpay.com/...</c>, issuer <c>login.microsoftonline.com/...</c>.
        ///      2b. Authority host is also a known Microsoft host in the SAME sovereign cloud.
        ///          Rejects e.g. Public authority + China issuer.
        ///   3. CIAM: issuer matches <c>{tenant}.ciamlogin.com</c>.
        /// </summary>
        internal static void ValidateIssuer(Uri authority, string issuer)
        {
            string normalizedIssuer = issuer?.TrimEnd('/');

            if (!string.IsNullOrEmpty(issuer) && Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
            {
                // Rule 1
                if (string.Equals(authority.Scheme, issuerUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(authority.Host, issuerUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Rule 2: known-MS issuer over HTTPS. Single lookup per host (no double-resolve).
                if (string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    KnownMetadataProvider.TryResolveKnownCloud(issuerUri.Host, out InstanceDiscoveryMetadataEntry issuerEntry))
                {
                    // 2a: custom-domain authority federating with Microsoft (#5927)
                    if (!KnownMetadataProvider.TryResolveKnownCloud(authority.Host, out InstanceDiscoveryMetadataEntry authorityEntry))
                    {
                        return;
                    }

                    // 2b: known-MS authority must share the issuer's cloud (singleton entry per cloud)
                    if (ReferenceEquals(issuerEntry, authorityEntry))
                    {
                        return;
                    }
                }
            }

            // CIAM-specific validation: In a CIAM scenario the issuer is expected to have "{tenant}.ciamlogin.com"
            // as the host, even when using a custom domain.
            string tenant = null;
            try
            {
                tenant = AuthorityInfo.GetFirstPathSegment(authority);
            }
            catch (InvalidOperationException)
            {
                // If no path segments exist, try to extract from hostname (first part)
                var hostParts = authority.Host.Split('.');
                tenant = hostParts.Length > 0 ? hostParts[0] : null;
            }

            // If tenant extraction failed or returned empty, validation fails
            if (!string.IsNullOrEmpty(tenant))
            {
                // Create a collection of valid CIAM issuer patterns for the tenant
                string[] validCiamPatterns =
                {
                    $"https://{tenant}{Constants.CiamAuthorityHostSuffix}",
                    $"https://{tenant}{Constants.CiamAuthorityHostSuffix}/{tenant}",
                    $"https://{tenant}{Constants.CiamAuthorityHostSuffix}/{tenant}/v2.0"
                };

                // Normalize and check if the issuer matches any of the valid patterns
                if (validCiamPatterns.Any(pattern =>
                    string.Equals(normalizedIssuer, pattern.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
            }

            // Validation failed
            throw new MsalServiceException(
                MsalError.AuthorityValidationFailed,
                string.Format(MsalErrorMessage.IssuerValidationFailed, authority, issuer));
        }

        // Defense-in-depth endpoint check. When the configured authority is itself a known
        // Microsoft cloud host, every endpoint published by the discovery doc MUST resolve to
        // the same sovereign cloud. Custom-domain authorities (federation, third-party IdPs)
        // are not constrained: the caller has chosen to trust that domain.
        // e.g. authority=login.microsoftonline.com (Public)
        //      token_endpoint=login.chinacloudapi.cn (China)        -> throw
        //      token_endpoint=attacker.example.com                  -> throw
        //      token_endpoint=sts.windows.net (Public alias)        -> pass
        private static void EnsureKnownAuthorityEndpointSameCloud(Uri authority, string endpoint, string endpointName, ILoggerAdapter logger)
        {
            if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
            {
                return;
            }

            if (!KnownMetadataProvider.TryResolveKnownCloud(authority.Host, out InstanceDiscoveryMetadataEntry authorityEntry))
            {
                return;
            }

            if (!KnownMetadataProvider.TryResolveKnownCloud(endpointUri.Host, out InstanceDiscoveryMetadataEntry endpointEntry) ||
                !ReferenceEquals(authorityEntry, endpointEntry))
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.CrossCloudEndpointMismatch,
                    authority.AbsoluteUri,
                    endpointName,
                    endpoint);

                logger.Error("[OIDC Discovery] " + message);

                throw new MsalServiceException(MsalError.CrossCloudEndpointMismatch, message);
            }
        }

        // For testing purposes only
        public static void ResetCacheForTest()
        {
            s_cache.Clear();
        }
    }
}
