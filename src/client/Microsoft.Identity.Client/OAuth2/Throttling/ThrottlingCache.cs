// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    internal class ThrottlingCache
    {
        internal const int DefaultCleanupIntervalMs = 5 * 60 * 1000; // internal for test

        private volatile bool _cleanupInProgress = false;
        private static readonly object _padlock = new object();

        /// <summary>
        /// To prevent the cache from becoming too large, purge expired entries every X seconds
        /// </summary>
        private readonly TimeSpan s_cleanupCacheInterval;

        private DateTimeOffset _lastCleanupTime = DateTimeOffset.UtcNow;

        private readonly ConcurrentDictionary<string, ThrottlingCacheEntry> _cache =
            new ConcurrentDictionary<string, ThrottlingCacheEntry>();

        public ThrottlingCache(int? customCleanupIntervalMs = null)
        {
            s_cleanupCacheInterval = customCleanupIntervalMs.HasValue ?
                TimeSpan.FromMilliseconds(customCleanupIntervalMs.Value) :
                TimeSpan.FromMilliseconds(DefaultCleanupIntervalMs);
        }

        public void AddAndCleanup(string key, ThrottlingCacheEntry entry, ICoreLogger logger)
        {
            // in a high concurrency scenario, pick the most fresh entry
            _cache.AddOrUpdate(
                key,
                entry,
                (_, oldEntry) => entry.CreationTime > oldEntry.CreationTime ? entry : oldEntry);

            logger.Verbose($"Cache size before cleaning up {_cache.Count}");
            CleanCache();
            logger.Verbose($"Cache size after cleaning up {_cache.Count}");
        }

        public bool TryGetOrRemoveExpired(string key, out MsalServiceException ex)
        {
            ex = null;
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    return false;
                }
                
                ex = entry.Exception;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        internal ConcurrentDictionary<string, ThrottlingCacheEntry> CacheForTest => _cache; 


        private void CleanCache()
        {

            if (_lastCleanupTime + s_cleanupCacheInterval < DateTimeOffset.UtcNow &&
                !_cleanupInProgress)
            {
                lock (_padlock)
                {
                    if (!_cleanupInProgress)
                    {
                        _cleanupInProgress = true;
                        CleanupCacheNoLocks();
                        _lastCleanupTime = DateTimeOffset.UtcNow;
                        _cleanupInProgress = false;
                    }
                }
            }
        }

        private void CleanupCacheNoLocks()
        {
            List<string> toRemove = new List<string>();
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
