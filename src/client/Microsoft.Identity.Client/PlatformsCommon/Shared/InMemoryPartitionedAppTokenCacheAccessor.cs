// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Stores tokens for an application.
    /// Partitions the access token collection by a key of client ID with tenant ID.
    /// App metadata collection is not partitioned.
    /// Refresh token, ID token, and account related methods are no-op.
    /// </summary>
    internal class InMemoryPartitionedAppTokenCacheAccessor : ITokenCacheAccessor
    {
        // perf: do not use ConcurrentDictionary.Values as it takes a lock
        // internal for test only
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> AccessTokenCacheDictionary;
        internal readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> AppMetadataDictionary;

        // static versions to support the "shared cache" mode
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>> s_accessTokenCacheDictionary =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
        private static readonly ConcurrentDictionary<string, MsalAppMetadataCacheItem> s_appMetadataDictionary =
           new ConcurrentDictionary<string, MsalAppMetadataCacheItem>(1, 1);

        protected readonly ILoggerAdapter _logger;
        private readonly CacheOptions _tokenCacheAccessorOptions;

        private int _entryCount = 0;
        private static int s_entryCount = 0;

        // Shared-cache configuration invariant: all accessors that use shared mode must agree
        // on bounded-cache settings so they do not disagree on thresholds for the same static store.
        private static readonly object s_sharedCacheConfigLock = new object();
        private static bool s_sharedCacheConfigInitialized = false;
        private static int s_sharedMaxEntries = 0;

        // Bounded-mode coordination. Eviction is single-threaded via a CAS flag
        // (0 = idle, 1 = running). Other threads observing the threshold while
        // eviction is running skip the trigger and let the in-progress pass do the work.
        private int _evictionRunning = 0;
        private static int s_evictionRunning = 0;

        // Cached settings — derived once in the constructor so the save-path hot check
        // is a single read of a bool plus an int comparison.
        //
        // Invariant for shared-cache mode (UseSharedCache == true):
        // all accessors that share the static dictionary must be built with the same
        // CacheOptions values for EnableAppCacheBounding and AppCacheMaxEntries.
        // Mixing values across accessors over the same static cache would cause
        // different accessors to disagree on the eviction threshold for shared data.
        private readonly bool _boundedEnabled;
        private readonly int _maxEntries;
        private readonly int _lowWatermark;

        // Sampled-eviction tuning. Kept conservative to bound per-eviction CPU and
        // to keep the sampling cost independent of partition size. Note that the
        // ReservoirScanCap intentionally biases reservoir picks toward the first 64
        // entries in enumeration order for pathologically large single partitions —
        // accepted trade-off for a constant-time per-sample cost. MSAL partition keys
        // are highly specific (clientId + tenant + keyId + extras) so real partitions
        // are tiny and the cap is essentially never hit in normal use.
        private const int EvictionSampleSize = 8;
        private const int ReservoirScanCap = 64;
        private const int MaxEvictionPassesPerTrigger = 3;

        public int EntryCount => GetEntryCountRef();
       
        public InMemoryPartitionedAppTokenCacheAccessor(
            ILoggerAdapter logger,
            CacheOptions tokenCacheAccessorOptions)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _tokenCacheAccessorOptions = tokenCacheAccessorOptions ?? new CacheOptions();

            if (_tokenCacheAccessorOptions.UseSharedCache)
            {
                AccessTokenCacheDictionary = s_accessTokenCacheDictionary;
                AppMetadataDictionary = s_appMetadataDictionary;
            }
            else
            {
                AccessTokenCacheDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MsalAccessTokenCacheItem>>();
                AppMetadataDictionary = new ConcurrentDictionary<string, MsalAppMetadataCacheItem>();
            }

            _boundedEnabled = _tokenCacheAccessorOptions.AppCacheMaxEntries > 0;
            _maxEntries = _tokenCacheAccessorOptions.AppCacheMaxEntries;
            _lowWatermark = _boundedEnabled
                ? Math.Max(1, (int)(_maxEntries * 0.75))
                : 0;

            EnsureSharedCacheInvariant();
        }

        #region Add
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            string itemKey = item.CacheKey;
            string partitionKey = CacheKeyFactory.GetAppTokenCacheItemKey(item.ClientId, item.TenantId, item.KeyId, item.AdditionalCacheKeyComponents);

            var partition = AccessTokenCacheDictionary.GetOrAdd(partitionKey, _ => new ConcurrentDictionary<string, MsalAccessTokenCacheItem>());
            bool added = partition.TryAdd(itemKey, item);

            // only increment the entry count if the item was added, not updated
            if (added)
            {
                Interlocked.Increment(ref GetEntryCountRef());

                // Bounded-mode hot path: one bool read + one int comparison when disabled.
                // Updates (added == false) don't change the count, so no need to re-check.
                if (_boundedEnabled && GetEntryCountRef() > _maxEntries)
                {
                    TryEvict();
                }
            }
            else
            {
                partition[itemKey] = item;
            }
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void SaveAccount(MsalAccountCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            string key = item.CacheKey;
            AppMetadataDictionary[key] = item;
        }
        #endregion

        #region Get
        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public MsalAccountCacheItem GetAccount(MsalAccountCacheItem accountCacheItem)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheItem appMetadataItem)
        {
            AppMetadataDictionary.TryGetValue(appMetadataItem.CacheKey, out MsalAppMetadataCacheItem cacheItem);
            return cacheItem;
        }
        #endregion

        #region Delete
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            var partitionKey = CacheKeyFactory.GetAppTokenCacheItemKey(item.ClientId, item.TenantId, item.KeyId);

            if (AccessTokenCacheDictionary.TryGetValue(partitionKey, out var partition))
            {
                bool removed = partition.TryRemove(item.CacheKey, out _);
                if (removed)
                {
                    Interlocked.Decrement(ref GetEntryCountRef());
                }
                else
                {
                    _logger.InfoPii(
                        () => $"[Internal cache] Cannot delete access token because it was not found in the cache. Key {item.CacheKey}.",
                        () => "[Internal cache] Cannot delete access token because it was not found in the cache.");
                }
            }
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no refresh tokens in a client credential flow.
        /// </summary>
        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no ID tokens in a client credential flow.
        /// </summary>
        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }

        /// <summary>
        /// This method is not supported for the app token cache because
        /// there are no user accounts in a client credential flow.
        /// </summary>
        public void DeleteAccount(MsalAccountCacheItem item)
        {
            throw new MsalClientException(MsalError.CombinedUserAppCacheNotSupported, MsalErrorMessage.CombinedUserAppCacheNotSupported);
        }
        #endregion

        #region Get All

        /// <summary>
        /// WARNING: if partitionKey = null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// </summary>
        public virtual List<MsalAccessTokenCacheItem> GetAllAccessTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            ILoggerAdapter logger = requestlogger ?? _logger;
            logger.Always($"[Internal cache] Total number of cache partitions found while getting access tokens: {AccessTokenCacheDictionary.Count}");
            if (string.IsNullOrEmpty(partitionKey))
            {
                return AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                AccessTokenCacheDictionary.TryGetValue(partitionKey, out ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition);
                return partition?.Select(kv => kv.Value)?.ToList() ?? CollectionHelpers.GetEmptyList<MsalAccessTokenCacheItem>();
            }
        }

        public virtual List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return CollectionHelpers.GetEmptyList<MsalRefreshTokenCacheItem>();
        }

        public virtual List<MsalIdTokenCacheItem> GetAllIdTokens(string partitionKey = null, ILoggerAdapter requestlogger = null)

        {
            return CollectionHelpers.GetEmptyList<MsalIdTokenCacheItem>();
        }

        public virtual List<MsalAccountCacheItem> GetAllAccounts(string partitionKey = null, ILoggerAdapter requestlogger = null)

        {
            return CollectionHelpers.GetEmptyList<MsalAccountCacheItem>();
        }

        public List<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            return AppMetadataDictionary.Select(kv => kv.Value).ToList();
        }
        #endregion

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            throw new NotImplementedException();
        }

        public virtual void Clear(ILoggerAdapter requestlogger = null)
        {
            var logger = requestlogger ?? _logger;
            AccessTokenCacheDictionary.Clear();
            Interlocked.Exchange(ref GetEntryCountRef(), 0);
            logger.Always("[Internal cache] Clearing app token cache accessor.");
            // app metadata isn't removable
        }

        public virtual bool HasAccessOrRefreshTokens()
        {
            return AccessTokenCacheDictionary.Any(partition => partition.Value.Any(token => !token.Value.IsExpiredWithBuffer()));
        }

        private ref int GetEntryCountRef()
        {
            return ref _tokenCacheAccessorOptions.UseSharedCache ? ref s_entryCount : ref _entryCount;
        }

        private ref int GetEvictionRunningRef()
        {
            return ref _tokenCacheAccessorOptions.UseSharedCache ? ref s_evictionRunning : ref _evictionRunning;
        }

        private void EnsureSharedCacheInvariant()
        {
            if (!_tokenCacheAccessorOptions.UseSharedCache)
            {
                return;
            }

            lock (s_sharedCacheConfigLock)
            {
                if (!s_sharedCacheConfigInitialized)
                {
                    s_sharedMaxEntries = _maxEntries;
                    s_sharedCacheConfigInitialized = true;
                    return;
                }

                if (s_sharedMaxEntries != _maxEntries)
                {
                    throw new InvalidOperationException(
                        "All shared-cache accessors must use identical bounded-cache settings. " +
                        $"Expected AppCacheMaxEntries={s_sharedMaxEntries}; " +
                        $"received AppCacheMaxEntries={_maxEntries}.");
                }
            }
        }

        /// <summary>
        /// Attempts to start a single eviction pass. If another thread is already evicting,
        /// returns immediately — the in-progress pass will trim the cache, and subsequent
        /// writes will trigger again if it is not enough. null
        /// </summary> 
        private void TryEvict()
        {
            if (Interlocked.CompareExchange(ref GetEvictionRunningRef(), 1, 0) != 0)
            {
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                int passes = 0;
                int countBefore = GetEntryCountRef();

                while (GetEntryCountRef() > _maxEntries && passes < MaxEvictionPassesPerTrigger)
                {
                    passes++;
                    bool hitIterationCap = EvictDown();
    
                    if (hitIterationCap)
                    {
                        _logger.Warning(
                            $"[Internal cache] Bounded app cache eviction pass {passes}/{MaxEvictionPassesPerTrigger} hit iteration cap. " +
                            $"Current count={GetEntryCountRef()}, max={_maxEntries}, target={_lowWatermark}.");
                    }
                }

                stopwatch.Stop();
                int countAfter = GetEntryCountRef();

                _logger.Info(() =>
                    $"[Internal cache] Bounded app cache eviction trigger completed in {stopwatch.ElapsedMilliseconds} ms " +
                    $"(passes: {passes}, count: {countBefore} -> {countAfter}, max: {_maxEntries}, target: {_lowWatermark}).");

                if (countAfter > _maxEntries)
                {
                    _logger.Warning(
                        $"[Internal cache] Bounded app cache remains above max after {passes} passes. " +
                        $"Current count={countAfter}, max={_maxEntries}, target={_lowWatermark}. Subsequent writes will re-trigger eviction.");
                }
            }
            finally
            {
                Volatile.Write(ref GetEvictionRunningRef(), 0);
            }
        }

        /// <summary>
        /// Approximate eviction (Redis-style sampled LRU + expired-first preference).
        /// Each sample picks <see cref="EvictionSampleSize"/> entries pseudo-randomly across partitions:
        /// any expired sample is evicted immediately; otherwise the oldest by <c>CachedAt</c> is evicted.
        /// Loops until <see cref="_lowWatermark"/> is reached or a safety cap fires.
        /// </summary>
        private bool EvictDown()
        {
            int target = _lowWatermark;
            int countBefore = GetEntryCountRef();
            if (countBefore <= target)
            {
                return false;
            }

            // Snapshot only non-empty partitions so samples don't waste iterations on drained ones.
            string[] partitionKeys = SnapshotNonEmptyPartitionKeys();
            if (partitionKeys.Length == 0)
            {
                return false;
            }

            // Eviction runs single-threaded under the CAS flag, so a local Random is safe.
            var rng = new Random();

            // Generous cap: empty partitions accumulate as we evict, so allow extra iterations
            // for misses. Still bounded \u2014 if we don't converge the next save re-triggers.
            // Capped at 20,000 to prevent CPU spikes or latency regressions on extreme bulk evictions.
            int iterCap = Math.Min(20000, Math.Max(64, (GetEntryCountRef() - target) * 16));
            int refreshesLeft = 4;
            int iters = 0;

            while (GetEntryCountRef() > target && iters++ < iterCap)
            {
                if (!TrySampleVictim(partitionKeys, rng, out var partition, out var key, out var item))
                {
                    // Sample landed entirely on empty partitions. Refresh the snapshot.
                    if (refreshesLeft-- <= 0)
                    {
                        break;
                    }

                    partitionKeys = SnapshotNonEmptyPartitionKeys();
                    if (partitionKeys.Length == 0)
                    {
                        break;
                    }

                    continue;
                }

                TryRemoveExact(partition, key, item);
            }

            int countAfter = GetEntryCountRef();
            int evicted = countBefore - countAfter;
            int capturedIters = iters;
            _logger.Info(() =>
                $"[Internal cache] Bounded app cache eviction trimmed {evicted} entries " +
                $"(count: {countBefore} -> {countAfter}, target: {target}, max: {_maxEntries}, iterations: {capturedIters}).");

            return capturedIters >= iterCap && countAfter > target;
        }

        private string[] SnapshotNonEmptyPartitionKeys()
        {
            var list = new List<string>();
            foreach (var kvp in AccessTokenCacheDictionary)
            {
                if (!kvp.Value.IsEmpty)
                {
                    list.Add(kvp.Key);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Picks <see cref="EvictionSampleSize"/> entries pseudo-randomly across partitions.
        /// Returns the first expired entry encountered (expired-first preference), or the
        /// oldest sampled entry by <c>CachedAt</c>. Returns <c>false</c> if no entry was sampled.
        /// </summary>
        private bool TrySampleVictim(
            string[] partitionKeys,
            Random rng,
            out ConcurrentDictionary<string, MsalAccessTokenCacheItem> bestPartition,
            out string bestKey,
            out MsalAccessTokenCacheItem bestItem)
        {
            bestPartition = null;
            bestKey = null;
            bestItem = null;
            DateTimeOffset oldestCachedAt = DateTimeOffset.MaxValue;

            for (int i = 0; i < EvictionSampleSize; i++)
            {
                string pKey = partitionKeys[rng.Next(partitionKeys.Length)];
                if (!AccessTokenCacheDictionary.TryGetValue(pKey, out var partition))
                {
                    continue;
                }

                if (!TryReservoirPick(partition, rng, out string itemKey, out MsalAccessTokenCacheItem item))
                {
                    continue;
                }

                // Expired-first: as soon as we see an expired sample, evict it.
                if (item.IsExpiredWithBuffer())
                {
                    bestPartition = partition;
                    bestKey = itemKey;
                    bestItem = item;
                    return true;
                }

                if (item.CachedAt < oldestCachedAt)
                {
                    oldestCachedAt = item.CachedAt;
                    bestPartition = partition;
                    bestKey = itemKey;
                    bestItem = item;
                }
            }

            return bestItem is not null;
        }

        /// <summary>
        /// Picks one random entry from a partition using reservoir sampling, capped at
        /// <see cref="ReservoirScanCap"/> to keep cost independent of partition size.
        /// </summary>
        private static bool TryReservoirPick(
            ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition,
            Random rng,
            out string pickedKey,
            out MsalAccessTokenCacheItem pickedItem)
        {
            pickedKey = null;
            pickedItem = null;
            int n = 0;

            foreach (var kvp in partition)
            {
                n++;
                // Reservoir: replace with probability 1/n.
                if (rng.Next(n) == 0)
                {
                    pickedKey = kvp.Key;
                    pickedItem = kvp.Value;
                }

                if (n >= ReservoirScanCap)
                {
                    break;
                }
            }

            return n > 0;
        }

        /// <summary>
        /// Removes an entry only if the current value still matches <paramref name="expected"/>
        /// (reference equality, since <see cref="MsalAccessTokenCacheItem"/> has no Equals override).
        /// Prevents evicting a fresher entry that was concurrently re-saved under the same key.
        /// </summary>
        private bool TryRemoveExact(
            ConcurrentDictionary<string, MsalAccessTokenCacheItem> partition,
            string key,
            MsalAccessTokenCacheItem expected)
        {
            var asCollection = (ICollection<KeyValuePair<string, MsalAccessTokenCacheItem>>)partition;
            if (asCollection.Remove(new KeyValuePair<string, MsalAccessTokenCacheItem>(key, expected)))
            {
                Interlocked.Decrement(ref GetEntryCountRef());
                return true;
            }

            return false;
        }

        public static void ClearStaticCacheForTest()
        {
            s_accessTokenCacheDictionary.Clear();
            s_appMetadataDictionary.Clear();
            Interlocked.Exchange(ref s_entryCount, 0);
            Interlocked.Exchange(ref s_evictionRunning, 0);
            lock (s_sharedCacheConfigLock)
            {
                s_sharedCacheConfigInitialized = false;
                s_sharedMaxEntries = 0;
            }
        }
    }
}
