// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace AgentCcaManagement.Shared;

/// <summary>
/// Abstraction for the agent CCA management layer. Each implementation
/// uses a different strategy for bounding the internal CCA dictionary.
/// </summary>
public interface IAgentTokenService : IDisposable
{
    /// <summary>
    /// Gets or creates a CCA instance for the given agent app ID.
    /// Implementations differ in how they bound the internal collection.
    /// </summary>
    Task<IConfidentialClientApplication> GetOrCreateAgentCcaAsync(string agentAppId);

    /// <summary>
    /// Returns the current number of cached CCA instances.
    /// Exposed for testing and diagnostics.
    /// </summary>
    int CachedCcaCount { get; }

    /// <summary>
    /// Returns true if the given agent app ID currently has a cached CCA instance.
    /// </summary>
    bool ContainsAgent(string agentAppId);
}
