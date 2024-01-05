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

            // Skip checking cache when force refresh is specified
            if (_managedIdentityParameters.ForceRefresh)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("[ManagedIdentityRequest] Skipped looking for a cached access token because ForceRefresh was set.");
                authResult = await GetAccessTokenAsync(cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            // No access token or cached access token needs to be refreshed 
            if (cachedAccessTokenItem != null)
            {
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);

                logger.Info("[ManagedIdentityRequest] Access token retrieved from cache.");

                try
                {  
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // If needed, refreshes token in the background
                    if (proactivelyRefresh)
                    {
                        logger.Info("[ManagedIdentityRequest] Initiating a proactive refresh.");

                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                        () =>
                        {
                            // Use a linked token source, in case the original cancellation token source is disposed before this background task completes.
                            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            return GetAccessTokenAsync(tokenSource.Token, logger);
                        }, logger);
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

                logger.Info("[ManagedIdentityRequest] No cached access token. Getting a token from the managed identity endpoint.");
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


            // Requests to a managed identity endpoint must be throttled; 
            // otherwise, the endpoint will throw a HTTP 429.
            logger.Verbose(() => "[ManagedIdentityRequest] Entering managed identity request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[ManagedIdentityRequest] Entered managed identity request semaphore.");

            try
            {
                // Bypass cache and send request to token endpoint, when
                // 1. Force refresh is requested, or
                // 2. If the access token needs to be refreshed proactively.
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
