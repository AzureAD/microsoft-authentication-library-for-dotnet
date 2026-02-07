---
skill_name: msal-mtls-pop-guidance
version: 1.0.0
description: Shared guidance and terminology for MSAL.NET mTLS Proof-of-Possession (PoP) authentication patterns
applies_to:
  - MSAL.NET
  - Microsoft.Identity.Client
tags:
  - mTLS
  - PoP
  - Proof-of-Possession
  - Certificate-Based-Authentication
  - Terminology
---

# MSAL.NET mTLS PoP Shared Guidance

This skill provides shared terminology, conventions, and reviewer expectations for MSAL.NET's mTLS Proof-of-Possession (PoP) authentication flows.

## Terminology and Concepts

### Authentication Patterns

**Vanilla Flow (Single-Leg, Direct Acquisition)**
- **What it is**: Direct token acquisition for a target resource (Graph, Key Vault, custom API) using mTLS PoP
- **Key characteristic**: Single call to acquire token directly for the final resource
- **NO "legs"**: This is NOT a multi-step exchange; it's a direct acquisition
- **Auth methods**: 
  - Managed Identity (MSI): System-assigned (SAMI) or User-assigned (UAMI)
  - Confidential Client: Certificate-based with SNI

**FIC Two-Leg Flow (Token Exchange)**
- **What it is**: Token exchange pattern using assertions from a first token to acquire a second token
- **Leg 1**: Acquire token for `api://AzureADTokenExchange` with mTLS PoP
  - Can use: MSI OR Confidential Client
- **Leg 2**: Exchange Leg 1 token for final resource token using `WithClientAssertion`
  - **ALWAYS Confidential Client** (MSI does NOT have WithClientAssertion API)
- **Final token**: Can be Bearer OR mTLS PoP (reuses Leg 1's BindingCertificate)

### Critical API Constraints

**MSI Limitations**
- ❌ MSI does NOT have `WithClientAssertion()` API
- ❌ MSI CANNOT perform Leg 2 of FIC two-leg exchange
- ✅ MSI CAN perform Leg 1 of FIC exchange (acquiring token for `api://AzureADTokenExchange`)
- ✅ MSI CAN perform vanilla flow (direct token acquisition)

**Confidential Client Capabilities**
- ✅ Supports both vanilla flow and FIC two-leg flow
- ✅ Can use `WithClientAssertion()` for Leg 2
- ✅ Can use certificate-based SNI with `WithCertificate(cert, sendX5C: true)`

### Key MSAL.NET APIs

**For Token Acquisition**
```csharp
// MSI - Vanilla flow
AuthenticationResult result = await managedIdentityApp
    .AcquireTokenForManagedIdentity(resource)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Confidential Client - Vanilla flow
AuthenticationResult result = await confidentialApp
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 1 (MSI or Confidential Client)
AuthenticationResult leg1 = await app
    .AcquireTokenFor[ManagedIdentity|Client](["api://AzureADTokenExchange/.default"])
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2 (Confidential Client ONLY)
AuthenticationResult leg2 = await confidentialApp
    .AcquireTokenForClient(finalScopes)
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1.AccessToken,              // Leg 1 token
            TokenBindingCertificate = leg1.BindingCertificate  // For mTLS PoP in Leg 2
        });
    })
    .WithMtlsProofOfPossession()  // Include for mTLS PoP final token
    .ExecuteAsync();
```

### AuthenticationResult Properties

After successful token acquisition with mTLS PoP:
- `TokenType` = `"pop"` (indicates mTLS PoP token)
- `AccessToken` = The token string to use in Authorization header
- `BindingCertificate` = The X509Certificate2 bound to this token (use for TLS handshake)

**Using the Token**
```csharp
// Authorization header format
httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
    result.TokenType,      // "pop"
    result.AccessToken     // Token string
);

// TLS handler configuration
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);
```

## Managed Identity (MSI) Variants

### System-Assigned Managed Identity (SAMI)
```csharp
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.SystemAssigned)
    .Build();
```

### User-Assigned Managed Identity (UAMI)

**By Client ID**
```csharp
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedClientId("6325cd32-9911-41f3-819c-416cdf9104e7"))
    .Build();
```

**By Resource ID**
```csharp
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedResourceId(
        "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami"))
    .Build();
```

**By Object ID**
```csharp
IManagedIdentityApplication app = ManagedIdentityApplicationBuilder.Create(
    ManagedIdentityId.WithUserAssignedObjectId("ecb2ad92-3e30-4505-b79f-ac640d069f24"))
    .Build();
```

## Confidential Client SNI Configuration

**Certificate Requirements**
- Must be configured with `sendX5C: true` for SNI
- Use real Azure region (e.g., "westus3", not "local")
- Certificate must have private key accessible

**Builder Pattern**
```csharp
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion("westus3")  // Real region required for SNI
    .WithCertificate(cert, sendX5C: true)  // SNI requires sendX5C: true
    .Build();
```

## MSAL.NET Coding Conventions

Helper classes in this skill follow MSAL.NET conventions:
- **Async/await**: All async methods use `ConfigureAwait(false)`
- **Cancellation**: Methods accept `CancellationToken` with default value `default`
- **Disposal**: Implement `IDisposable` with `_disposed` flag check
- **Input validation**: Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual checks for older targets
- **Disposal checks**: Use `ObjectDisposedException.ThrowIf()` for .NET 7+ or manual checks

**Example Pattern**
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
        .ExecuteAsync(cancellationToken)
        .ConfigureAwait(false);

    return result;
}
```

## Valid Scenario Combinations

### Vanilla Flow Scenarios
1. ✅ MSI (SAMI) → Target resource with mTLS PoP
2. ✅ MSI (UAMI) → Target resource with mTLS PoP
3. ✅ Confidential Client → Target resource with mTLS PoP

### FIC Two-Leg Scenarios
1. ✅ MSI Leg 1 PoP → Confidential Client Leg 2 → Bearer token
2. ✅ MSI Leg 1 PoP → Confidential Client Leg 2 → mTLS PoP token
3. ✅ Confidential Client Leg 1 PoP → Confidential Client Leg 2 → Bearer token
4. ✅ Confidential Client Leg 1 PoP → Confidential Client Leg 2 → mTLS PoP token

❌ **Invalid**: MSI Leg 2 scenarios (MSI lacks WithClientAssertion API)

## Testing References

**Test Files**
- Vanilla flow (Confidential Client): `ClientCredentialsMtlsPopTests.cs`, method `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84)
- FIC two-leg (Confidential Client): `ClientCredentialsMtlsPopTests.cs`, method `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178)
- MSI examples: `ManagedIdentityImdsV2Tests.cs` with UAMI IDs from test infrastructure

**Test Region**
- Use `"westus3"` as the test slice region for SNI scenarios

## Reviewer Expectations

When reviewing mTLS PoP code:
1. **Terminology**: Vanilla flow has NO legs; FIC is explicitly "two-leg"
2. **MSI constraints**: Never suggest MSI for Leg 2 or WithClientAssertion scenarios
3. **Certificate binding**: Verify Leg 2 reuses `leg1.BindingCertificate` when requesting mTLS PoP
4. **Token type checks**: Assert `TokenType == "pop"` for mTLS PoP tokens
5. **Resource calls**: Must use both `result.TokenType` + `result.AccessToken` in Authorization header AND `result.BindingCertificate` for TLS handshake

## Related Skills

- **msal-mtls-pop-vanilla**: Implementation guidance for vanilla flow
- **msal-mtls-pop-fic-two-leg**: Implementation guidance for FIC two-leg flow
