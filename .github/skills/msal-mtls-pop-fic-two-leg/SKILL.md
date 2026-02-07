---
skill_name: msal-mtls-pop-fic-two-leg
version: 1.0.0
description: |
  FIC (Federated Identity Credential) two-leg flow for token exchange using assertions.
  Implements cross-tenant delegation and service-to-service scenarios with jwt-pop binding.
applies_to:
  - .NET
  - C#
  - MSAL.NET
  - OAuth 2.0
  - mTLS
  - Federated Identity
tags:
  - authentication
  - mtls
  - proof-of-possession
  - token-exchange
  - federation
  - jwt-pop
  - assertion
---

# MSAL.NET FIC Two-Leg mTLS PoP Flow

This skill guides you through **token exchange flows using assertions** for cross-tenant scenarios, delegation, or federated identity credential (FIC) patterns.

## When to Use This Flow

- Cross-tenant access (Service A in Tenant A needs to call Resource B in Tenant B)
- Service-to-service delegation using assertions
- Federated identity credentials requiring token exchange
- Scenarios where you acquire a token, then exchange it for another token to access a different resource

**Not for**: Direct resource access (use `msal-mtls-pop-vanilla` instead).

## Flow Overview

```
┌─────────────────────────────────────────────────────────────┐
│ Leg 1: Token Exchange Acquisition                           │
│ ────────────────────────────────────                        │
│ • Build app with WithCertificate(cert, sendX5C: true)       │
│ • Acquire token for "api://AzureADTokenExchange/.default"   │
│ • Result: JWT token to use as assertion in Leg 2            │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Leg 2: Assertion Exchange                                   │
│ ─────────────────────────                                   │
│ • Build app with WithClientAssertion() (NO WithCertificate) │
│ • Assertion provider returns ClientSignedAssertion:         │
│   - Assertion = Leg 1 token                                 │
│   - TokenBindingCertificate = cert for jwt-pop              │
│ • Acquire token for target resource                         │
│ • MSAL sends client_assertion_type=jwt-pop automatically    │
│ • Result: PoP token for final resource                      │
└─────────────────────────────────────────────────────────────┘
```

## Prerequisites

- Certificate with private key (PFX/P12 or from cert store)
- App registration with certificate credential and FIC configured
- Understanding of OAuth 2.0 token exchange flows (RFC 8693)

## Step-by-Step Implementation

### Leg 1: Token Exchange Acquisition

Acquire a token that will be used as an assertion in Leg 2.

```csharp
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

// 1. Load certificate
X509Certificate2 cert = new X509Certificate2("cert.pfx", "password");

// 2. Build first app with certificate
IConfidentialClientApplication leg1App = ConfidentialClientApplicationBuilder
    .Create("YOUR_CLIENT_ID")
    .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
    .WithCertificate(cert, sendX5C: true)
    .WithAzureRegion("westus3")  // Optional: regional endpoint
    .Build();

// 3. Acquire token for token exchange
string[] leg1Scopes = new[] { "api://AzureADTokenExchange/.default" };

AuthenticationResult leg1Result = await leg1App
    .AcquireTokenForClient(leg1Scopes)
    .WithMtlsProofOfPossession()  // Optional: PoP for Leg 1
    .ExecuteAsync()
    .ConfigureAwait(false);

string assertionJwt = leg1Result.AccessToken;  // Save for Leg 2
```

**Key points**:
- Scope must be `"api://AzureADTokenExchange/.default"` for token exchange
- `WithMtlsProofOfPossession()` is optional for Leg 1 (depends on tenant config)
- Returned token will be forwarded as `client_assertion` in Leg 2

### Leg 2: Assertion Exchange

Use the Leg 1 token as an assertion to acquire a token for the target resource.

```csharp
using Microsoft.Identity.Client.Extensibility;

// 1. Build second app with assertion provider (NO WithCertificate!)
IConfidentialClientApplication leg2App = ConfidentialClientApplicationBuilder
    .Create("YOUR_CLIENT_ID")
    .WithExperimentalFeatures()  // Required for WithClientAssertion
    .WithAuthority("https://login.microsoftonline.com/YOUR_TENANT_ID")
    .WithAzureRegion("westus3")  // Optional: regional endpoint
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
    {
        // Return assertion with certificate binding
        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = assertionJwt,            // Leg 1 token
            TokenBindingCertificate = cert       // Same cert for jwt-pop
        });
    })
    .Build();

// 2. Acquire token for target resource
string[] leg2Scopes = new[] { "https://vault.azure.net/.default" };

AuthenticationResult leg2Result = await leg2App
    .AcquireTokenForClient(leg2Scopes)
    .WithMtlsProofOfPossession()  // Request PoP for final token
    .ExecuteAsync()
    .ConfigureAwait(false);

Console.WriteLine($"Token Type: {leg2Result.TokenType}");  // "mtls_pop"
Console.WriteLine($"Access Token: {leg2Result.AccessToken}");
```

**Key points**:
- **Do NOT** use `.WithCertificate()` in Leg 2 app builder
- Certificate is provided via `TokenBindingCertificate` in `ClientSignedAssertion`
- MSAL automatically sets `client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-pop`
- Final token is bound to the certificate for mTLS PoP

## Production Helper Classes

Copy these classes into your project for production-ready implementations.

### FicAssertionProvider.cs
Encapsulates assertion creation logic:

```csharp
// See: .github/skills/msal-mtls-pop-fic-two-leg/FicAssertionProvider.cs
FicAssertionProvider provider = new FicAssertionProvider(assertionJwt, cert);

ClientSignedAssertion assertion = await provider.GetAssertionAsync(
    tokenEndpoint: "https://login.microsoftonline.com/.../oauth2/v2.0/token",
    cancellationToken: CancellationToken.None
);
```

### FicLeg1Acquirer.cs
Simplifies Leg 1 token acquisition:

```csharp
// See: .github/skills/msal-mtls-pop-fic-two-leg/FicLeg1Acquirer.cs
FicLeg1Acquirer leg1 = new FicLeg1Acquirer(
    clientId: "YOUR_CLIENT_ID",
    tenantId: "YOUR_TENANT_ID",
    certificate: cert
);

AuthenticationResult leg1Result = await leg1.AcquireTokenExchangeTokenAsync(
    cancellationToken: CancellationToken.None
);
```

### FicLeg2Exchanger.cs
Simplifies Leg 2 assertion exchange:

```csharp
// See: .github/skills/msal-mtls-pop-fic-two-leg/FicLeg2Exchanger.cs
FicLeg2Exchanger leg2 = new FicLeg2Exchanger(
    clientId: "YOUR_CLIENT_ID",
    tenantId: "YOUR_TENANT_ID",
    assertionJwt: leg1Result.AccessToken,
    bindingCertificate: cert
);

AuthenticationResult leg2Result = await leg2.AcquireTokenAsync(
    scopes: new[] { "https://vault.azure.net/.default" },
    cancellationToken: CancellationToken.None
);
```

## Complete Example

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

public class FicTwoLegExample
{
    public static async Task Main()
    {
        X509Certificate2 cert = new X509Certificate2("cert.pfx", "password");

        // Leg 1: Acquire token exchange token
        FicLeg1Acquirer leg1 = new FicLeg1Acquirer(
            clientId: "YOUR_CLIENT_ID",
            tenantId: "YOUR_TENANT_ID",
            certificate: cert,
            azureRegion: "westus3"
        );

        AuthenticationResult leg1Result = await leg1.AcquireTokenExchangeTokenAsync(
            CancellationToken.None
        );

        Console.WriteLine($"Leg 1 complete. Token: {leg1Result.AccessToken.Substring(0, 20)}...");

        // Leg 2: Exchange assertion for target resource token
        FicLeg2Exchanger leg2 = new FicLeg2Exchanger(
            clientId: "YOUR_CLIENT_ID",
            tenantId: "YOUR_TENANT_ID",
            assertionJwt: leg1Result.AccessToken,
            bindingCertificate: cert,
            azureRegion: "westus3"
        );

        AuthenticationResult leg2Result = await leg2.AcquireTokenAsync(
            scopes: new[] { "https://vault.azure.net/.default" },
            cancellationToken: CancellationToken.None
        );

        Console.WriteLine($"Leg 2 complete. Token Type: {leg2Result.TokenType}");
        Console.WriteLine($"Expires: {leg2Result.ExpiresOn}");

        // Call target resource
        ResourceCaller caller = new ResourceCaller(cert);
        string response = await caller.CallResourceAsync(
            "https://your-keyvault.vault.azure.net/secrets/my-secret?api-version=7.4",
            leg2Result.AccessToken,
            CancellationToken.None
        );

        Console.WriteLine($"Secret retrieved: {response}");
    }
}
```

## Verifying jwt-pop Client Assertion Type

To verify MSAL is sending the correct `client_assertion_type`:

```csharp
bool sawJwtPop = false;

AuthenticationResult leg2Result = await leg2App
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .OnBeforeTokenRequest(data =>
    {
        if (data.BodyParameters != null &&
            data.BodyParameters.TryGetValue("client_assertion_type", out string assertionType))
        {
            sawJwtPop = (assertionType == "urn:ietf:params:oauth:client-assertion-type:jwt-pop");
        }
        return Task.CompletedTask;
    })
    .ExecuteAsync()
    .ConfigureAwait(false);

Console.WriteLine($"Used jwt-pop: {sawJwtPop}");
```

## Error Handling

```csharp
try
{
    // Leg 1
    AuthenticationResult leg1Result = await leg1App
        .AcquireTokenForClient(leg1Scopes)
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);

    // Leg 2
    AuthenticationResult leg2Result = await leg2App
        .AcquireTokenForClient(leg2Scopes)
        .WithMtlsProofOfPossession()
        .ExecuteAsync()
        .ConfigureAwait(false);
}
catch (MsalServiceException ex) when (ex.ErrorCode == "invalid_grant")
{
    Console.WriteLine("Assertion is invalid, expired, or audience mismatch.");
}
catch (MsalServiceException ex) when (ex.ErrorCode == "unauthorized_client")
{
    Console.WriteLine("App not authorized for token exchange. Check FIC configuration.");
}
catch (MsalClientException ex)
{
    Console.WriteLine($"Client error: {ex.ErrorCode} - {ex.Message}");
}
```

## Testing Your Implementation

Validate your code against the working test:

**Test reference**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`  
→ `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178)

Key assertions to verify:
- Leg 1 returns valid JWT token
- Leg 2 assertion provider is called with `AssertionRequestOptions.TokenEndpoint`
- Leg 2 token request includes `client_assertion_type=jwt-pop`
- Final token is for correct scopes (e.g., Key Vault)
- Request URI contains `mtlsauth.microsoft.com` (if using regional endpoints)

## Common Mistakes

### Mistake 1: Using `.WithCertificate()` in Leg 2
```csharp
// WRONG: Don't use WithCertificate in Leg 2
IConfidentialClientApplication leg2App = builder
    .WithCertificate(cert, sendX5C: true)  // ❌ Remove this
    .WithClientAssertion(...)
    .Build();

// RIGHT: Certificate via TokenBindingCertificate only
IConfidentialClientApplication leg2App = builder
    .WithClientAssertion(...)  // ✅ Cert in assertion
    .Build();
```

### Mistake 2: Wrong Leg 1 Scope
```csharp
// WRONG: Using final resource scope in Leg 1
string[] leg1Scopes = new[] { "https://vault.azure.net/.default" };

// RIGHT: Token exchange scope
string[] leg1Scopes = new[] { "api://AzureADTokenExchange/.default" };
```

### Mistake 3: Not Setting `TokenBindingCertificate`
```csharp
// WRONG: No certificate binding
return Task.FromResult(new ClientSignedAssertion
{
    Assertion = assertionJwt  // ❌ Missing TokenBindingCertificate
});

// RIGHT: Bind assertion to certificate
return Task.FromResult(new ClientSignedAssertion
{
    Assertion = assertionJwt,
    TokenBindingCertificate = cert  // ✅ Enables jwt-pop
});
```

### Mistake 4: Forgetting `.WithExperimentalFeatures()`
```csharp
// WRONG: Missing experimental features flag
IConfidentialClientApplication leg2App = builder
    .WithClientAssertion(...)  // ❌ Requires experimental features
    .Build();

// RIGHT: Enable experimental features
IConfidentialClientApplication leg2App = builder
    .WithExperimentalFeatures()  // ✅ Required for WithClientAssertion
    .WithClientAssertion(...)
    .Build();
```

## Troubleshooting

**Problem**: `invalid_grant` error in Leg 2  
**Solution**: Check that Leg 1 token is valid, not expired, and has correct audience

**Problem**: `unauthorized_client` error  
**Solution**: Verify FIC configuration in Azure AD, ensure app has token exchange permissions

**Problem**: Token type is `"Bearer"` instead of `"mtls_pop"` in Leg 2  
**Solution**: Ensure `TokenBindingCertificate` is set in `ClientSignedAssertion` and `.WithMtlsProofOfPossession()` is called

**Problem**: Assertion provider not called  
**Solution**: Check that `.WithExperimentalFeatures()` is present and `.WithClientAssertion()` is correctly configured

## Related Skills

- **msal-mtls-pop-guidance**: Core terminology and conventions
- **msal-mtls-pop-vanilla**: Direct PoP token acquisition (simpler, no exchange)

## Additional Resources

- [RFC 8693: OAuth 2.0 Token Exchange](https://datatracker.ietf.org/doc/html/rfc8693)
- [RFC 8705: OAuth 2.0 Mutual-TLS](https://datatracker.ietf.org/doc/html/rfc8705)
- [Azure AD Federated Identity Credentials](https://docs.microsoft.com/azure/active-directory/develop/workload-identity-federation)
- [MSAL.NET Client Assertion Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki)
