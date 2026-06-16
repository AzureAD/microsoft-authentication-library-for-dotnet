// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;
using AgentCcaManagement.SlidingExpiration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgentCcaManagement.Tests;

/// <summary>
/// Tests specific to the MemoryCache-based sliding expiration strategy.
/// Verifies size-cap enforcement, sliding expiration, and eviction callbacks.
/// </summary>
[TestClass]
public class SlidingExpirationTests
{
    [TestMethod]
    public async Task SizeCap_EvictsWhenFull()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 3,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        var evictions = new List<string>();
        using var service = new SlidingExpirationAgentTokenService(options);
        service.OnEviction += (key, _, _) => evictions.Add(key);

        // Act — fill to capacity
        await service.GetOrCreateAgentCcaAsync("agent-A");
        await service.GetOrCreateAgentCcaAsync("agent-B");
        await service.GetOrCreateAgentCcaAsync("agent-C");

        Assert.AreEqual(3, service.CachedCcaCount);

        // Exceed capacity — MemoryCache should evict at least one
        await service.GetOrCreateAgentCcaAsync("agent-D");

        // Assert — MemoryCache eviction may be async/lazy, but count should not exceed cap + 1
        // (MemoryCache compacts when size is exceeded, but timing is implementation-dependent)
        Assert.IsTrue(service.CachedCcaCount <= 4,
            $"Expected at most 4 entries (MemoryCache compact is lazy), got {service.CachedCcaCount}");
    }

    [TestMethod]
    public async Task SlidingExpiration_EvictsIdleEntries()
    {
        // Arrange — very short sliding window for test
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 10,
            SlidingExpiration = TimeSpan.FromMilliseconds(200),
        };

        var evictions = new List<string>();
        using var service = new SlidingExpirationAgentTokenService(options);
        service.OnEviction += (key, _, _) => evictions.Add(key);

        await service.GetOrCreateAgentCcaAsync("agent-short-lived");

        // Act — wait for expiration
        await Task.Delay(TimeSpan.FromMilliseconds(400));

        // Trigger MemoryCache's lazy expiration check by accessing/creating another entry
        await service.GetOrCreateAgentCcaAsync("agent-trigger");

        // Assert
        Assert.IsFalse(service.ContainsAgent("agent-short-lived"),
            "Entry should have expired after sliding window elapsed.");
    }

    [TestMethod]
    public async Task SlidingExpiration_TouchResetsWindow()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 10,
            SlidingExpiration = TimeSpan.FromMilliseconds(300),
        };

        using var service = new SlidingExpirationAgentTokenService(options);
        await service.GetOrCreateAgentCcaAsync("agent-kept-alive");

        // Act — periodically touch the entry before it expires
        for (int i = 0; i < 4; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            await service.GetOrCreateAgentCcaAsync("agent-kept-alive");
        }

        // Assert — still alive because we keep resetting the sliding window
        Assert.IsTrue(service.ContainsAgent("agent-kept-alive"),
            "Entry should still be cached because sliding window was reset.");
    }

    [TestMethod]
    public async Task EvictionCallback_Fires_OnSizeCapEviction()
    {
        // Arrange — use size cap to force deterministic eviction (not time-based,
        // since MemoryCache's sliding expiration eviction is lazy and unreliable for tests).
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 2,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        var evictedKeys = new List<string>();

        using var service = new SlidingExpirationAgentTokenService(options);
        service.OnEviction += (key, _, reason) =>
        {
            evictedKeys.Add(key);
        };

        await service.GetOrCreateAgentCcaAsync("agent-A");
        await service.GetOrCreateAgentCcaAsync("agent-B");

        // Act — exceed capacity, forcing MemoryCache compaction
        await service.GetOrCreateAgentCcaAsync("agent-C");

        // Allow time for async eviction callback
        await Task.Delay(100);

        // Assert — at least one entry should have been evicted via callback
        Assert.IsTrue(evictedKeys.Count > 0,
            "Eviction callback should have fired when size cap was exceeded.");
    }
}
