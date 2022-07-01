// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.ServiceEssentials;

namespace Microsoft.Identity.Client.Cache.Prototype
{
    internal class DefaultInMemoryCache : IIdentityCache
    {
        private readonly MemoryCache _memoryCache;

        public DefaultInMemoryCache(CacheOptions cacheOptions)
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = cacheOptions?.SizeLimit ?? 1000 });
        }

        public Task<ICacheEntry<T>> GetAsync<T>(string category, string key, CancellationToken cancellationToken = default)
        {
            ICacheEntry<T> result = null;
            _memoryCache?.TryGetValue(key, out result);
            return Task.FromResult(result);
        }

        public Task SetAsync<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
        {
            var cacheEntry = new CacheEntry<T>(value, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            var memoryCacheOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(cacheEntryOptions.TimeToExpire),
                Size = 1
            };
            _memoryCache.Set(key, cacheEntry, memoryCacheOptions);
            return Task.CompletedTask;
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
}
