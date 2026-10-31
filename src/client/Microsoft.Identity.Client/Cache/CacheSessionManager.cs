// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
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
        private TokenCache _readView;
        private ITokenCacheInternal ReadCache => _readView ?? TokenCacheInternal;

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

        private bool IsInternalCacheDisabled =>
            CacheOptions.IsDisabledFor(_requestParams.RequestContext.ServiceBundle.Config.AccessorOptions);

        private bool ShouldSkipInternalCacheRead(string operationName)
        {
            if (!IsInternalCacheDisabled)
            {
                return false;
            }
            _requestParams.RequestContext.Logger.Verbose(
                () => $"[Cache Session Manager] Internal cache disabled. Skipping {operationName}.");
            return true;
        }

        private async Task RefreshCacheForReadOperationsIfEnabledAsync(string operationName)
        {
            if (ShouldSkipInternalCacheRead(operationName))
            {
                return;
            }
            await RefreshCacheForReadOperationsAsync().ConfigureAwait(false);
        }

        public async Task<MsalAccessTokenCacheItem> FindAccessTokenAsync()
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("access token lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }
            return await ReadCache.FindAccessTokenAsync(_requestParams).ConfigureAwait(false);
        }

        public async Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>> SaveTokenResponseAsync(MsalTokenResponse tokenResponse)
        {
            Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account> result;
            try
            {
                result = await TokenCacheInternal.SaveTokenResponseAsync(_requestParams, tokenResponse).ConfigureAwait(false);
            }
            finally
            {
                if (_readView != null)
                {
                    // Persistence callbacks can fail after the root has already changed.
                    _readView = null;
                    _cacheRefreshedForRead = false;
                }
            }
            RequestContext.ApiEvent.CachedAccessTokenCount = GetInternalCacheEntryCountForTelemetry();
            return result;
        }

        public async Task<Account> GetAccountAssociatedWithAccessTokenAsync(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("account lookup for access token").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }
            return await ReadCache.GetAccountAssociatedWithAccessTokenAsync(_requestParams, msalAccessTokenCacheItem).ConfigureAwait(false);
        }

        public async Task<MsalIdTokenCacheItem> GetIdTokenCacheItemAsync(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("ID token lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }
            return ReadCache.GetIdTokenCacheItem(accessTokenCacheItem);
        }

        public async Task<MsalRefreshTokenCacheItem> FindFamilyRefreshTokenAsync(string familyId)
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("family refresh token lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }

            if (string.IsNullOrEmpty(familyId))
            {
                throw new ArgumentNullException(nameof(familyId));
            }

            return await ReadCache.FindRefreshTokenAsync(_requestParams, familyId).ConfigureAwait(false);
        }

        public async Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("refresh token lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }
            return await ReadCache.FindRefreshTokenAsync(_requestParams).ConfigureAwait(false);
        }

        public async Task<bool?> IsAppFociMemberAsync(string familyId)
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("FOCI membership lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return null;
            }
            return await ReadCache.IsFociMemberAsync(_requestParams, familyId).ConfigureAwait(false);
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            await RefreshCacheForReadOperationsIfEnabledAsync("accounts lookup").ConfigureAwait(false);
            if (IsInternalCacheDisabled)
            {
                return System.Array.Empty<IAccount>();
            }
            return await ReadCache.GetAccountsAsync(_requestParams).ConfigureAwait(false);
        }

        #endregion

        /// <remarks>
        /// Possibly refreshes the internal cache by calling OnBeforeAccessAsync and OnAfterAccessAsync delegates.
        /// </remarks>
        private async Task RefreshCacheForReadOperationsAsync()
        {
            PrepareReadView();
            var refreshCache = ReadCache;
            if (TokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                if (!_cacheRefreshedForRead)
                {
                    _requestParams.RequestContext.Logger.Verbose(()=>$"[Cache Session Manager] Entering the cache semaphore. { refreshCache.Semaphore.GetCurrentCountLogMessage()}");
                    await refreshCache.Semaphore.WaitAsync(_requestParams.RequestContext.UserCancellationToken).ConfigureAwait(false);
                    _requestParams.RequestContext.Logger.Verbose(()=>"[Cache Session Manager] Entered cache semaphore");

                    TelemetryData telemetryData = new TelemetryData();
                    IDisposable readLease = null;
                    try
                    {
                        if (!_cacheRefreshedForRead) // double check locking
                        {
                            if (_readView != null)
                            {
                                // Cover L2-to-L1 publication as well as private deserialization.
                                readLease = await TokenCacheInternal.Semaphore
                                    .AcquireReadAsync(_requestParams.RequestContext.UserCancellationToken).ConfigureAwait(false);
                            }
                            using var hydrationScope = _readView?.BeginReadHydration(TokenCacheInternal);
                            string key = CacheKeyFactory.GetKeyFromRequest(_requestParams);

                            try
                            {
                                var args = new TokenCacheNotificationArgs(
                                  refreshCache,
                                  _requestParams.AppConfig.ClientId,
                                  _requestParams.Account,                                  
                                  hasStateChanged: false,
                                  isApplicationCache: refreshCache.IsApplicationCache,
                                  suggestedCacheKey: key,
                                  hasTokens: refreshCache.HasTokensNoLocks(),
                                  cancellationToken: _requestParams.RequestContext.UserCancellationToken,
                                  suggestedCacheExpiry: null,
                                  correlationId: _requestParams.RequestContext.CorrelationId, 
                                  requestScopes: _requestParams.Scope, 
                                  requestTenantId: _requestParams.AuthorityManager.OriginalAuthority.TenantId,
                                  identityLogger: _requestParams.RequestContext.Logger.IdentityLogger,
                                  piiLoggingEnabled: _requestParams.RequestContext.Logger.PiiLoggingEnabled,
                                  telemetryData: telemetryData);

                                var measureDurationResult = await refreshCache.OnBeforeAccessAsync(args).MeasureAsync().ConfigureAwait(false);
                                RequestContext.ApiEvent.DurationInCacheInMs += measureDurationResult.Milliseconds;
                                _readView?.ValidateReadHydration();
                            }
                            finally
                            {
                                var measureDurationResult = await StopwatchService.MeasureCodeBlockAsync(async () =>
                                {
                                    var args = new TokenCacheNotificationArgs(
                                      refreshCache,
                                      _requestParams.AppConfig.ClientId,
                                      _requestParams.Account,
                                      hasStateChanged: false,
                                      isApplicationCache: refreshCache.IsApplicationCache,
                                      suggestedCacheKey: key,
                                      hasTokens: refreshCache.HasTokensNoLocks(),
                                      cancellationToken: _requestParams.RequestContext.UserCancellationToken,
                                      suggestedCacheExpiry: null,
                                      correlationId: _requestParams.RequestContext.CorrelationId,
                                      requestScopes: _requestParams.Scope,
                                      requestTenantId: _requestParams.AuthorityManager.OriginalAuthority.TenantId,
                                      identityLogger: _requestParams.RequestContext.Logger.IdentityLogger,
                                      piiLoggingEnabled: _requestParams.RequestContext.Logger.PiiLoggingEnabled,
                                      telemetryData: telemetryData);

                                    await refreshCache.OnAfterAccessAsync(args).ConfigureAwait(false);
                                }).ConfigureAwait(false);
                                RequestContext.ApiEvent.DurationInCacheInMs += measureDurationResult.Milliseconds;

                            }

                            _cacheRefreshedForRead = true;

                        }
                    }
                    finally
                    {
                        refreshCache.Semaphore.Release();
                        readLease?.Dispose();
                        _requestParams.RequestContext.Logger.Verbose(()=>"[Cache Session Manager] Released cache semaphore");
                        RequestContext.ApiEvent.CacheLevel = telemetryData.CacheLevel;
                    }
                }
            } else
            {
                RequestContext.ApiEvent.CacheLevel = CacheLevel.L1Cache;
            }

            RequestContext.ApiEvent.CachedAccessTokenCount = GetInternalCacheEntryCountForTelemetry();
        }

        private void PrepareReadView()
        {
            if (_cacheRefreshedForRead ||
                _readView != null ||
                !(TokenCacheInternal is TokenCache root) ||
                !root.UseIsolatedCacheReads ||
                !(root.Accessor is InMemoryPartitionedUserTokenCacheAccessor) ||
                string.IsNullOrEmpty(CacheKeyFactory.GetKeyFromRequest(_requestParams)) ||
                !TokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                return;
            }

            Interlocked.CompareExchange(ref _readView, root.CreateReadView(), null);
        }

        private int GetInternalCacheEntryCountForTelemetry()
        {
            if (IsInternalCacheDisabled)
            {
                return 0;
            }

            return ReadCache.Accessor.EntryCount;
        }
    }
}
