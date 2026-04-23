// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class InMemoryPartitionedTokenCache
    {
        private ConcurrentDictionary<string, byte[]> _cacheData = new ConcurrentDictionary<string, byte[]>();
        private bool _shouldClearExistingCache;

        public InMemoryPartitionedTokenCache(bool shouldClearExistingCache = true)
        {
            _shouldClearExistingCache = shouldClearExistingCache;
        }

        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(args.SuggestedCacheKey))
            {
                byte[] tokenCacheBytes = ReadCacheBytes(args.SuggestedCacheKey);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: _shouldClearExistingCache);
            }
        }

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
