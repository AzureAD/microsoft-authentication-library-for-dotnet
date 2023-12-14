// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ManagedIdentityAuthRequest : RequestBase
    {
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(1, 1);

        public ManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            AuthenticationResult authResult = null;
            ILoggerAdapter logger = AuthenticationRequestParameters.RequestContext.Logger;

            //skip checking cache for force refresh
            if (_managedIdentityParameters.ForceRefresh)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ManagedIdentityAuthRequest] Skipped looking for an Access Token in the cache because " +
                    "ForceRefresh was set.");
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            //check cache for AT
            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null)
            {
                //return the token in the cache and check if it needs to be proactively refreshed
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);

                logger.Info("[ManagedIdentityAuthRequest] Access token retrieved from cache for managed identity.");

                try
                {  
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // may fire a request to get a new token in the background when AT needs to be refreshed
                    if (proactivelyRefresh)
                    {
                        logger.Info("[ManagedIdentityAuthRequest] Initiating a proactive refresh.");

                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                        () => GetAccessTokenAsync(cancellationToken, logger), logger);
                    }
                }
                catch (MsalServiceException e)
                {
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }
            }
            else
            {
                //  No AT in the cache 
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
                }

                logger.Info("[ManagedIdentityAuthRequest] No access token in the cache. Initiating a request to the " +
                    "managed identity endpoint.");
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
            }

            return authResult;
        }

        private async Task<AuthenticationResult> GetAccessTokenAsync(
            CancellationToken cancellationToken, 
            ILoggerAdapter logger)
        {
            AuthenticationResult authResult;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            logger.Verbose(() => "[ManagedIdentityAuthRequest] Entering token acquire for managed identity " +
            "endpoint semaphore.");

            // Requests to a managed identity endpoint must be throttled; 
            // otherwise, the endpoint will throw a HTTP 429.
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityAuthRequest] Entered token acquire for managed identity " +
            "endpoint semaphore.");

            try
            {
                // Bypass cache and send request to token endpoint, when
                // 1. Force refresh is requested, or
                // 2. Proactively Refreshed
                if (_managedIdentityParameters.ForceRefresh || 
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed)
                {
                    logger.Info("[ManagedIdentityAuthRequest] Sending Token response to managed identity endpoint ...");
                    authResult = await SendTokenRequestForManagedIdentityAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.Info("[ManagedIdentityAuthRequest] checking token cache inside managed identity " +
                        "endpoint semaphore.");
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    //when multiple threads try reaching the managed identity endpoint 
                    //check cache again to make sure if a previous thread already acquired 
                    //a token from the endpoint and cached it
                    if (cachedAccessTokenItem != null)
                    {
                        logger.Info("[ManagedIdentityAuthRequest] Getting Access token from cache inside " +
                            "managed identity endpoint semaphore.");
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                    else
                    {
                        logger.Info("[ManagedIdentityAuthRequest] Sending Token response to managed identity endpoint ...");
                        authResult = await SendTokenRequestForManagedIdentityAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[ManagedIdentityAuthRequest] Released semaphore on token acquire " +
                "from managed identity endpoint.");
            }
        }

        private async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            ManagedIdentityClient managedIdentityClient = 
                new ManagedIdentityClient(AuthenticationRequestParameters.RequestContext);

            ManagedIdentityResponse managedIdentityResponse =
                await managedIdentityClient
                .SendTokenRequestForManagedIdentityAsync(_managedIdentityParameters, cancellationToken)
                .ConfigureAwait(false);

            var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
            msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
                Metrics.IncrementTotalAccessTokensFromCache();
                return cachedAccessTokenItem;
            }

            return null;
        }

        private AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            AuthenticationResult authResult = new AuthenticationResult(
                                                            cachedAccessTokenItem,
                                                            null,
                                                            AuthenticationRequestParameters.AuthenticationScheme,
                                                            AuthenticationRequestParameters.RequestContext.CorrelationId,
                                                            TokenSource.Cache,
                                                            AuthenticationRequestParameters.RequestContext.ApiEvent,
                                                            account: null,
                                                            spaAuthCode: null,
                                                            additionalResponseParameters: null);
            return authResult;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }
    }
}
