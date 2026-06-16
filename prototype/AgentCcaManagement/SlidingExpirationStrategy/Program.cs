// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AgentCcaManagement.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace AgentCcaManagement.SlidingExpiration;

/// <summary>
/// Demonstrates the MemoryCache-based sliding expiration strategy.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sliding Expiration CCA Management Strategy ===");
        Console.WriteLine();

        var options = new AgentTokenServiceOptions
        {
            MaxAgentCcas = 5,  // Artificially low for demonstration
            SlidingExpiration = TimeSpan.FromSeconds(3),  // Short for demo purposes
            Authority = "https://login.microsoftonline.com/contoso.onmicrosoft.com"
        };

        using var service = new SlidingExpirationAgentTokenService(options);

        // Track evictions
        var evicted = new List<string>();
        service.OnEviction += (agentId, cca, reason) =>
        {
            evicted.Add(agentId);
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

        // Exceed capacity — triggers eviction
        Console.WriteLine("Adding one more agent (exceeds capacity)...");
        await service.GetOrCreateAgentCcaAsync("agent-overflow");
        // MemoryCache eviction is not synchronous — it may happen lazily
        await Task.Delay(100);
        Console.WriteLine($"  Cached after overflow: {service.CachedCcaCount}");
        Console.WriteLine();

        // Demonstrate sliding expiration
        Console.WriteLine($"Waiting {options.SlidingExpiration.TotalSeconds}s for sliding expiration...");
        Console.WriteLine("  (Accessing agent-001 periodically to keep it alive)");

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await service.GetOrCreateAgentCcaAsync("agent-001");  // Touch to reset sliding window
        }

        // Wait for other entries to expire
        await Task.Delay(options.SlidingExpiration + TimeSpan.FromSeconds(1));

        // Force compaction by trying to access — MemoryCache is lazy about expiration
        await service.GetOrCreateAgentCcaAsync("trigger-compact");

        Console.WriteLine();
        Console.WriteLine($"After expiration window:");
        Console.WriteLine($"  agent-001 still cached: {service.ContainsAgent("agent-001")}");
        Console.WriteLine($"  agent-002 still cached: {service.ContainsAgent("agent-002")}");
        Console.WriteLine($"  Total cached: {service.CachedCcaCount}");
        Console.WriteLine($"  Total evicted: {evicted.Count}");
    }
}
