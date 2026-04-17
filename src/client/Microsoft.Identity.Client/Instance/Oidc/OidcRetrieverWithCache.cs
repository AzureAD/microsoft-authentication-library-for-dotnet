// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        // PPE hosts excluded from issuer validation – they should not be trusted as production issuers.
        private static readonly HashSet<string> s_ppeHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "login.windows-ppe.net",
            "sts.windows-ppe.net",
            "login.microsoft-ppe.com"
        };

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
        /// An issuer is valid if any of the following is true:
        /// 1. Same scheme and host as the authority (path can differ)
        /// 2. The issuer host is a well-known Microsoft authority host (HTTPS only, excludes PPE)
        /// 3. The issuer host is a regional variant of a well-known host (HTTPS only, excludes PPE)
        /// 4. CIAM-specific: the issuer matches {tenant}.ciamlogin.com patterns
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <param name="issuer">The issuer from the OIDC metadata - the single source of truth.</param>
        /// <exception cref="MsalServiceException">Thrown when issuer validation fails.</exception>
        internal static void ValidateIssuer(Uri authority, string issuer)
        {
            // Normalize both URLs to handle trailing slash differences
            string normalizedAuthority = authority.AbsoluteUri.TrimEnd('/');
            string normalizedIssuer = issuer?.TrimEnd('/');

            // OIDC validation: if the issuer's scheme and host match the authority's, consider it valid
            if (!string.IsNullOrEmpty(issuer) && Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
            {
                // Rule 1: Same scheme and host
                if (string.Equals(authority.Scheme, issuerUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(authority.Host, issuerUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Rule 2: The issuer host is a well-known Microsoft authority host (HTTPS only, excludes PPE)
                if (string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    KnownMetadataProvider.IsKnownEnvironment(issuerUri.Host) &&
                    !s_ppeHosts.Contains(issuerUri.Host))
                {
                    return;
                }

                // Rule 3: The issuer host is a regional variant ({region}.{host}) of a well-known host
                // (HTTPS only, excludes PPE). E.g. westus2.login.microsoft.com
                if (string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    string issuerHost = issuerUri.Host;
                    int firstDot = issuerHost.IndexOf('.');
                    if (firstDot > 0 && firstDot < issuerHost.Length - 1)
                    {
                        string hostWithoutRegion = issuerHost.Substring(firstDot + 1);

                        // Regional variant of a well-known host (e.g. westus2.login.microsoft.com)
                        if (KnownMetadataProvider.IsKnownEnvironment(hostWithoutRegion) &&
                            !s_ppeHosts.Contains(hostWithoutRegion))
                        {
                            return;
                        }
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

        // For testing purposes only
        public static void ResetCacheForTest()
        {
            s_cache.Clear();
        }
    }
}
