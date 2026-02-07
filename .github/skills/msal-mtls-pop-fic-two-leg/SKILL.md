---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0.0
description: FIC (Federated Identity Credential) two-leg flow for assertion-based token exchange with mTLS Proof of Possession using MSAL.NET
applies_to:
  - language: csharp
  - framework: dotnet
tags:
  - msal
  - authentication
  - mtls-pop
  - fic
  - federated-identity
  - assertion
  - token-exchange
  - managed-identity
  - confidential-client
  - security
---

# mTLS PoP FIC Two-Leg Flow - Assertion-Based Token Exchange

This skill covers the **FIC (Federated Identity Credential) two-leg flow** for token acquisition with mTLS Proof of Possession (PoP). This flow involves two distinct legs:

1. **Leg 1**: Acquire an assertion token from the identity provider
2. **Leg 2**: Exchange that assertion for an access token using a Confidential Client with mTLS PoP

## Prerequisites

- **MSAL.NET 4.82.1 or higher** (required for `WithMtlsProofOfPossession()` and `BindingCertificate`)
- .NET 6.0 or higher (net8.0 recommended)

### Update MSAL NuGet Package

If you're using an older version, update immediately:

```bash
dotnet add package Microsoft.Identity.Client --version 4.82.1
# or to get the latest version
dotnet package update Microsoft.Identity.Client
```

### Version Check

If you see errors like:
- "`WithMtlsProofOfPossession()` method not found"
- "`BindingCertificate` property missing on AuthenticationResult"
- "`ManagedIdentityId` not found"
- "`WithClientAssertion()` not available"

**Your MSAL package is outdated.** Update to 4.82.1+ immediately.

## What is the FIC Two-Leg Flow?

The FIC two-leg flow separates token acquisition into two distinct operations:

### Leg 1: Acquire Assertion Token
Get a token from your identity source (Managed Identity or Confidential Client). This token will be used as proof of identity.

**Important**: Leg 1 uses **vanilla flow** (no mTLS PoP) - it's a simple bearer token acquisition.

### Leg 2: Exchange Assertion for Access Token
Use a Confidential Client to exchange the assertion token from Leg 1 for an access token with mTLS PoP. This is where mTLS PoP is applied.

**Important**: Only Confidential Client supports `WithClientAssertion()` API. Managed Identity cannot perform Leg 2.

## Why Use FIC Two-Leg Flow?

Use FIC two-leg when you need:
- **Cross-tenant scenarios**: Identity from one tenant, access to resources in another
- **Federated identity**: Use external identity providers (Managed Identity, etc.) with Azure AD
- **Token transformation**: Convert one token type to another with different claims or scopes
- **Separation of concerns**: Decouple identity acquisition from resource access

For simple scenarios, use the **vanilla flow** instead (see msal-mtls-pop-vanilla skill).

## All Four Scenarios

The FIC two-leg flow supports all combinations of Leg 1 and Leg 2 identity sources:

### Scenario 1: SAMI (Leg 1) → Confidential Client (Leg 2)

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;  // ← CRITICAL for ManagedIdentityId

namespace MtlsPopFicSamiToConfidential
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Leg 1: Acquire assertion using System-Assigned Managed Identity (vanilla, no PoP)
                var leg1Acquirer = FicLeg1Acquirer.CreateForManagedIdentity(
                    ManagedIdentityId.SystemAssigned);
                
                var assertionResult = await leg1Acquirer.AcquireAssertionAsync(
                    "api://your-app-id");  // Your application's App ID URI
                
                Console.WriteLine($"Leg 1 complete. Assertion token type: {assertionResult.TokenType}");
                
                // Leg 2: Exchange assertion for access token with mTLS PoP
                var cert = new X509Certificate2("path/to/cert.pfx", "password");
                var leg2Exchanger = FicLeg2Exchanger.Create(
                    clientId: "your-client-id",
                    tenantId: "your-tenant-id",
                    certificate: cert,
                    region: "westus3");  // Optional
                
                var accessResult = await leg2Exchanger.ExchangeAssertionForAccessTokenAsync(
                    assertionResult.AccessToken,
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Leg 2 complete. Access token type: {accessResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {accessResult.BindingCertificate?.Thumbprint}");
                
                // Use the access token
                using var caller = new ResourceCaller(accessResult);
                string meJson = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/me");
                Console.WriteLine($"Graph /me response: {meJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### Scenario 2: UAMI (Leg 1) → Confidential Client (Leg 2)

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;  // ← CRITICAL for ManagedIdentityId

namespace MtlsPopFicUamiToConfidential
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Leg 1: Acquire assertion using User-Assigned Managed Identity
                
                // UAMI by Client ID
                var uamiClientId = ManagedIdentityId.WithUserAssignedClientId(
                    "6325cd32-9911-41f3-819c-416cdf9104e7");
                
                // Or UAMI by Resource ID
                var uamiResourceId = ManagedIdentityId.WithUserAssignedResourceId(
                    "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami");
                
                // Or UAMI by Object ID
                var uamiObjectId = ManagedIdentityId.WithUserAssignedObjectId(
                    "ecb2ad92-3e30-4505-b79f-ac640d069f24");
                
                var leg1Acquirer = FicLeg1Acquirer.CreateForManagedIdentity(uamiClientId);
                
                var assertionResult = await leg1Acquirer.AcquireAssertionAsync(
                    "api://your-app-id");
                
                Console.WriteLine($"Leg 1 complete. Assertion token type: {assertionResult.TokenType}");
                
                // Leg 2: Exchange assertion for access token with mTLS PoP
                var cert = new X509Certificate2("path/to/cert.pfx", "password");
                var leg2Exchanger = FicLeg2Exchanger.Create(
                    clientId: "your-client-id",
                    tenantId: "your-tenant-id",
                    certificate: cert);
                
                var accessResult = await leg2Exchanger.ExchangeAssertionForAccessTokenAsync(
                    assertionResult.AccessToken,
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Leg 2 complete. Access token type: {accessResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {accessResult.BindingCertificate?.Thumbprint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### Scenario 3: Confidential Client (Leg 1) → Confidential Client (Leg 2)

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MtlsPopFicConfidentialToConfidential
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Leg 1: Acquire assertion using Confidential Client (vanilla, no PoP)
                var cert1 = new X509Certificate2("path/to/cert1.pfx", "password1");
                var leg1Acquirer = FicLeg1Acquirer.CreateForConfidentialClient(
                    clientId: "identity-app-id",
                    tenantId: "identity-tenant-id",
                    certificate: cert1);
                
                var assertionResult = await leg1Acquirer.AcquireAssertionAsync(
                    "api://resource-app-id");
                
                Console.WriteLine($"Leg 1 complete. Assertion token type: {assertionResult.TokenType}");
                
                // Leg 2: Exchange assertion for access token with mTLS PoP
                var cert2 = new X509Certificate2("path/to/cert2.pfx", "password2");
                var leg2Exchanger = FicLeg2Exchanger.Create(
                    clientId: "resource-app-id",
                    tenantId: "resource-tenant-id",
                    certificate: cert2);
                
                var accessResult = await leg2Exchanger.ExchangeAssertionForAccessTokenAsync(
                    assertionResult.AccessToken,
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Leg 2 complete. Access token type: {accessResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {accessResult.BindingCertificate?.Thumbprint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### Scenario 4: Confidential Client (Leg 1) with PoP → Confidential Client (Leg 2) with PoP

In rare cases, you might want PoP for both legs:

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MtlsPopFicDoublePoP
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Leg 1: Acquire assertion with PoP (uncommon but supported)
                var cert1 = new X509Certificate2("path/to/cert1.pfx", "password1");
                var leg1Acquirer = FicLeg1Acquirer.CreateForConfidentialClient(
                    clientId: "identity-app-id",
                    tenantId: "identity-tenant-id",
                    certificate: cert1);
                
                // Note: usePoP: true in Leg 1 (rare scenario)
                var assertionResult = await leg1Acquirer.AcquireAssertionAsync(
                    "api://resource-app-id",
                    usePoP: true);
                
                Console.WriteLine($"Leg 1 complete with PoP. Token type: {assertionResult.TokenType}");
                
                // Leg 2: Exchange PoP assertion for access token with PoP
                var cert2 = new X509Certificate2("path/to/cert2.pfx", "password2");
                var leg2Exchanger = FicLeg2Exchanger.Create(
                    clientId: "resource-app-id",
                    tenantId: "resource-tenant-id",
                    certificate: cert2);
                
                var accessResult = await leg2Exchanger.ExchangeAssertionForAccessTokenAsync(
                    assertionResult.AccessToken,
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Leg 2 complete. Access token type: {accessResult.TokenType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

## Helper Classes

This skill provides two production-ready helper classes:

### FicLeg1Acquirer

Handles Leg 1: acquiring the assertion token:

```csharp
// For Managed Identity (SAMI or UAMI)
var leg1 = FicLeg1Acquirer.CreateForManagedIdentity(
    ManagedIdentityId.SystemAssigned);

// For Confidential Client
var leg1 = FicLeg1Acquirer.CreateForConfidentialClient(
    clientId: "app-id",
    tenantId: "tenant-id",
    certificate: cert);

// Acquire assertion (usually without PoP)
var assertion = await leg1.AcquireAssertionAsync(
    "api://your-app-id",
    usePoP: false);  // typically false for Leg 1
```

### FicLeg2Exchanger

Handles Leg 2: exchanging assertion for access token with mTLS PoP:

```csharp
var leg2 = FicLeg2Exchanger.Create(
    clientId: "app-id",
    tenantId: "tenant-id",
    certificate: cert);

// Exchange assertion for access token with PoP
var access = await leg2.ExchangeAssertionForAccessTokenAsync(
    assertion.AccessToken,
    "https://graph.microsoft.com",
    usePoP: true);  // typically true for Leg 2
```

## Local Testing

**System-Assigned Managed Identity (SAMI)** runs only in Azure services (App Service, VM, Functions, Container Instances, AKS, etc.).

For local testing of Leg 1, use:
- **User-Assigned Managed Identity (UAMI)** via Azure CLI login
- **Confidential Client** with certificate - works anywhere

For Leg 2, always use **Confidential Client** (this is a requirement of the FIC flow).

## Troubleshooting

### "`ManagedIdentityId` is not defined"
**Solution:** Add `using Microsoft.Identity.Client.AppConfig;`

### "`WithMtlsProofOfPossession()` method not found"
**Solution:** Upgrade MSAL to 4.82.1+: `dotnet package update Microsoft.Identity.Client`

### "`WithClientAssertion()` not available"
**Solution:** Ensure you're using `IConfidentialClientApplication` for Leg 2. Managed Identity cannot perform Leg 2 of the FIC flow.

### "`BindingCertificate` property is null" in Leg 2
**Solution:** Ensure `.WithMtlsProofOfPossession()` was called when building the Confidential Client in Leg 2, and that `usePoP: true` was passed to `ExchangeAssertionForAccessTokenAsync()`

### "Timeout calling IMDS endpoint" (local machine)
**Solution:** You're using SAMI outside Azure. Switch to UAMI or Confidential Client for local testing.

### "Invalid assertion" or "Assertion validation failed"
**Possible causes:**
- Assertion token is expired
- Assertion audience doesn't match the client ID in Leg 2
- Assertion issuer not trusted by Leg 2 tenant
- Check Federated Identity Credential configuration in Azure AD

### "Unable to get UAMI token" in Leg 1
**Possible causes:**
- UAMI doesn't exist in the subscription
- Current user lacks `Managed Identity Operator` role
- UAMI not assigned to your compute resource
- Try UAMI by different ID type (ClientId vs ResourceId vs ObjectId)

## Flow Comparison: Vanilla vs FIC Two-Leg

| Aspect | Vanilla Flow | FIC Two-Leg Flow |
|--------|--------------|------------------|
| **Legs** | Single operation | Two separate operations |
| **Use Case** | Simple, direct token acquisition | Cross-tenant, federated identity, token transformation |
| **Complexity** | Low | Medium |
| **Leg 1 Identity** | N/A | Managed Identity OR Confidential Client |
| **Leg 2 Identity** | N/A | Confidential Client only |
| **PoP Applied** | On the single operation | Usually on Leg 2 only |
| **Performance** | Faster (one round trip) | Slower (two round trips) |

See **msal-mtls-pop-guidance** skill for detailed guidance on choosing between flows.

## Security Considerations

1. **Assertion Security**: Protect assertion tokens as they represent your identity
2. **Token Lifetime**: Both assertion and access tokens have lifetimes; handle expiration
3. **Certificate Management**: Use separate certificates for Leg 1 and Leg 2 when possible
4. **Audience Validation**: Ensure assertion audience matches Leg 2 client ID
5. **Tenant Isolation**: Be aware of cross-tenant trust relationships

## Performance Tips

1. **Cache Assertions**: If acquiring multiple access tokens, reuse the same assertion (if not expired)
2. **Reuse Acquirers**: Create `FicLeg1Acquirer` and `FicLeg2Exchanger` once, use multiple times
3. **Parallel Operations**: Leg 1 and Leg 2 must be sequential, but you can parallelize multiple independent flows
4. **Regional Endpoints**: Specify `region` parameter in Leg 2 for lower latency

## Related Skills

- **msal-mtls-pop-vanilla** - Direct token acquisition (vanilla flow) - simpler alternative for non-federated scenarios
- **msal-mtls-pop-guidance** - High-level guidance on choosing between vanilla and FIC flows

## Additional Resources

- [MSAL.NET Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki)
- [mTLS PoP Design Document](../../../docs/sni_mtls_pop_token_design.md)
- [Test Examples](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [Federated Identity Credentials](https://learn.microsoft.com/entra/workload-id/workload-identity-federation)
