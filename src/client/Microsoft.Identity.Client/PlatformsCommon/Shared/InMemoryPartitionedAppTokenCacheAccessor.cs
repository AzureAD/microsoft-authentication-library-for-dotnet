// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Stores tokens for an application.
    /// Partitions the access token collection by a key of client ID with tenant ID.
    /// App metadata collection is not partitioned.
    /// Refresh token, ID token, and account related methods are no-op.
    /// </summary>
    internal class InMemoryPartitionedAppTokenCacheAccessor : ITokenCacheAccessor
    {
        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        // internal for test only
        internal readonly CacheWrapper AccessTokenCacheWrapper;
        internal readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary;

        // static versions to support the "shared cache" mode
        private static readonly CacheWrapper s_accessTokenCacheWrapper = new CacheWrapper(null);
        private static readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> s_appMetadataDictionary =
           new ConcurrentDictionary<string, MsalAppMetadataCacheItem>(1, 1);

        protected readonly ICoreLogger _logger;
        private readonly CacheOptions _tokenCacheAccessorOptions;

        public InMemoryPartitionedAppTokenCacheAccessor(
            ICoreLogger logger,
            CacheOptions tokenCacheAccessorOptions)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _tokenCacheAccessorOptions = tokenCacheAccessorOptions ?? new CacheOptions();

            if (_tokenCacheAccessorOptions.UseSharedCache)
            {
                AccessTokenCacheWrapper = s_accessTokenCacheWrapper;
                AppMetadataDictionary = s_appMetadataDictionary;
                if (_tokenCacheAccessorOptions.AccessTokenExpirationScanFrequency.HasValue)
                {
                    AccessTokenCacheWrapper.UpdateFrequencyIfLower(_tokenCacheAccessorOptions.AccessTokenExpirationScanFrequency.Value);
                }
            }
            else
            {
                AccessTokenCacheWrapper = new CacheWrapper(_tokenCacheAccessorOptions.AccessTokenExpirationScanFrequency);
                AppMetadataDictionary = new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();
            }
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetClientCredentialKey(item.ClientId, item.TenantId);

            // if a conflict occurs, pick the latest value
            AccessTokenCacheWrapper.AccessTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[itemKey] = item;

            AccessTokenCacheWrapper.StartScanForExpiredItemsIfNeeded();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void SaveAccount(MsalAccountCacheItem item)
        {
            throw new NotSupportedException();
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.GetKey().ToString();
            AppMetadataDictionary[key] = item;
        }
        #endregion

        #region Get
        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            throw new NotSupportedException();
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey)
        {
            AppMetadataDictionary.TryGetValue(appMetadataKey.ToString(), out MsalAppMetadataCacheItem cacheItem);
            return cacheItem;
        }
        #endregion

        #region Delete
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            var partitionKey = CacheKeyFactory.GetClientCredentialKey(item.ClientId, item.TenantId);

            AccessTokenCacheWrapper.AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete access token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete access token because it was not found in the cache.");
            }
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void DeleteAccount(MsalAccountCacheItem item)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Get All

        /// <summary>
        /// WARNING: if partitonKey = null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// </summary>
        public virtual IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null)
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache partitions found while getting access tokens: {AccessTokenCacheWrapper.AccessTokenCacheDictionary.Count}");
            AccessTokenCacheWrapper.StartScanForExpiredItemsIfNeeded();
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccessTokenCacheWrapper.AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccessTokenCacheWrapper.AccessTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyReadOnlyList<MsalAccessTokenCacheItem>();
            }
        }

        public virtual IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyReadOnlyList<MsalRefreshTokenCacheItem>();
        }

        public virtual IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyReadOnlyList<MsalIdTokenCacheItem>();
        }

        public virtual IReadOnlyList<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyReadOnlyList<MsalAccountCacheItem>();
        }

        public IReadOnlyList<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            return AppMetadataDictionary.Select(kv => kv.Value).ToList();
        }
        #endregion

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            throw new NotImplementedException();
        }

        public virtual void Clear()
        {
            AccessTokenCacheWrapper.AccessTokenCacheDictionary.Clear();
            _logger.Always("[Clear] Clearing access token cache data.");
            // app metadata isn't removable
        }

        public virtual bool HasAccessOrRefreshTokens()
        {
            return AccessTokenCacheWrapper.AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }

        internal class CacheWrapper
        {
            // perf: do not use ConcurrentDictionary.Values as it takes a lock
            // internal for test only
            internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> AccessTokenCacheDictionary;

            private DateTimeOffset _lastExpirationScan;

            private TimeSpan? _expirationScanFrequency;

            internal CacheWrapper(TimeSpan? expirationScanFrequency)
            {
                _expirationScanFrequency = expirationScanFrequency;
                _lastExpirationScan = DateTimeOffset.UtcNow;
                AccessTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
            }

            internal void UpdateFrequencyIfLower(TimeSpan expirationScanFrequency)
            {
                if (!_expirationScanFrequency.HasValue || _expirationScanFrequency.Value > expirationScanFrequency)
                {
                    _expirationScanFrequency = expirationScanFrequency;
                }
            }

            // Called by multiple actions to see how long it's been since we last checked for expired items.
            // If sufficient time has elapsed then a scan is initiated on a background task.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void StartScanForExpiredItemsIfNeeded()
            {

                if (_expirationScanFrequency.HasValue)
                {
                    var utcNow = DateTimeOffset.UtcNow;
                    if (_expirationScanFrequency.Value < utcNow - _lastExpirationScan)
                    {
                        ScheduleTask(utcNow);
                    }
                }

                void ScheduleTask(DateTimeOffset utcNow)
                {
                    _lastExpirationScan = utcNow;
                    Task.Factory.StartNew(state => ScanForExpiredItems((CacheWrapper)state), this,
                        CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
            }

            private static void ScanForExpiredItems(CacheWrapper cacheWrapper)
            {
                DateTimeOffset now = cacheWrapper._lastExpirationScan = DateTimeOffset.UtcNow;

                foreach (KeyValuePair<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> partitionItem in cacheWrapper.AccessTokenCacheDictionary)
                {
                    var partitionItemValue = partitionItem.Value;
                    foreach (KeyValuePair<string, MsalAccessTokenCacheItem> item in partitionItemValue)
                    {
                        if (item.Value.ExpiresOn < now)
                        {
                            partitionItemValue.TryRemove(item.Key, out _);
                        }
                    }
                    if (partitionItemValue.IsEmpty)
                    {
                        cacheWrapper.AccessTokenCacheDictionary.TryRemove(partitionItem.Key, out _);
                    }
                }
            }
        }
    }
}
