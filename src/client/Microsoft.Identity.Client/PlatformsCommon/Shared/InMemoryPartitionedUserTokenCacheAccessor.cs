// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Stores tokens for users.
    /// Partitions the access and refresh token collections by a user assertion hash in case of OBO and by home account ID otherwise.
    /// Partitions the ID token and account collections by home account ID.
    /// App metadata collection is not partitioned.
    /// </summary>
    internal class InMemoryPartitionedUserTokenCacheAccessor : ITokenCacheAccessor
    {
        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        // internal for test only
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> AccessTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>> RefreshTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>> IdTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>> AccountCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary;

        // static versions to support the "shared cache" mode
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> s_accessTokenCacheDictionary =
          new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>> s_refreshTokenCacheDictionary =
             new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>> s_idTokenCacheDictionary =
             new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>> s_accountCacheDictionary =
             new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>>();
        private static readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> s_appMetadataDictionary =
            new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();

        protected readonly ICoreLogger _logger;
        private readonly CacheOptions _tokenCacheAccessorOptions;

        // Approximate size of cache item objects
        private const long AccessTokenSizeInBytes = 6500;
        private const long RefreshTokenSizeInBytes = 3700;
        private const long IDTokenSizeInBytes = 3300;
        private const long AccountSizeInBytes = 1300;

        private long _userCacheSize;

        public InMemoryPartitionedUserTokenCacheAccessor(ICoreLogger logger, CacheOptions tokenCacheAccessorOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenCacheAccessorOptions = tokenCacheAccessorOptions ?? new CacheOptions();

            if (_tokenCacheAccessorOptions.UseSharedCache)
            {
                AccessTokenCacheDictionary = s_accessTokenCacheDictionary;
                RefreshTokenCacheDictionary = s_refreshTokenCacheDictionary;
                IdTokenCacheDictionary = s_idTokenCacheDictionary;
                AccountCacheDictionary = s_accountCacheDictionary;
                AppMetadataDictionary = s_appMetadataDictionary;
            }
            else
            {
                AccessTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
                RefreshTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>>();
                IdTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>>();
                AccountCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>>();
                AppMetadataDictionary = new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();
            }
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            // When saving tokens in SaveTokenResponseAsync, AT is saved first, then other tokens.
            // For the user cache, checking size limit and compacting only when saving AT,
            // because otherwise, if compact is run in other save methods,
            // it could leave unassociated tokens in the cache.
            // (For ex, compact in SaveAccount, would leave only account in the cache, without other tokens.)
            if (IsCacheOverCapacity(AccessTokenSizeInBytes))
            {
                _logger.Always("[UserCache] Cache is over capacity.");
                Compact();
            }

            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            // Update cache size only if cache item is added, not updated
            if (!AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition) || !partition.TryGetValue(itemKey, out _))
            {
                Interlocked.Add(ref _userCacheSize, AccessTokenSizeInBytes);
                Interlocked.Add(ref TokenCache.CacheSize, AccessTokenSizeInBytes);
            }

            // if a conflict occurs, pick the latest value
            AccessTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[itemKey] = item;
            _logger.Verbose($"[UserCache] Saved access token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            // Update cache size only if cache item is added, not updated
            if (!RefreshTokenCacheDictionary.TryGetValue(partitionKey, out var partition) || !partition.TryGetValue(itemKey, out _))
            {
                Interlocked.Add(ref _userCacheSize, RefreshTokenSizeInBytes);
                Interlocked.Add(ref TokenCache.CacheSize, RefreshTokenSizeInBytes);
            }

            RefreshTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>())[itemKey] = item;
            _logger.Verbose($"[UserCache] Saved refresh token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            // Update cache size only if cache item is added, not updated
            if (!IdTokenCacheDictionary.TryGetValue(partitionKey, out var partition) || !partition.TryGetValue(itemKey, out _))
            {
                Interlocked.Add(ref _userCacheSize, IDTokenSizeInBytes);
                Interlocked.Add(ref TokenCache.CacheSize, IDTokenSizeInBytes);
            }

            IdTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalIdTokenCacheItem>())[itemKey] = item;
            _logger.Verbose($"[UserCache] Saved ID token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            // Update cache size only if cache item is added, not updated
            if (!AccountCacheDictionary.TryGetValue(partitionKey, out var partition) || !partition.TryGetValue(itemKey, out _))
            {
                Interlocked.Add(ref _userCacheSize, AccountSizeInBytes);
                Interlocked.Add(ref TokenCache.CacheSize, AccountSizeInBytes);
            }

            AccountCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccountCacheItem>())[itemKey] = item;
            _logger.Verbose($"[UserCache] Saved account. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.GetKey().ToString();
            AppMetadataDictionary[key] = item;
        }
        #endregion

        #region Get

        public MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            string partitionKey = CacheKeyFactory.GetIdTokenKeyFromCachedItem(accessTokenCacheItem);

            IdTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition != null && partition.TryGetValue(accessTokenCacheItem.GetIdTokenItemKey().ToString(), out var idToken))
            {
                return idToken;
            }

            _logger.WarningPii(
                $"Could not find an id token for the access token with key {accessTokenCacheItem.GetKey()}",
                $"Could not find an id token for the access token for realm {accessTokenCacheItem.TenantId} ");
            return null;
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromAccount(accountKey);

            AccountCacheDictionary.TryGetValue(partitionKey, out var partition);
            MsalAccountCacheItem cacheItem = null;
            partition?.TryGetValue(accountKey.ToString(), out cacheItem);
            return cacheItem;
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
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"[UserCache] Cannot delete access token because it was not found in the cache. Key {item.GetKey()}.",
                    "[UserCache] Cannot delete access token because it was not found in the cache.");
                return;
            }

            Interlocked.Add(ref _userCacheSize, -AccessTokenSizeInBytes);
            Interlocked.Add(ref TokenCache.CacheSize, -AccessTokenSizeInBytes);
            _logger.Verbose($"[UserCache] Removed access token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            RefreshTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"[UserCache] Cannot delete refresh token because it was not found in the cache. Key {item.GetKey()}.",
                    "[UserCache] Cannot delete refresh token because it was not found in the cache.");
                return;
            }

            Interlocked.Add(ref _userCacheSize, -RefreshTokenSizeInBytes);
            Interlocked.Add(ref TokenCache.CacheSize, -RefreshTokenSizeInBytes);
            _logger.Verbose($"[UserCache] Removed refresh token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            IdTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"[UserCache] Cannot delete ID token because it was not found in the cache. Key {item.GetKey()}.",
                    "[UserCache] Cannot delete ID token because it was not found in the cache.");
                return;
            }

            Interlocked.Add(ref _userCacheSize, -IDTokenSizeInBytes);
            Interlocked.Add(ref TokenCache.CacheSize, -IDTokenSizeInBytes);
            _logger.Verbose($"[UserCache] Removed ID token. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            AccountCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"[UserCache] Cannot delete account because it was not found in the cache. Key {item.GetKey()}.",
                    "[UserCache] Cannot delete account because it was not found in the cache");
                return;
            }

            Interlocked.Add(ref _userCacheSize, -AccountSizeInBytes);
            Interlocked.Add(ref TokenCache.CacheSize, -AccountSizeInBytes);
            _logger.Verbose($"[UserCache] Removed account. User cache size: {Interlocked.Read(ref _userCacheSize)}.");
        }
        #endregion

        #region Get All

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null)
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache partitions found while getting access tokens: {AccessTokenCacheDictionary.Count}");
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccessTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyReadOnlyList<MsalAccessTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache partitions found while getting refresh tokens: {RefreshTokenCacheDictionary.Count}");
            if (string.IsNullOrEmpty(partitionKey))
            {
                return RefreshTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                RefreshTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalRefreshTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyReadOnlyList<MsalRefreshTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return IdTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                IdTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalIdTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyReadOnlyList<MsalIdTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccountCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccountCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccountCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyReadOnlyList<MsalAccountCacheItem>();
            }
        }

        public virtual IReadOnlyList<MsalAppMetadataCacheItem> GetAllAppMetadata()
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
            AccessTokenCacheDictionary.Clear();
            RefreshTokenCacheDictionary.Clear();
            IdTokenCacheDictionary.Clear();
            AccountCacheDictionary.Clear();
            Interlocked.Add(ref TokenCache.CacheSize, -_userCacheSize);
            Interlocked.Exchange(ref _userCacheSize, 0);
            _logger.Always("[UserCache] Cleared access token cache data.");
            // app metadata isn't removable
        }

        /// WARNING: this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual bool HasAccessOrRefreshTokens()
        {
            return RefreshTokenCacheDictionary.Any(partition => partition.Value.Count > 0) ||
                    AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }

        private bool IsCacheOverCapacity(long sizeToAdd)
        {
            return _tokenCacheAccessorOptions.SizeLimit.HasValue && (Interlocked.Read(ref TokenCache.CacheSize) + sizeToAdd) > _tokenCacheAccessorOptions.SizeLimit;
        }

        private void Compact()
        {
            _logger.Always("[UserCache] Compacting cache.");
            Clear();
        }
    }
}
