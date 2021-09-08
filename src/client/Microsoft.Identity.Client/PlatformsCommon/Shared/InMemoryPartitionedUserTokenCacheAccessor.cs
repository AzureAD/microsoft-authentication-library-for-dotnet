// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;

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
        internal /* internal for test only */ readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> AccessTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>(1, 1);

        internal /* internal for test only */ readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>> RefreshTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalRefreshTokenCacheItem>>(1, 1);

        internal /* internal for test only */ readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>> IdTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalIdTokenCacheItem>>(1, 1);

        internal /* internal for test only */ readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>> AccountCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccountCacheItem>>(1, 1);

        internal /* internal for test only */  readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary =
           new ConcurrentDictionary<string, MsalAppMetadataCacheItem>(1, 1);

        protected readonly ICoreLogger _logger;

        public InMemoryPartitionedUserTokenCacheAccessor(ICoreLogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            
            string partitionKey = !string.IsNullOrEmpty(item.UserAssertionHash) ?
                item.UserAssertionHash : item.HomeAccountId;

            AccessTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[itemKey] = item; // if a conflict occurs, pick the latest value
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = !string.IsNullOrEmpty(item.UserAssertionHash) ?
              item.UserAssertionHash : item.HomeAccountId;
            RefreshTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>())[itemKey] = item;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            IdTokenCacheDictionary
                .GetOrAdd(item.HomeAccountId, new ConcurrentDictionary<string, MsalIdTokenCacheItem>())[itemKey] = item;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            AccountCacheDictionary
                .GetOrAdd(item.HomeAccountId, new ConcurrentDictionary<string, MsalAccountCacheItem>())[itemKey] = item;
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.GetKey().ToString();
            AppMetadataDictionary[key] = item;
        }
        #endregion

        #region Get
        public MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            AccessTokenCacheDictionary.TryGetValue(accessTokenKey.HomeAccountId, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
            MsalAccessTokenCacheItem cacheItem = null;
            partition?.TryGetValue(accessTokenKey.ToString(), out cacheItem);
            return cacheItem;
        }

        public MsalRefreshTokenCacheItem GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            RefreshTokenCacheDictionary.TryGetValue(refreshTokenKey.HomeAccountId, out ConcurrentDictionary<string, MsalRefreshTokenCacheItem> partition);
            MsalRefreshTokenCacheItem cacheItem = null;
            partition?.TryGetValue(refreshTokenKey.ToString(), out cacheItem);
            return cacheItem;
        }

        public MsalIdTokenCacheItem GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            IdTokenCacheDictionary.TryGetValue(idTokenKey.HomeAccountId, out ConcurrentDictionary<string, MsalIdTokenCacheItem> partition);
            MsalIdTokenCacheItem cacheItem = null;
            partition?.TryGetValue(idTokenKey.ToString(), out cacheItem);
            return cacheItem;
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            AccountCacheDictionary.TryGetValue(accountKey.HomeAccountId, out ConcurrentDictionary<string, MsalAccountCacheItem> partition);
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
        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            AccessTokenCacheDictionary.TryGetValue(cacheKey.HomeAccountId, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
            if (partition == null || !partition.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete access token because it was not found in the cache. Key {cacheKey}.",
                    "Cannot delete access token because it was not found in the cache.");
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            RefreshTokenCacheDictionary.TryGetValue(cacheKey.HomeAccountId, out ConcurrentDictionary<string, MsalRefreshTokenCacheItem> partition);
            if (partition == null || !partition.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete refresh token because it was not found in the cache. Key {cacheKey}.",
                    "Cannot delete refresh token because it was not found in the cache.");
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            IdTokenCacheDictionary.TryGetValue(cacheKey.HomeAccountId, out ConcurrentDictionary<string, MsalIdTokenCacheItem> partition);
            if (partition == null || !partition.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete ID token because it was not found in the cache. Key {cacheKey}.",
                    "Cannot delete ID token because it was not found in the cache.");
            }
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            AccountCacheDictionary.TryGetValue(cacheKey.HomeAccountId, out ConcurrentDictionary<string, MsalAccountCacheItem> partition);
            if (partition == null || !partition.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete account because it was not found in the cache. Key {cacheKey}.",
                    "Cannot delete account because it was not found in the cache");
            }
        }

        #endregion

        #region Get All
        public IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccessTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? new List<MsalAccessTokenCacheItem>();
            }
        }

        public IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return RefreshTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                RefreshTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalRefreshTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? new List<MsalRefreshTokenCacheItem>();
            }
        }

        public IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return IdTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                IdTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalIdTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? new List<MsalIdTokenCacheItem>();
            }
        }

        public IReadOnlyList<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccountCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccountCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccountCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? new List<MsalAccountCacheItem>();
            }
        }

        public IReadOnlyList<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            return AppMetadataDictionary.Select(kv => kv.Value).ToList();
        }
        #endregion

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Clear()
        {
            AccessTokenCacheDictionary.Clear();
            RefreshTokenCacheDictionary.Clear();
            IdTokenCacheDictionary.Clear();
            AccountCacheDictionary.Clear();
            // app metadata isn't removable
        }
    }
}
