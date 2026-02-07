---
skill_name: "MSAL.NET mTLS PoP - Vanilla Flow"
version: "1.0"
description: "Direct mTLS PoP token acquisition for target resources without token exchange (MSI and SNI patterns)"
applies_to:
  - "**/ManagedIdentity*/**"
  - "**/*ClientCredentials*/**"
  - "**/ClientCredentialsMtlsPopTests.cs"
tags:
  - "msal"
  - "mtls-pop"
  - "vanilla"
  - "msi"
  - "sni"
  - "direct-acquisition"
---

# MSAL.NET mTLS PoP - Vanilla Flow

This skill covers the **vanilla flow** (no legs) for mTLS Proof-of-Possession token acquisition in MSAL.NET. Use this pattern when your application directly calls a target resource without token exchange.

## When to Use Vanilla Flow

Use the vanilla flow when:

✅ **Direct resource access**
- Application calls Microsoft Graph, Azure Key Vault, or custom APIs directly
- No cross-tenant authentication required
- No service chaining or delegation scenarios

✅ **Single-step token acquisition**
- One `AcquireTokenForClient()` call gets the target resource token
- Certificate bound directly to the final resource
- No intermediate assertion tokens needed

✅ **Supported identity types**
- **MSI (Managed System Identity)**: Azure-managed identity without explicit certificates
- **SNI (Service Named Identity)**: App-registered identity with `WithCertificate()`

❌ **Do NOT use vanilla flow when**
- Token exchange or delegation is required → Use [FIC Two-Leg Flow](../msal-mtls-pop-fic-two-leg/SKILL.md)
- Cross-tenant authentication needed → Use [FIC Two-Leg Flow](../msal-mtls-pop-fic-two-leg/SKILL.md)
- Service requires federated identity credentials → Use [FIC Two-Leg Flow](../msal-mtls-pop-fic-two-leg/SKILL.md)

## Flow Diagram

```
Application → MSAL.AcquireTokenForClient() → Azure AD → mTLS PoP Token
                  |                                            |
                  +--------------------------------------------+
                               Single call, no exchange
```

**Key Characteristics**:
- No "legs" or steps - single direct acquisition
- Certificate binding at request time via `.WithMtlsProofOfPossession()`
- Token is immediately usable for the target resource

## MSI Template (Managed System Identity)

Use when running on Azure infrastructure with system-assigned managed identity.

```csharp
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class MsiVanillaFlow
    {
        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForKeyVaultAsync()
        {
            // MSI Configuration - No certificate needed at app level
            // Azure manages the identity and provides certificates automatically
            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithAzureRegion("westus3")
                .Build();

            // Direct token acquisition for Key Vault with mTLS PoP
            string[] scopes = new[] { "https://vault.azure.net/.default" };
            
            AuthenticationResult result = await app
                .AcquireTokenForManagedIdentity(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            // Validate mTLS PoP token
            ValidateMtlsPopToken(result, "https://vault.azure.net/.default");

            return result;
        }

        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForGraphAsync()
        {
            // MSI for Microsoft Graph
            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithAzureRegion("eastus2")
                .Build();

            string[] scopes = new[] { "https://graph.microsoft.com/.default" };
            
            AuthenticationResult result = await app
                .AcquireTokenForManagedIdentity(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            ValidateMtlsPopToken(result, "https://graph.microsoft.com/.default");

            return result;
        }

        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForCustomApiAsync(
            string customApiScope)
        {
            // MSI for custom API resource
            // Example: "api://my-backend-service/.default"
            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithAzureRegion("westus3")
                .Build();

            string[] scopes = new[] { customApiScope };
            
            AuthenticationResult result = await app
                .AcquireTokenForManagedIdentity(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            ValidateMtlsPopToken(result, customApiScope);

            return result;
        }

        private static void ValidateMtlsPopToken(AuthenticationResult result, string expectedScope)
        {
            if (result == null)
                throw new InvalidOperationException("Authentication result is null");

            if (result.TokenType != "mtls_pop")
                throw new InvalidOperationException($"Expected token type 'mtls_pop', got '{result.TokenType}'");

            if (string.IsNullOrEmpty(result.AccessToken))
                throw new InvalidOperationException("Access token is null or empty");

            if (!result.Scopes.Contains(expectedScope))
                throw new InvalidOperationException($"Token does not contain expected scope: {expectedScope}");

            // Note: BindingCertificate may be available depending on MSI implementation
            Console.WriteLine($"✓ mTLS PoP token acquired for {expectedScope}");
            Console.WriteLine($"✓ Token type: {result.TokenType}");
            Console.WriteLine($"✓ Expires: {result.ExpiresOn}");
            Console.WriteLine($"✓ Token source: {result.AuthenticationResultMetadata.TokenSource}");
        }
    }
}
```

## SNI Template (Service Named Identity)

Use when running with app-registered identity and explicit certificate.

### Basic SNI Pattern

```csharp
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class SniVanillaFlow
    {
        private const string AppId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string TenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";
        
        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForKeyVaultAsync(
            X509Certificate2 certificate)
        {
            // SNI Configuration with certificate at app level
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("westus3")
                .WithCertificate(certificate, sendX5C: true)  // Certificate bound at app level
                .Build();

            // Direct token acquisition for Key Vault with mTLS PoP
            string[] scopes = new[] { "https://vault.azure.net/.default" };
            
            AuthenticationResult result = await app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()  // Enable mTLS PoP at request level
                .ExecuteAsync();

            // Validate mTLS PoP token
            ValidateMtlsPopToken(result, certificate, "https://vault.azure.net/.default");

            return result;
        }

        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForGraphAsync(
            X509Certificate2 certificate)
        {
            // SNI for Microsoft Graph
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("eastus2")
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            string[] scopes = new[] { "https://graph.microsoft.com/.default" };
            
            AuthenticationResult result = await app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            ValidateMtlsPopToken(result, certificate, "https://graph.microsoft.com/.default");

            return result;
        }

        public static async Task<AuthenticationResult> AcquireMtlsPopTokenForCustomApiAsync(
            X509Certificate2 certificate,
            string customApiScope)
        {
            // SNI for custom API resource
            // Example: "api://my-backend-service/.default"
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(AppId)
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithAzureRegion("westus3")
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            string[] scopes = new[] { customApiScope };
            
            AuthenticationResult result = await app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            ValidateMtlsPopToken(result, certificate, customApiScope);

            return result;
        }

        private static void ValidateMtlsPopToken(
            AuthenticationResult result, 
            X509Certificate2 expectedCert,
            string expectedScope)
        {
            if (result == null)
                throw new InvalidOperationException("Authentication result is null");

            if (result.TokenType != "mtls_pop")
                throw new InvalidOperationException($"Expected token type 'mtls_pop', got '{result.TokenType}'");

            if (string.IsNullOrEmpty(result.AccessToken))
                throw new InvalidOperationException("Access token is null or empty");

            if (!result.Scopes.Contains(expectedScope))
                throw new InvalidOperationException($"Token does not contain expected scope: {expectedScope}");

            // Verify certificate binding
            if (result.BindingCertificate == null)
                throw new InvalidOperationException("BindingCertificate is null - certificate binding failed");

            if (result.BindingCertificate.Thumbprint != expectedCert.Thumbprint)
                throw new InvalidOperationException(
                    $"Certificate thumbprint mismatch. Expected: {expectedCert.Thumbprint}, Got: {result.BindingCertificate.Thumbprint}");

            Console.WriteLine($"✓ mTLS PoP token acquired for {expectedScope}");
            Console.WriteLine($"✓ Token type: {result.TokenType}");
            Console.WriteLine($"✓ Expires: {result.ExpiresOn}");
            Console.WriteLine($"✓ Certificate thumbprint: {result.BindingCertificate.Thumbprint}");
            Console.WriteLine($"✓ Token source: {result.AuthenticationResultMetadata.TokenSource}");
        }
    }
}
```

### SNI with Token Caching

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class SniVanillaFlowWithCache
    {
        public static async Task DemonstrateTokenCachingAsync(X509Certificate2 certificate)
        {
            const string appId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
            const string tenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithAzureRegion("westus3")
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            string[] scopes = new[] { "https://vault.azure.net/.default" };

            // First call: Token acquired from Azure AD
            Console.WriteLine("First acquisition - expecting IdentityProvider source...");
            AuthenticationResult firstResult = await app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            Console.WriteLine($"✓ Token type: {firstResult.TokenType}");
            Console.WriteLine($"✓ Token source: {firstResult.AuthenticationResultMetadata.TokenSource}");
            Console.WriteLine($"✓ Expires: {firstResult.ExpiresOn}");

            // Second call: Token retrieved from cache
            Console.WriteLine("\nSecond acquisition - expecting Cache source...");
            AuthenticationResult secondResult = await app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync();

            Console.WriteLine($"✓ Token type: {secondResult.TokenType}");
            Console.WriteLine($"✓ Token source: {secondResult.AuthenticationResultMetadata.TokenSource}");

            // Verify caching worked
            if (secondResult.AuthenticationResultMetadata.TokenSource != TokenSource.Cache)
            {
                Console.WriteLine("⚠ Warning: Expected token from cache, but got from identity provider");
            }
            else
            {
                Console.WriteLine("✓ Token successfully retrieved from cache");
            }

            // Verify certificate binding persists through cache
            if (secondResult.BindingCertificate?.Thumbprint == certificate.Thumbprint)
            {
                Console.WriteLine($"✓ Certificate binding preserved: {certificate.Thumbprint}");
            }
        }
    }
}
```

## Resource Caller Helper

After acquiring the mTLS PoP token, use it to call the target resource. The token must be sent with the certificate for mutual TLS authentication.

```csharp
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopExamples
{
    public class ResourceCaller
    {
        /// <summary>
        /// Calls a resource API using mTLS PoP token with certificate binding
        /// </summary>
        public static async Task<string> CallResourceWithMtlsPopAsync(
            AuthenticationResult authResult,
            string resourceUrl,
            X509Certificate2 certificate)
        {
            // Validate mTLS PoP token
            if (authResult.TokenType != "mtls_pop")
                throw new InvalidOperationException($"Expected mtls_pop token, got {authResult.TokenType}");

            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate), "Certificate is required for mTLS");

            // Create HTTP handler with client certificate for mTLS
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);
            
            // Optional: Configure server certificate validation
            // handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;

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
                
                Console.WriteLine($"✓ Successfully called resource: {resourceUrl}");
                Console.WriteLine($"✓ Response status: {response.StatusCode}");
                
                return content;
            }
        }

        /// <summary>
        /// Example: Call Azure Key Vault with mTLS PoP
        /// </summary>
        public static async Task<string> CallKeyVaultWithMtlsPopAsync(
            AuthenticationResult authResult,
            X509Certificate2 certificate,
            string vaultName,
            string secretName)
        {
            string resourceUrl = $"https://{vaultName}.vault.azure.net/secrets/{secretName}?api-version=7.4";
            return await CallResourceWithMtlsPopAsync(authResult, resourceUrl, certificate);
        }

        /// <summary>
        /// Example: Call Microsoft Graph with mTLS PoP
        /// </summary>
        public static async Task<string> CallGraphWithMtlsPopAsync(
            AuthenticationResult authResult,
            X509Certificate2 certificate,
            string graphEndpoint = "https://graph.microsoft.com/v1.0/me")
        {
            return await CallResourceWithMtlsPopAsync(authResult, graphEndpoint, certificate);
        }
    }
}
```

## Test Reference Implementation

The vanilla flow is tested in:
- **File**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`
- **Method**: `Sni_Gets_Pop_Token_Successfully_TestAsync()` (lines 36-84)

### Key Test Patterns

```csharp
[TestMethod]
public async Task Sni_Gets_Pop_Token_Successfully_TestAsync()
{
    // Certificate from cert store (not disposed)
    X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
    
    // SNI app configuration with real region
    IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
        .Create(MsiAllowListedAppIdforSNI)
        .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
        .WithAzureRegion("westus3")  // Real region string
        .WithCertificate(cert, true)
        .WithTestLogging()
        .Build();
    
    // Single acquisition with mTLS PoP
    AuthenticationResult authResult = await confidentialApp
        .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
        .WithMtlsProofOfPossession()
        .ExecuteAsync();
    
    // Comprehensive validation
    Assert.IsNotNull(authResult);
    Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType);
    Assert.IsNotNull(authResult.AccessToken);
    Assert.IsNotNull(authResult.BindingCertificate);
    Assert.AreEqual(cert.Thumbprint, authResult.BindingCertificate.Thumbprint);
    
    // Verify caching behavior
    authResult = await confidentialApp
        .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
        .WithMtlsProofOfPossession()
        .ExecuteAsync();
    
    Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
}
```

## Verification Checklist

Before submitting vanilla flow code or documentation:

### Code Quality
- [ ] Single `AcquireTokenForClient()` call (no multi-step flow)
- [ ] Real Azure region string (e.g., `"westus3"`, not `"your-region"`)
- [ ] Certificate from store or trusted source (not created with `using`)
- [ ] `.WithMtlsProofOfPossession()` at request level
- [ ] No token or certificate secrets logged

### Token Validation
- [ ] `result.TokenType == "mtls_pop"` assertion
- [ ] `result.AccessToken` is not null or empty
- [ ] `result.Scopes` contains expected scope
- [ ] `result.BindingCertificate` validated (SNI only)
- [ ] Certificate thumbprint matches (SNI only)

### Documentation
- [ ] "Vanilla flow" terminology used (never "vanilla legs")
- [ ] "Resource" used for API endpoints
- [ ] "Scopes" only for token acquisition, not consumption
- [ ] No placeholder values in examples
- [ ] References to actual test implementations

### Testing
- [ ] First acquisition returns token from IdentityProvider
- [ ] Second acquisition returns token from Cache
- [ ] Certificate binding persists through cache (SNI)
- [ ] Token can successfully call target resource
- [ ] Error handling for invalid certificates

## Common Issues and Solutions

### Issue: Certificate Disposed Too Early
**Symptom**: MSAL fails with "Certificate private key inaccessible"  
**Cause**: Certificate wrapped in `using` statement  
**Solution**: Use certificate from store, don't dispose

```csharp
// ❌ Bad
using (var cert = new X509Certificate2(path, password))
{
    var app = ConfidentialClientApplicationBuilder.Create(appId)
        .WithCertificate(cert, true)  // Cert disposed after using block
        .Build();
}

// ✅ Good
X509Certificate2 cert = CertificateHelper.FindCertificateByName(certName);
var app = ConfidentialClientApplicationBuilder.Create(appId)
    .WithCertificate(cert, true)  // Cert remains valid
    .Build();
```

### Issue: Wrong Token Type
**Symptom**: `result.TokenType == "Bearer"` instead of `"mtls_pop"`  
**Cause**: Missing `.WithMtlsProofOfPossession()` call  
**Solution**: Add at request level

```csharp
// ❌ Bad
AuthenticationResult result = await app
    .AcquireTokenForClient(scopes)
    .ExecuteAsync();  // Returns Bearer token

// ✅ Good
AuthenticationResult result = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()  // Returns mtls_pop token
    .ExecuteAsync();
```

### Issue: BindingCertificate is Null
**Symptom**: `result.BindingCertificate == null` in SNI flow  
**Cause**: Certificate not properly configured at app level  
**Solution**: Ensure `WithCertificate()` called correctly

```csharp
// ✅ Good
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create(appId)
    .WithCertificate(cert, sendX5C: true)  // sendX5C enables binding
    .Build();
```

### Issue: Regional Endpoint Not Used
**Symptom**: Token request goes to global endpoint  
**Cause**: Region not specified or invalid  
**Solution**: Use real Azure region string

```csharp
// ❌ Bad
.WithAzureRegion("region-placeholder")  // Invalid region

// ✅ Good
.WithAzureRegion("westus3")  // Valid region
```

## Related Skills

- [Shared Guidance](../msal-mtls-pop-guidance/SKILL.md) - Common terminology and conventions
- [FIC Two-Leg Flow](../msal-mtls-pop-fic-two-leg/SKILL.md) - Token exchange patterns

---

**Version**: 1.0  
**Last Updated**: 2026-02-07  
**Maintainers**: MSAL.NET Team
