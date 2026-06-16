// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;
using AgentCcaManagement.TimestampSweep;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgentCcaManagement.Tests;

/// <summary>
/// Tests specific to the timestamp sweep strategy.
/// Verifies size-cap eviction (oldest first), manual/background sweep,
/// and touch-to-keep-alive behavior.
/// </summary>
[TestClass]
public class TimestampSweepTests
{
    [TestMethod]
    public async Task SizeCap_EvictsOldest_WhenFull()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 3,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        var evictions = new List<(string Key, string Reason)>();
        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: false);
        service.OnEviction += (key, _, reason) => evictions.Add((key, reason));

        // Act — fill to capacity with small delays for distinct timestamps
        await service.GetOrCreateAgentCcaAsync("agent-A");
        await Task.Delay(10);
        await service.GetOrCreateAgentCcaAsync("agent-B");
        await Task.Delay(10);
        await service.GetOrCreateAgentCcaAsync("agent-C");

        // Exceed capacity
        await service.GetOrCreateAgentCcaAsync("agent-D");

        // Assert — oldest (agent-A) should be evicted
        Assert.AreEqual(3, service.CachedCcaCount);
        Assert.IsFalse(service.ContainsAgent("agent-A"), "Oldest entry (agent-A) should have been evicted.");
        Assert.IsTrue(service.ContainsAgent("agent-D"), "New entry should be present.");
        Assert.AreEqual(1, evictions.Count);
        Assert.AreEqual("agent-A", evictions[0].Key);
        Assert.AreEqual("size_cap", evictions[0].Reason);
    }

    [TestMethod]
    public async Task SizeCap_TouchKeepsAlive_EvictsActualOldest()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 3,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: false);

        await service.GetOrCreateAgentCcaAsync("agent-A");
        await Task.Delay(10);
        await service.GetOrCreateAgentCcaAsync("agent-B");
        await Task.Delay(10);
        await service.GetOrCreateAgentCcaAsync("agent-C");

        // Touch agent-A to make it recent again
        await Task.Delay(10);
        await service.GetOrCreateAgentCcaAsync("agent-A");

        // Act — add one more; agent-B should be evicted (it's now oldest)
        await service.GetOrCreateAgentCcaAsync("agent-D");

        // Assert
        Assert.IsTrue(service.ContainsAgent("agent-A"), "agent-A was touched, should survive.");
        Assert.IsFalse(service.ContainsAgent("agent-B"), "agent-B is now oldest, should be evicted.");
        Assert.IsTrue(service.ContainsAgent("agent-C"));
        Assert.IsTrue(service.ContainsAgent("agent-D"));
    }

    [TestMethod]
    public async Task ManualSweep_RemovesExpiredEntries()
    {
        // Arrange — very short expiration
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 100,
            SlidingExpiration = TimeSpan.FromMilliseconds(50),
        };

        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: false);

        await service.GetOrCreateAgentCcaAsync("agent-1");
        await service.GetOrCreateAgentCcaAsync("agent-2");
        await service.GetOrCreateAgentCcaAsync("agent-3");

        // Wait for expiration
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Touch agent-3 to keep it alive
        await service.GetOrCreateAgentCcaAsync("agent-3");

        // Act
        int evictedCount = service.Sweep();

        // Assert
        Assert.AreEqual(2, evictedCount, "Two expired entries should be swept.");
        Assert.AreEqual(1, service.CachedCcaCount);
        Assert.IsTrue(service.ContainsAgent("agent-3"), "Touched entry should survive.");
        Assert.IsFalse(service.ContainsAgent("agent-1"));
        Assert.IsFalse(service.ContainsAgent("agent-2"));
    }

    [TestMethod]
    public async Task Sweep_ReturnsZero_WhenNothingExpired()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 100,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: false);
        await service.GetOrCreateAgentCcaAsync("agent-fresh");

        // Act
        int evictedCount = service.Sweep();

        // Assert
        Assert.AreEqual(0, evictedCount);
        Assert.AreEqual(1, service.CachedCcaCount);
    }

    [TestMethod]
    public async Task BackgroundSweep_EventuallyEvictsExpiredEntries()
    {
        // Arrange — enable background sweep with aggressive timing for test
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 100,
            SlidingExpiration = TimeSpan.FromMilliseconds(100),
            SweepInterval = TimeSpan.FromMilliseconds(50),
        };

        var evictions = new List<string>();
        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: true);
        service.OnEviction += (key, _, _) => evictions.Add(key);

        await service.GetOrCreateAgentCcaAsync("agent-auto-evict");

        // Act — wait for expiration + at least one sweep cycle
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        // Assert
        Assert.IsTrue(evictions.Contains("agent-auto-evict"),
            "Background sweep should have evicted the expired entry.");
        Assert.AreEqual(0, service.CachedCcaCount);
    }

    [TestMethod]
    public async Task Concurrency_SizeCap_NeverExceedsLimit()
    {
        // Arrange
        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 5,
            SlidingExpiration = TimeSpan.FromHours(1),
        };

        using var service = new TimestampSweepAgentTokenService(options, enableBackgroundSweep: false);

        // Act — many concurrent agents
        var tasks = Enumerable.Range(0, 20)
            .Select(i => service.GetOrCreateAgentCcaAsync($"agent-{i:D3}"))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert — should never exceed capacity
        Assert.IsTrue(service.CachedCcaCount <= options.MaxAgentCcas,
            $"Expected at most {options.MaxAgentCcas} CCAs, got {service.CachedCcaCount}");
    }
}
