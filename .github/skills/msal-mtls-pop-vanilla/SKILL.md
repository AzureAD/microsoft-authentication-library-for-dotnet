---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: Direct mTLS PoP token acquisition for target resources using MSI or Confidential Client
applies_to:
  - "**/*msal*.cs"
  - "**/*mtls*.cs"
  - "**/*pop*.cs"
  - "**/*managedidentity*.cs"
  - "**/*confidential*.cs"
tags:
  - msal
  - mtls
  - proof-of-possession
  - managed-identity
  - confidential-client
  - vanilla-flow
---

# MSAL.NET Vanilla mTLS PoP Flow

This skill covers **direct** mTLS Proof-of-Possession (PoP) token acquisition for target resources using either Managed Identity (MSI) or Confidential Client authentication.

## What Is Vanilla Flow?

The vanilla flow is the **simplest** mTLS PoP pattern:
1. Acquire an mTLS PoP token for a target resource (e.g., Graph, Key Vault, custom API)
2. Use the token + `BindingCertificate` to call the resource

**This is NOT a two-leg flow** - it's a single, direct token acquisition.

## When to Use Vanilla Flow

Use vanilla flow when:
- You need an mTLS PoP token for a resource you're directly authorized to access
- You're NOT doing token exchange or cross-tenant delegation
- Your app/MSI has direct permissions to the target resource
- You want the simplest possible mTLS PoP implementation

## Authentication Methods

### MSI (Managed Identity)

**System-Assigned:**
```csharp
var app = ManagedIdentityApplicationBuilder.Create()
    .WithTestLogging()  // Optional: for development
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://graph.microsoft.com/.default")
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);
```

**User-Assigned (by Client ID):**
```csharp
var app = ManagedIdentityApplicationBuilder.Create()
    .WithUserAssignedManagedIdentity(userAssignedClientId)
    .WithTestLogging()
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://vault.azure.net/.default")
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);
```

**User-Assigned (by Resource ID):**
```csharp
var app = ManagedIdentityApplicationBuilder.Create()
    .WithUserAssignedManagedIdentity(
        resourceId: "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{name}")
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);
```

### Confidential Client (Certificate-Based)

```csharp
X509Certificate2 cert = /* load your SNI certificate */;

var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .WithCertificate(cert, sendX5c: true)  // sendX5c=true enables SNI
    .WithAzureRegion("westus3")  // Optional: regional endpoint
    .WithTestLogging()  // Optional: for development
    .Build();

var result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);
```

## Calling the Resource

After acquiring the token, use `BindingCertificate` for the TLS handshake:

```csharp
// Create HttpClient with certificate binding
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

using var httpClient = new HttpClient(handler);

// Add Bearer token (despite mTLS, header still uses "Bearer")
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", result.AccessToken);

// Call the resource
var response = await httpClient.GetAsync("https://vault.azure.net/secrets/my-secret")
    .ConfigureAwait(false);

if (response.IsSuccessStatusCode)
{
    string content = await response.Content.ReadAsStringAsync()
        .ConfigureAwait(false);
    Console.WriteLine($"Success: {content}");
}
```

## Validation Checklist

When implementing vanilla flow:
- ✅ Token type is `Constants.MtlsPoPTokenType` (not "Bearer")
- ✅ `BindingCertificate` is not null
- ✅ `BindingCertificate` is used in `HttpClientHandler.ClientCertificates`
- ✅ Authorization header uses the access token
- ✅ Token can be retrieved from cache on second call (with `.WithMtlsProofOfPossession()`)

## Helper Classes

This skill includes production-ready helper classes you can copy into your project:

### ResourceCaller.cs
Handles calling resources with mTLS PoP tokens. Manages `HttpClient` lifecycle and certificate binding.

**Usage:**
```csharp
using var caller = new ResourceCaller();
var response = await caller.CallResourceAsync(
    authResult,
    resourceUrl,
    cancellationToken);
```

### MtlsPopTokenAcquirer.cs
Wrapper for acquiring mTLS PoP tokens with either MSI or Confidential Client.

**Usage (MSI):**
```csharp
using var acquirer = new MtlsPopTokenAcquirer(
    ManagedIdentityApplicationBuilder.Create().Build());

var result = await acquirer.AcquireTokenAsync(
    "https://graph.microsoft.com/.default",
    cancellationToken);
```

**Usage (Confidential Client):**
```csharp
var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(cert, sendX5c: true)
    .Build();

using var acquirer = new MtlsPopTokenAcquirer(app);
var result = await acquirer.AcquireTokenAsync(
    new[] { "https://vault.azure.net/.default" },
    cancellationToken);
```

### VanillaMsiMtlsPop.cs
Complete example implementation showing MSI-based vanilla flow end-to-end.

**Usage:**
```csharp
using var example = new VanillaMsiMtlsPop(userAssignedClientId);
string content = await example.GetResourceDataAsync(
    "https://graph.microsoft.com/.default",
    "https://graph.microsoft.com/v1.0/me",
    cancellationToken);
```

## Complete Example (MSI)

```csharp
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public class VanillaMsiExample
{
    public async Task<string> CallGraphWithMtlsPopAsync(CancellationToken ct = default)
    {
        // 1. Build MSI app
        var app = ManagedIdentityApplicationBuilder.Create()
            .Build();

        // 2. Acquire mTLS PoP token for Graph
        var result = await app
            .AcquireTokenForManagedIdentity("https://graph.microsoft.com/.default")
            .WithMtlsProofOfPossession()
            .ExecuteAsync(ct)
            .ConfigureAwait(false);

        // 3. Validate token type and certificate
        if (result.TokenType != Constants.MtlsPoPTokenType)
            throw new InvalidOperationException("Expected mTLS PoP token");

        if (result.BindingCertificate == null)
            throw new InvalidOperationException("BindingCertificate is required");

        // 4. Call Graph with certificate binding
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(result.BindingCertificate);

        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", result.AccessToken);

        var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me")
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);
    }
}
```

## Complete Example (Confidential Client)

See `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`:
- Method: `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84)

Key points from the test:
```csharp
// Build app with SNI certificate
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion("westus3")
    .WithCertificate(cert, sendX5c: true)  // sendX5c=true critical for SNI
    .Build();

// Acquire mTLS PoP token
var result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Validate
Assert.AreEqual(Constants.MtlsPoPTokenType, result.TokenType);
Assert.IsNotNull(result.BindingCertificate);
Assert.AreEqual(cert.Thumbprint, result.BindingCertificate.Thumbprint);
```

## Caching Behavior

mTLS PoP tokens are cached separately from Bearer tokens:

```csharp
// First call: acquires from AAD
var result1 = await app.AcquireToken...
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

Assert.AreEqual(TokenSource.IdentityProvider, 
    result1.AuthenticationResultMetadata.TokenSource);

// Second call: retrieved from cache
var result2 = await app.AcquireToken...
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

Assert.AreEqual(TokenSource.Cache, 
    result2.AuthenticationResultMetadata.TokenSource);
```

## Regional Endpoints

mTLS PoP uses `mtlsauth.microsoft.com` instead of `login.microsoftonline.com`:

```csharp
// Enable regional endpoints (optional but recommended)
.WithAzureRegion("westus3")

// Token requests go to: https://{region}.mtlsauth.microsoft.com/...
```

## Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| `invalid_client` | Certificate not SNI-enabled | Ensure `.WithCertificate(cert, sendX5c: true)` |
| `unauthorized_client` | App lacks permissions | Grant app/MSI permissions to target resource |
| `BindingCertificate` is null | Not using `.WithMtlsProofOfPossession()` | Add modifier to request |
| TLS handshake fails | Wrong certificate used | Use `result.BindingCertificate`, not original cert |

## References

- Guidance Skill: `.github/skills/msal-mtls-pop-guidance/SKILL.md`
- Test Example: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 36-84)
- API Docs: `src/client/Microsoft.Identity.Client/AuthenticationResult.cs` (line 346)

## Version History

- **1.0.0**: Initial vanilla flow skill with MSI and Confidential Client coverage
