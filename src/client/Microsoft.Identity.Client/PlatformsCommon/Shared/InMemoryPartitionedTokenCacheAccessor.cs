// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class InMemoryPartitionedTokenCacheAccessor : InMemoryTokenCacheAccessor
    {
        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        internal /* internal for test only */ readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> _accessTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>(1, 1);

        public InMemoryPartitionedTokenCacheAccessor(ICoreLogger logger) : base(logger)
        {
        }

        public override void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string key = item.GetKey().ToString();

            // if a conflict occurs, pick the latest value
            _accessTokenCacheDictionary
                .GetOrAdd(item.TenantId, new ConcurrentDictionary<string, MsalAccessTokenCacheItem>())[key] = item;
        }

        public override MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            _accessTokenCacheDictionary.TryGetValue(accessTokenKey.TenantId, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
            MsalAccessTokenCacheItem cacheItem = null;
            partition?.TryGetValue(accessTokenKey.ToString(), out cacheItem);
            return cacheItem;
        }

        public override void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            _accessTokenCacheDictionary.TryGetValue(cacheKey.TenantId, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
            if (partition == null || !partition.TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an access token because it was already deleted. Key {cacheKey}",
                    "Cannot delete an access token because it was already deleted");
            }
        }

        public override IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string filterByTenantId = null)
        {
            if (string.IsNullOrEmpty(filterByTenantId))
            {
                return _accessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                _accessTokenCacheDictionary.TryGetValue(filterByTenantId, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? new List<MsalAccessTokenCacheItem>();
            }
        }

        public override void Clear()
        {
            _accessTokenCacheDictionary.Clear();
            base.Clear();
        }
    }
}
