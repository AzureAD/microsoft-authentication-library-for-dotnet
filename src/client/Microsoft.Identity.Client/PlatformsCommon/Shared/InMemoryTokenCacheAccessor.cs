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
    /// Keeps the token cache dictionaries in memory. Token cache extensions
    /// are responsible for persistence.
    /// </summary>
    internal class InMemoryTokenCacheAccessor : ITokenCacheAccessor
    {
        // perf improvement: initialize the capacity and concurrency level 
        // since most websites would use distributed caching, these dictionaries would mostly hold a single item

        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        private readonly ConcurrentDictionary<string, MsalAccessTokenCacheItem> _accessTokenCacheDictionary =
            new ConcurrentDictionary<string, MsalAccessTokenCacheItem>(1, 1);

        private readonly ConcurrentDictionary<string, MsalRefreshTokenCacheItem> _refreshTokenCacheDictionary =
            new ConcurrentDictionary<string, MsalRefreshTokenCacheItem>(1, 1);

        private readonly ConcurrentDictionary<string, MsalIdTokenCacheItem> _idTokenCacheDictionary =
            new ConcurrentDictionary<string, MsalIdTokenCacheItem>(1, 1);

        private readonly ConcurrentDictionary<string, MsalAccountCacheItem> _accountCacheDictionary =
            new ConcurrentDictionary<string, MsalAccountCacheItem>(1, 1);

        private readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> _appMetadataDictionary =
           new ConcurrentDictionary<string, MsalAppMetadataCacheItem>(1, 1);

        protected readonly ICoreLogger _logger;

        public InMemoryTokenCacheAccessor(ICoreLogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        #region Add
        public virtual void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string key = item.GetKey().ToString();

            // if a conflict occurs, pick the latest value
            _accessTokenCacheDictionary[key] = item;
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            string key = item.GetKey().ToString();
            _refreshTokenCacheDictionary[key] = item;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            string key = item.GetKey().ToString();
            _idTokenCacheDictionary[key] = item;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            string key = item.GetKey().ToString();
            _accountCacheDictionary[key] = item;
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.GetKey().ToString();
            _appMetadataDictionary[key] = item;
        }
        #endregion

        #region Get
        public virtual MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            _accessTokenCacheDictionary.TryGetValue(accessTokenKey.ToString(), out MsalAccessTokenCacheItem cacheItem);
            return cacheItem;
        }

        public MsalRefreshTokenCacheItem GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            _refreshTokenCacheDictionary.TryGetValue(refreshTokenKey.ToString(), out var cacheItem);
            return cacheItem;
        }

        public MsalIdTokenCacheItem GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            _idTokenCacheDictionary.TryGetValue(idTokenKey.ToString(), out var cacheItem);
            return cacheItem;
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            _accountCacheDictionary.TryGetValue(accountKey.ToString(), out var cacheItem);
            return cacheItem;
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey)
        {
            _appMetadataDictionary.TryGetValue(appMetadataKey.ToString(), out var cacheItem);
            return cacheItem;
        }
        #endregion

        #region Delete
        public virtual void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            if (!_accessTokenCacheDictionary.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an access token because it was already deleted. Key {cacheKey}",
                    "Cannot delete an access token because it was already deleted");
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            if (!_refreshTokenCacheDictionary.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an refresh token because it was already deleted. Key {cacheKey}",
                    "Cannot delete an refresh token because it was already deleted");
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            if (!_idTokenCacheDictionary.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an id token because it was already deleted. Key {cacheKey}",
                    "Cannot delete an id token because it was already deleted");
            }
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            if (!_accountCacheDictionary.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an account because it was already deleted. Key {cacheKey}",
                    "Cannot delete an account because it was already deleted");
            }
        }

        #endregion

        #region Get All Values
        public virtual IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string filterByTenantId = null)
        {
            return _accessTokenCacheDictionary.Select(kv => kv.Value).ToList();
        }

        public IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens()
        {
            return _refreshTokenCacheDictionary.Select(kv => kv.Value).ToList();
        }

        public IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens()
        {
            return _idTokenCacheDictionary.Select(kv => kv.Value).ToList();
        }

        public IReadOnlyList<MsalAccountCacheItem> GetAllAccounts()
        {
            return _accountCacheDictionary.Select(kv => kv.Value).ToList();
        }

        public IReadOnlyList<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            return _appMetadataDictionary.Select(kv => kv.Value).ToList();
        }
        #endregion

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Clear()
        {
            _accessTokenCacheDictionary.Clear();
            _refreshTokenCacheDictionary.Clear();
            _idTokenCacheDictionary.Clear();
            _accountCacheDictionary.Clear();
            // app metadata isn't removable
        }
    }
}
