using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

// Federated Identity Credentials (FIC) using User-Assigned Managed Identity
// This pattern uses a managed identity to provide a federated assertion

// Get MI assertion using user-assigned managed identity (client ID)
var miClientId = "YOUR_MI_CLIENT_ID";
var appClientId = "YOUR_APP_CLIENT_ID";
var tenantId = "YOUR_TENANT_ID";

var miAssertionProvider = async (AssertionRequestOptions _) =>
{
    // Get token from managed identity with FIC audience
    var miApplication = ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.WithUserAssignedClientId(miClientId))
        .Build();

    var miResult = await miApplication
        .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
        .ExecuteAsync()
        .ConfigureAwait(false);

    return miResult.AccessToken;
};

// Build confidential client application using FIC
var app = ConfidentialClientApplicationBuilder
    .Create(appClientId)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}", false)
    .WithClientAssertion(miAssertionProvider)
    .Build();

// Acquire token for downstream API
var resource = "resource-uri";
var result = await app.AcquireTokenForClient(new[] { resource }).ExecuteAsync();

// Use the token
var token = result.AccessToken;

// Note: For system-assigned managed identity, use:
// ManagedIdentityId.SystemAssigned instead of WithUserAssignedClientId()
// See federated-identity-credentials.md for more details.
