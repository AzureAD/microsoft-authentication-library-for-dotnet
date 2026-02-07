---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0.0
description: Token exchange pattern using assertions for cross-tenant/delegation scenarios with mTLS PoP
applies_to:
  - MSAL.NET
  - Microsoft.Identity.Client
  - mTLS
  - Proof-of-Possession
  - OAuth 2.0
  - Token Exchange
  - FIC
tags:
  - authentication
  - mtls
  - pop
  - certificate-binding
  - token-exchange
  - assertion
  - two-leg
  - fic
---

# MSAL.NET mTLS PoP - FIC Two-Leg Flow

This skill covers the **FIC (Federated Identity Credential) two-leg token exchange flow** with mTLS Proof-of-Possession. This pattern is used for cross-tenant scenarios, delegation, and assertion-based authentication.

## Overview

The **FIC two-leg flow** involves two separate token acquisitions:

1. **Leg 1**: Acquire an exchange token using standard client credentials
2. **Leg 2**: Exchange that token (as an assertion) for a token to the target resource

**Key characteristic**: This IS a multi-leg flow. Always use "Leg 1" and "Leg 2" terminology.

## When to Use This Flow

Use FIC two-leg flow when:
- ✅ You need token exchange or delegation
- ✅ You're implementing cross-tenant scenarios
- ✅ You're using assertion-based authentication patterns
- ✅ You need to exchange one token for another with different scopes

Don't use FIC two-leg flow when:
- ❌ You just need direct token acquisition (use vanilla flow instead)
- ❌ Simple service-to-service auth is sufficient (use vanilla flow instead)

## Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│ Leg 1: Acquire Exchange Token                           │
│                                                          │
│  App (with cert) → AAD                                  │
│  Request: api://AzureADTokenExchange/.default           │
│  Response: Exchange Token (JWT)                         │
└─────────────────────────────────────────────────────────┘
                          ↓ (Use as assertion)
┌─────────────────────────────────────────────────────────┐
│ Leg 2: Exchange Assertion for Target Token              │
│                                                          │
│  App (with assertion) → AAD                             │
│  Request: https://graph.microsoft.com/.default          │
│  Assertion: Exchange Token from Leg 1                   │
│  Binding: TokenBindingCertificate                       │
│  Response: Target PoP Token                             │
└─────────────────────────────────────────────────────────┘
```

## Basic Pattern

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

public class FicTwoLegPopExample
{
    private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
    
    public async Task<AuthenticationResult> GetTokenViaTwoLegAsync()
    {
        X509Certificate2 cert = LoadCertificate();
        string clientId = "your-client-id";
        string tenantId = "your-tenant-id";
        
        // ═══════════════════════════════════════════════════════════════
        // LEG 1: Acquire exchange token
        // ═══════════════════════════════════════════════════════════════
        IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .WithCertificate(cert, sendX5C: true)
            .WithAzureRegion("eastus")
            .Build();
        
        AuthenticationResult leg1Result = await leg1App
            .AcquireTokenForClient(new[] { TokenExchangeUrl })
            .WithMtlsProofOfPossession()
            .ExecuteAsync()
            .ConfigureAwait(false);
        
        string exchangeToken = leg1Result.AccessToken;
        
        // ═══════════════════════════════════════════════════════════════
        // LEG 2: Exchange assertion for target token
        // ═══════════════════════════════════════════════════════════════
        IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithExperimentalFeatures()
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .WithAzureRegion("eastus")
            .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
            {
                // Build ClientSignedAssertion with token binding
                return Task.FromResult(new ClientSignedAssertion
                {
                    Assertion = exchangeToken,              // Leg 1 token
                    TokenBindingCertificate = cert          // Binds for jwt-pop
                });
            })
            .Build();
        
        AuthenticationResult leg2Result = await leg2App
            .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
            .WithMtlsProofOfPossession()
            .ExecuteAsync()
            .ConfigureAwait(false);
        
        return leg2Result;
    }
    
    private X509Certificate2 LoadCertificate()
    {
        // Load certificate from store or SNI
        throw new NotImplementedException();
    }
}
```

## Complete Working Example

See `ClientCredentialsMtlsPopTests.cs` for a full working test:
- **Test**: `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync()` (lines 88-178)
- **Location**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`

```csharp
[TestMethod]
public async Task Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync()
{
    X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
    
    // ─────────────────────────────────────────────────────────────────
    // Leg 1: Obtain exchange token
    // ─────────────────────────────────────────────────────────────────
    IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")
        .WithCertificate(cert, true)
        .WithTestLogging()
        .Build();
    
    AuthenticationResult first = await firstApp
        .AcquireTokenForClient(new[] { TokenExchangeUrl })
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
    
    string assertionJwt = first.AccessToken;
    
    // ─────────────────────────────────────────────────────────────────
    // Leg 2: Exchange assertion for target token
    // ─────────────────────────────────────────────────────────────────
    IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithExperimentalFeatures()
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")
        .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
        {
            return Task.FromResult(new ClientSignedAssertion
            {
                Assertion = assertionJwt,      // Leg 1 token as assertion
                TokenBindingCertificate = cert // Binds for jwt-pop
            });
        })
        .WithTestLogging()
        .Build();
    
    AuthenticationResult second = await assertionApp
        .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
    
    // Verify jwt-pop client_assertion_type
    Assert.AreEqual(
        "urn:ietf:params:oauth:client-assertion-type:jwt-pop",
        clientAssertionType);
}
```

## Production Helper Classes

This skill includes production-ready helper classes you can copy into your project:

### 1. FicAssertionProvider.cs
Builds `ClientSignedAssertion` from Leg 1 token.

**Usage**:
```csharp
var provider = new FicAssertionProvider(leg1Token, certificate);
var assertion = provider.GetAssertion();
// Returns ClientSignedAssertion with token binding
```

See: [FicAssertionProvider.cs](./FicAssertionProvider.cs)

### 2. FicLeg1Acquirer.cs
Leg 1 token acquisition wrapper.

**Usage**:
```csharp
var leg1 = new FicLeg1Acquirer(clientId, tenantId, certificate, region);
var exchangeToken = await leg1.GetExchangeTokenAsync(CancellationToken.None);
```

See: [FicLeg1Acquirer.cs](./FicLeg1Acquirer.cs)

### 3. FicLeg2Exchanger.cs
Leg 2 assertion exchange wrapper.

**Usage**:
```csharp
var leg2 = new FicLeg2Exchanger(clientId, tenantId, exchangeToken, certificate, region);
var targetToken = await leg2.ExchangeForTargetTokenAsync(
    new[] { "https://graph.microsoft.com/.default" },
    CancellationToken.None
);
```

See: [FicLeg2Exchanger.cs](./FicLeg2Exchanger.cs)

### Complete Helper Usage
```csharp
// Leg 1
var leg1 = new FicLeg1Acquirer(clientId, tenantId, cert, region);
var exchangeToken = await leg1.GetExchangeTokenAsync(CancellationToken.None);

// Leg 2
var leg2 = new FicLeg2Exchanger(clientId, tenantId, exchangeToken, cert, region);
var result = await leg2.ExchangeForTargetTokenAsync(
    new[] { "https://graph.microsoft.com/.default" },
    CancellationToken.None
);

// Call resource
using (var caller = new ResourceCaller(result))
{
    var response = await caller.CallResourceAsync(
        "https://graph.microsoft.com/v1.0/me",
        HttpMethod.Get,
        CancellationToken.None
    );
}
```

## Key Points

### Leg 1 Configuration
```csharp
// Standard certificate-based auth
.WithCertificate(certificate, sendX5C: true)
.WithMtlsProofOfPossession()

// Scopes: Exchange endpoint
new[] { "api://AzureADTokenExchange/.default" }
```

### Leg 2 Configuration
```csharp
// Assertion-based auth with token binding
.WithExperimentalFeatures()  // Required for WithClientAssertion
.WithClientAssertion((options, ct) => Task.FromResult(new ClientSignedAssertion
{
    Assertion = leg1Token,              // Exchange token from Leg 1
    TokenBindingCertificate = cert      // Binds for jwt-pop
}))
.WithMtlsProofOfPossession()

// Scopes: Target resource
new[] { "https://graph.microsoft.com/.default" }
```

### JWT-PoP Verification
When `TokenBindingCertificate` is provided in Leg 2, MSAL uses `jwt-pop` client_assertion_type:

```csharp
// Token request body includes:
// client_assertion_type: urn:ietf:params:oauth:client-assertion-type:jwt-pop
// client_assertion: <Leg 1 token>
```

## Common Scenarios

### Cross-Tenant Token Exchange
```csharp
// Leg 1: Get token in tenant A
var leg1 = new FicLeg1Acquirer(clientId, tenantA, cert, region);
var exchangeToken = await leg1.GetExchangeTokenAsync(ct);

// Leg 2: Exchange for token in tenant B
var leg2 = new FicLeg2Exchanger(clientId, tenantB, exchangeToken, cert, region);
var result = await leg2.ExchangeForTargetTokenAsync(scopes, ct);
```

### Delegation with PoP
```csharp
// Leg 1: Get assertion token
// Leg 2: Exchange for delegated token with PoP binding
// Use result.BindingCertificate for mTLS calls
```

## Troubleshooting

### Problem: "client_assertion_type" is not "jwt-pop"
**Solution**: Ensure you provide `TokenBindingCertificate` in `ClientSignedAssertion`.

### Problem: Leg 2 fails with invalid assertion
**Solution**:
- Verify Leg 1 token is not expired
- Check Leg 1 scopes are correct (`api://AzureADTokenExchange/.default`)
- Ensure Leg 1 completed successfully

### Problem: Certificate mismatch error
**Solution**: Use the **same certificate** in both Leg 1 and Leg 2.

### Problem: Missing WithExperimentalFeatures()
**Solution**: Add `.WithExperimentalFeatures()` to Leg 2 app builder (required for `WithClientAssertion`).

## Comparison: Vanilla vs FIC Two-Leg

| Aspect | Vanilla Flow | FIC Two-Leg Flow |
|--------|--------------|------------------|
| **Steps** | Single call | Two calls (Leg 1 + Leg 2) |
| **Use "legs"** | ❌ No | ✅ Yes |
| **Auth Method** | `WithCertificate()` | Leg 1: `WithCertificate()`, Leg 2: `WithClientAssertion()` |
| **Token Exchange** | No | Yes |
| **client_assertion_type** | N/A | `urn:ietf:params:oauth:client-assertion-type:jwt-pop` |
| **Use Case** | Direct S2S auth | Cross-tenant, delegation |

## Related Skills

- **[Shared Guidance](../msal-mtls-pop-guidance/SKILL.md)** - Terminology and conventions
- **[Vanilla Flow](../msal-mtls-pop-vanilla/SKILL.md)** - For direct acquisition scenarios

## References

- **Test code**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- **RFC 8705**: [OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
- **OAuth Token Exchange**: [RFC 8693](https://datatracker.ietf.org/doc/html/rfc8693)
- **MSAL.NET docs**: [Client Assertions](https://learn.microsoft.com/entra/msal/dotnet/)
