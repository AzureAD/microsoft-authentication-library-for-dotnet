// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.ServiceEssentials.Implementation
{
    /// <summary>
    /// Starting a sample...
    /// </summary>
    public class TestIdentityCache : IIdentityCache, IDisposable
    {
        private bool disposed;

        private readonly Dictionary<string, MemoryCache> memoryCaches
            = new Dictionary<string, MemoryCache>(StringComparer.OrdinalIgnoreCase);

        private readonly IDistributedCache _distributedCache;

        // also takes IIdentityLogger, ITelemetryClient
        // when IDistributedCache is present, takes also IEncryptionProvider

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inMemoryCacheOptions"></param>
        public TestIdentityCache(InMemoryCacheOptions inMemoryCacheOptions)
        {
            _ = inMemoryCacheOptions ?? throw new ArgumentNullException(nameof(inMemoryCacheOptions));

            foreach (var option in inMemoryCacheOptions.CategoryOptions)
            {
                memoryCaches[option.Key] = new MemoryCache(option.Value);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="inMemoryCacheOptions"></param>
        public TestIdentityCache(IOptions<InMemoryCacheOptions> inMemoryCacheOptions)
        {
            _ = inMemoryCacheOptions ?? throw new ArgumentNullException(nameof(inMemoryCacheOptions));

            foreach (var option in inMemoryCacheOptions.Value.CategoryOptions)
            {
                memoryCaches[option.Key] = new MemoryCache(option.Value);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="inMemoryCacheOptions"></param>
        /// <param name="distributedCache"></param>
        public TestIdentityCache(IOptions<InMemoryCacheOptions> inMemoryCacheOptions, IDistributedCache distributedCache)
        {
            _ = inMemoryCacheOptions ?? throw new ArgumentNullException(nameof(inMemoryCacheOptions));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));

            foreach (var option in inMemoryCacheOptions.Value.CategoryOptions)
            {
                memoryCaches[option.Key] = new MemoryCache(option.Value);
            }
        }

        /// <inheritdoc/>
        public async Task<CacheEntry<T>> GetAsync<T>(string category, string key, CancellationToken cancellationToken = default) where T : ICacheObject
        {
            return await GetAsyncInternalAsync<T>(category, key, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<CacheEntry<string>> GetAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            return await GetAsyncInternalAsync<string>(category, key, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CacheEntry<T>> GetAsyncInternalAsync<T>(string category, string key, CancellationToken cancellationToken)
        {
            var cache = GetMemoryCache(category);

            CacheEntry<T> result = null;
            if (cache?.TryGetValue(key, out result) == true)
                return result;
            else if (_distributedCache != null)
            {
                var l2CacheValue = await _distributedCache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
                if (l2CacheValue == null)
                    return null;

                // todo: decrypt

                DistributedCacheEntry<T> entry = new DistributedCacheEntry<T>();
                entry.Deserialize(l2CacheValue);

                // propagate to L1
                SetToMemoryCacheInternal(category, key, entry.Value, new CacheEntryOptions(entry.ExpirationTimeUTC, entry.RefreshTimeUTC, entry.MaxCategoryCount) { JitterInSeconds = entry.JitterInSeconds });

                if (cache?.TryGetValue(key, out result) == true)
                    return result;
                else
                    return null;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string category, string key, CancellationToken cancellationToken = default)
        {
            var cache = GetMemoryCache(category);
            cache?.Remove(key);
            if (_distributedCache != null)
                await _distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default) where T : ICacheObject
        {
            await SetAsyncInternalAsync(category, key, value, cacheEntryOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SetAsync(string category, string key, string value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken = default)
        {
            await SetAsyncInternalAsync(category, key, value, cacheEntryOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SetAsyncInternalAsync<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions, CancellationToken cancellationToken)
        {
            _ = cacheEntryOptions ?? throw new ArgumentNullException(nameof(cacheEntryOptions));
            SetToMemoryCacheInternal(category, key, value, cacheEntryOptions);

            if (_distributedCache != null)
            {
                // set to L2 too
                var distributedCacheEntry = new DistributedCacheEntry<T>()
                {
                    Value = value,
                    ExpirationTimeUTC = cacheEntryOptions.ExpirationTimeUTC,
                    RefreshTimeUTC = cacheEntryOptions.RefreshTimeUTC,
                    MaxCategoryCount = cacheEntryOptions.MaxCategoryCount,
                    JitterInSeconds = cacheEntryOptions.JitterInSeconds
                };
                string serializedCacheEntry = distributedCacheEntry.Serialize();
                // todo: encrypt
                await _distributedCache.SetStringAsync(key, serializedCacheEntry, cancellationToken).ConfigureAwait(false);
            }
        }

        internal void SetToMemoryCacheInternal<T>(string category, string key, T value, CacheEntryOptions cacheEntryOptions)
        {
            var cache = GetOrCreateMemoryCache(category, cacheEntryOptions);

            // apply jitter
            var expirationTime = cacheEntryOptions.ExpirationTimeUTC.AddOrCap(cacheEntryOptions.JitterInSeconds);
            var refreshTime = cacheEntryOptions.RefreshTimeUTC.AddOrCap(cacheEntryOptions.JitterInSeconds);

            var cacheEntry = new CacheEntry<T>(value, expirationTime, refreshTime);
            var memoryCacheOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpiration = expirationTime,
                Size = 1
            };
            cache?.Set(key, cacheEntry, memoryCacheOptions);
        }

        private MemoryCache GetMemoryCache(string category)
        {
            if (memoryCaches.TryGetValue(category, out var cache))
                return cache;


            return null;
        }

        private MemoryCache GetOrCreateMemoryCache(string category, CacheEntryOptions cacheEntryOptions)
        {
            if (memoryCaches.TryGetValue(category, out var cache))
                return cache;

            memoryCaches[category] = new MemoryCache(new MemoryCacheOptions() { SizeLimit = cacheEntryOptions.MaxCategoryCount });

            return memoryCaches[category];
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) below.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        /// <param name="disposing">Whether this is called by user code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var memoryCache in memoryCaches)
                    {
                        memoryCache.Value.Dispose();
                    }
                }

                disposed = true;
            }
        }
    }
}
