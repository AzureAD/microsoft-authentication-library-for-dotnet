---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: Direct mTLS PoP token acquisition for target resources (Graph, Key Vault, custom APIs)
applies_to:
  - MSAL.NET
  - Microsoft.Identity.Client
  - mTLS
  - Proof-of-Possession
  - OAuth 2.0
tags:
  - authentication
  - mtls
  - pop
  - certificate-binding
  - direct-acquisition
  - vanilla-flow
---

# MSAL.NET mTLS PoP - Vanilla Flow

This skill covers **direct mTLS Proof-of-Possession (PoP) token acquisition** - the simplest pattern for getting PoP-bound tokens for target resources.

## Overview

The **vanilla flow** is a single-step token acquisition where:
1. You configure a confidential client app with a certificate
2. You call `AcquireTokenForClient()` with `.WithMtlsProofOfPossession()`
3. MSAL returns a PoP token bound to your certificate

**Key characteristic**: This is NOT a token exchange flow. No "legs" terminology applies.

## When to Use This Flow

Use vanilla flow when:
- ✅ You need direct service-to-service authentication
- ✅ Your app has a certificate for authentication
- ✅ You want the simplest mTLS PoP implementation
- ✅ You're calling Graph, Key Vault, or other Azure resources

Don't use vanilla flow when:
- ❌ You need token exchange or delegation (use FIC two-leg flow instead)
- ❌ You're implementing cross-tenant scenarios (use FIC two-leg flow instead)

## Basic Pattern

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

public class VanillaPopExample
{
    public async Task<string> GetPopTokenAsync()
    {
        // 1. Load certificate (MSI/SNI)
        X509Certificate2 cert = LoadCertificate();
        
        // 2. Build confidential client with certificate
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
            .Create("your-client-id")
            .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
            .WithCertificate(cert, sendX5C: true)  // sendX5C: true for SNI
            .WithAzureRegion("eastus")             // Required for mTLS
            .Build();
        
        // 3. Acquire PoP token
        AuthenticationResult result = await app
            .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
            .WithMtlsProofOfPossession()
            .ExecuteAsync()
            .ConfigureAwait(false);
        
        // 4. Use token and certificate together
        Console.WriteLine($"Token type: {result.TokenType}");  // "pop"
        Console.WriteLine($"Certificate: {result.BindingCertificate?.Thumbprint}");
        
        return result.AccessToken;
    }
    
    private X509Certificate2 LoadCertificate()
    {
        // MSI: Load from machine store
        using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                "your-cert-thumbprint",
                validOnly: false);
            return certs[0];
        }
        
        // OR SNI: Certificate provided by platform
        // (In SNI scenarios, cert comes from Windows SNI API)
    }
}
```

## Complete Working Example

See `ClientCredentialsMtlsPopTests.cs` for a full working test:
- **Test**: `Sni_Gets_Pop_Token_Successfully_TestAsync()` (lines 36-84)
- **Location**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`

```csharp
[TestMethod]
public async Task Sni_Gets_Pop_Token_Successfully_TestAsync()
{
    // Load certificate
    X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
    
    string[] appScopes = new[] { "https://vault.azure.net/.default" };
    
    // Build app with SNI certificate
    IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")
        .WithCertificate(cert, true)  // sendX5C: true for SNI
        .WithTestLogging()
        .Build();
    
    // Acquire PoP token
    AuthenticationResult authResult = await confidentialApp
        .AcquireTokenForClient(appScopes)
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
    
    // Verify PoP token
    Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType);
    Assert.IsNotNull(authResult.BindingCertificate);
    Assert.AreEqual(cert.Thumbprint, authResult.BindingCertificate.Thumbprint);
}
```

## Production Helper Classes

This skill includes production-ready helper classes you can copy into your project:

### 1. MtlsPopTokenAcquirer.cs
Simplified token acquisition wrapper for vanilla flow.

**Usage**:
```csharp
var acquirer = new MtlsPopTokenAcquirer(
    clientId: "your-client-id",
    tenantId: "your-tenant-id",
    certificate: certificate,
    region: "eastus"
);

var token = await acquirer.GetTokenAsync(
    new[] { "https://graph.microsoft.com/.default" },
    CancellationToken.None
);
```

See: [MtlsPopTokenAcquirer.cs](./MtlsPopTokenAcquirer.cs)

### 2. ResourceCaller.cs
Unified helper for calling resources with PoP tokens + mTLS binding.

**Usage**:
```csharp
var caller = new ResourceCaller(authResult);

// GET request with PoP token and certificate
var response = await caller.CallResourceAsync(
    "https://vault.azure.net/secrets/my-secret",
    HttpMethod.Get,
    CancellationToken.None
);
```

See: [ResourceCaller.cs](./ResourceCaller.cs)

## Key Points

### Required Configuration
```csharp
.WithCertificate(certificate, sendX5C: true)  // Send X5C chain for SNI
.WithAzureRegion("your-region")               // Regional mTLS endpoint
.WithMtlsProofOfPossession()                  // Enable PoP
```

### Token Properties
```csharp
authResult.TokenType           // "pop" (not "Bearer")
authResult.BindingCertificate  // The certificate used (SNI/MSI)
authResult.AccessToken         // PoP token (use with certificate)
```

### Caching
PoP tokens are cached like regular tokens:
```csharp
// First call: network request
var result1 = await app.AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Second call: cached (if same scopes + certificate)
var result2 = await app.AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
```

## Common Scenarios

### Calling Microsoft Graph
```csharp
var result = await app
    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Use result.AccessToken + result.BindingCertificate to call Graph
```

### Calling Azure Key Vault
```csharp
var result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Use result.AccessToken + result.BindingCertificate to call Key Vault
```

### Calling Custom APIs
```csharp
var result = await app
    .AcquireTokenForClient(new[] { "api://your-custom-api/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Use result.AccessToken + result.BindingCertificate to call your API
```

## Troubleshooting

### Problem: Token type is "Bearer" instead of "pop"
**Solution**: Make sure you called `.WithMtlsProofOfPossession()` on the request.

### Problem: BindingCertificate is null
**Solution**: 
- For SNI: Use `WithCertificate(cert, sendX5C: true)`
- For MSI: Ensure certificate is properly loaded

### Problem: mTLS handshake fails at resource
**Solution**:
- Verify you're using the same certificate that was bound to the token
- Check `authResult.BindingCertificate` matches your certificate
- Ensure certificate has a private key

### Problem: Regional endpoint not used
**Solution**: Add `.WithAzureRegion("your-region")` to app builder.

## Related Skills

- **[Shared Guidance](../msal-mtls-pop-guidance/SKILL.md)** - Terminology and conventions
- **[FIC Two-Leg Flow](../msal-mtls-pop-fic-two-leg/SKILL.md)** - For token exchange scenarios

## References

- **Test code**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- **RFC 8705**: [OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
- **MSAL.NET docs**: [Proof-of-Possession tokens](https://learn.microsoft.com/entra/msal/dotnet/)
