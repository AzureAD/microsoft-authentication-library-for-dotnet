---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0.0
description: FIC (Federated Identity Credential) two-leg token exchange flow with mTLS PoP support for MSI and Confidential Client
applies_to:
  - MSAL.NET
  - Microsoft.Identity.Client
tags:
  - mTLS
  - PoP
  - Proof-of-Possession
  - Token-Exchange
  - FIC
  - Two-Leg
  - Assertion
  - Managed-Identity
  - Confidential-Client
---

# MSAL.NET FIC Two-Leg mTLS PoP Flow

This skill guides developers through the Federated Identity Credential (FIC) two-leg token exchange pattern with mTLS Proof-of-Possession support.

## What is the FIC Two-Leg Flow?

The **FIC two-leg flow** is a token exchange pattern where:
1. **Leg 1**: Acquire a token for `api://AzureADTokenExchange` with mTLS PoP
   - Can use: Managed Identity (MSI) OR Confidential Client
2. **Leg 2**: Exchange Leg 1's token for a final resource token using `WithClientAssertion`
   - **ALWAYS Confidential Client** (MSI does NOT have WithClientAssertion API)
   - Final token can be: Bearer OR mTLS PoP (reuses Leg 1's BindingCertificate)

**Use two-leg flow when:**
- You need token exchange via assertion-based authentication
- You want to chain authentication contexts
- You're implementing federated identity scenarios

## Critical Constraint: MSI and Leg 2

**⚠️ IMPORTANT**: MSI does NOT have the `WithClientAssertion()` API. Only Confidential Client can perform Leg 2.

**Valid Leg Combinations:**
1. ✅ MSI Leg 1 → Confidential Client Leg 2 (Bearer or PoP)
2. ✅ Confidential Client Leg 1 → Confidential Client Leg 2 (Bearer or PoP)

**Invalid:**
- ❌ MSI Leg 2 (MSI lacks WithClientAssertion API)

## Quick Start: All Four Valid Scenarios

### Scenario 1: MSI Leg 1 PoP → Confidential Client Leg 2 → Bearer

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

// Leg 1: MSI acquires PoP token for token exchange endpoint
IManagedIdentityApplication msiApp = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

AuthenticationResult leg1 = await msiApp
    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: Confidential Client exchanges Leg 1 token for final Bearer token
X509Certificate2 confClientCert = /* load certificate */;

IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/tenant-id")
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1.AccessToken  // Use Leg 1 token as assertion
            // NO TokenBindingCertificate - final token will be Bearer
        });
    })
    .Build();

AuthenticationResult leg2 = await confidentialApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .ExecuteAsync();

Console.WriteLine($"Final Token Type: {leg2.TokenType}");  // "Bearer"
```

### Scenario 2: MSI Leg 1 PoP → Confidential Client Leg 2 → mTLS PoP

```csharp
// Leg 1: Same as Scenario 1
AuthenticationResult leg1 = await msiApp
    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: Confidential Client with TokenBindingCertificate for mTLS PoP
IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/tenant-id")
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1.AccessToken,
            TokenBindingCertificate = leg1.BindingCertificate  // Bind Leg 1's cert
        });
    })
    .Build();

AuthenticationResult leg2 = await confidentialApp
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()  // Enable PoP in final token
    .ExecuteAsync();

Console.WriteLine($"Final Token Type: {leg2.TokenType}");  // "pop"
Console.WriteLine($"Bound Certificate: {leg2.BindingCertificate.Thumbprint}");
```

### Scenario 3: Confidential Client Leg 1 PoP → Leg 2 → Bearer

```csharp
// Leg 1: Confidential Client acquires PoP token for token exchange
X509Certificate2 cert = /* load certificate */;

IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder
    .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")  // Allow-listed app ID
    .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
    .WithAzureRegion("westus3")
    .WithCertificate(cert, sendX5C: true)
    .Build();

AuthenticationResult leg1 = await leg1App
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: Same client exchanges for Bearer token
IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder
    .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")
    .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1.AccessToken
        });
    })
    .Build();

AuthenticationResult leg2 = await leg2App
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .ExecuteAsync();

Console.WriteLine($"Final Token Type: {leg2.TokenType}");  // "Bearer"
```

### Scenario 4: Confidential Client Leg 1 PoP → Leg 2 → mTLS PoP

```csharp
// Leg 1: Same as Scenario 3
AuthenticationResult leg1 = await leg1App
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Leg 2: With TokenBindingCertificate for mTLS PoP final token
IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder
    .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")
    .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = leg1.AccessToken,
            TokenBindingCertificate = leg1.BindingCertificate  // Bind for PoP
        });
    })
    .Build();

AuthenticationResult leg2 = await leg2App
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Console.WriteLine($"Final Token Type: {leg2.TokenType}");  // "pop"
```

## Understanding ClientSignedAssertion

The `ClientSignedAssertion` returned by the `WithClientAssertion` callback controls Leg 2 behavior:

```csharp
new ClientSignedAssertion
{
    Assertion = /* Leg 1's AccessToken */,
    TokenBindingCertificate = /* Optional: Leg 1's BindingCertificate for PoP */
}
```

**Fields:**
- `Assertion`: The token from Leg 1 (sent as `client_assertion` parameter)
- `TokenBindingCertificate`: Optional certificate for binding (required for mTLS PoP final token)

**Token Type Determination:**
- **Bearer**: Omit `TokenBindingCertificate` OR omit `.WithMtlsProofOfPossession()` in Leg 2
- **mTLS PoP**: Include `TokenBindingCertificate` AND call `.WithMtlsProofOfPossession()` in Leg 2

## Client Assertion Type Selection

MSAL.NET automatically selects the `client_assertion_type` parameter:

| Leg 2 Configuration | client_assertion_type |
|---------------------|----------------------|
| No TokenBindingCertificate | `urn:ietf:params:oauth:client-assertion-type:jwt-bearer` |
| With TokenBindingCertificate + WithMtlsProofOfPossession | `urn:ietf:params:oauth:client-assertion-type:jwt-pop` |

## Production Helper Classes

This skill includes production-ready helper classes:

### FicAssertionProvider.cs
Builds `ClientSignedAssertion` from Leg 1 result with optional certificate binding.

**Usage:**
```csharp
var provider = new FicAssertionProvider(leg1Result);
ClientSignedAssertion assertion = provider.CreateAssertion(includeBindingCertificate: true);
```

### FicLeg1Acquirer.cs
Handles Leg 1 token acquisition for both MSI and Confidential Client.

**Usage (MSI):**
```csharp
using var leg1 = FicLeg1Acquirer.CreateForManagedIdentity(ManagedIdentityId.SystemAssigned);
AuthenticationResult leg1Result = await leg1.AcquireTokenAsync();
```

**Usage (Confidential Client):**
```csharp
using var leg1 = FicLeg1Acquirer.CreateForConfidentialClient(
    clientId: "your-client-id",
    authority: "https://login.microsoftonline.com/tenant-id",
    certificate: cert,
    region: "westus3");
AuthenticationResult leg1Result = await leg1.AcquireTokenAsync();
```

### FicLeg2Exchanger.cs
Handles Leg 2 exchange with Confidential Client, supporting both Bearer and mTLS PoP output.

**Usage (Bearer):**
```csharp
using var leg2 = new FicLeg2Exchanger(
    clientId: "your-client-id",
    authority: "https://login.microsoftonline.com/tenant-id",
    region: "westus3",
    leg1Result: leg1Result);

AuthenticationResult leg2Result = await leg2.ExchangeForBearerAsync(
    new[] { "https://vault.azure.net/.default" });
```

**Usage (mTLS PoP):**
```csharp
using var leg2 = new FicLeg2Exchanger(
    clientId: "your-client-id",
    authority: "https://login.microsoftonline.com/tenant-id",
    region: "westus3",
    leg1Result: leg1Result);

AuthenticationResult leg2Result = await leg2.ExchangeForMtlsPopAsync(
    new[] { "https://vault.azure.net/.default" });
```

### ResourceCaller.cs
Calls protected resources with mTLS PoP tokens (same as vanilla skill).

**Usage:**
```csharp
using var caller = new ResourceCaller(leg2Result);
string response = await caller.CallResourceAsync("https://vault.azure.net/secrets/my-secret?api-version=7.4");
```

## Complete End-to-End Example

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using MsalMtlsPopHelpers;

// Step 1: Leg 1 - MSI acquires PoP token for token exchange
using var leg1Acquirer = FicLeg1Acquirer.CreateForManagedIdentity(
    ManagedIdentityId.SystemAssigned);
AuthenticationResult leg1 = await leg1Acquirer.AcquireTokenAsync();

Console.WriteLine($"Leg 1 Token Type: {leg1.TokenType}");  // "pop"
Console.WriteLine($"Leg 1 Binding Cert: {leg1.BindingCertificate.Thumbprint}");

// Step 2: Leg 2 - Confidential Client exchanges for mTLS PoP token
using var leg2Exchanger = new FicLeg2Exchanger(
    clientId: "your-client-id",
    authority: "https://login.microsoftonline.com/tenant-id",
    region: "westus3",
    leg1Result: leg1);

AuthenticationResult leg2 = await leg2Exchanger.ExchangeForMtlsPopAsync(
    new[] { "https://vault.azure.net/.default" });

Console.WriteLine($"Leg 2 Token Type: {leg2.TokenType}");  // "pop"
Console.WriteLine($"Leg 2 Binding Cert: {leg2.BindingCertificate.Thumbprint}");

// Step 3: Call protected resource with Leg 2 token
using var resourceCaller = new ResourceCaller(leg2);
string secretValue = await resourceCaller.CallResourceAsync(
    "https://vault.azure.net/secrets/my-secret?api-version=7.4");

Console.WriteLine($"Secret Value: {secretValue}");
```

## Key Differences from Vanilla Flow

| Aspect | Vanilla Flow | FIC Two-Leg Flow |
|--------|-------------|------------------|
| **Steps** | Single acquisition | Two sequential acquisitions |
| **Target (Leg 1)** | Final resource directly | `api://AzureADTokenExchange` |
| **Leg 1 Auth** | MSI or Confidential Client | MSI or Confidential Client |
| **Leg 2 Auth** | N/A | **ALWAYS Confidential Client** |
| **API Used (Leg 2)** | N/A | `WithClientAssertion()` |
| **Certificate Binding** | Automatic | Manual via TokenBindingCertificate |
| **Use Case** | Direct resource access | Token exchange, federated identity |

## Common Patterns

### Caching Between Legs

MSAL caches tokens automatically, but each leg maintains separate cache entries:

```csharp
// Leg 1 - first call acquires, second call uses cache
var leg1a = await msiApp.AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession().ExecuteAsync();
// leg1a.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider

var leg1b = await msiApp.AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
    .WithMtlsProofOfPossession().ExecuteAsync();
// leg1b.AuthenticationResultMetadata.TokenSource == TokenSource.Cache
```

### Error Handling

```csharp
try
{
    // Leg 1
    AuthenticationResult leg1 = await leg1App.AcquireTokenFor[...]
        .WithMtlsProofOfPossession()
        .ExecuteAsync();

    // Leg 2
    AuthenticationResult leg2 = await leg2App.AcquireTokenForClient(scopes)
        .WithClientAssertion(...)
        .WithMtlsProofOfPossession()
        .ExecuteAsync();
}
catch (MsalServiceException ex) when (ex.ErrorCode == "invalid_grant")
{
    Console.WriteLine($"Leg 1 token rejected by Leg 2: {ex.Message}");
}
catch (MsalServiceException ex)
{
    Console.WriteLine($"Token acquisition failed: {ex.ErrorCode} - {ex.Message}");
}
```

## Testing Reference

See `ClientCredentialsMtlsPopTests.cs`, method `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178) for a working two-leg example with Confidential Client for both legs.

## Best Practices

1. **Reuse Leg 1's BindingCertificate** for Leg 2 when requesting mTLS PoP final token
2. **Validate TokenBindingCertificate** is set in ClientSignedAssertion for PoP scenarios
3. **Check client_assertion_type** in token request (use `OnBeforeTokenRequest` for debugging)
4. **Handle token expiration** - Leg 1 token may expire before Leg 2 exchange
5. **Never attempt MSI Leg 2** - MSI lacks WithClientAssertion API

## Related Skills

- **msal-mtls-pop-guidance**: Terminology and conventions
- **msal-mtls-pop-vanilla**: Direct token acquisition patterns
