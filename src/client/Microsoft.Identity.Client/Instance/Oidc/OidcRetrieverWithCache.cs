// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
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
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <param name="issuer">The issuer from the OIDC metadata - the single source of truth.</param>
        /// <exception cref="MsalServiceException">Thrown when issuer validation fails.</exception>
        private static void ValidateIssuer(Uri authority, string issuer)
        {
            // Normalize both URLs to handle trailing slash differences
            string normalizedAuthority = authority.AbsoluteUri.TrimEnd('/');
            string normalizedIssuer = issuer?.TrimEnd('/');

            // Primary validation: check if normalized authority starts with normalized issuer (case-insensitive comparison)
            if (normalizedAuthority.StartsWith(normalizedIssuer, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Extract tenant for CIAM scenarios. In a CIAM scenario the issuer is expected to have "{tenant}.ciamlogin.com"
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
