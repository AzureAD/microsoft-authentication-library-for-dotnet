// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using AgentCcaManagement.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;

namespace AgentCcaManagement.SlidingExpiration;

/// <summary>
/// Bounded strategy using <see cref="MemoryCache"/> with sliding expiration
/// and a hard size cap. When either the sliding window expires or the size
/// limit is reached, entries are evicted automatically.
///
/// Key design decisions:
/// - Sliding expiration aligns with token lifetimes: a CCA idle for hours
///   likely has expired tokens, so eviction cost is near-zero.
/// - Size cap prevents burst scenarios (many agents created in a short window)
///   from growing unbounded before expiration kicks in.
/// - Eviction callbacks enable cleanup of companion data structures
///   (e.g., account identifier maps in ID Web).
/// - Double-checked locking via per-key semaphores prevents the MemoryCache
///   "stampede" problem (multiple concurrent callers all building the same CCA).
/// </summary>
public sealed class SlidingExpirationAgentTokenService : IAgentTokenService
{
    private readonly MemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly AgentTokenServiceOptions _options;
    private readonly Func<string, Task<IConfidentialClientApplication>> _ccaFactory;
    private int _currentCount;

    /// <summary>
    /// Raised when a CCA is evicted from the cache.
    /// Consumers can use this to clean up companion state (e.g., account ID maps).
    /// </summary>
    public event Action<string, IConfidentialClientApplication, EvictionReason>? OnEviction;

    public SlidingExpirationAgentTokenService(
        AgentTokenServiceOptions options,
        Func<string, Task<IConfidentialClientApplication>>? ccaFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ccaFactory = ccaFactory ?? (agentAppId => Task.FromResult(MockCcaFactory.CreateMockCca(agentAppId, _options.Authority)));

        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxAgentCcas > 0 ? _options.MaxAgentCcas : null
        });
    }

    public int CachedCcaCount => _currentCount;

    public bool ContainsAgent(string agentAppId)
    {
        return _cache.TryGetValue(agentAppId, out _);
    }

    public async Task<IConfidentialClientApplication> GetOrCreateAgentCcaAsync(string agentAppId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentAppId);

        // Fast path: check cache without locking
        if (_cache.TryGetValue(agentAppId, out IConfidentialClientApplication? existing) && existing is not null)
        {
            return existing;
        }

        // Slow path: acquire per-key semaphore to prevent stampede
        var semaphore = _semaphores.GetOrAdd(agentAppId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(agentAppId, out existing) && existing is not null)
            {
                return existing;
            }

            var newApp = await _ccaFactory(agentAppId).ConfigureAwait(false);

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = _options.SlidingExpiration,
            };

            cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                Interlocked.Decrement(ref _currentCount);

                if (value is IConfidentialClientApplication evictedCca)
                {
                    OnEviction?.Invoke((string)key, evictedCca, reason);
                }

                // Clean up the semaphore for this key (it's no longer needed)
                if (key is string keyStr)
                {
                    _semaphores.TryRemove(keyStr, out _);
                }
            });

            _cache.Set(agentAppId, newApp, cacheEntryOptions);
            Interlocked.Increment(ref _currentCount);

            return newApp;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
