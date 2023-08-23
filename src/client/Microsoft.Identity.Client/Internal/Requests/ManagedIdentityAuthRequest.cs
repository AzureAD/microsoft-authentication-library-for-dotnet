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
            if (string.IsNullOrEmpty(_managedIdentityParameters.Resource))
            {
                throw new MsalClientException(
                    MsalError.ScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }

            AuthenticationResult authResult = null;
            bool proactivelyRefresh = false;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            var logger = AuthenticationRequestParameters.RequestContext.Logger;
            CacheRefreshReason cacheInfoTelemetry = CacheRefreshReason.NotApplicable;

            if (!_managedIdentityParameters.ForceRefresh)
            {
                cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                if (cachedAccessTokenItem != null)
                {
                    authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                }
                else
                {
                    if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                    {
                        cacheInfoTelemetry = CacheRefreshReason.NoCachedAccessToken;
                    }
                }
            }
            else
            {
                cacheInfoTelemetry = CacheRefreshReason.ForceRefreshOrClaims;
                logger.Info("Skipped looking for an Access Token in the cache because ForceRefresh or Claims were set. ");
            }

            if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.NotApplicable)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = cacheInfoTelemetry;
            }

            // No AT in the cache or AT needs to be refreshed
            try
            {
                if (cachedAccessTokenItem == null)
                {
                    authResult = await FetchNewAccessTokenAsync(proactivelyRefresh, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // may fire a request to get a new token in the background
                    if (proactivelyRefresh)
                    {
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                        () => FetchNewAccessTokenAsync(proactivelyRefresh, cancellationToken), logger);
                    }
                }

                return authResult;
            }
            catch (MsalServiceException e)
            {
                return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
            }

        }

        private async Task<AuthenticationResult> FetchNewAccessTokenAsync(bool proactivelyRefresh, CancellationToken cancellationToken)
        {
            AuthenticationResult authResult;
            
            await s_semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                // Bypass cache and send request to token endpoint, when
                // 1. Force refresh is requested, or
                // 2. No AT is found in the cache, or
                // 3. If the AT needs to be refreshed pro-actively 
                if (cachedAccessTokenItem == null || _managedIdentityParameters.ForceRefresh || proactivelyRefresh)
                {
                    authResult = await SendTokenRequestForManagedIdentityAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                }

                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
            }
        }

        private async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            ManagedIdentityClient managedIdentityClient = new ManagedIdentityClient(AuthenticationRequestParameters.RequestContext);

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

            if (cachedAccessTokenItem != null && !_managedIdentityParameters.ForceRefresh)
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
