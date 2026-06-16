// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AgentCcaManagement.Shared;

/// <summary>
/// Configuration for bounded agent CCA management strategies.
/// </summary>
public class AgentTokenServiceOptions
{
    /// <summary>
    /// Maximum number of agent CCA instances to keep in memory.
    /// When the limit is reached, the eviction strategy removes entries.
    /// Default: 100. Set to 0 for unbounded (not recommended for production).
    /// </summary>
    public int MaxAgentCcas { get; set; } = 100;

    /// <summary>
    /// How long an agent CCA can remain idle (no access) before being
    /// eligible for eviction. Default: 8 hours. Aligns with typical
    /// access token lifetimes — a CCA idle for this long likely has
    /// mostly-expired tokens, making eviction nearly free.
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// For the TimestampSweep strategy: how often the background sweep runs.
    /// Default: 30 minutes.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Authority URL for creating agent CCAs.
    /// </summary>
    public string Authority { get; set; } = "https://login.microsoftonline.com/common";
}
