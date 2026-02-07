---
skill_name: "MSAL.NET mTLS PoP - FIC Two-Leg Flow"
version: "1.0"
description: "Token exchange pattern using assertions for cross-tenant and delegation scenarios with mTLS Proof-of-Possession"
applies_to:
  - "**/*assertion*/**"
  - "**/*fic*/**"
  - "**/*token*exchange*/**"
  - "**/ClientCredentialsMtlsPopTests.cs"
tags:
  - "msal"
  - "mtls-pop"
  - "fic"
  - "two-leg"
  - "assertion"
  - "jwt-pop"
  - "token-exchange"
---

# MSAL.NET mTLS PoP - FIC Two-Leg Flow

This skill covers the **FIC two-leg flow** for mTLS Proof-of-Possession token acquisition in MSAL.NET. Use this pattern when your application needs to exchange an assertion token for a target resource token, enabling cross-tenant authentication and delegation scenarios.

## When to Use FIC Two-Leg Flow

Use the FIC two-leg flow when:

✅ **Token exchange required**
- Application uses Federated Identity Credentials (FIC)
- Cross-tenant service-to-service authentication
- Token delegation or service chaining scenarios
- Assertion-based authentication required

✅ **Two-step acquisition needed**
- **Leg 1**: Acquire assertion token for `api://AzureADTokenExchange`
- **Leg 2**: Exchange assertion for target resource token with certificate binding

✅ **jwt-pop client assertion type**
- Client assertion bound to certificate for mTLS
- `client_assertion_type == "urn:ietf:params:oauth:client-assertion-type:jwt-pop"`
- Assertion provider supplies both token and certificate

❌ **Do NOT use FIC two-leg flow when**
- Direct resource access without exchange → Use [Vanilla Flow](../msal-mtls-pop-vanilla/SKILL.md)
- Single-step token acquisition sufficient → Use [Vanilla Flow](../msal-mtls-pop-vanilla/SKILL.md)
- No cross-tenant or delegation requirements → Use [Vanilla Flow](../msal-mtls-pop-vanilla/SKILL.md)

## Flow Diagram

```
Leg 1: Application → MSAL.AcquireTokenForClient() → Azure AD
                          ↓
                  Assertion Token (api://AzureADTokenExchange)
                          ↓
Leg 2: Application → MSAL.AcquireTokenForClient() + WithClientAssertion()
                          ↓
                     Azure AD (validates assertion + certificate)
                          ↓
                  mTLS PoP Token (for target resource)
```

**Key Characteristics**:
- **Two legs**: Assertion acquisition + Token exchange
- **Leg 1 output**: Assertion token (used as credential in Leg 2)
- **Leg 2 binding**: Certificate attached via `TokenBindingCertificate` in assertion provider
- **Result**: mTLS PoP token with jwt-pop client assertion type

## Leg 1: Acquire Assertion Token

The first leg acquires an assertion token that will be used as the client credential in Leg 2.

### Leg 1 with MSI

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class FicTwoLegFlow_Leg1_Msi
    {
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        /// <summary>
        /// Leg 1: Acquire assertion token using Managed System Identity
        /// </summary>
        public static async Task<string> AcquireAssertionTokenViaMsiAsync()
        {
            // MSI configuration for assertion acquisition
            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithAzureRegion("westus3")
                .Build();

            // Acquire token for token exchange endpoint with mTLS PoP
            AuthenticationResult result = await app
                .AcquireTokenForManagedIdentity(new[] { TokenExchangeScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            // Validate assertion token
            ValidateAssertionToken(result);

            Console.WriteLine($"✓ Leg 1 complete: Assertion token acquired");
            Console.WriteLine($"✓ Token type: {result.TokenType}");
            Console.WriteLine($"✓ Scope: {result.Scopes.FirstOrDefault()}");

            return result.AccessToken;
        }

        private static void ValidateAssertionToken(AuthenticationResult result)
        {
            if (result == null)
                throw new InvalidOperationException("Assertion token result is null");

            if (string.IsNullOrEmpty(result.AccessToken))
                throw new InvalidOperationException("Assertion token is null or empty");

            if (!result.Scopes.Contains(TokenExchangeScope))
                throw new InvalidOperationException(
                    $"Assertion token does not contain required scope: {TokenExchangeScope}");
        }
    }
}
```

### Leg 1 with SNI

```csharp
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class FicTwoLegFlow_Leg1_Sni
    {
        private const string AppId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string TenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        /// <summary>
        /// Leg 1: Acquire assertion token using Service Named Identity with certificate
        /// </summary>
        public static async Task<string> AcquireAssertionTokenViaSniAsync(
            X509Certificate2 certificate)
        {
            // SNI configuration for assertion acquisition
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("westus3")
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            // Acquire token for token exchange endpoint with mTLS PoP
            AuthenticationResult result = await app
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            // Validate assertion token
            ValidateAssertionToken(result, certificate);

            Console.WriteLine($"✓ Leg 1 complete: Assertion token acquired");
            Console.WriteLine($"✓ Token type: {result.TokenType}");
            Console.WriteLine($"✓ Scope: {result.Scopes.FirstOrDefault()}");
            Console.WriteLine($"✓ Certificate: {certificate.Thumbprint}");

            return result.AccessToken;
        }

        private static void ValidateAssertionToken(
            AuthenticationResult result,
            X509Certificate2 certificate)
        {
            if (result == null)
                throw new InvalidOperationException("Assertion token result is null");

            if (string.IsNullOrEmpty(result.AccessToken))
                throw new InvalidOperationException("Assertion token is null or empty");

            if (!result.Scopes.Contains(TokenExchangeScope))
                throw new InvalidOperationException(
                    $"Assertion token does not contain required scope: {TokenExchangeScope}");

            // Verify certificate binding in SNI flow
            if (result.BindingCertificate == null)
                throw new InvalidOperationException("BindingCertificate is null in SNI flow");

            if (result.BindingCertificate.Thumbprint != certificate.Thumbprint)
                throw new InvalidOperationException(
                    $"Certificate thumbprint mismatch. Expected: {certificate.Thumbprint}, Got: {result.BindingCertificate.Thumbprint}");
        }
    }
}
```

## Assertion Provider Helper

The assertion provider is called by MSAL during Leg 2 to supply the assertion token and bind it to the certificate for jwt-pop.

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopExamples
{
    public class AssertionProvider
    {
        /// <summary>
        /// Creates an assertion provider that supplies assertion token with certificate binding
        /// for jwt-pop client assertion type
        /// </summary>
        public static Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
            CreateAssertionProvider(string assertionToken, X509Certificate2 certificate)
        {
            if (string.IsNullOrEmpty(assertionToken))
                throw new ArgumentException("Assertion token cannot be null or empty", nameof(assertionToken));

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate), "Certificate is required for jwt-pop binding");

            return async (AssertionRequestOptions options, CancellationToken cancellationToken) =>
            {
                // Log token endpoint for diagnostics (optional)
                if (!string.IsNullOrEmpty(options.TokenEndpoint))
                {
                    Console.WriteLine($"Assertion provider called for: {options.TokenEndpoint}");
                }

                // Return assertion with certificate binding for jwt-pop
                return new ClientSignedAssertion
                {
                    Assertion = assertionToken,              // From Leg 1
                    TokenBindingCertificate = certificate     // Binds for jwt-pop
                };
            };
        }

        /// <summary>
        /// Creates an assertion provider with callback for monitoring
        /// </summary>
        public static Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
            CreateAssertionProviderWithMonitoring(
                string assertionToken,
                X509Certificate2 certificate,
                Action<string> onProviderCalled = null)
        {
            if (string.IsNullOrEmpty(assertionToken))
                throw new ArgumentException("Assertion token cannot be null or empty", nameof(assertionToken));

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate), "Certificate is required for jwt-pop binding");

            return async (AssertionRequestOptions options, CancellationToken cancellationToken) =>
            {
                // Notify caller that provider was invoked
                onProviderCalled?.Invoke(options.TokenEndpoint ?? "unknown");

                // Return assertion with certificate binding
                return new ClientSignedAssertion
                {
                    Assertion = assertionToken,
                    TokenBindingCertificate = certificate
                };
            };
        }
    }
}
```

## Leg 2: Exchange for Target Resource Token

The second leg exchanges the assertion token (from Leg 1) for a target resource token using the assertion provider.

### Leg 2 with SNI (Complete Example)

```csharp
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopExamples
{
    public class FicTwoLegFlow_Leg2
    {
        private const string AppId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string TenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";

        /// <summary>
        /// Leg 2: Exchange assertion for target resource token with jwt-pop binding
        /// </summary>
        public static async Task<AuthenticationResult> ExchangeAssertionForResourceTokenAsync(
            string assertionToken,
            X509Certificate2 certificate,
            string targetResourceScope)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(assertionToken))
                throw new ArgumentException("Assertion token is required", nameof(assertionToken));

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            // Build app with assertion provider (NO WithCertificate - cert comes from assertion)
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithExperimentalFeatures()  // Required for WithClientAssertion
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("westus3")
                .WithClientAssertion(AssertionProvider.CreateAssertionProvider(assertionToken, certificate))
                .Build();

            // Acquire token for target resource with mTLS PoP
            AuthenticationResult result = await app
                .AcquireTokenForClient(new[] { targetResourceScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            // Validate target resource token
            ValidateTargetResourceToken(result, targetResourceScope);

            Console.WriteLine($"✓ Leg 2 complete: Target resource token acquired");
            Console.WriteLine($"✓ Token type: {result.TokenType}");
            Console.WriteLine($"✓ Scope: {result.Scopes.FirstOrDefault()}");

            return result;
        }

        /// <summary>
        /// Leg 2 for Key Vault resource
        /// </summary>
        public static async Task<AuthenticationResult> ExchangeForKeyVaultTokenAsync(
            string assertionToken,
            X509Certificate2 certificate)
        {
            return await ExchangeAssertionForResourceTokenAsync(
                assertionToken,
                certificate,
                "https://vault.azure.net/.default");
        }

        /// <summary>
        /// Leg 2 for Microsoft Graph resource
        /// </summary>
        public static async Task<AuthenticationResult> ExchangeForGraphTokenAsync(
            string assertionToken,
            X509Certificate2 certificate)
        {
            return await ExchangeAssertionForResourceTokenAsync(
                assertionToken,
                certificate,
                "https://graph.microsoft.com/.default");
        }

        /// <summary>
        /// Leg 2 for custom API resource
        /// </summary>
        public static async Task<AuthenticationResult> ExchangeForCustomApiTokenAsync(
            string assertionToken,
            X509Certificate2 certificate,
            string customApiScope)
        {
            return await ExchangeAssertionForResourceTokenAsync(
                assertionToken,
                certificate,
                customApiScope);
        }

        private static void ValidateTargetResourceToken(
            AuthenticationResult result,
            string expectedScope)
        {
            if (result == null)
                throw new InvalidOperationException("Target resource token result is null");

            if (result.TokenType != "mtls_pop")
                throw new InvalidOperationException(
                    $"Expected token type 'mtls_pop', got '{result.TokenType}'");

            if (string.IsNullOrEmpty(result.AccessToken))
                throw new InvalidOperationException("Target resource token is null or empty");

            if (!result.Scopes.Contains(expectedScope))
                throw new InvalidOperationException(
                    $"Token does not contain expected scope: {expectedScope}");
        }
    }
}
```

### Leg 2 with Monitoring (Advanced)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopExamples
{
    public class FicTwoLegFlow_Leg2_WithMonitoring
    {
        private const string AppId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string TenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";

        /// <summary>
        /// Leg 2 with detailed monitoring of jwt-pop parameters
        /// </summary>
        public static async Task<(AuthenticationResult Result, JwtPopMetadata Metadata)>
            ExchangeWithMonitoringAsync(
                string assertionToken,
                X509Certificate2 certificate,
                string targetResourceScope)
        {
            var metadata = new JwtPopMetadata();

            // Build app with monitored assertion provider
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithExperimentalFeatures()
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("westus3")
                .WithClientAssertion(AssertionProvider.CreateAssertionProviderWithMonitoring(
                    assertionToken,
                    certificate,
                    tokenEndpoint => metadata.TokenEndpoint = tokenEndpoint))
                .Build();

            // Acquire token with request monitoring
            AuthenticationResult result = await app
                .AcquireTokenForClient(new[] { targetResourceScope })
                .WithMtlsProofOfPossession()
                .OnBeforeTokenRequest(data =>
                {
                    metadata.RequestUri = data.RequestUri?.ToString();

                    if (data.BodyParameters != null)
                    {
                        metadata.HasClientAssertion = data.BodyParameters.ContainsKey("client_assertion");
                        metadata.HasClientAssertionType = data.BodyParameters.ContainsKey("client_assertion_type");

                        if (data.BodyParameters.TryGetValue("client_assertion_type", out string assertionType))
                        {
                            metadata.ClientAssertionType = assertionType;
                        }
                    }

                    return Task.CompletedTask;
                })
                .ExecuteAsync();

            // Validate jwt-pop configuration
            ValidateJwtPopConfiguration(metadata);

            return (result, metadata);
        }

        private static void ValidateJwtPopConfiguration(JwtPopMetadata metadata)
        {
            Console.WriteLine("\n=== JWT-PoP Configuration Validation ===");

            if (metadata.HasClientAssertion)
                Console.WriteLine("✓ client_assertion parameter present");
            else
                Console.WriteLine("✗ client_assertion parameter MISSING");

            if (metadata.HasClientAssertionType)
                Console.WriteLine("✓ client_assertion_type parameter present");
            else
                Console.WriteLine("✗ client_assertion_type parameter MISSING");

            const string expectedJwtPopType = "urn:ietf:params:oauth:client-assertion-type:jwt-pop";
            if (metadata.ClientAssertionType == expectedJwtPopType)
                Console.WriteLine($"✓ client_assertion_type is jwt-pop: {metadata.ClientAssertionType}");
            else
                Console.WriteLine($"✗ Expected jwt-pop, got: {metadata.ClientAssertionType}");

            if (metadata.RequestUri?.Contains("mtlsauth.microsoft.com") == true)
                Console.WriteLine($"✓ Regional mTLS endpoint used: {metadata.RequestUri}");
            else
                Console.WriteLine($"⚠ Non-mTLS endpoint: {metadata.RequestUri}");

            Console.WriteLine($"Token endpoint: {metadata.TokenEndpoint}");
        }

        public class JwtPopMetadata
        {
            public string TokenEndpoint { get; set; }
            public string RequestUri { get; set; }
            public bool HasClientAssertion { get; set; }
            public bool HasClientAssertionType { get; set; }
            public string ClientAssertionType { get; set; }
        }
    }
}
```

## Complete End-to-End Flow

Combines both legs into a complete FIC two-leg flow implementation.

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class FicTwoLegFlow_Complete
    {
        /// <summary>
        /// Complete FIC two-leg flow: Acquire assertion, then exchange for target resource
        /// </summary>
        public static async Task<AuthenticationResult> AcquireMtlsPopTokenViaFicAsync(
            X509Certificate2 certificate,
            string targetResourceScope)
        {
            Console.WriteLine("=== Starting FIC Two-Leg Flow ===\n");

            // ===== LEG 1: Acquire assertion token =====
            Console.WriteLine("--- Leg 1: Acquiring assertion token ---");
            string assertionToken = await FicTwoLegFlow_Leg1_Sni
                .AcquireAssertionTokenViaSniAsync(certificate);

            if (string.IsNullOrEmpty(assertionToken))
                throw new InvalidOperationException("Leg 1 failed: No assertion token acquired");

            Console.WriteLine($"Leg 1 complete. Assertion length: {assertionToken.Length} characters\n");

            // ===== LEG 2: Exchange assertion for target resource token =====
            Console.WriteLine("--- Leg 2: Exchanging assertion for target resource ---");
            AuthenticationResult targetToken = await FicTwoLegFlow_Leg2
                .ExchangeAssertionForResourceTokenAsync(
                    assertionToken,
                    certificate,
                    targetResourceScope);

            Console.WriteLine($"\n=== FIC Two-Leg Flow Complete ===");
            Console.WriteLine($"Target resource: {targetResourceScope}");
            Console.WriteLine($"Token type: {targetToken.TokenType}");
            Console.WriteLine($"Expires: {targetToken.ExpiresOn}");

            return targetToken;
        }

        /// <summary>
        /// Example: Complete flow for Key Vault
        /// </summary>
        public static async Task<AuthenticationResult> GetKeyVaultTokenViaFicAsync(
            X509Certificate2 certificate)
        {
            return await AcquireMtlsPopTokenViaFicAsync(
                certificate,
                "https://vault.azure.net/.default");
        }

        /// <summary>
        /// Example: Complete flow for Microsoft Graph
        /// </summary>
        public static async Task<AuthenticationResult> GetGraphTokenViaFicAsync(
            X509Certificate2 certificate)
        {
            return await AcquireMtlsPopTokenViaFicAsync(
                certificate,
                "https://graph.microsoft.com/.default");
        }
    }
}
```

## Resource Caller Helper

After completing both legs, use the resulting token to call the target resource. This is identical to the vanilla flow pattern.

```csharp
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class FicResourceCaller
    {
        /// <summary>
        /// Calls a resource API using the mTLS PoP token acquired via FIC two-leg flow
        /// </summary>
        public static async Task<string> CallResourceWithFicTokenAsync(
            AuthenticationResult authResult,
            string resourceUrl,
            X509Certificate2 certificate)
        {
            // Validate mTLS PoP token from FIC flow
            if (authResult.TokenType != "mtls_pop")
                throw new InvalidOperationException(
                    $"Expected mtls_pop token from FIC flow, got {authResult.TokenType}");

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate), "Certificate is required for mTLS");

            // Create HTTP handler with client certificate for mTLS
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);

            using (var httpClient = new HttpClient(handler))
            {
                // Add mTLS PoP token to Authorization header
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                // Call the resource
                HttpResponseMessage response = await httpClient.GetAsync(resourceUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Resource call failed. Status: {response.StatusCode}, Error: {errorContent}");
                }

                string content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"✓ Successfully called resource via FIC token: {resourceUrl}");
                Console.WriteLine($"✓ Response status: {response.StatusCode}");
                
                return content;
            }
        }

        /// <summary>
        /// Complete FIC flow + resource call example
        /// </summary>
        public static async Task<string> FicFlowAndCallResourceAsync(
            X509Certificate2 certificate,
            string targetResourceScope,
            string resourceUrl)
        {
            // Complete FIC two-leg flow
            AuthenticationResult token = await FicTwoLegFlow_Complete
                .AcquireMtlsPopTokenViaFicAsync(certificate, targetResourceScope);

            // Call resource with acquired token
            return await CallResourceWithFicTokenAsync(token, resourceUrl, certificate);
        }
    }
}
```

## Test Reference Implementation

The FIC two-leg flow is tested in:
- **File**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- **Method**: `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync()` (lines 86-178)

### Key Test Patterns

```csharp
[TestMethod]
public async Task Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync()
{
    X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

    // ===== LEG 1: Acquire assertion token =====
    IConfidentialClientApplication firstApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")
        .WithCertificate(cert, true)
        .WithTestLogging()
        .Build();

    AuthenticationResult first = await firstApp
        .AcquireTokenForClient(new[] { TokenExchangeUrl })  // api://AzureADTokenExchange/.default
        .WithMtlsProofOfPossession()
        .ExecuteAsync();

    string assertionJwt = first.AccessToken;
    Assert.IsFalse(string.IsNullOrEmpty(assertionJwt));

    // ===== LEG 2: Exchange assertion for target resource =====
    bool assertionProviderCalled = false;
    string clientAssertionType = null;

    IConfidentialClientApplication assertionApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithExperimentalFeatures()
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")
        .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
        {
            assertionProviderCalled = true;
            return Task.FromResult(new ClientSignedAssertion
            {
                Assertion = assertionJwt,           // From Leg 1
                TokenBindingCertificate = cert      // Binds for jwt-pop
            });
        })
        .WithTestLogging()
        .Build();

    AuthenticationResult second = await assertionApp
        .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
        .WithMtlsProofOfPossession()
        .OnBeforeTokenRequest(data =>
        {
            data.BodyParameters?.TryGetValue("client_assertion_type", out clientAssertionType);
            return Task.CompletedTask;
        })
        .ExecuteAsync();

    // Validate jwt-pop configuration
    Assert.IsTrue(assertionProviderCalled);
    Assert.AreEqual("urn:ietf:params:oauth:client-assertion-type:jwt-pop", clientAssertionType);
    Assert.IsNotNull(second.AccessToken);
}
```

## Verification Checklist

Before submitting FIC two-leg flow code or documentation:

### Leg 1 Validation
- [ ] Targets `api://AzureADTokenExchange/.default` scope
- [ ] Uses `.WithMtlsProofOfPossession()` if mTLS binding required
- [ ] Returns non-empty assertion token
- [ ] Real Azure region string used
- [ ] Certificate properly configured (SNI only)

### Leg 2 Validation
- [ ] Uses `WithClientAssertion()` with assertion provider
- [ ] Assertion provider returns `ClientSignedAssertion`
- [ ] `TokenBindingCertificate` set to certificate for jwt-pop
- [ ] Uses `.WithMtlsProofOfPossession()` at request level
- [ ] NO `WithCertificate()` at app level (cert comes from assertion)
- [ ] `WithExperimentalFeatures()` called before `WithClientAssertion()`

### jwt-pop Validation
- [ ] `client_assertion` parameter present in token request
- [ ] `client_assertion_type` equals `"urn:ietf:params:oauth:client-assertion-type:jwt-pop"`
- [ ] Regional mTLS endpoint used (`mtlsauth.microsoft.com`)
- [ ] Assertion provider callback invoked
- [ ] `TokenEndpoint` provided to assertion provider

### Token Quality
- [ ] Result token type is `"mtls_pop"`
- [ ] Result contains target resource scope
- [ ] Token can successfully call target resource
- [ ] Certificate binding maintained through flow

### Documentation
- [ ] "FIC two-leg" or "two-leg flow" terminology used
- [ ] "Leg 1" and "Leg 2" clearly distinguished
- [ ] "Assertion" used for intermediate token
- [ ] "jwt-pop" mentioned for client assertion type
- [ ] No confusion with vanilla flow (no mixing)

## Common Issues and Solutions

### Issue: Leg 2 Uses WithCertificate
**Symptom**: Double certificate binding or assertion not used  
**Cause**: Both `WithCertificate()` and `WithClientAssertion()` configured  
**Solution**: Use ONLY `WithClientAssertion()` in Leg 2

```csharp
// ❌ Bad: Double certificate configuration
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(appId)
    .WithCertificate(cert, true)           // Wrong: conflicts with assertion
    .WithClientAssertion(assertionProvider) // Assertion should provide cert
    .Build();

// ✅ Good: Certificate only in assertion provider
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(appId)
    .WithExperimentalFeatures()
    .WithClientAssertion(assertionProvider)  // Cert comes from here
    .Build();
```

### Issue: Missing TokenBindingCertificate
**Symptom**: `client_assertion_type` is not jwt-pop  
**Cause**: `TokenBindingCertificate` not set in `ClientSignedAssertion`  
**Solution**: Always include certificate in assertion

```csharp
// ❌ Bad: No certificate binding
.WithClientAssertion((options, ct) =>
{
    return Task.FromResult(new ClientSignedAssertion
    {
        Assertion = assertionToken
        // Missing TokenBindingCertificate
    });
})

// ✅ Good: Certificate binding for jwt-pop
.WithClientAssertion((options, ct) =>
{
    return Task.FromResult(new ClientSignedAssertion
    {
        Assertion = assertionToken,
        TokenBindingCertificate = certificate  // Enables jwt-pop
    });
})
```

### Issue: Wrong Leg 1 Scope
**Symptom**: Leg 2 fails with invalid assertion  
**Cause**: Leg 1 acquired token for wrong scope  
**Solution**: Use `api://AzureADTokenExchange/.default` in Leg 1

```csharp
// ❌ Bad: Wrong scope in Leg 1
AuthenticationResult leg1 = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })  // Wrong
    .ExecuteAsync();

// ✅ Good: Token exchange scope in Leg 1
AuthenticationResult leg1 = await app
    .AcquireTokenForClient(new[] { "api://AzureADTokenExchange/.default" })  // Correct
    .ExecuteAsync();
```

### Issue: WithExperimentalFeatures Not Called
**Symptom**: Build fails or `WithClientAssertion()` not available  
**Cause**: Experimental features not enabled  
**Solution**: Call `WithExperimentalFeatures()` before `WithClientAssertion()`

```csharp
// ❌ Bad: Missing experimental features
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(appId)
    .WithClientAssertion(assertionProvider)  // May not be available
    .Build();

// ✅ Good: Experimental features enabled
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(appId)
    .WithExperimentalFeatures()              // Required for WithClientAssertion
    .WithClientAssertion(assertionProvider)
    .Build();
```

### Issue: Assertion Reused Incorrectly
**Symptom**: Token validation fails or expired assertion errors  
**Cause**: Assertion token expired or used for wrong resource  
**Solution**: Acquire fresh assertion per target resource

```csharp
// ❌ Bad: Reusing old assertion
string cachedAssertion = GetOldAssertion();  // May be expired
AuthenticationResult token = await ExchangeAssertionForResourceTokenAsync(
    cachedAssertion, cert, targetScope);

// ✅ Good: Fresh assertion per exchange
string freshAssertion = await AcquireAssertionTokenViaSniAsync(cert);
AuthenticationResult token = await ExchangeAssertionForResourceTokenAsync(
    freshAssertion, cert, targetScope);
```

## Terminology Quick Reference

| Term | Meaning | When to Use |
|------|---------|-------------|
| **FIC** | Federated Identity Credential | Describing this flow type |
| **Two-leg** | Two-step process (assertion + exchange) | Always when describing FIC flow |
| **Leg 1** | Assertion token acquisition | First step only |
| **Leg 2** | Token exchange for target resource | Second step only |
| **Assertion** | Intermediate token used as credential | Leg 1 output, Leg 2 input |
| **jwt-pop** | JWT Proof-of-Possession | client_assertion_type value |
| **Token exchange** | Swapping assertion for target token | Describing Leg 2 operation |

## Related Skills

- [Shared Guidance](../msal-mtls-pop-guidance/SKILL.md) - Common terminology and conventions
- [Vanilla Flow](../msal-mtls-pop-vanilla/SKILL.md) - Direct token acquisition patterns

---

**Version**: 1.0  
**Last Updated**: 2026-02-07  
**Maintainers**: MSAL.NET Team
