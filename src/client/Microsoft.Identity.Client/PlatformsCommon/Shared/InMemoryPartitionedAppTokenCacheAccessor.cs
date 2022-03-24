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
    /// Stores tokens for an application.
    /// Partitions the access token collection by a key of client ID with tenant ID.
    /// App metadata collection is not partitioned.
    /// Refresh token, ID token, and account related methods are no-op.
    /// </summary>
    internal class InMemoryPartitionedAppTokenCacheAccessor : ITokenCacheAccessor
    {
        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        // internal for test only
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> AccessTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary;

        // static versions to support the "shared cache" mode
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> s_accessTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
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
                AccessTokenCacheDictionary = s_accessTokenCacheDictionary;
                AppMetadataDictionary = s_appMetadataDictionary;
            }
            else
            {
                AccessTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
                AppMetadataDictionary = new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();
            }
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string itemKey = item.GetKey().ToString();
            string partitionKey = CacheKeyFactory.GetClientCredentialKey(item.ClientId, item.TenantId, item.KeyId);

            // if a conflict occurs, pick the latest value
            AccessTokenCacheDictionary
                .GetOrAdd(partitionKey, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[itemKey] = item;
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
            var partitionKey = CacheKeyFactory.GetClientCredentialKey(item.ClientId, item.TenantId, item.KeyId);

            AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition);
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

        public virtual List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyList<MsalRefreshTokenCacheItem>();
        }

        public virtual List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyList<MsalIdTokenCacheItem>();
        }

        public virtual List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null)
        {
            return CollectionHelpers.GetEmptyList<MsalAccountCacheItem>();
        }

        public List<MsalAppMetadataCacheItem> GetAllAppMetadata()
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
            _logger.Always("[Clear] Clearing access token cache data.");
            // app metadata isn't removable
        }

        public virtual bool HasAccessOrRefreshTokens()
        {
            return AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }
    }
}
