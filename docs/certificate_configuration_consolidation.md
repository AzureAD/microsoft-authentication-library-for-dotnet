# Certificate Configuration Consolidation

## Overview

This document describes the consolidated API for configuring certificates in MSAL.NET confidential client applications. The consolidation addresses the fragmentation of multiple certificate-related APIs and provides a clear, unified approach.

## Background: The Fragmentation Problem

Previously, MSAL.NET had multiple certificate-related APIs scattered across different builder classes:

1. `WithCertificate(certificate)` - Basic certificate setup
2. `WithCertificate(certificate, sendX5C)` - Certificate with X5C option
3. `WithClientClaims(certificate, claims, merge)` - Certificate with custom claims
4. `WithClientClaims(certificate, claims, merge, sendX5C)` - Certificate with claims and X5C
5. `ConfidentialClientApplicationBuilderForResourceProviders.WithCertificate(...)` - Extension with serial number association
6. `AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession()` - mTLS PoP at request level
7. Various `WithClientAssertion` overloads for custom assertions

This fragmentation made it difficult for developers to:
- Understand which method to use for their scenario
- Discover all available certificate options
- Combine multiple certificate features
- Migrate between different authentication patterns

## Solution: Two Complementary APIs

### 1. CertificateConfiguration (Simple Scenarios)

For most certificate-based authentication scenarios, use the new `CertificateConfiguration` class with `WithCertificateConfiguration()`:

```csharp
var certConfig = new CertificateConfiguration(certificate)
{
    SendX5C = true,
    EnableMtlsProofOfPossession = true,
    UseBearerTokenWithMtls = false,  // PoP or Bearer
    Claims = claimsChallenge
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .Build();
```

**Best for:**
- Certificate-based client authentication
- mTLS scenarios with certificates
- Standard certificate workflows
- Most confidential client applications

### 2. WithClientAssertion with AssertionResponse (Advanced Scenarios)

For advanced scenarios requiring custom assertion logic or external assertion providers, use the existing `WithClientAssertion` API with a delegate:

```csharp
// Future enhancement aligned with PR #5399
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientAssertion(async (options, ct) => 
    {
        // Custom logic to generate or retrieve assertion
        string jwt = await GenerateCustomAssertionAsync(options, ct);
        
        // Return assertion with optional certificate for token binding
        return new ClientSignedAssertion
        {
            Assertion = jwt,
            // Certificate optional - for mTLS token binding
            // Certificate = cert  // if needed
        };
    })
    .Build();
```

**Best for:**
- Custom assertion generation logic
- External assertion providers (e.g., Key Vault, HSM)
- Federated credentials
- Non-certificate-based client credentials
- Managed Identity with assertions (future)

## Design Philosophy

### Alignment with PR #5399

This consolidation aligns with the vision outlined in [PR #5399](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5399), which proposes a unified approach for client assertions:

**Key Principles:**
1. **Forward Compatibility**: APIs designed to accommodate future enhancements
2. **Separation of Concerns**: 
   - `CertificateConfiguration` for certificate workflows
   - `WithClientAssertion` for assertion workflows
3. **Optional Token Binding**: Support both bearer and PoP tokens with mTLS
4. **Extensibility**: New properties can be added without breaking changes

### Relationship to Existing APIs

```
┌─────────────────────────────────────────────────┐
│    Confidential Client Builder                   │
├─────────────────────────────────────────────────┤
│                                                   │
│  Certificate-Based                                │
│  ┌─────────────────────────────────┐             │
│  │ WithCertificateConfiguration()  │             │
│  │   - Simple certificate setup    │             │
│  │   - mTLS with PoP/Bearer        │             │
│  │   - Claims challenge support    │             │
│  └─────────────────────────────────┘             │
│                                                   │
│  Assertion-Based                                  │
│  ┌─────────────────────────────────┐             │
│  │ WithClientAssertion()           │             │
│  │   - Custom assertion logic      │             │
│  │   - External providers          │             │
│  │   - Federated credentials       │             │
│  │   - Optional cert for binding   │             │
│  └─────────────────────────────────┘             │
│                                                   │
│  Secret-Based (unchanged)                         │
│  ┌─────────────────────────────────┐             │
│  │ WithClientSecret()              │             │
│  └─────────────────────────────────┘             │
│                                                   │
└─────────────────────────────────────────────────┘
```

## CertificateConfiguration API

### Certificate (Required)
- **Type**: `X509Certificate2`
- **Description**: The X509 certificate used for authentication
- **Requirement**: Must have a private key

### SendX5C
- **Type**: `bool`
- **Default**: `false`
- **Description**: Whether to send the X5C (certificate chain) with each request
- **Use Case**: First-party applications for certificate roll-over scenarios
- **Reference**: https://aka.ms/msal-net-sni

### AssociateTokensWithCertificateSerialNumber
- **Type**: `bool`
- **Default**: `false`
- **Description**: Associates tokens with the certificate serial number for cache partitioning
- **Use Case**: Resource provider scenarios where different certificates should have separate token caches

### ClaimsToSign
- **Type**: `IDictionary<string, string>`
- **Default**: `null`
- **Description**: Custom claims to be included in the client assertion JWT
- **Reference**: https://aka.ms/msal-net-client-assertion
- **Note**: These are different from the `Claims` property which is for token request claims

### MergeWithDefaultClaims
- **Type**: `bool`
- **Default**: `true`
- **Description**: Whether to merge custom claims with the default required claims
- **Note**: Only applicable when `ClaimsToSign` is specified

### EnableMtlsProofOfPossession
- **Type**: `bool`
- **Default**: `false`
- **Description**: Enables mTLS (mutual TLS) authentication with the certificate
- **Requirements**: 
  - Azure region must be configured
  - Tenanted authority required
- **Reference**: https://aka.ms/msal-net-pop

### UseBearerTokenWithMtls
- **Type**: `bool`
- **Default**: `false`
- **Description**: When true, requests a bearer token over mTLS instead of a PoP token
- **Use Case**: When you need mTLS for transport security but want standard bearer tokens
- **Note**: Only applicable when `EnableMtlsProofOfPossession` is true
- **Details**:
  - `false` (default): Returns mTLS PoP token (token is bound to the certificate)
  - `true`: Returns bearer token over mTLS (mTLS only at transport layer)

### Claims
- **Type**: `string`
- **Default**: `null`
- **Description**: Claims to be included in the token request (not in the client assertion)
- **Use Case**: Claims challenge scenarios, such as Conditional Access requirements
- **Reference**: https://aka.ms/msal-net-claim-challenge
- **Note**: This is automatically applied at request time via the `WithClaims` API
- **Difference from ClaimsToSign**: 
  - `ClaimsToSign`: Claims signed into the client assertion JWT for app authentication
  - `Claims`: Claims sent in the token request to satisfy Conditional Access policies

## Migration Guide

### From WithCertificate(certificate)

**Before:**
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate)
    .Build();
```

**After:**
```csharp
var certConfig = new CertificateConfiguration(certificate);

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .Build();
```

### From WithCertificate(certificate, sendX5C)

**Before:**
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate, true)
    .Build();
```

**After:**
```csharp
var certConfig = new CertificateConfiguration(certificate)
{
    SendX5C = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .Build();
```

### From WithClientClaims

**Before:**
```csharp
var claims = new Dictionary<string, string> 
{ 
    { "client_ip", "192.168.1.1" } 
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientClaims(certificate, claims, mergeWithDefaultClaims: true, sendX5C: true)
    .Build();
```

**After:**
```csharp
var claims = new Dictionary<string, string> 
{ 
    { "client_ip", "192.168.1.1" } 
};

var certConfig = new CertificateConfiguration(certificate)
{
    ClaimsToSign = claims,
    MergeWithDefaultClaims = true,
    SendX5C = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .Build();
```

### From Resource Provider Extension with Serial Number

**Before:**
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate, sendX5C: true, associateTokensWithCertificateSerialNumber: true)
    .Build();
```

**After:**
```csharp
var certConfig = new CertificateConfiguration(certificate)
{
    SendX5C = true,
    AssociateTokensWithCertificateSerialNumber = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .Build();
```

### From WithMtlsProofOfPossession at Request Level

**Before:**
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(certificate)
    .WithAzureRegion(region)
    .Build();

var result = await app.AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

**After:**
```csharp
var certConfig = new CertificateConfiguration(certificate)
{
    EnableMtlsProofOfPossession = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificateConfiguration(certConfig)
    .WithAzureRegion(region)
    .Build();

// mTLS PoP is automatically applied - no need for WithMtlsProofOfPossession()
var result = await app.AcquireTokenForClient(scopes)
    .ExecuteAsync();
```

## Complete Example with All Options

```csharp
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

// Load your certificate
X509Certificate2 certificate = LoadCertificate();

// Configure all certificate options
var certificateConfig = new CertificateConfiguration(certificate)
{
    // Enable X5C for certificate roll-over scenarios
    SendX5C = true,
    
    // Partition cache by certificate serial number
    AssociateTokensWithCertificateSerialNumber = true,
    
    // Add custom claims to the client assertion JWT (for app authentication)
    ClaimsToSign = new Dictionary<string, string>
    {
        { "client_ip", "192.168.1.1" },
        { "custom_claim", "value" }
    },
    
    // Merge custom claims with default required claims
    MergeWithDefaultClaims = true,
    
    // Enable mTLS for certificate-bound authentication
    EnableMtlsProofOfPossession = true,
    
    // Request PoP token (default, false) or bearer token (true) over mTLS
    UseBearerTokenWithMtls = false,  // false = PoP token (more secure)
    
    // Add claims for Conditional Access (claims challenge scenario)
    Claims = "{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"urn:microsoft:req1\"}}}"
};

// Build the confidential client application
var app = ConfidentialClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/your-tenant-id")
    .WithAzureRegion("eastus")  // Required for mTLS
    .WithCertificateConfiguration(certificateConfig)
    .Build();

// Acquire token - all configuration is automatically applied
var result = await app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
    .ExecuteAsync();
```

## Example: mTLS with PoP Token (Default)

```csharp
var certificateConfig = new CertificateConfiguration(certificate)
{
    EnableMtlsProofOfPossession = true,
    UseBearerTokenWithMtls = false  // PoP token - token is bound to certificate
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion(region)
    .WithCertificateConfiguration(certificateConfig)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
// result.TokenType will be "mtls_pop"
// result.BindingCertificate will contain the certificate
```

## Example: mTLS with Bearer Token

```csharp
var certificateConfig = new CertificateConfiguration(certificate)
{
    EnableMtlsProofOfPossession = true,
    UseBearerTokenWithMtls = true  // Bearer token over mTLS transport
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion(region)
    .WithCertificateConfiguration(certificateConfig)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
// result.TokenType will be "Bearer"
// Transport uses mTLS but token is not bound to certificate
// result.BindingCertificate will still contain the certificate for reference
```

## Example: Claims Challenge Scenario

```csharp
// Initial token request
var certificateConfig = new CertificateConfiguration(certificate)
{
    EnableMtlsProofOfPossession = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion(region)
    .WithCertificateConfiguration(certificateConfig)
    .Build();

try
{
    var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
}
catch (MsalUiRequiredException ex)
{
    // Conditional Access policy requires additional claims
    if (!string.IsNullOrEmpty(ex.Claims))
    {
        // Option 1: Use WithClaims on the request
        var result = await app.AcquireTokenForClient(scopes)
            .WithClaims(ex.Claims)
            .ExecuteAsync();
        
        // Option 2: Configure it in CertificateConfiguration for all requests
        certificateConfig.Claims = ex.Claims;
        // Rebuild app or all subsequent requests will include these claims automatically
    }
}
```

## Example: Client Assertion with Custom Claims

```csharp
// Add custom claims to the client assertion JWT (not the token request)
var assertionClaims = new Dictionary<string, string>
{
    { "client_ip", "192.168.1.1" },
    { "device_id", "device-12345" }
};

var certificateConfig = new CertificateConfiguration(certificate)
{
    ClaimsToSign = assertionClaims,
    MergeWithDefaultClaims = true,  // Also include default required claims
    SendX5C = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificateConfiguration(certificateConfig)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

## Benefits of the Consolidated API

### For Developers
1. **Clarity**: All certificate-related options in one place
2. **Discoverability**: All options visible through IntelliSense
3. **Type Safety**: Configuration object ensures valid combinations
4. **Simplicity**: Fewer methods to learn
5. **Flexibility**: Choose between PoP and bearer tokens with mTLS

### For MSAL Maintainability
1. **Reduced API Surface**: Fewer overloads to maintain
2. **Forward Compatibility**: Easy to add new properties
3. **Consistency**: Unified pattern for configuration
4. **Clear Migration Path**: From old APIs to new

### Relationship to Future Work

This consolidation provides the foundation for:

**Issue #5568 Goals:**
- Single, intuitive API for certificate scenarios
- Clear distinction between certificate and assertion workflows
- Support for both bearer and PoP tokens over mTLS
- Built-in support for claims challenge scenarios

**PR #5399 Vision:**
- `AssertionResponse` pattern for advanced scenarios
- Token binding certificate as optional property
- Extensible design for future assertion types
- Unified approach across all client credential types

## Backward Compatibility

**All existing APIs remain fully functional:**
- `WithCertificate(certificate)`
- `WithCertificate(certificate, sendX5C)`
- `WithClientClaims(certificate, claims, merge)`
- `WithClientClaims(certificate, claims, merge, sendX5C)`
- `WithMtlsProofOfPossession()` at request level
- `WithClientAssertion()` overloads

**Migration is optional** - developers can:
1. Continue using existing APIs indefinitely
2. Migrate gradually to the new API
3. Mix old and new APIs during transition

**No breaking changes** - this is an additive enhancement.

## Use Case Matrix

| Scenario | Recommended API | Example |
|----------|----------------|---------|
| Basic certificate auth | `CertificateConfiguration` | Authentication with cert |
| Certificate + X5C | `CertificateConfiguration.SendX5C` | SNI certificate scenarios |
| mTLS with PoP tokens | `CertificateConfiguration.EnableMtlsProofOfPossession` | Secure token binding |
| mTLS with bearer tokens | `CertificateConfiguration.UseBearerTokenWithMtls = true` | mTLS transport only |
| Claims challenge | `CertificateConfiguration.Claims` | Conditional Access |
| Custom client assertions | `WithClientAssertion` | Key Vault, HSM, federated creds |
| Client secret | `WithClientSecret` | Development/testing |
| Cache partitioning by cert | `CertificateConfiguration.AssociateTokensWithCertificateSerialNumber` | Resource providers |

## See Also

- [mTLS PoP Design Document](../sni_mtls_pop_token_design.md)
- [Client Assertion Documentation](https://aka.ms/msal-net-client-assertion)
- [SNI Certificate Information](https://aka.ms/msal-net-sni)
- [Proof-of-Possession Tokens](https://aka.ms/msal-net-pop)
