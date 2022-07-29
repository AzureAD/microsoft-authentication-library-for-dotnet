// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.ServiceEssentials;

namespace Net5TestApp
{
    public class CompositeCacheAdapter : IIdentityCache
    {
        private readonly MemCacheProvider<object> _cache = new();

        public async Task<Microsoft.Identity.ServiceEssentials.CacheEntry<T>> GetAsync<T>(string category, string key, CancellationToken cancellationToken = default) where T : ICacheObject
        {
            var compositeCacheEntry = await _cache.GetAsync(key).ConfigureAwait(false);
            return compositeCacheEntry != null ?
                new Microsoft.Identity.ServiceEssentials.CacheEntry<T>(
                    (T)compositeCacheEntry.Value,
                    compositeCacheEntry.Expiration,
                    compositeCacheEntry.Refresh) :
                null;
        }

        public async Task<CacheEntry<T>> SetAsync<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default) where T : ICacheObject
        {
            var cacheEntry = new CompositeCache.CacheEntry<object>(
                key,
                value,
                DateTimeOffset.UtcNow.Add(cacheEntryOptions.ExpirationTimeRelativeToNow),
                DateTimeOffset.UtcNow.Add(cacheEntryOptions.ExpirationTimeRelativeToNow),
                false);
            await _cache.SetAsync(cacheEntry).ConfigureAwait(false);

            return new CacheEntry<T>(value, DateTimeOffset.UtcNow.Add(cacheEntryOptions.ExpirationTimeRelativeToNow), DateTimeOffset.UtcNow.Add(cacheEntryOptions.RefreshTimeRelativeToNow));
        }

        #region Not Implemented
        public Task RemoveAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<Microsoft.Identity.ServiceEssentials.CacheEntry<string>> GetAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string category, string key, string value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
