---
skill_name: msal-mtls-pop-guidance
version: 1.0.0
description: Shared terminology and conventions for MSAL.NET mTLS Proof-of-Possession flows
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
  - security
---

# MSAL.NET mTLS Proof-of-Possession - Shared Guidance

This skill provides shared terminology and conventions for implementing mTLS Proof-of-Possession (PoP) with MSAL.NET.

## What is mTLS PoP?

**mTLS Proof-of-Possession (PoP)** is a security mechanism that binds OAuth 2.0 access tokens to a specific X.509 certificate. This prevents token theft and replay attacks because the token can only be used when accompanied by the bound certificate.

Key characteristics:
- **Token binding**: Access token is cryptographically bound to a certificate
- **mTLS enforcement**: Resource servers require the client to present the bound certificate
- **Enhanced security**: Even if a token is stolen, it cannot be used without the private key

## Core Terminology

### Vanilla Flow
**Direct token acquisition** with mTLS PoP, without token exchange or assertion-based authentication.

Key characteristics:
- **Single-step**: One call to acquire the token
- **Direct credential**: Uses certificate at application level (`WithCertificate()`)
- **No legs**: This is NOT a multi-leg flow
- **Use case**: Direct service-to-service authentication with PoP

**Example**:
```csharp
// Vanilla flow: direct PoP token acquisition
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate, sendX5C: true)  // SNI certificate
    .Build();

var result = await app
    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

### FIC Two-Leg Flow
**Token exchange flow** that uses an assertion-based authentication pattern.

Key characteristics:
- **Two legs**: Leg 1 acquires an exchange token, Leg 2 exchanges it for the final token
- **Assertion-based**: Uses `WithClientAssertion()` with a token from Leg 1
- **Use case**: Cross-tenant scenarios, delegation, token exchange patterns

**Leg 1** - Acquire exchange token:
```csharp
// Leg 1: Get token for exchange
var leg1App = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate, sendX5C: true)
    .Build();

var leg1Result = await leg1App
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

**Leg 2** - Exchange assertion for final token:
```csharp
// Leg 2: Exchange assertion for target token
var leg2App = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithExperimentalFeatures()
    .WithClientAssertion((options, ct) => Task.FromResult(new ClientSignedAssertion
    {
        Assertion = leg1Result.AccessToken,
        TokenBindingCertificate = certificate  // Binds for jwt-pop
    }))
    .Build();

var leg2Result = await leg2App
    .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

## Critical Distinctions

| Aspect | Vanilla Flow | FIC Two-Leg Flow |
|--------|--------------|------------------|
| **Steps** | Single call | Two separate calls (Leg 1 + Leg 2) |
| **Authentication** | `WithCertificate()` | `WithClientAssertion()` in Leg 2 |
| **Token Exchange** | No | Yes (Leg 1 token → Leg 2 token) |
| **client_assertion_type** | N/A | `urn:ietf:params:oauth:client-assertion-type:jwt-pop` |
| **Use "legs" terminology** | ❌ No | ✅ Yes |

## Implementation Patterns

### MSI (Managed Service Identity)
- Certificate loaded from **local machine store**
- Typical in Azure VMs, container instances
- Use `CertificateHelper.FindCertificateByName()` or `X509Store`

### SNI (Secure Nested Injection)
- Certificate provided via **Windows SNI API**
- Typical in Azure Functions, App Service
- Use `WithCertificate(cert, sendX5C: true)`

## Key MSAL.NET APIs

### Token Acquisition
```csharp
.WithMtlsProofOfPossession()  // Enable mTLS PoP at request level
```

### Certificate Binding
```csharp
// At application level (vanilla flow)
.WithCertificate(certificate, sendX5C: true)

// Via assertion (FIC two-leg flow)
new ClientSignedAssertion
{
    Assertion = jwtToken,
    TokenBindingCertificate = certificate
}
```

### Token Inspection
```csharp
authResult.TokenType           // "pop" for PoP tokens
authResult.BindingCertificate  // The bound certificate (vanilla & SNI flows)
```

## Common Pitfalls

1. **Mixing terminology**: Don't call vanilla flow a "leg" - it's a direct acquisition
2. **Missing `sendX5C`**: Required for SNI scenarios (`WithCertificate(cert, true)`)
3. **Certificate disposal**: Don't dispose certificates loaded from the store
4. **Token type confusion**: PoP tokens have `TokenType = "pop"`, not "Bearer"
5. **Regional endpoints**: mTLS requires regional token endpoint (`WithAzureRegion()`)

## Reviewer Expectations

When reviewing code or questions about mTLS PoP:

✅ **DO**:
- Use "vanilla flow" for direct acquisition (no token exchange)
- Use "FIC two-leg flow" or "Leg 1/Leg 2" for assertion exchange
- Specify MSI or SNI context when relevant
- Reference the token type (`pop` vs `Bearer`)

❌ **DON'T**:
- Call vanilla flow "Leg 1" or use "legs" terminology
- Assume all PoP flows involve token exchange
- Forget to mention `WithMtlsProofOfPossession()` requirement

## Related Resources

- **Vanilla flow skill**: [msal-mtls-pop-vanilla](../msal-mtls-pop-vanilla/SKILL.md)
- **FIC two-leg flow skill**: [msal-mtls-pop-fic-two-leg](../msal-mtls-pop-fic-two-leg/SKILL.md)
- **Test reference**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- **RFC 8705**: [OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
