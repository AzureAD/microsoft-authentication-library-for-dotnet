// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace AgentCcaManagement.Shared;

/// <summary>
/// Factory for creating mock CCA instances in tests and prototypes.
/// Each CCA is a real MSAL ConfidentialClientApplication with a static
/// assertion callback (no real credential needed), allowing cache behavior
/// testing without hitting Entra ID.
/// </summary>
public static class MockCcaFactory
{
    private const string DefaultAuthority = "https://login.microsoftonline.com/contoso.onmicrosoft.com";

    /// <summary>
    /// Creates a lightweight CCA instance with a static assertion callback.
    /// The CCA is fully functional for cache operations but will fail on
    /// actual network calls (unless mock HTTP is wired up).
    /// </summary>
    public static IConfidentialClientApplication CreateMockCca(
        string clientId,
        string? authority = null)
    {
        return ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientAssertion((AssertionRequestOptions _) =>
                Task.FromResult("mock-assertion-for-" + clientId))
            .WithAuthority(authority ?? DefaultAuthority)
            .WithExperimentalFeatures()
            .Build();
    }
}
