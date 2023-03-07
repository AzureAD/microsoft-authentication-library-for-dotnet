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

        internal const string OpenIdConfigurationEndpointSuffix = ".well-known/openid-configuration";

        public static async Task<OidcMetadata> GetOidcAsync(
            string authority,
            RequestContext requestContext)
        {
            OidcMetadata configuration = null;

            // Conccurent dictionary get or add
            if (s_cache.TryGetValue(authority, out configuration))
                return configuration;

            await s_lockOidcRetrieval.WaitAsync().ConfigureAwait(false);

            try
            {
                // try again in critical section
                if (s_cache.TryGetValue(authority, out configuration))
                    return configuration;

                Uri oidcMetadataEndpoint = new Uri(authority + OpenIdConfigurationEndpointSuffix);

                var client = new OAuth2Client(requestContext.Logger, requestContext.ServiceBundle.HttpManager);
                configuration = await client.DiscoverOidcMetadataAsync(oidcMetadataEndpoint, requestContext).ConfigureAwait(false);

                s_cache[authority] = configuration;
                return configuration;
            }
            catch (Exception ex)
            {
                requestContext.Logger.Error(
                    $"Failed to retrieve OpenId configuration from the OpenId endpoint {authority + OpenIdConfigurationEndpointSuffix} " +
                    $"due to {ex}");
                
                if (ex is MsalServiceException)
                    throw;

                throw new MsalServiceException(
                    "oidc_failure",
                    $"Failed to retrieve OIDC configuration from {authority + OpenIdConfigurationEndpointSuffix}. See inner exception. ",
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
