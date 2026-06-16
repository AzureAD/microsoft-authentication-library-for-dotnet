// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using AgentCcaManagement.Shared;
using Microsoft.Identity.Client;

namespace AgentCcaManagement.TimestampSweep;

/// <summary>
/// Entry wrapper that tracks last-access time for eviction decisions.
/// </summary>
internal sealed class TimestampedCca
{
    public IConfidentialClientApplication Cca { get; }
    private long _lastAccessedTicks;

    public TimestampedCca(IConfidentialClientApplication cca)
    {
        Cca = cca;
        _lastAccessedTicks = Environment.TickCount64;
    }

    public long LastAccessedTicks => Volatile.Read(ref _lastAccessedTicks);

    public void Touch() => Volatile.Write(ref _lastAccessedTicks, Environment.TickCount64);

    public bool IsExpired(long maxIdleMilliseconds)
    {
        return (Environment.TickCount64 - LastAccessedTicks) > maxIdleMilliseconds;
    }
}

/// <summary>
/// Bounded strategy using a ConcurrentDictionary with timestamp-tracked entries
/// and a periodic background sweep that removes idle entries.
///
/// Key design decisions:
/// - Stays close to the current ConcurrentDictionary approach — minimal conceptual
///   change for developers familiar with the existing pattern.
/// - Background Timer runs at a configurable interval (default 30 min) and removes
///   entries that haven't been accessed within the sliding window.
/// - Optional hard size cap: when exceeded during GetOrCreate, the oldest entry
///   is evicted synchronously before adding the new one.
/// - No GC-triggered eviction surprises (unlike MemoryCache).
/// - Access tracking uses lock-free Volatile.Write on a long tick count.
///
/// Tradeoffs vs MemoryCache:
/// - Pro: Predictable eviction timing (only during sweep or size-cap enforcement).
/// - Pro: Simpler mental model for developers — it's still a dictionary.
/// - Con: You maintain the sweep logic yourself.
/// - Con: Between sweeps, stale entries remain (lazy vs eager cleanup).
/// </summary>
public sealed class TimestampSweepAgentTokenService : IAgentTokenService
{
    private readonly ConcurrentDictionary<string, TimestampedCca> _agentCcas = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly AgentTokenServiceOptions _options;
    private readonly Func<string, Task<IConfidentialClientApplication>> _ccaFactory;
    private readonly Timer? _sweepTimer;
    private readonly long _maxIdleMs;

    /// <summary>
    /// Raised when a CCA is evicted (either by sweep or by size-cap enforcement).
    /// </summary>
    public event Action<string, IConfidentialClientApplication, string>? OnEviction;

    public TimestampSweepAgentTokenService(
        AgentTokenServiceOptions options,
        Func<string, Task<IConfidentialClientApplication>>? ccaFactory = null,
        bool enableBackgroundSweep = true)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ccaFactory = ccaFactory ?? (agentAppId => Task.FromResult(MockCcaFactory.CreateMockCca(agentAppId, _options.Authority)));
        _maxIdleMs = (long)_options.SlidingExpiration.TotalMilliseconds;

        if (enableBackgroundSweep && _options.SweepInterval > TimeSpan.Zero)
        {
            _sweepTimer = new Timer(
                _ => Sweep(),
                null,
                _options.SweepInterval,
                _options.SweepInterval);
        }
    }

    public int CachedCcaCount => _agentCcas.Count;

    public bool ContainsAgent(string agentAppId) => _agentCcas.ContainsKey(agentAppId);

    public async Task<IConfidentialClientApplication> GetOrCreateAgentCcaAsync(string agentAppId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentAppId);

        // Fast path: check existing + touch timestamp
        if (_agentCcas.TryGetValue(agentAppId, out var existing))
        {
            existing.Touch();
            return existing.Cca;
        }

        // Slow path: acquire per-key semaphore
        var semaphore = _semaphores.GetOrAdd(agentAppId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_agentCcas.TryGetValue(agentAppId, out existing))
            {
                existing.Touch();
                return existing.Cca;
            }

            // Enforce size cap before adding
            if (_options.MaxAgentCcas > 0 && _agentCcas.Count >= _options.MaxAgentCcas)
            {
                EvictOldest();
            }

            var newApp = await _ccaFactory(agentAppId).ConfigureAwait(false);
            _agentCcas[agentAppId] = new TimestampedCca(newApp);
            return newApp;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Runs the sweep: removes all entries whose idle time exceeds the sliding window.
    /// Called automatically by the background timer, but can also be triggered manually
    /// in tests.
    /// </summary>
    public int Sweep()
    {
        int evictedCount = 0;

        foreach (var kvp in _agentCcas)
        {
            if (kvp.Value.IsExpired(_maxIdleMs))
            {
                if (_agentCcas.TryRemove(kvp.Key, out var removed))
                {
                    _semaphores.TryRemove(kvp.Key, out _);
                    OnEviction?.Invoke(kvp.Key, removed.Cca, "expired");
                    evictedCount++;
                }
            }
        }

        return evictedCount;
    }

    /// <summary>
    /// Evicts the single oldest (least recently accessed) entry.
    /// Called when the size cap is hit during GetOrCreate.
    /// </summary>
    private void EvictOldest()
    {
        string? oldestKey = null;
        long oldestTicks = long.MaxValue;

        foreach (var kvp in _agentCcas)
        {
            if (kvp.Value.LastAccessedTicks < oldestTicks)
            {
                oldestTicks = kvp.Value.LastAccessedTicks;
                oldestKey = kvp.Key;
            }
        }

        if (oldestKey is not null && _agentCcas.TryRemove(oldestKey, out var removed))
        {
            _semaphores.TryRemove(oldestKey, out _);
            OnEviction?.Invoke(oldestKey, removed.Cca, "size_cap");
        }
    }

    public void Dispose()
    {
        _sweepTimer?.Dispose();
    }
}
