---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0.0
description: FIC two-leg token exchange using mTLS PoP with MSI or Confidential Client for both legs
applies_to:
  - "**/*msal*.cs"
  - "**/*mtls*.cs"
  - "**/*pop*.cs"
  - "**/*assertion*.cs"
  - "**/*token*exchange*.cs"
tags:
  - msal
  - mtls
  - proof-of-possession
  - fic
  - two-leg
  - token-exchange
  - managed-identity
  - confidential-client
  - assertion
---

# MSAL.NET FIC Two-Leg mTLS PoP Flow

This skill covers **Federated Identity Credential (FIC) two-leg** token exchange using mTLS Proof-of-Possession with either Managed Identity (MSI) or Confidential Client for both legs.

## What Is FIC Two-Leg Flow?

FIC two-leg flow is an **assertion-based token exchange** pattern:

**Leg 1**: Acquire token for `api://AzureADTokenExchange` with mTLS PoP
- Targets the special Azure AD Token Exchange endpoint
- Uses MSI or Confidential Client authentication
- Results in a token bound to a certificate (`BindingCertificate`)

**Leg 2**: Exchange Leg 1 token as assertion for final token
- Uses MSI or Confidential Client authentication (can differ from Leg 1)
- Provides Leg 1 token via `ClientSignedAssertion`
- Final token can be Bearer OR mTLS PoP
- If mTLS PoP: uses Leg 1's `BindingCertificate` for resource TLS handshake

## When to Use FIC Two-Leg Flow

Use FIC two-leg when:
- You need cross-tenant token exchange or delegation
- Your app uses Federated Identity Credentials
- You're implementing workload identity scenarios
- You need to exchange one token for another with different scopes/audiences

## All Supported Combinations

### Leg 1: MSI or Confidential Client
### Leg 2: MSI or Confidential Client
### Final Token: Bearer or mTLS PoP

1. **MSI Leg 1 → MSI Leg 2 → Bearer**
2. **MSI Leg 1 → MSI Leg 2 → mTLS PoP**
3. **MSI Leg 1 → Confidential Client Leg 2 → Bearer**
4. **MSI Leg 1 → Confidential Client Leg 2 → mTLS PoP**
5. **Confidential Client Leg 1 → MSI Leg 2 → Bearer**
6. **Confidential Client Leg 1 → MSI Leg 2 → mTLS PoP**
7. **Confidential Client Leg 1 → Confidential Client Leg 2 → Bearer**
8. **Confidential Client Leg 1 → Confidential Client Leg 2 → mTLS PoP**

## Leg 1: Token Acquisition

### Leg 1 with MSI

```csharp
var app1 = ManagedIdentityApplicationBuilder.Create()
    .WithUserAssignedManagedIdentity(userAssignedClientId)  // Optional
    .Build();

var leg1Result = await app1
    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Store for Leg 2
string leg1Token = leg1Result.AccessToken;
X509Certificate2 leg1Cert = leg1Result.BindingCertificate;
```

### Leg 1 with Confidential Client

```csharp
X509Certificate2 cert = /* load your SNI certificate */;

var app1 = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .WithCertificate(cert, sendX5c: true)  // sendX5c=true enables SNI
    .WithAzureRegion("westus3")  // Optional: regional endpoint
    .Build();

var leg1Result = await app1
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Store for Leg 2
string leg1Token = leg1Result.AccessToken;
X509Certificate2 leg1Cert = leg1Result.BindingCertificate;
```

## Leg 2: Token Exchange

### Leg 2 with MSI (Bearer)

```csharp
var app2 = ManagedIdentityApplicationBuilder.Create()
    .WithExperimentalFeatures()
    .WithClientAssertion((options, ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Token,              // From Leg 1
            TokenBindingCertificate = leg1Cert  // From Leg 1 (enables jwt-pop)
        });
    })
    .Build();

// Acquire Bearer token (no .WithMtlsProofOfPossession())
var leg2Result = await app2
    .AcquireTokenForManagedIdentity("https://storage.azure.com/.default")
    .ExecuteAsync()
    .ConfigureAwait(false);

// Result: Bearer token (TokenType == "Bearer")
```

### Leg 2 with MSI (mTLS PoP)

```csharp
var app2 = ManagedIdentityApplicationBuilder.Create()
    .WithExperimentalFeatures()
    .WithClientAssertion((options, ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Token,              // From Leg 1
            TokenBindingCertificate = leg1Cert  // From Leg 1 (enables jwt-pop)
        });
    })
    .Build();

// Acquire mTLS PoP token (WITH .WithMtlsProofOfPossession())
var leg2Result = await app2
    .AcquireTokenForManagedIdentity("https://vault.azure.net/.default")
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Result: mTLS PoP token
// BindingCertificate == leg1Cert (use this for resource TLS handshake)
```

### Leg 2 with Confidential Client (Bearer)

```csharp
var app2 = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithExperimentalFeatures()
    .WithAuthority(authority)
    .WithClientAssertion((options, ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Token,              // From Leg 1
            TokenBindingCertificate = leg1Cert  // From Leg 1 (enables jwt-pop)
        });
    })
    .Build();

// Acquire Bearer token (no .WithMtlsProofOfPossession())
var leg2Result = await app2
    .AcquireTokenForClient(new[] { "https://storage.azure.com/.default" })
    .ExecuteAsync()
    .ConfigureAwait(false);

// Result: Bearer token (TokenType == "Bearer")
```

### Leg 2 with Confidential Client (mTLS PoP)

```csharp
var app2 = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithExperimentalFeatures()
    .WithAuthority(authority)
    .WithClientAssertion((options, ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Token,              // From Leg 1
            TokenBindingCertificate = leg1Cert  // From Leg 1 (enables jwt-pop)
        });
    })
    .Build();

// Acquire mTLS PoP token (WITH .WithMtlsProofOfPossession())
var leg2Result = await app2
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Result: mTLS PoP token
// BindingCertificate == leg1Cert (use this for resource TLS handshake)
```

## Critical: BindingCertificate in Leg 2

When Leg 2 uses mTLS PoP, `AuthenticationResult.BindingCertificate` is **ALWAYS** set to Leg 1's certificate:

```csharp
// After Leg 2 with mTLS PoP
Assert.IsNotNull(leg2Result.BindingCertificate);
Assert.AreEqual(leg1Cert.Thumbprint, leg2Result.BindingCertificate.Thumbprint);

// Use BindingCertificate for resource TLS handshake
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(leg2Result.BindingCertificate);
```

## ClientSignedAssertion Details

The `ClientSignedAssertion` structure binds Leg 1's token and certificate:

```csharp
new ClientSignedAssertion
{
    // The Leg 1 access token (passed as client_assertion)
    Assertion = leg1Token,
    
    // The Leg 1 binding certificate (enables jwt-pop client_assertion_type)
    TokenBindingCertificate = leg1Cert
}
```

When `TokenBindingCertificate` is provided:
- MSAL sends `client_assertion_type: urn:ietf:params:oauth:client-assertion-type:jwt-pop`
- Azure AD validates the jwt-pop binding
- If Leg 2 uses `.WithMtlsProofOfPossession()`, final token is mTLS PoP

## Calling the Resource (mTLS PoP)

For Bearer tokens (Leg 2 without `.WithMtlsProofOfPossession()`):
```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", leg2Result.AccessToken);

var response = await httpClient.GetAsync(resourceUrl)
    .ConfigureAwait(false);
```

For mTLS PoP tokens (Leg 2 WITH `.WithMtlsProofOfPossession()`):
```csharp
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(leg2Result.BindingCertificate);  // From Leg 1!

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", leg2Result.AccessToken);

var response = await httpClient.GetAsync(resourceUrl)
    .ConfigureAwait(false);
```

## Helper Classes

This skill includes production-ready helper classes:

### FicAssertionProvider.cs
Builds `ClientSignedAssertion` from Leg 1 results.

**Usage:**
```csharp
var provider = new FicAssertionProvider(leg1Result);
var assertion = await provider.GetAssertionAsync();

// Use in Leg 2 app builder
.WithClientAssertion((options, ct) => Task.FromResult(assertion))
```

### FicLeg1Acquirer.cs
Handles Leg 1 token acquisition for MSI or Confidential Client.

**Usage (MSI):**
```csharp
using var leg1 = new FicLeg1Acquirer(
    ManagedIdentityApplicationBuilder.Create().Build());

var leg1Result = await leg1.AcquireTokenAsync(cancellationToken);
```

**Usage (Confidential Client):**
```csharp
var app = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(cert, sendX5c: true)
    .Build();

using var leg1 = new FicLeg1Acquirer(app);
var leg1Result = await leg1.AcquireTokenAsync(cancellationToken);
```

### FicLeg2Exchanger.cs
Handles Leg 2 token exchange with optional mTLS PoP.

**Usage (MSI, mTLS PoP):**
```csharp
var leg2App = ManagedIdentityApplicationBuilder.Create()
    .WithExperimentalFeatures()
    .WithClientAssertion((opt, ct) => Task.FromResult(assertion))
    .Build();

using var leg2 = new FicLeg2Exchanger(leg2App);
var leg2Result = await leg2.ExchangeTokenAsync(
    "https://vault.azure.net/.default",
    useMtlsPop: true,
    cancellationToken);
```

**Usage (Confidential Client, Bearer):**
```csharp
var leg2App = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithExperimentalFeatures()
    .WithAuthority(authority)
    .WithClientAssertion((opt, ct) => Task.FromResult(assertion))
    .Build();

using var leg2 = new FicLeg2Exchanger(leg2App);
var leg2Result = await leg2.ExchangeTokenAsync(
    new[] { "https://storage.azure.com/.default" },
    useMtlsPop: false,
    cancellationToken);
```

### ResourceCaller.cs
Reuses the pattern from vanilla skill for calling resources.

## Complete Example (Confidential Client Leg 1 + Leg 2 → mTLS PoP)

See `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`:
- Method: `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178)

Key points from the test:

```csharp
// Leg 1: Acquire token for TokenExchange
var app1 = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion("westus3")
    .WithCertificate(cert, sendX5c: true)
    .Build();

var leg1Result = await app1
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

string leg1Token = leg1Result.AccessToken;
X509Certificate2 leg1Cert = leg1Result.BindingCertificate;

// Leg 2: Exchange with jwt-pop binding
var app2 = ConfidentialClientApplicationBuilder.Create(clientId)
    .WithExperimentalFeatures()
    .WithAuthority(authority)
    .WithAzureRegion("westus3")
    .WithClientAssertion((options, ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1Token,
            TokenBindingCertificate = leg1Cert  // Enables jwt-pop
        });
    })
    .Build();

var leg2Result = await app2
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);

// Validate
Assert.IsNotNull(leg2Result);
Assert.AreEqual(Constants.MtlsPoPTokenType, leg2Result.TokenType);
Assert.AreEqual(leg1Cert.Thumbprint, leg2Result.BindingCertificate.Thumbprint);

// Call resource using Leg 2 token + Leg 1 cert
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(leg2Result.BindingCertificate);

using var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", leg2Result.AccessToken);

var response = await httpClient.GetAsync("https://vault.azure.net/...");
```

## Validation Checklist

When implementing FIC two-leg flow:

### Leg 1
- ✅ Targets `api://AzureADTokenExchange` (MSI) or `api://AzureADTokenExchange/.default` (Confidential Client)
- ✅ Uses `.WithMtlsProofOfPossession()`
- ✅ Token type is `Constants.MtlsPoPTokenType`
- ✅ `BindingCertificate` is not null

### Leg 2
- ✅ Uses `.WithExperimentalFeatures()` on app builder
- ✅ Provides `ClientSignedAssertion` with Leg 1 token and certificate
- ✅ `client_assertion_type` is `urn:ietf:params:oauth:client-assertion-type:jwt-pop` (check in OnBeforeTokenRequest)
- ✅ If mTLS PoP: Uses `.WithMtlsProofOfPossession()` and `BindingCertificate` matches Leg 1 cert
- ✅ If Bearer: No `.WithMtlsProofOfPossession()` modifier

### Resource Call
- ✅ For Bearer: Standard HttpClient, no certificate binding
- ✅ For mTLS PoP: HttpClient with `leg2Result.BindingCertificate` in handler

## Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| `invalid_client` | Certificate not SNI-enabled (Leg 1) | Ensure `.WithCertificate(cert, sendX5c: true)` |
| `invalid_grant` | Assertion token invalid/expired | Check Leg 1 token is valid and not expired |
| `unauthorized_client` | App lacks permissions | Grant permissions to target resource |
| jwt-pop not detected | Missing `TokenBindingCertificate` | Ensure `ClientSignedAssertion` includes the certificate |
| BindingCertificate mismatch | Using wrong cert for resource | Always use `leg2Result.BindingCertificate` (from Leg 1) |

## Regional Endpoints

Both legs use `mtlsauth.microsoft.com` for mTLS PoP:

```csharp
// Leg 1 and Leg 2
.WithAzureRegion("westus3")

// Requests go to: https://{region}.mtlsauth.microsoft.com/...
```

## References

- Guidance Skill: `.github/skills/msal-mtls-pop-guidance/SKILL.md`
- Vanilla Skill: `.github/skills/msal-mtls-pop-vanilla/SKILL.md`
- Test Example: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 86-178)
- RFC 7521: OAuth 2.0 Assertion Framework
- RFC 8705: OAuth 2.0 Mutual-TLS Client Authentication and Certificate-Bound Access Tokens

## Version History

- **1.0.0**: Initial FIC two-leg skill with all 8 combinations (MSI/Confidential Client × MSI/Confidential Client × Bearer/mTLS PoP)
