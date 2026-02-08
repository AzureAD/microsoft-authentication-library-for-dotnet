---
skill_name: msal-mtls-pop-vanilla
version: 1.0
description: Direct mTLS PoP token acquisition for target resources using Managed Identity or Confidential Client
applies_to:
  - MSAL.NET/mTLS-PoP
  - MSAL.NET/Managed-Identity
  - MSAL.NET/Confidential-Client
tags:
  - msal
  - mtls
  - pop
  - proof-of-possession
  - managed-identity
  - confidential-client
  - vanilla-flow
---

# MSAL.NET mTLS PoP - Vanilla Flow (Direct Token Acquisition)

This skill covers direct mTLS Proof-of-Possession (PoP) token acquisition for target resources without intermediate token exchanges. Use this when you need to acquire an mTLS PoP token directly for a resource like Microsoft Graph, Azure Key Vault, or custom APIs.

## What is Vanilla Flow?

**Vanilla flow** is a single-step, direct token acquisition from Azure AD for a target resource:
- **One call**: `AcquireTokenForManagedIdentity()` or `AcquireTokenForClient()`
- **No intermediate steps**: Direct to target resource (e.g., `https://graph.microsoft.com`)
- **No "legs"**: This is NOT a multi-step process (do not confuse with FIC two-leg flow)

## Authentication Methods Supported

### 1. System-Assigned Managed Identity (SAMI)
Works in Azure environments only (VM, App Service, Functions, Container Instances, AKS).

### 2. User-Assigned Managed Identity (UAMI)
Can be specified using any of three ID types (all refer to the same identity):

### 3. Confidential Client with Certificate (SNI)
Works anywhere with certificate access (local dev, Azure, on-premises).

## Requirements

- **MSAL.NET**: 4.82.1 minimum
- **NuGet Packages**:
  ```bash
  dotnet add package Microsoft.Identity.Client --version 4.82.1
  dotnet add package Microsoft.Identity.Client.KeyAttestation
  ```
- **Target Framework**: net8.0 recommended
- **Namespaces**:
  ```csharp
  using Microsoft.Identity.Client;
  using Microsoft.Identity.Client.AppConfig;        // For ManagedIdentityId
  using Microsoft.Identity.Client.KeyAttestation;   // For WithAttestationSupport()
  ```

## Quick Start Examples

### SAMI (System-Assigned Managed Identity)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;

// Build SAMI app
var app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.SystemAssigned)
    .Build();

// Acquire mTLS PoP token with Credential Guard attestation
var result = await app
    .AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()  // ← Credential Guard support
    .ExecuteAsync();

Console.WriteLine($"Token Type: {result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Certificate Thumbprint: {result.BindingCertificate?.Thumbprint}");

// Configure HttpClient with the binding certificate for mTLS
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("mtls_pop", result.AccessToken);

// Call Microsoft Graph
var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/applications");
response.EnsureSuccessStatusCode();

string json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

### UAMI by Client ID

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;

// Build UAMI app with Client ID
var app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7"))
    .Build();

// Acquire mTLS PoP token
var result = await app
    .AcquireTokenForManagedIdentity("https://vault.azure.net")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

// Configure HttpClient with the binding certificate for mTLS
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("mtls_pop", result.AccessToken);

// Call Azure Key Vault
var response = await httpClient.GetAsync("https://your-vault.vault.azure.net/secrets/my-secret?api-version=7.4");
response.EnsureSuccessStatusCode();

string json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

### UAMI by Resource ID

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;

// Build UAMI app with Resource ID (ARM path)
var app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedResourceId(
        "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami"))
    .Build();

// Acquire mTLS PoP token
var result = await app
    .AcquireTokenForManagedIdentity("https://storage.azure.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

Console.WriteLine($"Token Type: {result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Certificate Thumbprint: {result.BindingCertificate?.Thumbprint}");

// Configure HttpClient with the binding certificate for mTLS
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("mtls_pop", result.AccessToken);

// Call Azure Storage
var response = await httpClient.GetAsync("https://your-storage-account.blob.core.windows.net/?comp=list");
response.EnsureSuccessStatusCode();

string json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

### UAMI by Object ID

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;

// Build UAMI app with Object ID (Principal ID)
var app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedObjectId("ecb2ad92-3e30-4505-b79f-ac640d069f24"))
    .Build();

// Acquire mTLS PoP token
var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

Console.WriteLine($"Token Type: {result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Certificate Thumbprint: {result.BindingCertificate?.Thumbprint}");

// Configure HttpClient with the binding certificate for mTLS
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("mtls_pop", result.AccessToken);

// Call Azure Resource Manager
var response = await httpClient.GetAsync("https://management.azure.com/subscriptions?api-version=2021-04-01");
response.EnsureSuccessStatusCode();

string json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

### Confidential Client with Certificate (SNI)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.KeyAttestation;

// Load certificate from Windows Certificate Store
var cert = GetCertificateFromStore("CN=MyAppCertificate");

// Build Confidential Client with SNI
var app = ConfidentialClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")  // Use actual region
    .WithCertificate(cert, sendX5c: true)  // SNI: send X.509 chain
    .Build();

// Acquire mTLS PoP token
var result = await app
    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Console.WriteLine($"Token Type: {result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Binding Certificate matches SNI cert: {result.BindingCertificate.Thumbprint == cert.Thumbprint}");

// Configure HttpClient with the binding certificate for mTLS
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("mtls_pop", result.AccessToken);

// Call Microsoft Graph
var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/applications");
response.EnsureSuccessStatusCode();

string json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

## Production Helper Classes

This skill includes three production-ready helper classes:

### 1. VanillaMsiMtlsPop.cs
Complete MSI implementation supporting SAMI and all 3 UAMI ID types with Credential Guard attestation.

### 2. MtlsPopTokenAcquirer.cs
Unified token acquisition for both MSI and Confidential Client with attestation support.

### 3. ResourceCaller.cs
Helper for calling protected resources with mTLS PoP tokens.

See the `.cs` files in this directory for complete implementations.

## Usage Pattern

```csharp
// 1. Acquire token with PoP
var result = await app
    .AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

// 2. Verify PoP token
if (result.TokenType != "mtls_pop")
{
    throw new InvalidOperationException("Expected mTLS PoP token");
}

if (result.BindingCertificate == null)
{
    throw new InvalidOperationException("BindingCertificate is required for mTLS calls");
}

// 3. Call resource with mTLS binding
using var caller = new ResourceCaller(result);
string response = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/applications");
```

## Key Points

1. **Vanilla flow is NOT multi-step**: Direct acquisition, no "legs"
2. **Always include `.WithAttestationSupport()`**: Required for Credential Guard in production
3. **SAMI only works in Azure**: Use UAMI or Confidential Client for local development
4. **All 3 UAMI ID types are equivalent**: Use whichever is most convenient
5. **Check `BindingCertificate` for null**: Required for making mTLS calls to target resource
6. **SNI requires `sendX5c: true`**: In `.WithCertificate(cert, sendX5c: true)`
7. **Use actual Azure region**: e.g., "westus3", not placeholders

## Troubleshooting

| Error/Issue | Solution |
|-------------|----------|
| `ManagedIdentityId` is not defined | Add `using Microsoft.Identity.Client.AppConfig;` |
| `WithMtlsProofOfPossession()` not found | Upgrade to MSAL.NET 4.82.1+ |
| `BindingCertificate` is null | Ensure `.WithMtlsProofOfPossession()` was called before `ExecuteAsync()` |
| `WithAttestationSupport()` not found | Add NuGet: `Microsoft.Identity.Client.KeyAttestation` |
| "Timeout calling IMDS endpoint" (local) | SAMI doesn't work locally. Use UAMI or Confidential Client |
| "Unable to get UAMI token" | Check: UAMI exists, assigned to resource, correct ID type used |
| Certificate not found in store | Verify certificate is in Current User or Local Machine store |

## Additional Resources

- [Shared Guidance Skill](../msal-mtls-pop-guidance/SKILL.md) - Terminology and conventions
- [FIC Two-Leg Flow Skill](../msal-mtls-pop-fic-two-leg/SKILL.md) - Token exchange scenarios
- [ClientCredentialsMtlsPopTests.cs](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs) - Integration test examples
