// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using AgentCcaManagement.Shared;
using Microsoft.Identity.Client;

namespace AgentCcaManagement.Unbounded;

/// <summary>
/// Baseline: unbounded ConcurrentDictionary of agent CCAs.
/// This is the current approach used in ID Web (PR #3842) and the
/// AgentTokenService example in the guidance doc — simple and correct,
/// but with no eviction, the dictionary grows without bound.
///
/// Use as a reference point for comparing bounded strategies.
/// </summary>
public sealed class UnboundedAgentTokenService : IAgentTokenService
{
    private readonly ConcurrentDictionary<string, IConfidentialClientApplication> _agentCcas = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly AgentTokenServiceOptions _options;
    private readonly Func<string, Task<IConfidentialClientApplication>> _ccaFactory;

    public UnboundedAgentTokenService(AgentTokenServiceOptions options, Func<string, Task<IConfidentialClientApplication>>? ccaFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ccaFactory = ccaFactory ?? (agentAppId => Task.FromResult(MockCcaFactory.CreateMockCca(agentAppId, _options.Authority)));
    }

    public int CachedCcaCount => _agentCcas.Count;

    public bool ContainsAgent(string agentAppId) => _agentCcas.ContainsKey(agentAppId);

    public async Task<IConfidentialClientApplication> GetOrCreateAgentCcaAsync(string agentAppId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentAppId);

        if (_agentCcas.TryGetValue(agentAppId, out var existing))
        {
            return existing;
        }

        var semaphore = _semaphores.GetOrAdd(agentAppId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_agentCcas.TryGetValue(agentAppId, out var app))
            {
                return app;
            }

            var newApp = await _ccaFactory(agentAppId).ConfigureAwait(false);
            _agentCcas[agentAppId] = newApp;
            return newApp;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        // No cleanup needed — CCAs don't implement IDisposable
    }
}
