# Certificate Configuration Consolidation

## Overview

The `CertificateConfiguration` class and `WithCertificateConfiguration` method provide a unified, consolidated API for configuring certificates in MSAL.NET confidential client applications. This replaces the need to use multiple overloads of `WithCertificate`, `WithClientClaims`, and request-level `WithMtlsProofOfPossession` methods.

## Problem Statement

Previously, MSAL.NET had multiple certificate-related APIs scattered across different builder classes:

1. `WithCertificate(certificate)` - Basic certificate setup
2. `WithCertificate(certificate, sendX5C)` - Certificate with X5C option
3. `WithClientClaims(certificate, claims, merge)` - Certificate with custom claims
4. `WithClientClaims(certificate, claims, merge, sendX5C)` - Certificate with claims and X5C
5. `ConfidentialClientApplicationBuilderForResourceProviders.WithCertificate(...)` - Extension with serial number association
6. `AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession()` - mTLS PoP at request level

This fragmentation made it difficult for developers to understand which method to use for their specific scenario.

## Solution

The new consolidated API uses a configuration object pattern:

```csharp
var certificateConfig = new CertificateConfiguration(certificate)
{
    SendX5C = true,
    AssociateTokensWithCertificateSerialNumber = true,
    ClaimsToSign = customClaims,
    MergeWithDefaultClaims = true,
    EnableMtlsProofOfPossession = true
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithAzureRegion(region)
    .WithCertificateConfiguration(certificateConfig)
    .Build();
```

## CertificateConfiguration Properties

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

1. **Clarity**: All certificate-related options are in one place
2. **Discoverability**: Developers can see all available options through IntelliSense
3. **Maintainability**: Easier to add new certificate options in the future
4. **Type Safety**: Configuration object ensures valid combinations
5. **Simplicity**: Reduces the number of methods developers need to learn

## Backward Compatibility

All existing `WithCertificate` and `WithClientClaims` methods remain available and fully functional. The new `WithCertificateConfiguration` method is an additive change that provides a better developer experience without breaking existing code.

## See Also

- [mTLS PoP Design Document](../sni_mtls_pop_token_design.md)
- [Client Assertion Documentation](https://aka.ms/msal-net-client-assertion)
- [SNI Certificate Information](https://aka.ms/msal-net-sni)
- [Proof-of-Possession Tokens](https://aka.ms/msal-net-pop)
