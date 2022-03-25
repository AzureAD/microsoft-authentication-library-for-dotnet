// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            AccessTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[itemKey] = item; // if a conflict occurs, pick the latest value
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            RefreshTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>())[itemKey] = item;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            IdTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalIdTokenCacheItem>())[itemKey] = item;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            AccountCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccountCacheItem>())[itemKey] = item;
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
                    $"Cannot delete access token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete access token because it was not found in the cache.");
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            RefreshTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete refresh token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete refresh token because it was not found in the cache.");
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            IdTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete ID token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete ID token because it was not found in the cache.");
            }
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            string partitionKey = CacheKeyFactory.GetKeyFromCachedItem(item);

            AccountCacheDictionary.TryGetValue(partitionKey, out var partition);
            if (partition == null || !partition.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete account because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete account because it was not found in the cache");
            }
        }
        #endregion

        #region Get All

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null)
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache partitions found while getting access tokens: {AccessTokenCacheDictionary.Count}");
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccessTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyList<MsalAccessTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache partitions found while getting refresh tokens: {RefreshTokenCacheDictionary.Count}");
            if (string.IsNullOrEmpty(partitionKey))
            {
                return RefreshTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                RefreshTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalRefreshTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyList<MsalRefreshTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return IdTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                IdTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalIdTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyList<MsalIdTokenCacheItem>();
            }
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccountCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccountCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccountCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyList<MsalAccountCacheItem>();
            }
        }

        public virtual List<MsalAppMetadataCacheItem> GetAllAppMetadata()
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
            _logger.Always("[Clear] Clearing access token cache data.");
            AccessTokenCacheDictionary.Clear();
            RefreshTokenCacheDictionary.Clear();
            IdTokenCacheDictionary.Clear();
            AccountCacheDictionary.Clear();
            // app metadata isn't removable
        }

        /// WARNING: this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual bool HasAccessOrRefreshTokens()
        {
            return RefreshTokenCacheDictionary.Any(partition => partition.Value.Count > 0) ||
                    AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }
    }
}
