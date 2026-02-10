using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;

// Create ManagedIdentityApplication for User-Assigned identity
IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId))
    .Build();

// Acquire token
var resource = "resource-uri";
var result = await mi.AcquireTokenForManagedIdentity(resource)
    .ExecuteAsync()
    .ConfigureAwait(false);

// Use the token
var token = result.AccessToken;
