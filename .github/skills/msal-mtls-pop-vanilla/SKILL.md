---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: |
  Vanilla mTLS PoP flow for direct token acquisition to protected resources (Azure Key Vault, Microsoft Graph, custom APIs).
  No delegation or token exchange - single-step acquisition with certificate binding.
applies_to:
  - .NET
  - C#
  - MSAL.NET
  - OAuth 2.0
  - mTLS
tags:
  - authentication
  - mtls
  - proof-of-possession
  - client-credentials
  - azure-key-vault
---

# MSAL.NET Vanilla mTLS PoP Flow

This skill guides you through **direct mTLS Proof-of-Possession token acquisition** for calling protected resources like Azure Key Vault, Microsoft Graph, or custom APIs.

## When to Use This Flow

- You need to call a protected resource directly (no cross-tenant delegation)
- Your app has a certificate for SNI or runs on Azure with MSI
- You want mTLS-bound tokens for enhanced security

**Not for**: Token exchange scenarios (use `msal-mtls-pop-fic-two-leg` instead).

## Prerequisites

- Certificate with private key (PFX/P12 or from cert store)
- App registration with certificate credential configured
- Target resource supports mTLS PoP tokens (e.g., Azure Key Vault with PoP-enabled scopes)

## Step-by-Step Implementation

### 1. Load Your Certificate

```csharp
using System.Security.Cryptography.X509Certificates;

// From file
X509Certificate2 cert = new X509Certificate2("path/to/cert.pfx", "password");

// From cert store (Windows)
X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint("THUMBPRINT_HERE");
```

### 2. Build the Confidential Client

```csharp
using Microsoft.Identity.Client;

IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create("YOUR_CLIENT_ID")
    .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
    .WithCertificate(cert, sendX5C: true)  // sendX5C=true enables PoP
    .Build();
```

**Key point**: `sendX5C: true` is required for mTLS PoP binding.

### 3. Acquire mTLS PoP Token

```csharp
string[] scopes = new[] { "https://vault.azure.net/.default" };

AuthenticationResult result = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()  // Request-level PoP enablement
    .ExecuteAsync()
    .ConfigureAwait(false);

// Verify token type
Console.WriteLine($"Token Type: {result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Binding Cert Thumbprint: {result.BindingCertificate?.Thumbprint}");
```

### 4. Call the Protected Resource

Use the PoP token with mTLS-enabled HTTP client:

```csharp
using System.Net.Http;

HttpClientHandler handler = new HttpClientHandler();
handler.ClientCertificates.Add(cert);  // Same cert for mTLS transport

using HttpClient client = new HttpClient(handler);
client.DefaultRequestHeaders.Add("Authorization", $"pop {result.AccessToken}");

HttpResponseMessage response = await client
    .GetAsync("https://your-resource.vault.azure.net/secrets/my-secret?api-version=7.4")
    .ConfigureAwait(false);

if (response.IsSuccessStatusCode)
{
    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    Console.WriteLine($"Secret value: {content}");
}
```

**Important**: Authorization header uses lowercase `"pop"`, not `"PoP"` or `"Bearer"`.

## Production Helper Classes

Copy these classes into your project for production-ready implementations.

### MtlsPopTokenAcquirer.cs
Simplified wrapper for token acquisition:

```csharp
// See: .github/skills/msal-mtls-pop-vanilla/MtlsPopTokenAcquirer.cs
MtlsPopTokenAcquirer acquirer = new MtlsPopTokenAcquirer(
    clientId: "YOUR_CLIENT_ID",
    tenantId: "YOUR_TENANT_ID",
    certificate: cert
);

AuthenticationResult result = await acquirer.AcquireTokenAsync(
    scopes: new[] { "https://vault.azure.net/.default" },
    cancellationToken: CancellationToken.None
);
```

### ResourceCaller.cs
Unified helper for calling resources with PoP tokens:

```csharp
// See: .github/skills/msal-mtls-pop-vanilla/ResourceCaller.cs
ResourceCaller caller = new ResourceCaller(cert);

string responseBody = await caller.CallResourceAsync(
    resourceUrl: "https://your-resource.vault.azure.net/secrets/my-secret?api-version=7.4",
    popToken: result.AccessToken,
    cancellationToken: CancellationToken.None
);
```

## Complete Example

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

public class VanillaPopExample
{
    public static async Task Main()
    {
        // 1. Load certificate
        X509Certificate2 cert = new X509Certificate2("cert.pfx", "password");

        // 2. Build app
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
            .Create("YOUR_CLIENT_ID")
            .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
            .WithCertificate(cert, sendX5C: true)
            .Build();

        // 3. Acquire token
        string[] scopes = new[] { "https://vault.azure.net/.default" };
        
        AuthenticationResult result = await app
            .AcquireTokenForClient(scopes)
            .WithMtlsProofOfPossession()
            .ExecuteAsync()
            .ConfigureAwait(false);

        Console.WriteLine($"Token acquired: {result.TokenType}");
        Console.WriteLine($"Expires: {result.ExpiresOn}");

        // 4. Call resource
        ResourceCaller caller = new ResourceCaller(cert);
        string response = await caller.CallResourceAsync(
            "https://your-keyvault.vault.azure.net/secrets/my-secret?api-version=7.4",
            result.AccessToken,
            CancellationToken.None
        );

        Console.WriteLine($"Secret retrieved: {response}");
    }
}
```

## Token Caching

MSAL automatically caches PoP tokens. Subsequent calls retrieve from cache:

```csharp
// First call - hits network
AuthenticationResult result1 = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

Console.WriteLine($"Source: {result1.AuthenticationResultMetadata.TokenSource}");  // "IdentityProvider"

// Second call - from cache
AuthenticationResult result2 = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

Console.WriteLine($"Source: {result2.AuthenticationResultMetadata.TokenSource}");  // "Cache"
```

## Regional Endpoints (Optional)

For lower latency and resilience, use regional mTLS endpoints:

```csharp
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create("YOUR_CLIENT_ID")
    .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
    .WithAzureRegion("westus3")  // Auto-discovers or specify region
    .WithCertificate(cert, sendX5C: true)
    .Build();
```

MSAL routes requests to `https://<region>.mtlsauth.microsoft.com` automatically.

## Error Handling

```csharp
using Microsoft.Identity.Client;

try
{
    AuthenticationResult result = await app
        .AcquireTokenForClient(scopes)
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
}
catch (MsalServiceException ex)
{
    // Azure AD service errors (e.g., invalid client, unauthorized scopes)
    Console.WriteLine($"Service error: {ex.ErrorCode} - {ex.Message}");
}
catch (MsalClientException ex)
{
    // Client-side errors (e.g., certificate issues, network failures)
    Console.WriteLine($"Client error: {ex.ErrorCode} - {ex.Message}");
}
```

## Testing Your Implementation

Validate your code against the working test:

**Test reference**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`  
→ `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84)

Key assertions to verify:
- `authResult.TokenType == "mtls_pop"`
- `authResult.BindingCertificate.Thumbprint == cert.Thumbprint`
- Second acquisition uses cache (`TokenSource.Cache`)

## Troubleshooting

**Problem**: Token type is `"Bearer"` instead of `"mtls_pop"`  
**Solution**: Ensure `sendX5C: true` in `.WithCertificate()` and `.WithMtlsProofOfPossession()` is called

**Problem**: `BindingCertificate` is null  
**Solution**: Check that certificate has private key and is accessible

**Problem**: Resource returns 401 Unauthorized  
**Solution**: Verify Authorization header uses lowercase `"pop"` and certificate is sent in mTLS handshake

**Problem**: Certificate not found in store  
**Solution**: Use `certmgr.msc` (Windows) or `security find-certificate` (macOS) to verify installation

## Related Skills

- **msal-mtls-pop-guidance**: Core terminology and conventions
- **msal-mtls-pop-fic-two-leg**: Token exchange patterns for delegation scenarios

## Additional Resources

- [MSAL.NET Client Credentials Flow](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows)
- [Azure Key Vault with PoP Tokens](https://docs.microsoft.com/azure/key-vault/general/authentication)
- [RFC 8705: OAuth 2.0 Mutual-TLS](https://datatracker.ietf.org/doc/html/rfc8705)
