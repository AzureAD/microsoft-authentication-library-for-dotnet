using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;

// Create ManagedIdentityApplication for System-Assigned identity
IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

// Acquire token
var resource = "resource-uri";
var result = await mi.AcquireTokenForManagedIdentity(resource)
    .ExecuteAsync()
    .ConfigureAwait(false);

// Use the token
var token = result.AccessToken;
