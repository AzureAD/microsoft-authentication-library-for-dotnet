// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;

namespace AgentCcaManagement.Unbounded;

/// <summary>
/// Demonstrates the unbounded strategy under a simulated workload.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Unbounded CCA Management Strategy (Baseline) ===");
        Console.WriteLine();

        var options = new AgentTokenServiceOptions
        {
            Authority = "https://login.microsoftonline.com/contoso.onmicrosoft.com"
        };

        using var service = new UnboundedAgentTokenService(options);

        // Simulate 20 agents being created over time
        var agentIds = Enumerable.Range(1, 20)
            .Select(i => $"agent-app-{i:D3}")
            .ToList();

        Console.WriteLine($"Simulating {agentIds.Count} agents...");
        Console.WriteLine();

        foreach (var agentId in agentIds)
        {
            var cca = await service.GetOrCreateAgentCcaAsync(agentId);
            Console.WriteLine($"  Created CCA for {agentId} (AppId: {cca.AppConfig.ClientId})");
        }

        Console.WriteLine();
        Console.WriteLine($"Total cached CCAs: {service.CachedCcaCount}");
        Console.WriteLine();

        // Demonstrate that repeated access returns the same instance
        var first = await service.GetOrCreateAgentCcaAsync("agent-app-001");
        var second = await service.GetOrCreateAgentCcaAsync("agent-app-001");
        Console.WriteLine($"Same instance on re-access: {ReferenceEquals(first, second)}");
        Console.WriteLine();

        // Show the problem: count never decreases
        Console.WriteLine("[!] Notice: CCA count only grows. No eviction occurs.");
        Console.WriteLine($"    After all operations: {service.CachedCcaCount} CCAs in memory.");
        Console.WriteLine("    In a long-running service with many agents, this is unbounded growth.");
    }
}
