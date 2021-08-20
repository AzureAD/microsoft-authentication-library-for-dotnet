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
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> _accessTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>(1, 1);

        public InMemoryPartitionedTokenCacheAccessor(ICoreLogger logger) : base(logger)
        {

        }

        public new void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string key = item.GetKey().ToString();

            // if a conflict occurs, pick the latest value
            _accessTokenCacheDictionary[item.TenantId][key] = item;
        }

        public new MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            _accessTokenCacheDictionary[accessTokenKey.TenantId].TryGetValue(accessTokenKey.ToString(), out MsalAccessTokenCacheItem cacheItem);
            return cacheItem;
        }

        public new void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            if (!_accessTokenCacheDictionary[cacheKey.TenantId].TryRemove(cacheKey.ToString(), out _))
            {
                _logger.InfoPii(
                    $"Cannot delete an access token because it was already deleted. Key {cacheKey}",
                    "Cannot delete an access token because it was already deleted");
            }
        }

        public new IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string tenantID = null)
        {
            if (!string.IsNullOrEmpty(tenantID))
            {
                return _accessTokenCacheDictionary[tenantID].Select(kv => kv.Value).ToList();
            }
            else
            {
                return _accessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
        }

        public new void Clear()
        {
            _accessTokenCacheDictionary.Clear();
            base.Clear();
        }
    }
}
