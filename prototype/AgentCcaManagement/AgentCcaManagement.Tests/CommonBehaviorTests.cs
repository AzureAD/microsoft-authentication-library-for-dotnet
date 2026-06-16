// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;
using AgentCcaManagement.Unbounded;
using AgentCcaManagement.SlidingExpiration;
using AgentCcaManagement.TimestampSweep;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgentCcaManagement.Tests;

/// <summary>
/// Tests that apply to all three strategies: basic cache hit/miss behavior,
/// instance identity, and concurrency correctness.
/// </summary>
[TestClass]
public class CommonBehaviorTests
{
    private static readonly AgentTokenServiceOptions s_options = new()
    {
        MaxAgentCcas = 10,
        SlidingExpiration = TimeSpan.FromHours(1),
        Authority = "https://login.microsoftonline.com/test-tenant"
    };

    private static IEnumerable<object[]> AllStrategies()
    {
        yield return new object[] { new UnboundedAgentTokenService(s_options), "Unbounded" };
        yield return new object[] { new SlidingExpirationAgentTokenService(s_options), "SlidingExpiration" };
        yield return new object[] { new TimestampSweepAgentTokenService(s_options, enableBackgroundSweep: false), "TimestampSweep" };
    }

    [TestMethod]
    [DynamicData(nameof(AllStrategies), DynamicDataSourceType.Method)]
    public async Task GetOrCreate_ReturnsSameInstance_OnSecondCall(IAgentTokenService service, string _)
    {
        // Arrange
        using var svc = service;
        string agentId = "agent-001";

        // Act
        var first = await svc.GetOrCreateAgentCcaAsync(agentId);
        var second = await svc.GetOrCreateAgentCcaAsync(agentId);

        // Assert
        Assert.AreSame(first, second, "Same agent ID should return the same CCA instance (cache hit).");
    }

    [TestMethod]
    [DynamicData(nameof(AllStrategies), DynamicDataSourceType.Method)]
    public async Task GetOrCreate_ReturnsDifferentInstances_ForDifferentAgents(IAgentTokenService service, string _)
    {
        // Arrange
        using var svc = service;

        // Act
        var cca1 = await svc.GetOrCreateAgentCcaAsync("agent-A");
        var cca2 = await svc.GetOrCreateAgentCcaAsync("agent-B");

        // Assert
        Assert.AreNotSame(cca1, cca2);
        Assert.AreEqual("agent-A", cca1.AppConfig.ClientId);
        Assert.AreEqual("agent-B", cca2.AppConfig.ClientId);
    }

    [TestMethod]
    [DynamicData(nameof(AllStrategies), DynamicDataSourceType.Method)]
    public async Task GetOrCreate_CorrectlyReportsCount(IAgentTokenService service, string _)
    {
        // Arrange
        using var svc = service;

        // Act
        await svc.GetOrCreateAgentCcaAsync("agent-1");
        await svc.GetOrCreateAgentCcaAsync("agent-2");
        await svc.GetOrCreateAgentCcaAsync("agent-3");
        await svc.GetOrCreateAgentCcaAsync("agent-1"); // duplicate

        // Assert
        Assert.AreEqual(3, svc.CachedCcaCount);
    }

    [TestMethod]
    [DynamicData(nameof(AllStrategies), DynamicDataSourceType.Method)]
    public async Task GetOrCreate_ConcurrentCalls_SameAgent_ReturnsSameInstance(IAgentTokenService service, string _)
    {
        // Arrange
        using var svc = service;
        string agentId = "concurrent-agent";
        int concurrency = 20;

        // Act — launch many concurrent requests for the same agent
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => svc.GetOrCreateAgentCcaAsync(agentId))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert — all should be the same instance (semaphore prevents stampede)
        var firstInstance = results[0];
        Assert.IsTrue(results.All(r => ReferenceEquals(r, firstInstance)),
            "All concurrent callers for the same agent should get the same CCA instance.");
        Assert.AreEqual(1, svc.CachedCcaCount);
    }

    [TestMethod]
    [DynamicData(nameof(AllStrategies), DynamicDataSourceType.Method)]
    public async Task GetOrCreate_ThrowsOnNullOrEmpty(IAgentTokenService service, string _)
    {
        // Arrange
        using var svc = service;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => svc.GetOrCreateAgentCcaAsync(string.Empty));
    }
}
