---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0
description: FIC token exchange pattern using assertions for mTLS PoP with MSI and Confidential Client support
applies_to:
  - MSAL.NET/mTLS-PoP
  - MSAL.NET/Managed-Identity
  - MSAL.NET/Confidential-Client
  - MSAL.NET/FIC
tags:
  - msal
  - mtls
  - pop
  - proof-of-possession
  - fic
  - token-exchange
  - two-leg
  - workload-identity
---

# MSAL.NET mTLS PoP - FIC Two-Leg Flow (Token Exchange)

This skill covers Federated Identity Credential (FIC) token exchange using assertions with mTLS Proof-of-Possession. Use this for workload identity federation scenarios in Kubernetes, multi-tenant authentication chains, or any case requiring token exchange.

## What is FIC Two-Leg Flow?

**FIC two-leg flow** is a two-step token exchange process:

1. **Leg 1**: Acquire token for `api://AzureADTokenExchange` (MSI or Confidential Client)
   - Always targets `api://AzureADTokenExchange`
   - Can use Managed Identity (SAMI/UAMI) OR Confidential Client
   - Includes `.WithMtlsProofOfPossession()` and `.WithAttestationSupport()`

2. **Leg 2**: Exchange Leg 1 token for final target resource (Confidential Client ONLY)
   - Uses Leg 1's AccessToken as the `Assertion` in `ClientSignedAssertion`
   - **MUST use Confidential Client** (MSI does NOT have `WithClientAssertion()` API)
   - Can request Bearer OR mTLS PoP final token
   - If mTLS PoP: Uses Leg 1's `BindingCertificate` as `TokenBindingCertificate`

## Valid Combinations

| Leg 1 Auth Method | Leg 1 Token Type | Leg 2 Auth Method | Leg 2 Token Type | Valid? |
|-------------------|------------------|-------------------|------------------|--------|
| MSI (SAMI/UAMI) | mTLS PoP | Confidential Client | Bearer | ✅ Yes |
| MSI (SAMI/UAMI) | mTLS PoP | Confidential Client | mTLS PoP | ✅ Yes |
| Confidential Client | mTLS PoP | Confidential Client | Bearer | ✅ Yes |
| Confidential Client | mTLS PoP | Confidential Client | mTLS PoP | ✅ Yes |
| MSI | mTLS PoP | **MSI** | Any | ❌ **NO** - MSI lacks WithClientAssertion |

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
  using Microsoft.Identity.Client.Extensibility;    // For ClientSignedAssertion
  ```

## Complete Examples

### Scenario 1: MSI Leg 1 → Confidential Client Leg 2 → Bearer Token

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.Extensibility;

// Leg 1: MSI acquires token for api://AzureADTokenExchange with PoP
var leg1App = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7"))
    .Build();

var leg1Result = await leg1App
    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()  // Credential Guard attestation
    .ExecuteAsync();

Console.WriteLine($"Leg 1 Token Type: {leg1Result.TokenType}");  // "mtls_pop"

// Leg 2: Confidential Client exchanges token for final resource (Bearer)
var leg2App = ConfidentialClientApplicationBuilder
    .Create("your-leg2-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")  // Specify region of your Azure resource
    .WithClientAssertion((options, ct) => 
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Result.AccessToken,  // Use Leg 1's token
            TokenBindingCertificate = leg1Result.BindingCertificate  // Always pass Leg 1's cert
        });
    })
    .Build();

var leg2Result = await leg2App
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .ExecuteAsync();  // No .WithMtlsProofOfPossession() → Bearer token

Console.WriteLine($"Leg 2 Token Type: {leg2Result.TokenType}");  // "Bearer"
```

### Scenario 2: MSI Leg 1 → Confidential Client Leg 2 → mTLS PoP Token

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.Extensibility;

// Leg 1: MSI acquires token for api://AzureADTokenExchange with PoP
var leg1App = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.SystemAssigned)
    .Build();

var leg1Result = await leg1App
    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

// Leg 2: Confidential Client exchanges token for final resource (mTLS PoP)
var leg2App = ConfidentialClientApplicationBuilder
    .Create("your-leg2-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")  // Specify region of your Azure resource
    .WithClientAssertion((options, ct) => 
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Result.AccessToken,
            TokenBindingCertificate = leg1Result.BindingCertificate  // ← Bind with Leg 1's cert
        });
    })
    .Build();

var leg2Result = await leg2App
    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .WithMtlsProofOfPossession()  // ← Request PoP token
    .ExecuteAsync();

Console.WriteLine($"Leg 2 Token Type: {leg2Result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Cert matches Leg 1: {leg2Result.BindingCertificate?.Thumbprint == leg1Result.BindingCertificate?.Thumbprint}");
```

### Scenario 3: Confidential Client Leg 1 → Confidential Client Leg 2 → Bearer Token

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

// Load certificate for Leg 1
var leg1Cert = GetCertificateFromStore("CN=Leg1Certificate");

// Leg 1: Confidential Client acquires token for api://AzureADTokenExchange
var leg1App = ConfidentialClientApplicationBuilder
    .Create("your-leg1-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")
    .WithCertificate(leg1Cert, sendX5c: true)
    .Build();

var leg1Result = await leg1App
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: Different Confidential Client exchanges token (Bearer)
var leg2App = ConfidentialClientApplicationBuilder
    .Create("your-leg2-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")  // Specify region of your Azure resource
    .WithClientAssertion((options, ct) => 
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Result.AccessToken,
            TokenBindingCertificate = leg1Result.BindingCertificate  // Always pass Leg 1's cert
        });
    })
    .Build();

var leg2Result = await leg2App
    .AcquireTokenForClient(new[] { "https://storage.azure.com/.default" })
    .ExecuteAsync();  // No .WithMtlsProofOfPossession() → Bearer token

Console.WriteLine($"Leg 2 Token Type: {leg2Result.TokenType}");  // "Bearer"
```

### Scenario 4: Confidential Client Leg 1 → Confidential Client Leg 2 → mTLS PoP Token

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

// Load certificate for Leg 1
var leg1Cert = GetCertificateFromStore("CN=Leg1Certificate");

// Leg 1: Confidential Client acquires token for api://AzureADTokenExchange
var leg1App = ConfidentialClientApplicationBuilder
    .Create("your-leg1-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")
    .WithCertificate(leg1Cert, sendX5c: true)
    .Build();

var leg1Result = await leg1App
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: Different Confidential Client exchanges token (mTLS PoP)
var leg2App = ConfidentialClientApplicationBuilder
    .Create("your-leg2-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("westus3")  // Specify region of your Azure resource
    .WithClientAssertion((options, ct) => 
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Result.AccessToken,
            TokenBindingCertificate = leg1Result.BindingCertificate  // ← Bind with Leg 1's cert
        });
    })
    .Build();

var leg2Result = await leg2App
    .AcquireTokenForClient(new[] { "https://management.azure.com/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Console.WriteLine($"Leg 2 Token Type: {leg2Result.TokenType}");  // "mtls_pop"
```

## Production Helper Classes

This skill includes four production-ready helper classes:

### 1. FicLeg1Acquirer.cs
Handles Leg 1 token acquisition for both MSI and Confidential Client with attestation support.

### 2. FicAssertionProvider.cs
Builds `ClientSignedAssertion` from Leg 1 token with optional certificate binding.

### 3. FicLeg2Exchanger.cs
Handles Leg 2 token exchange (Confidential Client only) with Bearer or PoP support.

### 4. ResourceCaller.cs
Helper for calling protected resources with mTLS PoP tokens (reuses vanilla pattern).

See the `.cs` files in this directory for complete implementations.

## Usage Pattern

```csharp
// 1. Leg 1: Acquire token for api://AzureADTokenExchange
var leg1Acquirer = new FicLeg1Acquirer(msiApp);  // or confApp
var leg1Result = await leg1Acquirer.AcquireTokenAsync();

// 2. Build assertion from Leg 1 result
var assertionProvider = new FicAssertionProvider(leg1Result);
var assertion = assertionProvider.CreateAssertion(
    bindCertificate: true);  // true for PoP, false for Bearer

// 3. Leg 2: Exchange for final resource
var leg2Exchanger = new FicLeg2Exchanger(leg2ConfApp, assertionProvider);
var leg2Result = await leg2Exchanger.ExchangeTokenAsync(
    new[] { "https://graph.microsoft.com/.default" },
    requestMtlsPop: true);  // true for PoP, false for Bearer

// 4. Call resource with final token
if (leg2Result.TokenType == "mtls_pop")
{
    using var caller = new ResourceCaller(leg2Result);
    string response = await caller.CallResourceAsync("https://mtlstb.graph.microsoft.com/v1.0/applications");
}
```

## Key Points

1. **Leg 1 always targets `api://AzureADTokenExchange`**: This is the FIC token exchange endpoint
2. **Leg 2 MUST be Confidential Client**: MSI does NOT have `WithClientAssertion()` API
3. **Four valid combinations**: All permutations of MSI/ConfApp Leg 1 × Bearer/PoP Leg 2
4. **Always pass Leg 1's certificate**: Include `TokenBindingCertificate = leg1Result.BindingCertificate` in `ClientSignedAssertion` for all scenarios (both ****** PoP Leg 2)
5. **Always include `.WithAttestationSupport()`** in Leg 1 for production Credential Guard support
6. **Test slice region**: Use "westus3" for MSAL.NET integration tests

## Why MSI Cannot Do Leg 2

The `IManagedIdentityApplication` interface does NOT provide a `WithClientAssertion()` method:
- MSI is designed for direct authentication only
- Assertion-based auth requires `IConfidentialClientApplication`
- This is a fundamental MSAL.NET API limitation, not a configuration issue

## Troubleshooting

| Error/Issue | Solution |
|-------------|----------|
| "MSI doesn't have WithClientAssertion" | Use Confidential Client for Leg 2 (MSI can only do Leg 1) |
| `ClientSignedAssertion` is not defined | Add `using Microsoft.Identity.Client.Extensibility;` |
| `ManagedIdentityId` is not defined | Add `using Microsoft.Identity.Client.AppConfig;` |
| `WithMtlsProofOfPossession()` not found | Upgrade to MSAL.NET 4.82.1+ |
| `WithAttestationSupport()` not found | Add NuGet: `Microsoft.Identity.Client.KeyAttestation` |
| Leg 2 cert mismatch | Ensure Leg 1's `BindingCertificate` is passed as `TokenBindingCertificate` |
| "urn:ietf:params:oauth:client-assertion-type:jwt-pop" error | Certificate binding is automatic when `TokenBindingCertificate` is set |

## Additional Resources

- [Shared Guidance Skill](../msal-mtls-pop-guidance/SKILL.md) - Terminology and conventions
- [Vanilla Flow Skill](../msal-mtls-pop-vanilla/SKILL.md) - Direct token acquisition
- [ClientCredentialsMtlsPopTests.cs](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs) - See `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync`
