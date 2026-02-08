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

Always include these namespaces in mTLS PoP code:

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

## Version Requirements

- **MSAL.NET**: 4.82.1 minimum (earlier versions lack PoP + attestation APIs)
- **Target Framework**: net8.0 recommended (LTS, best performance)
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

## Testing Guidance

### Local Development
- **SAMI**: Not available locally (requires Azure environment)
- **UAMI**: Not available locally without special setup
- **Confidential Client**: Works locally with certificate from Windows Certificate Store

### Azure Environments
- **SAMI**: Azure VM, App Service, Functions, Container Instances, AKS
- **UAMI**: Same as SAMI, plus requires UAMI assignment to resource
- **Region**: Use actual region (e.g., "westus3") for SNI scenarios

### Test Slice Region
For MSAL.NET integration tests, the test slice region is **westus3**.

## Troubleshooting Quick Reference

| Error/Issue | Solution |
|-------------|----------|
| `ManagedIdentityId` is not defined | Add `using Microsoft.Identity.Client.AppConfig;` |
| `WithMtlsProofOfPossession()` not found | Upgrade to MSAL.NET 4.82.1+ |
| `BindingCertificate` is null | Ensure `.WithMtlsProofOfPossession()` was called |
| `WithAttestationSupport()` not found | Add `Microsoft.Identity.Client.KeyAttestation` NuGet |
| IMDS timeout (local machine) | Use UAMI or Confidential Client for local dev |
| Unable to get UAMI token | Check UAMI exists, assigned to resource, correct ID type |

## Additional Resources

- [Vanilla Flow Skill](../msal-mtls-pop-vanilla/SKILL.md)
- [FIC Two-Leg Flow Skill](../msal-mtls-pop-fic-two-leg/SKILL.md)
- [MSAL.NET mTLS PoP Integration Tests](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [MSAL.NET Managed Identity E2E Tests](../../../tests/Microsoft.Identity.Test.E2e/)
