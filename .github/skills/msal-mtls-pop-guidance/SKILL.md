---
skill_name: msal-mtls-pop-guidance
version: 1.0.0
description: Shared terminology, conventions, and patterns for MSAL.NET mTLS Proof-of-Possession (PoP) implementations
applies_to:
  - "**/*msal*.cs"
  - "**/*mtls*.cs"
  - "**/*pop*.cs"
  - "**/*authentication*.cs"
tags:
  - msal
  - mtls
  - proof-of-possession
  - authentication
  - managed-identity
  - confidential-client
---

# MSAL.NET mTLS PoP Guidance

This skill provides shared terminology, conventions, and reviewer expectations for mTLS Proof-of-Possession (PoP) flows in MSAL.NET.

## Terminology

### Flow Types

**Vanilla Flow (Single Request)**
- Direct token acquisition for a target resource
- No intermediate tokens or exchanges
- One MSAL call: `AcquireTokenForClient` or `AcquireTokenForManagedIdentity` with `.WithMtlsProofOfPossession()`
- **Never** refer to this as having "legs" - it's a single operation

**FIC Two-Leg Flow (Token Exchange)**
- **Leg 1**: Acquire token for `api://AzureADTokenExchange` with mTLS PoP
- **Leg 2**: Exchange Leg 1 token as assertion for final token (Bearer or mTLS PoP)
- Used for cross-tenant delegation, token exchange scenarios
- If Leg 2 uses mTLS PoP: the final resource call uses Leg 1's BindingCertificate for TLS

### Authentication Methods

**MSI (Managed Identity)**
- System-assigned: No user-assigned ID needed
- User-assigned: Requires `WithUserAssignedManagedIdentity(clientId)` or `WithUserAssignedManagedIdentity(resourceId: "...")`
- Uses `IManagedIdentityApplication` from `ManagedIdentityApplicationBuilder`
- Token acquisition: `AcquireTokenForManagedIdentity(scopes)`

**Confidential Client (Certificate-Based)**
- Uses `IConfidentialClientApplication` from `ConfidentialClientApplicationBuilder`
- Certificate provided via `.WithCertificate(cert, sendX5c: true)`
- Token acquisition: `AcquireTokenForClient(scopes)`
- Certificate must support SNI (Subject Name Identification)

## mTLS PoP Core Concepts

### BindingCertificate

- Exposed via `AuthenticationResult.BindingCertificate` property
- In vanilla flow: matches the certificate used for authentication
- In FIC two-leg flow: always set to Leg 1's certificate (used for final resource TLS handshake)
- **Critical**: Use this certificate for the TLS client certificate when calling the resource

### Token Types

- **Bearer**: Standard OAuth2 bearer token (no certificate binding)
- **mTLS PoP**: Token bound to a certificate (`Constants.MtlsPoPTokenType`)
- Token type determined by `AuthenticationResult.TokenType`

### Request Modifiers

- `.WithMtlsProofOfPossession()`: Enables mTLS PoP for the token request
- `.WithTenantId(tenantId)`: Specifies target tenant (useful in multi-tenant scenarios)
- `.WithForceRefresh(true)`: Bypasses cache, forces fresh token

## Key Patterns

### Certificate Management

```csharp
// Load certificate (example using cert store)
X509Certificate2 cert = /* load from store, file, or Key Vault */;

// For Confidential Client: sendX5c=true enables SNI
.WithCertificate(cert, sendX5c: true)

// BindingCertificate is set automatically in AuthenticationResult
Assert.IsNotNull(authResult.BindingCertificate);
```

### Resource Calls with mTLS PoP

```csharp
// Use BindingCertificate for TLS handshake
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(authResult.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

var response = await httpClient.GetAsync(resourceUrl);
```

### Error Handling

```csharp
try
{
    var result = await app.AcquireToken...
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
}
catch (MsalServiceException ex) when (ex.ErrorCode == "invalid_client")
{
    // Certificate not allowed for SNI/mTLS PoP
}
catch (MsalServiceException ex) when (ex.ErrorCode == "unauthorized_client")
{
    // App not authorized for target scope
}
```

## Coding Conventions

### MSAL.NET Patterns

- **Always** use `ConfigureAwait(false)` on async calls
- **Always** provide `CancellationToken` parameters with default values
- **Always** implement `IDisposable` properly with `_disposed` flag
- **Always** validate input parameters with `ArgumentNullException`

### Helper Class Structure

```csharp
public class MyHelper : IDisposable
{
    private bool _disposed;

    public async Task<AuthenticationResult> AcquireTokenAsync(
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(scopes);

        // Implementation
        var result = await app.AcquireToken...
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        // Cleanup
        _disposed = true;
    }
}
```

## Reviewer Expectations

When reviewing mTLS PoP code:

1. **Flow Type Clarity**: Is it vanilla or FIC two-leg? Never mix terminology.
2. **Certificate Binding**: Is `BindingCertificate` used correctly for resource calls?
3. **Token Type Validation**: Does the code check `TokenType` when PoP is expected?
4. **Error Handling**: Are mTLS-specific errors handled appropriately?
5. **Async Patterns**: Does every async call use `ConfigureAwait(false)`?
6. **Cancellation**: Are `CancellationToken` parameters propagated correctly?
7. **Disposal**: Are `IDisposable` resources properly cleaned up?
8. **Null Checks**: Are input parameters validated?

## Common Pitfalls

1. **Forgetting BindingCertificate**: Always use `authResult.BindingCertificate` for TLS, not the original cert
2. **Mixing Flow Types**: Don't refer to vanilla flows as having "legs"
3. **Token Type Confusion**: Bearer vs. mTLS PoP tokens require different handling
4. **Regional Endpoints**: mTLS uses `mtlsauth.microsoft.com` instead of `login.microsoftonline.com`
5. **Certificate Disposal**: Don't dispose the BindingCertificate before calling the resource
6. **Cache Behavior**: mTLS PoP tokens are cached separately from Bearer tokens

## References

- Test Examples: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- Public API: `src/client/Microsoft.Identity.Client/AuthenticationResult.cs` (line 346)
- RFC 8705: OAuth 2.0 Mutual-TLS Client Authentication and Certificate-Bound Access Tokens

## Version History

- **1.0.0**: Initial guidance for vanilla and FIC two-leg flows with MSI and Confidential Client coverage
