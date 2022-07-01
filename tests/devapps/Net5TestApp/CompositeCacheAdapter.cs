// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using CompositeCache;
using Microsoft.Identity.ServiceEssentials;

namespace Net5TestApp
{
    public class CompositeCacheAdapter : IIdentityCache
    {
        private MemCacheProvider<object> _cache = new();

        public async Task<ICacheEntry<T>> GetAsync<T>(string category, string key, CancellationToken cancellationToken = default)
        {
            var cachedResult = await _cache.GetAsync(key).ConfigureAwait(false);
            return cachedResult != null ? new MsalCacheEntry<T>((T)cachedResult.Value) : null;
        }

        public async Task SetAsync<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
        {
            var expirationDate = DateTimeOffset.UtcNow.Add(cacheEntryOptions.TimeToExpire);
            var cacheEntry = new CacheEntry<object>(key, value, expirationDate, expirationDate, false);
            await _cache.SetAsync(cacheEntry).ConfigureAwait(false);
        }

        #region Not Implemented
        public Task<ICacheEntry<string>> GetAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ICacheEntry<T>> GetWithRefreshFunctionAsync<T>(string category, string key, CacheEntryOptions cacheEntryOptions, Func<string, string, CacheEntryOptions, CancellationToken, Task<T>> refreshFunction, CancellationToken cancellationToken = default) where T : ICacheObject, new()
        {
            throw new NotImplementedException();
        }

        public Task<ICacheEntry<string>> GetWithRefreshFunctionAsync(string category, string key, CacheEntryOptions cacheEntryOptions, Func<string, string, CacheEntryOptions, CancellationToken, Task<string>> refreshFunction, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string category, string key, string value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class MsalCacheEntry<T> : ICacheEntry<T>
    {
        public MsalCacheEntry(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public bool IsValid()
        {
            throw new NotImplementedException();
        }

        public bool IsValidAsLastKnownGood()
        {
            throw new NotImplementedException();
        }
    }
}
