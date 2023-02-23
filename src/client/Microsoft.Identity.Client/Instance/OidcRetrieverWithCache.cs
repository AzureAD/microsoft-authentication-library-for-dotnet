// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Client.Instance
{
    internal static class OidcRetrieverWithCache
    {
        private static readonly ConcurrentDictionary<string, OpenIdConnectConfiguration> s_cache = new();
        private static readonly SemaphoreSlim s_lockOidcRetrieval = new SemaphoreSlim(1);

        internal const string OpenIdConfigurationEndpointSuffix = ".well-known/openid-configuration";

        public static async Task<OpenIdConnectConfiguration> GetOidcAsync(
            string authority, 
            IHttpManager httpManager, 
            ILoggerAdapter logger, 
            CancellationToken cancellationToken)
        {
            // Conccurent dictionary get or add
            if (s_cache.TryGetValue(authority, out var configuration))
                return configuration;

            await s_lockOidcRetrieval.WaitAsync().ConfigureAwait(false);

            try
            {
                // try again in critical section
                if (s_cache.TryGetValue(authority, out configuration))
                    return configuration;

                HttpClient httpClient = httpManager.GetHttpClient();
                HttpDocumentRetriever httpDocumentRetriever = new HttpDocumentRetriever(httpClient);

                configuration = await OpenIdConnectConfigurationRetriever.GetAsync(
                        authority + OpenIdConfigurationEndpointSuffix,
                        httpDocumentRetriever,
                        cancellationToken).ConfigureAwait(false);

                
                s_cache[authority] = configuration;
                return configuration;
            }
            catch (Exception ex)
            {
                logger.Error(
                    $"Failed to retrieve OpenId configuration from the OpenId endpoint {authority + OpenIdConfigurationEndpointSuffix} " +
                    $"due to {ex}");
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
    }
}
