// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// A partitioned in-memory token cache implementation for testing purposes. Stores cache data keyed
    /// by <see cref="TokenCacheNotificationArgs.SuggestedCacheKey"/> to simulate a partitioned cache.
    /// </summary>
    public class InMemoryPartitionedTokenCache
    {
        private ConcurrentDictionary<string, byte[]> _cacheData = new ConcurrentDictionary<string, byte[]>();
        private bool _shouldClearExistingCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryPartitionedTokenCache"/> class.
        /// </summary>
        /// <param name="shouldClearExistingCache">Whether to clear existing cache data before deserializing.</param>
        public InMemoryPartitionedTokenCache(bool shouldClearExistingCache = true)
        {
            _shouldClearExistingCache = shouldClearExistingCache;
        }

        /// <summary>
        /// Binds this cache to the specified <see cref="ITokenCache"/> instance.
        /// </summary>
        /// <param name="tokenCache">The token cache instance to bind to.</param>
        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        /// <summary>
        /// Handles the before access notification for the token cache.
        /// </summary>
        /// <param name="args">The token cache notification arguments.</param>
        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                byte[] tokenCacheBytes = ReadCacheBytes(args.SuggestedCacheKey);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: _shouldClearExistingCache);
            }
        }

        /// <summary>
        /// Handles the after access notification for the token cache.
        /// </summary>
        /// <param name="args">The token cache notification arguments.</param>
        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                if (args.HasTokens)
                {
                    WriteCacheBytes(args.SuggestedCacheKey, args.TokenCache.SerializeMsalV3());
                }
                else
                {
                    // No token in the cache. we can remove the cache entry
                    RemoveKey(args.SuggestedCacheKey);
                }
            }
        }

        private byte[] ReadCacheBytes(string cacheKey)
        {
            if (_cacheData.TryGetValue(cacheKey, out byte[] blob))
            {
                return blob;
            }
            return null;
        }

        private void RemoveKey(string cacheKey)
        {
            _cacheData.TryRemove(cacheKey, out _);
        }

        private void WriteCacheBytes(string cacheKey, byte[] bytes)
        {
            _cacheData[cacheKey] = bytes;
        }
    }
}
