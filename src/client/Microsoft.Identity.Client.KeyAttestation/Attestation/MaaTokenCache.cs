// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Orchestrates MAA token caching with dual-layer storage:
    ///   1) In-memory cache (fast per-process access)
    ///   2) Persistent cache (best-effort cross-process/restart)
    /// Tokens are refreshed when less than 50% of their lifetime remains.
    /// Persistence is best-effort and never blocks token acquisition.
    /// </summary>
    internal sealed class MaaTokenCache
    {
        private readonly ConcurrentDictionary<string, MaaTokenCacheEntry> _memoryCache = new();
        private readonly KeyedSemaphorePool _gates = new();
        private readonly IPersistentMaaTokenCache _persistedCache;

        /// <summary>
        /// Creates a new MAA token cache with the specified persistent cache.
        /// </summary>
        /// <param name="persistedCache">The persistent cache implementation (can be null for in-memory only).</param>
        public MaaTokenCache(IPersistentMaaTokenCache persistedCache)
        {
            _persistedCache = persistedCache;
        }

        /// <summary>
        /// Gets or creates an MAA attestation token using the factory function.
        /// Implements intelligent caching with 50% lifetime refresh threshold.
        /// </summary>
        /// <param name="cacheKey">The cache key (typically: endpoint + clientId + key identifier).</param>
        /// <param name="factory">Factory function to mint a new token if cache miss or refresh needed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="logVerbose">Optional logging callback.</param>
        /// <returns>The MAA attestation token.</returns>
        public async Task<string> GetOrCreateAsync(
            string cacheKey,
            Func<Task<AttestationResult>> factory,
            CancellationToken cancellationToken,
            Action<string> logVerbose = null)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("cacheKey must be non-empty.", nameof(cacheKey));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var now = DateTimeOffset.UtcNow;

            // 1) In-memory cache first
            if (_memoryCache.TryGetValue(cacheKey, out var cachedEntry))
            {
                if (!cachedEntry.NeedsRefresh(now))
                {
                    logVerbose?.Invoke($"[MaaTokenCache] Cache HIT (memory) for '{cacheKey}'. Remaining: {(cachedEntry.ExpiresAt - now).TotalMinutes:F1} min.");
                    return cachedEntry.Token;
                }
                else
                {
                    logVerbose?.Invoke($"[MaaTokenCache] Cache entry needs refresh (50% threshold). Remaining: {(cachedEntry.ExpiresAt - now).TotalMinutes:F1} min.");
                }
            }

            // 2) Per-key gate (dedupe concurrent mint)
            await _gates.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                // Re-check after acquiring the gate
                if (_memoryCache.TryGetValue(cacheKey, out cachedEntry))
                {
                    if (!cachedEntry.NeedsRefresh(now))
                    {
                        logVerbose?.Invoke($"[MaaTokenCache] Cache HIT (memory-after-gate) for '{cacheKey}'.");
                        return cachedEntry.Token;
                    }
                }

                // 3) Try persistent cache (best-effort)
                if (_persistedCache != null)
                {
                    if (_persistedCache.TryRead(cacheKey, out var persistedEntry, logVerbose))
                    {
                        now = DateTimeOffset.UtcNow; // Refresh time
                        if (!persistedEntry.NeedsRefresh(now))
                        {
                            logVerbose?.Invoke($"[MaaTokenCache] Cache HIT (persistent) for '{cacheKey}'. Remaining: {(persistedEntry.ExpiresAt - now).TotalMinutes:F1} min.");

                            // Back-fill memory cache
                            _memoryCache[cacheKey] = persistedEntry;
                            return persistedEntry.Token;
                        }
                        else
                        {
                            logVerbose?.Invoke($"[MaaTokenCache] Persistent cache entry needs refresh (50% threshold).");
                        }
                    }
                }

                // 4) Mint new token + back-fill both caches
                logVerbose?.Invoke($"[MaaTokenCache] Cache MISS -> minting new token for '{cacheKey}'.");

                var result = await factory().ConfigureAwait(false);

                if (result.Status != AttestationStatus.Success || string.IsNullOrWhiteSpace(result.Jwt))
                {
                    throw new InvalidOperationException(
                        $"Attestation failed: status={result.Status}, nativeRc={result.NativeErrorCode}, msg={result.ErrorMessage}");
                }

                // Extract timestamps from JWT
                if (!JwtHelper.TryExtractTimestamps(result.Jwt, out var issuedAt, out var expiresAt))
                {
                    // Fallback: use current time + 1 hour if extraction fails
                    logVerbose?.Invoke("[MaaTokenCache] Warning: Could not extract JWT timestamps. Using default 1-hour expiry.");
                    issuedAt = DateTimeOffset.UtcNow;
                    expiresAt = issuedAt.AddHours(1);
                }

                var newEntry = new MaaTokenCacheEntry(result.Jwt, issuedAt, expiresAt);

                // Back-fill memory cache
                _memoryCache[cacheKey] = newEntry;
                logVerbose?.Invoke($"[MaaTokenCache] Cached new token. Expires in {(expiresAt - DateTimeOffset.UtcNow).TotalMinutes:F1} min.");

                // Best-effort persist (never blocks)
                if (_persistedCache != null)
                {
                    _persistedCache.TryWrite(cacheKey, newEntry, logVerbose);
                }

                return newEntry.Token;
            }
            finally
            {
                _gates.Release(cacheKey);
            }
        }

        /// <summary>
        /// Clears the in-memory cache. Persistent cache is not affected.
        /// </summary>
        public void ClearMemoryCache()
        {
            _memoryCache.Clear();
        }
    }
}
