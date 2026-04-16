// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
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
                ValidateIssuer(new Uri(authority), configuration.Issuer);

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
        /// Validates that the issuer in the OIDC metadata matches the authority.
        /// Aligned with Python MSAL's has_valid_issuer().
        /// An issuer is valid if any of the following is true:
        /// 1. Same scheme and host as the authority (path can differ)
        /// 2. The issuer host is a well-known Microsoft authority host (HTTPS only)
        /// 3. The issuer host is a regional variant of a well-known host or the authority host (HTTPS only)
        /// 4. The issuer host ends with a well-known B2C/CIAM suffix (e.g., tenant.b2clogin.com, tenant.ciamlogin.com) (HTTPS only)
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <param name="issuer">The issuer from the OIDC metadata - the single source of truth.</param>
        /// <exception cref="MsalServiceException">Thrown when issuer validation fails.</exception>
        internal static void ValidateIssuer(Uri authority, string issuer)
        {
            // Early null/empty check
            if (string.IsNullOrEmpty(issuer) || !Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
            {
                throw new MsalServiceException(
                    MsalError.AuthorityValidationFailed,
                    string.Format(MsalErrorMessage.IssuerValidationFailed, authority, issuer));
            }

            // Check 1: Same scheme and host (existing behavior)
            if (string.Equals(authority.Scheme, issuerUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(authority.Host, issuerUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string issuerHost = issuerUri.Host;

            // Checks 2-4 require HTTPS scheme to prevent scheme-downgrade attacks
            bool issuerIsHttps = string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

            if (issuerIsHttps)
            {
                // Check 2: Well-known authority host allowlist (O(1) lookup)
                if (Constants.IsWellKnownAuthorityHost(issuerHost))
                {
                    return;
                }

                // Check 3: Regional variant (e.g., westus2.login.microsoft.com)
                int dotIndex = issuerHost.IndexOf('.');
                if (dotIndex > 0)
                {
                    string potentialBase = issuerHost.Substring(dotIndex + 1);
                    // 3a: Base host is a well-known authority host
                    if (Constants.IsWellKnownAuthorityHost(potentialBase))
                    {
                        return;
                    }
                    // 3b: Base host matches the authority host (e.g., issuer=eastus.myidp.example.com, authority=myidp.example.com)
                    if (string.Equals(potentialBase, authority.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                // Check 4: B2C host suffix (e.g., tenant.b2clogin.com, tenant.ciamlogin.com)
                if (Constants.HasWellKnownB2CHostSuffix(issuerHost))
                {
                    return;
                }
            }

            // Validation failed
            throw new MsalServiceException(
                MsalError.AuthorityValidationFailed,
                string.Format(MsalErrorMessage.IssuerValidationFailed, authority, issuer));
        }

        // For testing purposes only
        public static void ResetCacheForTest()
        {
            s_cache.Clear();
        }
    }
}
