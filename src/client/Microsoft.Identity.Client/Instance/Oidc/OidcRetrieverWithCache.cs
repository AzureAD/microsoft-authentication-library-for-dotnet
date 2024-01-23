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

            try
            {
                // try again in critical section
                if (s_cache.TryGetValue(authority, out configuration))
                {
                    requestContext.Logger.Verbose(() => $"[OIDC Discovery] OIDC discovery found a cached entry for {authority}");
                    return configuration;
                }

                Uri oidcMetadataEndpoint = new Uri(authority + Constants.WellKnownOpenIdConfigurationPath);

                var client = new OAuth2Client(requestContext.Logger, requestContext.ServiceBundle.HttpManager);
                configuration = await client.DiscoverOidcMetadataAsync(oidcMetadataEndpoint, requestContext).ConfigureAwait(false);

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
                    $"Failed to retrieve OIDC configuration from {authority + Constants.WellKnownOpenIdConfigurationPath}. See inner exception. ",
                    ex);
            }
            finally
            {
                s_lockOidcRetrieval.Release();
            }
        }

        // For testing purposes only
        public static void ResetCacheForTest()
        {
            s_cache.Clear();
        }
    }
}
