---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: Direct mTLS Proof-of-Possession token acquisition for target resources using MSI or Confidential Client
applies_to:
  - MSAL.NET
  - Microsoft.Identity.Client
tags:
  - mTLS
  - PoP
  - Proof-of-Possession
  - Managed-Identity
  - Confidential-Client
  - Direct-Acquisition
---

# MSAL.NET Vanilla mTLS PoP Token Acquisition

This skill guides developers through direct (single-step) mTLS Proof-of-Possession token acquisition for target resources using Managed Identity (MSI) or Confidential Client authentication.

## What is the Vanilla Flow?

The **vanilla flow** is a direct token acquisition pattern where you acquire an mTLS PoP token directly for your target resource in a single call. This is NOT a multi-step exchange—there are NO "legs" in this flow.

**Use vanilla flow when:**
- You need direct access to a resource (Graph, Key Vault, custom API)
- You don't need token exchange or assertion-based flows
- You want the simplest mTLS PoP implementation

## Authentication Methods Supported

### Managed Identity (MSI)
- **System-Assigned (SAMI)**: Default identity on Azure VM/App Service/etc.
- **User-Assigned (UAMI)**: Explicitly configured identity
  - By Client ID: `6325cd32-9911-41f3-819c-416cdf9104e7`
  - By Resource ID: `/subscriptions/.../providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami`
  - By Object ID: `ecb2ad92-3e30-4505-b79f-ac640d069f24`

### Confidential Client (Certificate-Based SNI)
- Uses X509 certificate with `sendX5C: true`
- Requires real Azure region (e.g., "westus3")
- Certificate must have accessible private key

## Quick Start Examples

### MSI - System-Assigned (SAMI)

```csharp
using Microsoft.Identity.Client;

// Build Managed Identity application for system-assigned identity
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

// Acquire mTLS PoP token for target resource
AuthenticationResult result = await app
    .AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Use the token
Console.WriteLine($"Token Type: {result.TokenType}");  // "pop"
Console.WriteLine($"Access Token: {result.AccessToken}");
Console.WriteLine($"Binding Certificate: {result.BindingCertificate.Thumbprint}");
```

### MSI - User-Assigned by Client ID

```csharp
// Build Managed Identity application with user-assigned identity (by Client ID)
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7"))
    .Build();

// Acquire token
AuthenticationResult result = await app
    .AcquireTokenForManagedIdentity("https://vault.azure.net")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

### MSI - User-Assigned by Resource ID

```csharp
// Build with full resource ID
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedResourceId(
        "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami"))
    .Build();

AuthenticationResult result = await app
    .AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

### MSI - User-Assigned by Object ID

```csharp
// Build with object ID
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedObjectId("ecb2ad92-3e30-4505-b79f-ac640d069f24"))
    .Build();

AuthenticationResult result = await app
    .AcquireTokenForManagedIdentity("https://vault.azure.net")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

### Confidential Client - Certificate-Based SNI

```csharp
using System.Security.Cryptography.X509Certificates;

// Load certificate (from store, file, etc.)
X509Certificate2 cert = /* your certificate loading logic */;

// Build Confidential Client with SNI
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")  // Client ID
    .WithAuthority("https://login.microsoftonline.com/[tenant-id]")
    .WithAzureRegion("westus3")  // Real region required for SNI
    .WithCertificate(cert, sendX5C: true)  // SNI requires sendX5C: true
    .Build();

// Acquire mTLS PoP token
AuthenticationResult result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Console.WriteLine($"Token Type: {result.TokenType}");  // "pop"
Console.WriteLine($"Binding Certificate: {result.BindingCertificate.Thumbprint}");
```

## Calling Resources with mTLS PoP Tokens

Once you have an mTLS PoP token, you must:
1. Use `result.TokenType` + `result.AccessToken` in the Authorization header
2. Use `result.BindingCertificate` for the TLS client certificate

```csharp
// Configure HttpClient with mTLS binding certificate
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);

// Set Authorization header with PoP token
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
    result.TokenType,      // "pop"
    result.AccessToken
);

// Make request to protected resource
HttpResponseMessage response = await httpClient.GetAsync("https://vault.azure.net/secrets/my-secret?api-version=7.4");
string content = await response.Content.ReadAsStringAsync();
```

## Production Helper Classes

This skill includes production-ready helper classes:

### ResourceCaller.cs
Encapsulates the logic for calling protected resources with mTLS PoP tokens. Handles:
- Authorization header construction
- TLS client certificate binding
- HTTP client lifecycle

**Usage:**
```csharp
using var caller = new ResourceCaller(authResult);
string response = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/me");
```

### MtlsPopTokenAcquirer.cs
Abstracts token acquisition for both MSI and Confidential Client scenarios. Provides:
- Unified interface for token acquisition
- Support for MSI (SAMI/UAMI) and Confidential Client
- Automatic mTLS PoP configuration

**Usage (MSI):**
```csharp
using var acquirer = MtlsPopTokenAcquirer.CreateForManagedIdentity(
    ManagedIdentityId.SystemAssigned);
    
AuthenticationResult result = await acquirer.AcquireTokenAsync("https://graph.microsoft.com");
```

**Usage (Confidential Client):**
```csharp
using var acquirer = MtlsPopTokenAcquirer.CreateForConfidentialClient(
    clientId: "your-client-id",
    authority: "https://login.microsoftonline.com/tenant-id",
    certificate: cert,
    region: "westus3");
    
AuthenticationResult result = await acquirer.AcquireTokenAsync(new[] { "https://vault.azure.net/.default" });
```

### VanillaMsiMtlsPop.cs
Concrete implementation showcasing all MSI variants with mTLS PoP. Demonstrates:
- System-assigned identity usage
- User-assigned identity by Client ID, Resource ID, and Object ID
- Resource calling with mTLS binding

**Usage:**
```csharp
// System-assigned
using var msi = new VanillaMsiMtlsPop(ManagedIdentityId.SystemAssigned);
string response = await msi.CallResourceWithPopAsync("https://graph.microsoft.com/v1.0/me");

// User-assigned by Client ID
using var msi = new VanillaMsiMtlsPop(
    ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7"));
string response = await msi.CallResourceWithPopAsync("https://vault.azure.net/secrets/my-secret");
```

## Key Properties After Token Acquisition

After successful mTLS PoP token acquisition:
- `result.TokenType` = `"pop"` (indicates mTLS PoP token)
- `result.AccessToken` = Token string for Authorization header
- `result.BindingCertificate` = Certificate bound to this token (for TLS handshake)

## Common Patterns

### Caching
MSAL.NET automatically caches mTLS PoP tokens. Subsequent calls will retrieve from cache:

```csharp
// First call - acquires new token
var result1 = await app.AcquireTokenForManagedIdentity(resource)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
// result1.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider

// Second call - from cache
var result2 = await app.AcquireTokenForManagedIdentity(resource)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
// result2.AuthenticationResultMetadata.TokenSource == TokenSource.Cache
```

### Error Handling

```csharp
try
{
    AuthenticationResult result = await app
        .AcquireTokenForManagedIdentity(resource)
        .WithMtlsProofOfPossession()
        .ExecuteAsync();
}
catch (MsalServiceException ex) when (ex.ErrorCode == "invalid_resource")
{
    Console.WriteLine($"Invalid resource: {ex.Message}");
}
catch (MsalServiceException ex)
{
    Console.WriteLine($"Token acquisition failed: {ex.ErrorCode} - {ex.Message}");
}
```

## Testing Reference

See `ClientCredentialsMtlsPopTests.cs`, method `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84) for a working Confidential Client example with SNI.

MSI test examples with UAMI IDs are available in `ManagedIdentityImdsV2Tests.cs`.

## Best Practices

1. **Always use ConfigureAwait(false)** in async calls to avoid deadlocks
2. **Dispose HttpClient properly** or use dependency injection with HttpClientFactory
3. **Validate token type** after acquisition: `Assert.AreEqual("pop", result.TokenType)`
4. **Check BindingCertificate** is not null before using
5. **Use real Azure regions** for SNI (e.g., "westus3"), not "local"

## Related Skills

- **msal-mtls-pop-guidance**: Terminology and conventions
- **msal-mtls-pop-fic-two-leg**: Token exchange patterns
