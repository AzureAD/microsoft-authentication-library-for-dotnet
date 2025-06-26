﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// MSAL should only interact with the cache though this object. It is responsible for firing cache notifications.
    /// Flows should only perform (at most) 2 cache accesses: one to read data and one to write tokens. Reading data multiple times 
    /// (e.g. read all ATs, read all RTs) should not refresh the cache from disk because of performance impact.
    /// Write operations are still the responsibility of TokenCache.
    /// </summary>
    internal class CacheSessionManager : ICacheSessionManager
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private bool _cacheRefreshedForRead = false;

        public CacheSessionManager(
            ITokenCacheInternal tokenCacheInternal,
            AuthenticationRequestParameters requestParams)
        {
            TokenCacheInternal = tokenCacheInternal ?? throw new ArgumentNullException(nameof(tokenCacheInternal));
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            RequestContext = _requestParams.RequestContext;
        }

        public RequestContext RequestContext { get; }

        #region ICacheSessionManager implementation
        public ITokenCacheInternal TokenCacheInternal { get; }

        public async Task<MsalAccessTokenCacheItem> FindAccessTokenAsync()
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return await TokenCacheInternal.FindAccessTokenAsync(_requestParams).ConfigureAwait(false);
        }

        public async Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>> SaveTokenResponseAsync(MsalTokenResponse tokenResponse)
        {
            var result = await TokenCacheInternal.SaveTokenResponseAsync(_requestParams, tokenResponse).ConfigureAwait(false);
            RequestContext.ApiEvent.CachedAccessTokenCount = TokenCacheInternal.Accessor.EntryCount;
            return result;
        }

        public async Task<Account> GetAccountAssociatedWithAccessTokenAsync(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return await TokenCacheInternal.GetAccountAssociatedWithAccessTokenAsync(_requestParams, msalAccessTokenCacheItem).ConfigureAwait(false);
        }

        public async Task<MsalIdTokenCacheItem> GetIdTokenCacheItemAsync(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return TokenCacheInternal.GetIdTokenCacheItem(accessTokenCacheItem);
        }

        public async Task<MsalRefreshTokenCacheItem> FindFamilyRefreshTokenAsync(string familyId)
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(familyId))
            {
                throw new ArgumentNullException(nameof(familyId));
            }

            return await TokenCacheInternal.FindRefreshTokenAsync(_requestParams, familyId).ConfigureAwait(false);
        }

        public async Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return await TokenCacheInternal.FindRefreshTokenAsync(_requestParams).ConfigureAwait(false);
        }

        public async Task<bool?> IsAppFociMemberAsync(string familyId)
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return await TokenCacheInternal.IsFociMemberAsync(_requestParams, familyId).ConfigureAwait(false);
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
            return await TokenCacheInternal.GetAccountsAsync(_requestParams).ConfigureAwait(false);
        }

        #endregion

        /// <remarks>
        /// Possibly refreshes the internal cache by calling OnBeforeAccessAsync and OnAfterAccessAsync delegates.
        /// </remarks>
        private async Task RefreshCacheForReadOperationsAsync()
        {
            if (TokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                if (!_cacheRefreshedForRead)
                {
                    _requestParams.RequestContext.Logger.Verbose(()=>$"[Cache Session Manager] Entering the cache semaphore. { TokenCacheInternal.Semaphore.GetCurrentCountLogMessage()}");
                    await TokenCacheInternal.Semaphore.WaitAsync(_requestParams.RequestContext.UserCancellationToken).ConfigureAwait(false);
                    _requestParams.RequestContext.Logger.Verbose(()=>"[Cache Session Manager] Entered cache semaphore");

                    TelemetryData telemetryData = new TelemetryData();
                    try
                    {
                        if (!_cacheRefreshedForRead) // double check locking
                        {
                            string key = CacheKeyFactory.GetKeyFromRequest(_requestParams);

                            try
                            {
                                var args = new TokenCacheNotificationArgs(
                                  TokenCacheInternal,
                                  _requestParams.AppConfig.ClientId,
                                  _requestParams.Account,                                  
                                  hasStateChanged: false,
                                  isApplicationCache: TokenCacheInternal.IsApplicationCache,
                                  suggestedCacheKey: key,
                                  hasTokens: TokenCacheInternal.HasTokensNoLocks(),
                                  cancellationToken: _requestParams.RequestContext.UserCancellationToken,
                                  suggestedCacheExpiry: null,
                                  correlationId: _requestParams.RequestContext.CorrelationId, 
                                  requestScopes: _requestParams.Scope, 
                                  requestTenantId: _requestParams.AuthorityManager.OriginalAuthority.TenantId,
                                  identityLogger: _requestParams.RequestContext.Logger.IdentityLogger,
                                  piiLoggingEnabled: _requestParams.RequestContext.Logger.PiiLoggingEnabled,
                                  telemetryData: telemetryData);

                                var measureDurationResult = await TokenCacheInternal.OnBeforeAccessAsync(args).MeasureAsync().ConfigureAwait(false);
                                RequestContext.ApiEvent.DurationInCacheInMs += measureDurationResult.Milliseconds;
                            }
                            finally
                            {
                                var measureDurationResult = await StopwatchService.MeasureCodeBlockAsync(async () =>
                                {
                                    var args = new TokenCacheNotificationArgs(
                                      TokenCacheInternal,
                                      _requestParams.AppConfig.ClientId,
                                      _requestParams.Account,
                                      hasStateChanged: false,
                                      isApplicationCache: TokenCacheInternal.IsApplicationCache,
                                      suggestedCacheKey: key,
                                      hasTokens: TokenCacheInternal.HasTokensNoLocks(),
                                      cancellationToken: _requestParams.RequestContext.UserCancellationToken,
                                      suggestedCacheExpiry: null,
                                      correlationId: _requestParams.RequestContext.CorrelationId,
                                      requestScopes: _requestParams.Scope,
                                      requestTenantId: _requestParams.AuthorityManager.OriginalAuthority.TenantId,
                                      identityLogger: _requestParams.RequestContext.Logger.IdentityLogger,
                                      piiLoggingEnabled: _requestParams.RequestContext.Logger.PiiLoggingEnabled,
                                      telemetryData: telemetryData);

                                    await TokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                                }).ConfigureAwait(false);
                                RequestContext.ApiEvent.DurationInCacheInMs += measureDurationResult.Milliseconds;

                            }

                            _cacheRefreshedForRead = true;

                        }
                    }
                    finally
                    {
                        TokenCacheInternal.Semaphore.Release();
                        _requestParams.RequestContext.Logger.Verbose(()=>"[Cache Session Manager] Released cache semaphore");
                        RequestContext.ApiEvent.CacheLevel = telemetryData.CacheLevel;
                    }
                }
            } else
            {
                RequestContext.ApiEvent.CacheLevel = CacheLevel.L1Cache;
            }

            RequestContext.ApiEvent.CachedAccessTokenCount = TokenCacheInternal.Accessor.EntryCount;
        }
    }
}
