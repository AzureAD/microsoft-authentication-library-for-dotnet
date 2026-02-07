---
skill_name: msal-mtls-pop-guidance
version: 1.0.0
description: |
  Shared terminology, conventions, and reviewer expectations for MSAL.NET mTLS Proof-of-Possession flows.
  Use this skill as foundational context before implementing vanilla or FIC two-leg flows.
applies_to:
  - .NET
  - C#
  - MSAL.NET
  - OAuth 2.0
  - mTLS
  - Proof-of-Possession
tags:
  - authentication
  - mtls
  - proof-of-possession
  - terminology
  - conventions
---

# MSAL.NET mTLS PoP: Shared Guidance

This skill provides **shared terminology and conventions** for MSAL.NET's mTLS Proof-of-Possession (PoP) implementations. Use it as foundational context before implementing specific flows.

## Core Terminology

### mTLS PoP vs. jwt-pop
- **mTLS PoP**: Token type returned when acquiring tokens with `.WithMtlsProofOfPossession()` at the request level. The token is bound to the certificate provided via `.WithCertificate(cert, sendX5C: true)` at app level. Used for direct resource access.
  
- **jwt-pop**: Client assertion type (`urn:ietf:params:oauth:client-assertion-type:jwt-pop`) used in FIC two-leg flows. The assertion from Leg 1 is bound to a certificate via `ClientSignedAssertion.TokenBindingCertificate`. MSAL automatically sets `client_assertion_type=jwt-pop` when both PoP and `TokenBindingCertificate` are used.

**Key distinction**: mTLS PoP is a **token type**, jwt-pop is a **client assertion type** for token exchange.

### Flow Types

#### Vanilla Flow (No Legs)
**Direct token acquisition** with mTLS PoP for a specific resource (Azure Key Vault, Microsoft Graph, custom API).

**Structure**:
1. Build `IConfidentialClientApplication` with `.WithCertificate(cert, sendX5C: true)`
2. Call `.AcquireTokenForClient(scopes).WithMtlsProofOfPossession().ExecuteAsync()`
3. Use the returned PoP token to call the target resource over mTLS

**No delegation, no token exchange**. Single-step acquisition.

**Test reference**: `ClientCredentialsMtlsPopTests.cs` → `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84)

#### FIC Two-Leg Flow (Assertion Exchange)
**Token exchange pattern** for cross-tenant scenarios or delegation using federated identity credentials (FIC).

**Structure**:
1. **Leg 1 (Token Exchange Acquisition)**:
   - Build first app with `.WithCertificate(cert, sendX5C: true)`
   - Acquire token with scope `"api://AzureADTokenExchange/.default"` and `.WithMtlsProofOfPossession()`
   - Result: JWT token to use as assertion in Leg 2

2. **Leg 2 (Assertion Exchange)**:
   - Build second app with `.WithClientAssertion()` (NO `.WithCertificate()`)
   - Assertion provider returns `ClientSignedAssertion` with:
     - `Assertion` = Leg 1 token
     - `TokenBindingCertificate` = certificate for jwt-pop binding
   - Call `.AcquireTokenForClient(finalScopes).WithMtlsProofOfPossession().ExecuteAsync()`
   - MSAL sends `client_assertion_type=jwt-pop` automatically
   - Result: PoP token for target resource

**Test reference**: `ClientCredentialsMtlsPopTests.cs` → `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178)

### SNI vs. MSI
- **SNI (Secure Network Infrastructure)**: Certificate-based client credential flow using `.WithCertificate()`. App identity proven via X.509 certificate. Used for both vanilla and FIC flows.

- **MSI (Managed Identity)**: Azure-managed identity flow where the app runs on Azure infrastructure (VM, App Service, etc.) and acquires tokens without explicit credentials. Can also support mTLS PoP via IMDSv2 attestation.

**Most examples use SNI** for clarity and reproducibility outside Azure environments.

## MSAL.NET Conventions

### Certificate Configuration
```csharp
// Correct: sendX5C=true enables mTLS PoP
.WithCertificate(cert, sendX5C: true)

// Incorrect: sendX5C=false (or omitted) disables PoP binding
.WithCertificate(cert, sendX5C: false)
```

**Why `sendX5C=true`?** The certificate chain is sent in the `x5c` header to Azure AD, enabling mTLS PoP flows.

### PoP Token Acquisition
```csharp
// Request-level PoP enablement
authResult = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()  // Required for PoP
    .ExecuteAsync()
    .ConfigureAwait(false);

// Check token type
Assert.AreEqual("mtls_pop", authResult.TokenType);
```

### Async/Await Patterns
**Always use**:
- `.ConfigureAwait(false)` on all `await` calls (avoid deadlocks)
- `CancellationToken` parameters where applicable
- `async Task` return types (not `async void`)

### BindingCertificate Property
```csharp
// AuthenticationResult exposes the binding certificate
X509Certificate2 bindingCert = authResult.BindingCertificate;

// In SNI flows, this matches the WithCertificate() cert
Assert.AreEqual(cert.Thumbprint, bindingCert.Thumbprint);
```

## Reviewer Expectations

When reviewing mTLS PoP code, check for:

1. **Certificate Configuration**:
   - `sendX5C: true` is present in `.WithCertificate()`
   - Certificate is loaded correctly (from file, cert store, or Key Vault)

2. **PoP Enablement**:
   - `.WithMtlsProofOfPossession()` is called on token acquisition requests
   - Token type is verified as `"mtls_pop"` in tests

3. **Flow Clarity**:
   - Vanilla flow: Single app, direct acquisition, no assertion provider
   - FIC two-leg: Two apps, assertion provider in Leg 2 with `TokenBindingCertificate`

4. **Assertion Provider (FIC only)**:
   - Returns `ClientSignedAssertion` with both `Assertion` and `TokenBindingCertificate`
   - Does NOT call `.WithCertificate()` on the assertion app (Leg 2)
   - Uses `AssertionRequestOptions.TokenEndpoint` if needed

5. **Error Handling**:
   - Wrap token acquisition in try/catch for `MsalServiceException`, `MsalClientException`
   - Handle certificate loading failures gracefully

6. **Resource Calls**:
   - Use `HttpClient` with mTLS-enabled handler (e.g., `HttpClientHandler` with `ClientCertificates.Add(cert)`)
   - Set `Authorization: pop {token}` header (lowercase "pop")
   - Verify response status codes (200, 401 with WWW-Authenticate challenges)

## Common Mistakes

### Mistake 1: Forgetting `sendX5C: true`
```csharp
// WRONG: PoP won't work
.WithCertificate(cert)  // defaults to sendX5C=false

// RIGHT:
.WithCertificate(cert, sendX5C: true)
```

### Mistake 2: Mixing Vanilla and FIC Terminology
```csharp
// WRONG: Vanilla flow doesn't have "legs"
// "Step 1: Acquire Leg 1 token with .WithCertificate()..."

// RIGHT: "Acquire PoP token with .WithCertificate() and .WithMtlsProofOfPossession()"
```

### Mistake 3: Using `.WithCertificate()` in FIC Leg 2
```csharp
// WRONG: Leg 2 should use assertion provider, NOT WithCertificate
IConfidentialClientApplication leg2App = builder
    .WithCertificate(cert, sendX5C: true)  // ❌ Remove this
    .WithClientAssertion(...)
    .Build();

// RIGHT: Only assertion provider
IConfidentialClientApplication leg2App = builder
    .WithClientAssertion(...)  // ✅ Certificate via TokenBindingCertificate
    .Build();
```

### Mistake 4: Incorrect Authorization Header
```csharp
// WRONG: Uppercase "PoP" or "Bearer"
headers.Add("Authorization", "Bearer " + authResult.AccessToken);
headers.Add("Authorization", "PoP " + authResult.AccessToken);

// RIGHT: Lowercase "pop"
headers.Add("Authorization", "pop " + authResult.AccessToken);
```

## Related Skills

- **msal-mtls-pop-vanilla**: Vanilla flow implementation with helper classes
- **msal-mtls-pop-fic-two-leg**: FIC two-leg flow implementation with helper classes

## Documentation References

- [MSAL.NET Wiki: Client Credentials](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows)
- [Azure AD mTLS PoP Overview](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-mtls)
- [RFC 8705: OAuth 2.0 Mutual-TLS](https://datatracker.ietf.org/doc/html/rfc8705)
- Test code: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
