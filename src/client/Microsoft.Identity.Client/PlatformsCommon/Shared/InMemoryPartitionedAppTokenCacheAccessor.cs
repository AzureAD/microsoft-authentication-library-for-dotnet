﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
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

        protected readonly ILoggerAdapter _logger;
        private readonly CacheOptions _tokenCacheAccessorOptions;

        private int _entryCount = 0;
        private static int s_entryCount = 0;

        public int EntryCount => GetEntryCountRef();
       
        public InMemoryPartitionedAppTokenCacheAccessor(
            ILoggerAdapter logger,
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
            string itemKey = item.CacheKey;
            string partitionKey = CacheKeyFactory.GetAppTokenCacheItemKey(item.ClientId, item.TenantId, item.KeyId, item.AdditionalCacheKeyComponents);

            var partition = AccessTokenCacheDictionary.GetOrAdd(partitionKey, _ => new ConcurrentDictionary<string, MsalAccessTokenCacheItem>());
            bool added = partition.TryAdd(itemKey, item);

            // only increment the entry count if the item was added, not updated
            if (added)
            {
                Interlocked.Increment(ref GetEntryCountRef());
            }
            else
            {
                partition[itemKey] = item;
            }
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void SaveAccount(MsalAccountCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.CacheKey;
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
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public MsalAccountCacheItem GetAccount(MsalAccountCacheItem accountCacheItem)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheItem appMetadataItem)
        {
            AppMetadataDictionary.TryGetValue(appMetadataItem.CacheKey, out MsalAppMetadataCacheItem cacheItem);
            return cacheItem;
        }
        #endregion

        #region Delete
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            var partitionKey = CacheKeyFactory.GetAppTokenCacheItemKey(item.ClientId, item.TenantId, item.KeyId);

            if (AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition))
            {
                bool removed = partition.TryRemove(item.CacheKey, out _);
                if (removed)
                {
                    Interlocked.Decrement(ref GetEntryCountRef());
                }
                else
                {
                    _logger.InfoPii(
                        () => $"[Internal cache] Cannot delete access token because it was not found in the cache. Key {item.CacheKey}.",
                        () => "[Internal cache] Cannot delete access token because it was not found in the cache.");
                }
            }
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void DeleteAccount(MsalAccountCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }
        #endregion

        #region Get All

        /// <summary>
        /// WARNING: if partitionKey = null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// </summary>
        public virtual List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ILoggerAdapter logger = requestlogger ?? _logger;
            logger.Always($"[Internal cache] Total number of cache partitions found while getting access tokens: {AccessTokenCacheDictionary.Count}");
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

        public virtual List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return CollectionHelpers.GetEmptyList<MsalRefreshTokenCacheItem>();
        }

        public virtual List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)

        {
            return CollectionHelpers.GetEmptyList<MsalIdTokenCacheItem>();
        }

        public virtual List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null, ILoggerAdapter requestlogger = null)

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

        public virtual void Clear(ILoggerAdapter requestlogger = null)
        {
            var logger = requestlogger ?? _logger;
            AccessTokenCacheDictionary.Clear();
            Interlocked.Exchange(ref GetEntryCountRef(), 0);
            logger.Always("[Internal cache] Clearing app token cache accessor.");
            // app metadata isn't removable
        }

        public virtual bool HasAccessOrRefreshTokens()
        {
            return AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }

        private ref int GetEntryCountRef()
        {
            return ref _tokenCacheAccessorOptions.UseSharedCache ? ref s_entryCount : ref _entryCount;
        }

    }
}
