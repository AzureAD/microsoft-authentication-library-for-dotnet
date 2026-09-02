---
skill_name: msal-mtls-pop-guidance
version: 1.0
description: Shared terminology, conventions, and patterns for mTLS Proof-of-Possession (PoP) flows in MSAL.NET
applies_to:
  - MSAL.NET/mTLS-PoP
  - MSAL.NET/Managed-Identity
  - MSAL.NET/Confidential-Client
tags:
  - msal
  - mtls
  - pop
  - proof-of-possession
  - terminology
  - conventions
  - sni
---

# MSAL.NET mTLS PoP Guidance - Shared Terminology & Conventions

This skill provides shared terminology, conventions, and patterns for working with mTLS Proof-of-Possession (PoP) flows in MSAL.NET. Use this as a reference when implementing or reviewing any mTLS PoP scenario.

## Core Terminology

### Authentication Methods

**MSI (Managed Identity)**
- Cloud-native identity for Azure resources that eliminates credential management
- Two variants:
  - **SAMI (System-Assigned Managed Identity)**: Automatically created with Azure resource, tied to resource lifecycle
  - **UAMI (User-Assigned Managed Identity)**: Standalone identity that can be shared across multiple resources
- Works in: Azure VMs, App Service, Functions, Container Instances, AKS, Azure Arc
- **Limitation**: MSI does NOT have `WithClientAssertion()` API - cannot be used for Leg 2 in FIC flows

**Confidential Client**
- Traditional application identity using certificates or secrets
- Uses `IConfidentialClientApplication` from MSAL.NET
- Required for: FIC Leg 2, local development, non-Azure environments
- Supports: Certificate-based SNI (Subject Name/Issuer) authentication

### Flow Patterns

**Vanilla Flow (Single-Step, No "Legs")**
- Direct token acquisition from Azure AD for a target resource
- One call: `AcquireTokenForManagedIdentity()` or `AcquireTokenForClient()`
- Example: Acquire token directly for `https://graph.microsoft.com`
- **Never** refer to vanilla flow as having "legs" - it's a single direct acquisition

**FIC Two-Leg Flow (Token Exchange)**
- Two-step process using Federated Identity Credentials (workload identity)
- **Leg 1**: Acquire token for `api://AzureADTokenExchange` (MSI or Confidential Client)
- **Leg 2**: Exchange Leg 1 token for final target resource (Confidential Client ONLY)
- Used in: Kubernetes workload identity, multi-tenant scenarios, complex authentication chains

**SNI mTLS PoP Flow (Single-Step, Confidential Client)**
- SNI (Subject Name/Issuer) certificate configured at the **app builder level**: `.WithCertificate(cert, sendX5c: true)`
- mTLS PoP requested at the **request level**: `.WithMtlsProofOfPossession()`
- Result token type is `mtls_pop` (`Constants.MtlsPoPTokenType`)
- `AuthenticationResult.BindingCertificate` is populated; its `Thumbprint` matches the certificate passed to `WithCertificate()`
- Supports regional endpoints (`.WithAzureRegion("westus3")`) and the global `mtlsauth.microsoft.com` endpoint
- SNI certificate authentication works **cross-platform, including Linux**;

### Token Types

**Bearer Token**
- Standard OAuth 2.0 token type
- Sent as `Authorization: Bearer <token>` header
- No cryptographic binding to client

**mTLS PoP Token**
- Proof-of-Possession token cryptographically bound to a certificate
- Prevents token theft/replay attacks
- Requires mTLS (mutual TLS) when calling target resource
- Token type in response: `"mtls_pop"`
- Enabled via `.WithMtlsProofOfPossession()` API

### Key Concepts

**SNI (Subject Name/Issuer)**
- Certificate authentication method using X.509 certificate subject and issuer
- Configured at app builder level: `.WithCertificate(cert, sendX5c: true)`
- Used with Confidential Client only
- **Works cross-platform, including Linux** 

**BindingCertificate**
- Certificate that was cryptographically bound to a PoP token
- Accessed via `AuthenticationResult.BindingCertificate` property
- Required for making mTLS calls to target resources
- In FIC Leg 2: Can reuse Leg 1's `BindingCertificate` by passing it as `TokenBindingCertificate`

**Credential Guard Attestation**
- Windows security feature that protects credentials in virtualized containers
- Enabled via `.WithAttestationSupport()` API
- Requires: `Microsoft.Identity.Client.KeyAttestation` NuGet package
- Supported: MSI flows (SAMI, UAMI) and Confidential Client flows
- **Always include in production code** for enhanced security

## UAMI Identifier Types

User-Assigned Managed Identities can be specified using any of three ID types:

### 1. Client ID (Application ID)
```csharp
ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7")
```
- Most commonly used
- Same as the "Application (client) ID" in Azure Portal

### 2. Resource ID (ARM Path)
```csharp
ManagedIdentityId.WithUserAssignedResourceId(
    "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami")
```
- Full Azure Resource Manager path
- Useful in ARM templates or scripts

### 3. Object ID (Principal ID)
```csharp
ManagedIdentityId.WithUserAssignedObjectId("ecb2ad92-3e30-4505-b79f-ac640d069f24")
```
- Azure AD object ID of the managed identity
- Same as the "Object (principal) ID" in Azure Portal

**Note**: All three types refer to the same identity and are functionally equivalent. Use whichever is most convenient for your scenario.

## FIC Two-Leg Flow - Valid Combinations

### Four Valid Scenarios

| Leg 1 Auth Method | Leg 1 Token Type | Leg 2 Auth Method | Leg 2 Token Type | Valid? |
|-------------------|------------------|-------------------|------------------|--------|
| MSI | mTLS PoP | Confidential Client | Bearer | ✅ Yes |
| MSI | mTLS PoP | Confidential Client | mTLS PoP | ✅ Yes |
| Confidential Client | mTLS PoP | Confidential Client | Bearer | ✅ Yes |
| Confidential Client | mTLS PoP | Confidential Client | mTLS PoP | ✅ Yes |
| MSI | mTLS PoP | **MSI** | Any | ❌ **NO** - MSI lacks WithClientAssertion |

### Key Rules
1. **Leg 1** can use MSI or Confidential Client
2. **Leg 2 MUST be Confidential Client** - MSI cannot perform assertion-based authentication
3. Leg 2 can request ****** mTLS PoP final token
4. **Always pass Leg 1's certificate**: Include `TokenBindingCertificate = leg1Result.BindingCertificate` in `ClientSignedAssertion` for all scenarios (both ****** PoP Leg 2)

## Required Namespaces

Include these core namespaces in mTLS PoP code:

```csharp
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;        // ← For ManagedIdentityId
using Microsoft.Identity.Client.KeyAttestation;   // ← For WithAttestationSupport()
```

Add `using Microsoft.Identity.Client.Extensibility;` only when you use extensibility hooks such as `OnBeforeTokenRequest`.

## Version Requirements

- **MSAL.NET**: 4.82.1 minimum (earlier versions lack PoP + attestation APIs)
- **Target Framework**: net10.0 recommended (LTS, best performance)
- **NuGet Packages**:
  ```bash
  dotnet add package Microsoft.Identity.Client --version 4.82.1
  dotnet add package Microsoft.Identity.Client.KeyAttestation
  ```

## Code Conventions

All helper classes and examples follow MSAL.NET conventions:

1. **Async/Await**: Use `ConfigureAwait(false)` on all awaits
2. **Cancellation**: Accept `CancellationToken` with default `= default`
3. **Disposal**: Implement `IDisposable` with `_disposed` flag
4. **Validation**: Use `ArgumentNullException.ThrowIfNull()` for inputs
5. **Disposal Checks**: Use `ObjectDisposedException.ThrowIf()` before operations

### Example Pattern
```csharp
public async Task<AuthenticationResult> AcquireTokenAsync(
    string resource,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(resource);
    ObjectDisposedException.ThrowIf(_disposed, this);

    var result = await _app
        .AcquireTokenForManagedIdentity(resource)
        .WithMtlsProofOfPossession()
        .WithAttestationSupport()
        .ExecuteAsync(cancellationToken)
        .ConfigureAwait(false);

    return result;
}
```

## SNI mTLS PoP Flow - Details & Examples

### 1. Basic SNI mTLS PoP (Single-Step, Confidential Client)

Configure SNI at the app builder level with `sendX5c: true`, then request PoP at the request level:

```csharp
X509Certificate2 cert = /* load your certificate */;

IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")          // use an actual Azure production region
    .WithCertificate(cert, sendX5c: true) // SNI at app level
    .Build();

AuthenticationResult result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()          // PoP at request level
    .ExecuteAsync()
    .ConfigureAwait(false);

// Verify token type and binding
Assert.AreEqual("mtls_pop", result.TokenType);
Assert.IsNotNull(result.BindingCertificate);
Assert.AreEqual(cert.Thumbprint, result.BindingCertificate.Thumbprint);
```

### 2. Regional vs Global mTLS Endpoints

- **Regional**: Call `.WithAzureRegion("westus3")` to route to the regional endpoint, e.g. `westus3.mtlsauth.microsoft.com`. `westus3` is a stable Azure production region — no extra query parameters or test-slice configuration is involved.
- **Global**: Omit `.WithAzureRegion(...)` to use the global endpoint `mtlsauth.microsoft.com`.
- Verify the endpoint used via `result.AuthenticationResultMetadata.TokenEndpoint`.

```csharp
// Regional endpoint
IConfidentialClientApplication regionalApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")           // routes to westus3.mtlsauth.microsoft.com
    .WithCertificate(cert, sendX5c: true)
    .Build();

// Global endpoint (no WithAzureRegion)
IConfidentialClientApplication globalApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithCertificate(cert, sendX5c: true) // uses mtlsauth.microsoft.com
    .Build();

AuthenticationResult globalResult = await globalApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

Uri endpoint = new Uri(globalResult.AuthenticationResultMetadata.TokenEndpoint);
Assert.AreEqual("mtlsauth.microsoft.com", endpoint.Host);
```

### 3. `CertificateOptions.SendCertificateOverMtls`

`CertificateOptions { SendCertificateOverMtls = true }` routes authentication over the mTLS transport but yields a **Bearer** token when PoP is **not** requested. When `.WithMtlsProofOfPossession()` is explicitly called, the result is **always** an `mtls_pop` token regardless of `SendCertificateOverMtls`.

```csharp
var certOptions = new CertificateOptions { SendCertificateOverMtls = true };

// Bearer token over mTLS transport (NO WithMtlsProofOfPossession)
IConfidentialClientApplication bearerApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")
    .WithCertificate(cert, certOptions)
    .Build();

AuthenticationResult bearerResult = await bearerApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .ExecuteAsync()                        // no WithMtlsProofOfPossession → Bearer token
    .ConfigureAwait(false);

Assert.AreEqual("Bearer", bearerResult.TokenType);

// mTLS PoP always wins when WithMtlsProofOfPossession() is called
IConfidentialClientApplication popApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")
    .WithCertificate(cert, certOptions)    // SendCertificateOverMtls = true or false
    .Build();

AuthenticationResult popResult = await popApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()           // always produces mtls_pop
    .ExecuteAsync()
    .ConfigureAwait(false);

Assert.AreEqual("mtls_pop", popResult.TokenType);
Assert.AreEqual(cert.Thumbprint, popResult.BindingCertificate.Thumbprint);
```

### 4. SNI + jwt-pop Assertion Flow (Two-Leg)

In the assertion (FIC) flow, the second app has **no** `WithCertificate` call — the certificate is supplied via `ClientSignedAssertion.TokenBindingCertificate`. When `TokenBindingCertificate` is set and `.WithMtlsProofOfPossession()` is called, MSAL emits `client_assertion_type = urn:ietf:params:oauth:client-assertion-type:jwt-pop`.

`AssertionRequestOptions.TokenEndpoint` and `AssertionRequestOptions.CorrelationId` are passed into the assertion callback; use `.WithCorrelationId(...)` to set a correlation ID and verify it flows through.

```csharp
using Microsoft.Identity.Client; // ClientSignedAssertion, AssertionRequestOptions

// Leg 1: acquire assertion token using SNI cert (regional or global)
IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")
    .WithCertificate(cert, sendX5c: true)
    .Build();

AuthenticationResult leg1 = await firstApp
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

string assertionJwt = leg1.AccessToken;

// Leg 2: assertion app — NO WithCertificate; cert supplied via TokenBindingCertificate
IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder.Create("<client-id>")
    .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        // options.TokenEndpoint and options.CorrelationId are populated by MSAL
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = assertionJwt,
            TokenBindingCertificate = cert   // binds assertion → jwt-pop client_assertion_type
        });
    })
    .Build();

Guid correlationId = Guid.NewGuid();
AuthenticationResult leg2 = await assertionApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .WithCorrelationId(correlationId)
    .ExecuteAsync()
    .ConfigureAwait(false);

Assert.AreEqual("mtls_pop", leg2.TokenType);
```

**Global-endpoint variant**: omit `.WithAzureRegion(...)` on the assertion app — the global `mtlsauth.microsoft.com` endpoint is used automatically.

## Reviewer Expectations

When reviewing mTLS PoP code, check for:

### Must Have
- [ ] MSAL.NET version 4.82.1 or later documented
- [ ] `.WithMtlsProofOfPossession()` called on token requests
- [ ] `.WithAttestationSupport()` included (production code)
- [ ] Complete namespace declarations (including `AppConfig` and `KeyAttestation`)
- [ ] Correct flow terminology (vanilla vs FIC two-leg, no "legs" in vanilla)
- [ ] MSI limitation documented (no WithClientAssertion for Leg 2)
- [ ] All 3 UAMI ID types shown in examples
- [ ] **SNI**: `sendX5c: true` passed to `.WithCertificate()` at app builder level
- [ ] **SNI**: `.WithMtlsProofOfPossession()` called at request level (not app level)
- [ ] **SNI**: `result.BindingCertificate` is non-null and its `Thumbprint` matches the certificate passed to `WithCertificate()`
- [ ] **SNI assertion flow**: `ClientSignedAssertion.TokenBindingCertificate` set (not `WithCertificate` on the assertion app)

### Should Have
- [ ] `ConfigureAwait(false)` on all awaits
- [ ] `CancellationToken` parameters with defaults
- [ ] Proper `IDisposable` implementation
- [ ] Input validation with `ArgumentNullException.ThrowIfNull`
- [ ] Disposal checks with `ObjectDisposedException.ThrowIf`
- [ ] Certificate null checks after PoP acquisition
- [ ] Proper HttpClient disposal patterns

### Common Mistakes to Avoid
- ❌ Using MSI for FIC Leg 2 (doesn't have WithClientAssertion)
- ❌ Referring to vanilla flow as having "legs"
- ❌ Missing `using Microsoft.Identity.Client.AppConfig;`
- ❌ Forgetting `.WithAttestationSupport()` in production code
- ❌ Using MSAL version < 4.82.1
- ❌ Not checking `BindingCertificate` for null
- ❌ Disposing RSA keys from `GetRSAPrivateKey()` (handled by cert)
- ❌ **SNI**: Forgetting `sendX5c: true` in `.WithCertificate(cert, sendX5c: true)` — SNI requires the public certificate to be sent
- ❌ **SNI**: Assuming `SendCertificateOverMtls = true` controls PoP vs Bearer when PoP is explicitly requested — `.WithMtlsProofOfPossession()` always produces `mtls_pop` regardless of `SendCertificateOverMtls`
- ❌ **SNI assertion flow**: Omitting `TokenBindingCertificate` in `ClientSignedAssertion` — without it, MSAL cannot emit `client_assertion_type: jwt-pop`
- ❌ **SNI assertion flow**: Importing `Microsoft.Identity.Client.Extensibility` for `ClientSignedAssertion`/`AssertionRequestOptions` — these types are in `Microsoft.Identity.Client`
- ❌ Claiming SNI is unsupported on Linux — **SNI certificate authentication works on Linux**; 

## Testing Guidance

### Local Development
- **SAMI**: Not available locally (requires Azure environment)
- **UAMI**: Not available locally without special setup
- **Confidential Client**: Works locally with certificate from Windows Certificate Store

### Azure Environments
- **SAMI**: Azure VM, App Service, Functions, Container Instances, AKS
- **UAMI**: Same as SAMI, plus requires UAMI assignment to resource
- **Region**: Use an actual Azure production region (e.g., `westus3`) for SNI scenarios

### SNI Test Notes
- mTLS PoP tests only run on the allow-listed SNI app and tenant.
- `[RunOn(SkipConditions.Linux)]` is required on all mTLS PoP tests — the mTLS PoP token flow is not supported on Linux. SNI certificate authentication itself is cross-platform and works on Linux.

## Troubleshooting Quick Reference

### mTLS PoP-Specific Issues

| Error/Issue | Solution |
|-------------|----------|
| `ManagedIdentityId` is not defined | Add `using Microsoft.Identity.Client.AppConfig;` |
| `WithMtlsProofOfPossession()` not found | Upgrade to MSAL.NET 4.82.1+ |
| `BindingCertificate` is null | Ensure `.WithMtlsProofOfPossession()` was called |
| `WithAttestationSupport()` not found | Add `Microsoft.Identity.Client.KeyAttestation` NuGet |
| IMDS timeout (local machine) | Use UAMI or Confidential Client for local dev |
| Unable to get UAMI token | Check UAMI exists, assigned to resource, correct ID type |
| `ClientSignedAssertion` or `AssertionRequestOptions` not found | Add `using Microsoft.Identity.Client;` |
| SNI assertion flow not using `jwt-pop` `client_assertion_type` | Set `ClientSignedAssertion.TokenBindingCertificate` and call `.WithMtlsProofOfPossession()` |

### General Credential and Authentication Issues

For comprehensive troubleshooting, certificate setup, error handling, and token caching guidance, see:
- [Troubleshooting Guide](../msal-shared/patterns/troubleshooting.md) - Comprehensive troubleshooting for all credential types
- [Certificate Setup](../msal-shared/credential-setup/certificate-setup.md) - Certificate loading and validation
- [Error Handling Patterns](../msal-shared/patterns/error-handling-patterns.md) - Common error scenarios and solutions
- [Token Caching Strategies](../msal-shared/patterns/token-caching-strategies.md) - Cache management best practices

## Additional Resources

- [Vanilla Flow Skill](../msal-mtls-pop-vanilla/SKILL.md)
- [FIC Two-Leg Flow Skill](../msal-mtls-pop-fic-two-leg/SKILL.md)
- [MSAL.NET mTLS PoP Integration Tests](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [MSAL.NET Managed Identity E2E Tests](../../../tests/Microsoft.Identity.Test.E2e/)
