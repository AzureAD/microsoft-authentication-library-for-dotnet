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
        internal readonly ConcurrentDictionary<string, MsalAccessTokenCacheItem> AccessTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalRefreshTokenCacheItem> RefreshTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalIdTokenCacheItem> IdTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalAccountCacheItem> AccountCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary;

        // static versions to support the "shared cache" mode
        private static readonly ConcurrentDictionary<string, MsalAccessTokenCacheItem> s_accessTokenCacheDictionary =
          new ConcurrentDictionary<string, MsalAccessTokenCacheItem>();
        private static readonly ConcurrentDictionary<string, MsalRefreshTokenCacheItem> s_refreshTokenCacheDictionary =
             new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>();
        private static readonly ConcurrentDictionary<string, MsalIdTokenCacheItem> s_idTokenCacheDictionary =
             new ConcurrentDictionary<string, MsalIdTokenCacheItem>();
        private static readonly ConcurrentDictionary<string, MsalAccountCacheItem> s_accountCacheDictionary =
             new ConcurrentDictionary<string, MsalAccountCacheItem>();
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
                AccessTokenCacheDictionary = new ConcurrentDictionary<string, MsalAccessTokenCacheItem>();
                RefreshTokenCacheDictionary = new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>();
                IdTokenCacheDictionary = new ConcurrentDictionary<string, MsalIdTokenCacheItem>();
                AccountCacheDictionary = new ConcurrentDictionary<string, MsalAccountCacheItem>();
                AppMetadataDictionary = new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();
            }
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();

            AccessTokenCacheDictionary[itemKey] = item; // if a conflict occurs, pick the latest value
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();

            RefreshTokenCacheDictionary[itemKey] = item;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();

            IdTokenCacheDictionary[itemKey] = item;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            string itemKey = item.GetKey().ToString();

            AccountCacheDictionary[itemKey] = item;
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
            if (IdTokenCacheDictionary.TryGetValue(accessTokenCacheItem.GetIdTokenItemKey().ToString(), out var idToken))
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
            AccountCacheDictionary.TryGetValue(accountKey.ToString(), out var cacheItem);
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
            if (!AccessTokenCacheDictionary.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete access token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete access token because it was not found in the cache.");
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            if (!RefreshTokenCacheDictionary.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete refresh token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete refresh token because it was not found in the cache.");
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            if (!IdTokenCacheDictionary.TryRemove(item.GetKey().ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete ID token because it was not found in the cache. Key {item.GetKey()}.",
                    "Cannot delete ID token because it was not found in the cache.");
            }
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            if (!AccountCacheDictionary.TryRemove(item.GetKey().ToString(), out _))
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
        public virtual IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens()
        {
            _logger.Always($"[GetAllAccessTokens] Total number of cache items found while getting access tokens: {AccessTokenCacheDictionary.Count}");
            return AccessTokenCacheDictionary.Select(kv => kv.Value).ToList();

        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens()
        {
            _logger.Always($"[GetAllRefreshTokens] Total number of cache items found while getting refresh tokens: {RefreshTokenCacheDictionary.Count}");
            return RefreshTokenCacheDictionary.Select(kv => kv.Value).ToList();
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens()
        {
            return IdTokenCacheDictionary.Select(kv => kv.Value).ToList();
        }

        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        public virtual IReadOnlyList<MsalAccountCacheItem> GetAllAccounts()
        {
            return AccountCacheDictionary.Select(kv => kv.Value).ToList();
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
            return RefreshTokenCacheDictionary.Count > 0 ||
                    AccessTokenCacheDictionary.Any(token => !token.Value.IsExpiredWithBuffer());
        }
    }
}
