// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

            CleanCache(logger);
        }

        public bool TryGetOrRemoveExpired(string key, ICoreLogger logger, out MsalServiceException ex)
        {
            ex = null;
            if (_cache.TryGetValue(key, out var entry))
            {
                logger.Info($"[Throttling] Entry found. Creation: {entry.CreationTime} Expiration: {entry.ExpirationTime} ");
                if (entry.IsExpired)
                {
                    logger.Info($"[Throttling] Removing entry because it is expired");
                    _cache.TryRemove(key, out _);
                    return false;
                }

                logger.InfoPii($"[Throttling] Returning valid entry for key {key}", "[Throttling] Returning valid entry.");
                ex = entry.Exception;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public bool IsEmpty()
        {
            return !_cache.Any();
        }

        internal ConcurrentDictionary<string, ThrottlingCacheEntry> CacheForTest => _cache; 

        private void CleanCache(ICoreLogger logger)
        {
            if (_lastCleanupTime + s_cleanupCacheInterval < DateTimeOffset.UtcNow &&
                !_cleanupInProgress)
            {
                logger.Verbose($"[Throttling] Acquiring lock to cleanup throttling state");

                lock (_padlock)
                {
                    if (!_cleanupInProgress)
                    {
                        logger.Verbose($"[Throttling] Cache size before cleaning up {_cache.Count}");

                        _cleanupInProgress = true;
                        CleanupCacheNoLocks();
                        _lastCleanupTime = DateTimeOffset.UtcNow;
                        _cleanupInProgress = false;

                        logger.Verbose($"[Throttling] Cache size after cleaning up {_cache.Count}");
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
