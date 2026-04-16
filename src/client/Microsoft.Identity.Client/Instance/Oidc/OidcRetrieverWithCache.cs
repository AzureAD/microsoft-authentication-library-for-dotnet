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
        /// Aligned with Python MSAL's has_valid_issuer() logic.
        /// </summary>
        /// <param name="authority">The authority URL.</param>
        /// <param name="issuer">The issuer from the OIDC metadata - the single source of truth.</param>
        /// <exception cref="MsalServiceException">Thrown when issuer validation fails.</exception>
        internal static void ValidateIssuer(Uri authority, string issuer)
        {
            if (string.IsNullOrEmpty(issuer) || !Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
            {
                throw new MsalServiceException(
                    MsalError.AuthorityValidationFailed,
                    string.Format(MsalErrorMessage.IssuerValidationFailed, authority, issuer));
            }

            string issuerHost = issuerUri.Host;

            // Check 1: Scheme + Host match (existing check)
            if (string.Equals(authority.Scheme, issuerUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(authority.Host, issuerHost, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Check 2: Well-known authority host allowlist (O(1) lookup)
            if (Constants.WellKnownAuthorityHosts.Contains(issuerHost))
            {
                return;
            }

            // Check 3: Regional variant (e.g., westus2.login.microsoft.com)
            int dotIndex = issuerHost.IndexOf('.');
            if (dotIndex > 0)
            {
                string potentialBase = issuerHost.Substring(dotIndex + 1);

                // 3a: Base host is a well-known authority host
                if (Constants.WellKnownAuthorityHosts.Contains(potentialBase))
                {
                    return;
                }

                // 3b: Base host matches the authority host (e.g., issuer=us.custom.com, authority=custom.com)
                if (string.Equals(potentialBase, authority.Host, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            // Check 4: B2C host suffix (e.g., mytenant.b2clogin.com, mytenant.ciamlogin.com)
            // Uses dot prefix to prevent spoofing (fakeb2clogin.com won't match)
            foreach (string suffix in Constants.WellKnownB2CHostSuffixes)
            {
                if (issuerHost.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
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
