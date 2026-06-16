// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;

namespace AgentCcaManagement.TimestampSweep;

/// <summary>
/// Demonstrates the timestamp sweep strategy with a simulated workload.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Timestamp Sweep CCA Management Strategy ===");
        Console.WriteLine();

        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 5,
            SlidingExpiration = TimeSpan.FromSeconds(2),  // Short for demo
            SweepInterval = TimeSpan.FromSeconds(1),      // Frequent sweep for demo
            Authority = "https://login.microsoftonline.com/contoso.onmicrosoft.com"
        };

        using var service = new TimestampSweepAgentTokenService(options);

        // Track evictions
        service.OnEviction += (agentId, cca, reason) =>
        {
            Console.WriteLine($"  [EVICTED] {agentId} (reason: {reason})");
        };

        // Fill to capacity
        Console.WriteLine($"Creating CCAs up to capacity ({options.MaxAgentCcas})...");
        for (int i = 1; i <= options.MaxAgentCcas; i++)
        {
            await service.GetOrCreateAgentCcaAsync($"agent-{i:D3}");
        }
        Console.WriteLine($"  Cached: {service.CachedCcaCount}");
        Console.WriteLine();

        // Demonstrate size-cap eviction (oldest gets evicted synchronously)
        Console.WriteLine("Adding agent beyond capacity (triggers oldest eviction)...");
        await service.GetOrCreateAgentCcaAsync("agent-overflow");
        Console.WriteLine($"  Cached after overflow: {service.CachedCcaCount}");
        Console.WriteLine($"  agent-001 still cached: {service.ContainsAgent("agent-001")}");
        Console.WriteLine();

        // Demonstrate time-based sweep
        Console.WriteLine($"Waiting for sweep cycle ({options.SweepInterval.TotalSeconds}s interval, {options.SlidingExpiration.TotalSeconds}s expiry)...");
        Console.WriteLine("  (Touching agent-003 to keep it alive)");

        await Task.Delay(TimeSpan.FromSeconds(1.5));
        await service.GetOrCreateAgentCcaAsync("agent-003");  // Touch to reset

        // Wait for remaining entries to expire + sweep to run
        await Task.Delay(options.SlidingExpiration + options.SweepInterval);

        Console.WriteLine();
        Console.WriteLine($"After sweep:");
        Console.WriteLine($"  agent-003 still cached: {service.ContainsAgent("agent-003")}");
        Console.WriteLine($"  Total remaining: {service.CachedCcaCount}");
        Console.WriteLine();

        // Manual sweep (for test scenarios)
        Console.WriteLine("Manual sweep result: " + service.Sweep() + " entries evicted");
    }
}
