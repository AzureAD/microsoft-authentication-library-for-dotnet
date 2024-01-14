// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Old implementation of MSI, which works by getting tokens directly from the MSI infrastructure. 
    /// </summary>
    internal class LegacyMsiAuthRequest
        : MsiAuthRequest
    {
        public LegacyMsiAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {}

        protected override async Task<AuthenticationResult> GetAccessTokenAsync(
            IKeyMaterialManager keyMaterial,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            AuthenticationResult authResult;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            // Requests to a managed identity endpoint must be throttled; 
            // otherwise, the endpoint will throw a HTTP 429.
            logger.Verbose(() => "[ManagedIdentityRequest] Entering managed identity request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityRequest] Entered managed identity request semaphore.");

            try
            {
                // Bypass cache and send request to token endpoint, when
                // 1. Force refresh is requested, or
                // 2. Proactively Refreshed
                if (_managedIdentityParameters.ForceRefresh ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed)
                {
                    authResult = await SendTokenRequestForManagedIdentityAsync(logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.Info("[ManagedIdentityRequest] Checking for a cached access token.");
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    // Check the cache again after acquiring the semaphore in case the previous request cached a new token.
                    if (cachedAccessTokenItem != null)
                    {
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                    else
                    {
                        authResult = await SendTokenRequestForManagedIdentityAsync(logger, cancellationToken).ConfigureAwait(false);
                    }
                }

                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ManagedIdentityRequest] Released managed identity request semaphore.");
            }
        }

        private async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            logger.Info("[ManagedIdentityRequest] Acquiring a token from the managed identity endpoint.");

            await ResolveAuthorityAsync().ConfigureAwait(false);

            ManagedIdentityClient managedIdentityClient = new(AuthenticationRequestParameters.RequestContext);

            ManagedIdentityResponse managedIdentityResponse =
                await managedIdentityClient
                .SendTokenRequestForManagedIdentityAsync(_managedIdentityParameters, cancellationToken)
                .ConfigureAwait(false);

            var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
            msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }
    }
}
