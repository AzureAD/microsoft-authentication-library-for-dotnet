# MSAL.NET Certificate API Consolidation Specification

## Overview

This specification describes a clean, user-friendly, and future-proof API for certificate-based authentication in MSAL.NET confidential client applications. The design consolidates multiple fragmented certificate APIs into a unified fluent interface.

## Motivation

### Current State (Fragmented APIs)

MSAL.NET currently has 7+ different certificate-related APIs scattered across multiple classes:

```csharp
// 1. Basic certificate
builder.WithCertificate(certificate)

// 2. Certificate with X5C
builder.WithCertificate(certificate, sendX5C: true)

// 3. Certificate with custom claims
builder.WithClientClaims(certificate, claims, merge: true)

// 4. Certificate with custom claims and X5C
builder.WithClientClaims(certificate, claims, merge: true, sendX5C: true)

// 5. Resource Provider extension with serial number
builder.WithCertificate(certificate, sendX5C: true, associateTokensWithCertificateSerialNumber: true)

// 6. mTLS PoP at request level
app.AcquireTokenForClient(scopes)
   .WithMtlsProofOfPossession()
   .ExecuteAsync()

// 7. Various WithClientAssertion overloads
builder.WithClientAssertion(...)
```

### Problems

1. **Discoverability**: Developers don't know which method to use
2. **Fragmentation**: Related options are split across different methods
3. **Complexity**: Boolean parameters create confusion (sendX5C, merge, etc.)
4. **Inflexibility**: Cannot easily combine features
5. **Not Future-Proof**: Adding new options requires new overloads

### User Feedback

From issue #5568 and team discussions:
- "Too many ways to do the same thing"
- "I never know which WithCertificate to call"
- "How do I use mTLS with my certificate?"
- "The boolean parameters are confusing"

## Goals

1. ✅ **Clean**: Simple, intuitive API that reads naturally
2. ✅ **User-Friendly**: IntelliSense-discoverable with clear method names
3. ✅ **Future-Proof**: Easy to extend without breaking changes
4. ✅ **Type-Safe**: Compiler prevents invalid configurations
5. ✅ **Flexible**: Supports all current and future scenarios
6. ✅ **Backward Compatible**: Existing APIs continue to work

## Design: Fluent Builder Pattern

### Core Principle

**Replace configuration objects and boolean parameters with fluent method chains.**

### API Structure

```
ConfidentialClientApplicationBuilder
  └─ WithCertificate(cert)
       └─ CertificateAuthenticationBuilder
            ├─ SendCertificateChain()
            ├─ WithAdditionalClaims(claims, merge)
            ├─ PartitionCacheBySerialNumber()
            ├─ UseMutualTls()
            │    └─ MutualTlsBuilder
            │         ├─ WithProofOfPossession() [default]
            │         ├─ WithBearerToken()
            │         └─ And() → back to CertificateAuthenticationBuilder
            └─ Build() → back to ConfidentialClientApplicationBuilder
```

## Public API

### 1. CertificateAuthenticationBuilder

```csharp
/// <summary>
/// Fluent builder for configuring certificate-based authentication.
/// </summary>
public sealed class CertificateAuthenticationBuilder
{
    /// <summary>
    /// Sends the X.509 certificate chain (X5C) with token requests.
    /// Enables certificate roll-over for first-party applications.
    /// See https://aka.ms/msal-net-sni
    /// </summary>
    public CertificateAuthenticationBuilder SendCertificateChain();

    /// <summary>
    /// Adds custom claims to the client assertion JWT.
    /// See https://aka.ms/msal-net-client-assertion
    /// </summary>
    /// <param name="claims">Custom claims to include in the JWT</param>
    /// <param name="mergeWithDefaults">If true, merges with default required claims. Default is true.</param>
    public CertificateAuthenticationBuilder WithAdditionalClaims(
        IDictionary<string, string> claims, 
        bool mergeWithDefaults = true);

    /// <summary>
    /// Partitions the token cache by certificate serial number.
    /// Tokens acquired with different certificates will be cached separately.
    /// Applicable to resource provider scenarios.
    /// </summary>
    public CertificateAuthenticationBuilder PartitionCacheBySerialNumber();

    /// <summary>
    /// Enables mutual TLS (mTLS) authentication with the certificate.
    /// Requires Azure region to be configured.
    /// Returns a MutualTlsBuilder to configure PoP or Bearer token type.
    /// See https://aka.ms/msal-net-mtls
    /// </summary>
    public MutualTlsBuilder UseMutualTls();

    /// <summary>
    /// Returns to the ConfidentialClientApplicationBuilder for further configuration.
    /// </summary>
    /// <remarks>
    /// This allows you to continue configuring other aspects of the application
    /// after setting up certificate authentication.
    /// </remarks>
    public ConfidentialClientApplicationBuilder Build();
}
```

### 2. MutualTlsBuilder

```csharp
/// <summary>
/// Fluent builder for configuring mutual TLS (mTLS) options.
/// </summary>
public sealed class MutualTlsBuilder
{
    /// <summary>
    /// Uses Proof-of-Possession (PoP) tokens with mTLS.
    /// The token is cryptographically bound to the certificate.
    /// This is the default and most secure option.
    /// See https://aka.ms/msal-net-pop
    /// </summary>
    public MutualTlsBuilder WithProofOfPossession();

    /// <summary>
    /// Uses standard Bearer tokens with mTLS transport security.
    /// The certificate is used for mTLS handshake but the token is not bound to it.
    /// Use this when you need mTLS at the transport layer but standard token format.
    /// </summary>
    public MutualTlsBuilder WithBearerToken();

    /// <summary>
    /// Returns to the CertificateAuthenticationBuilder to configure additional certificate options.
    /// </summary>
    public CertificateAuthenticationBuilder And();

    /// <summary>
    /// Returns to the ConfidentialClientApplicationBuilder for further configuration.
    /// </summary>
    public ConfidentialClientApplicationBuilder Build();
}
```

### 3. ConfidentialClientApplicationBuilder Extension

```csharp
public partial class ConfidentialClientApplicationBuilder
{
    /// <summary>
    /// Configures certificate-based authentication for the confidential client.
    /// Returns a fluent builder to configure certificate options.
    /// </summary>
    /// <param name="certificate">The X.509 certificate with private key</param>
    /// <returns>A CertificateAuthenticationBuilder for fluent configuration</returns>
    /// <exception cref="ArgumentNullException">If certificate is null</exception>
    /// <exception cref="MsalClientException">If certificate doesn't have a private key</exception>
    /// <example>
    /// <code>
    /// var app = ConfidentialClientApplicationBuilder
    ///     .Create(clientId)
    ///     .WithCertificate(certificate)
    ///         .SendCertificateChain()
    ///         .UseMutualTls()
    ///     .WithAzureRegion("eastus")
    ///     .Build();
    /// </code>
    /// </example>
    public CertificateAuthenticationBuilder WithCertificate(X509Certificate2 certificate);
}
```

## Usage Examples

### Example 1: Simple Certificate Authentication

The most basic scenario - just authenticate with a certificate.

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Example 2: Certificate with X5C (SNI Scenario)

For first-party applications using Subject Name/Issuer (SNI) certificates.

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
        .SendCertificateChain()
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Example 3: mTLS with Proof-of-Possession (Default)

Most secure option - certificate-bound PoP tokens with mTLS transport.

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
        .UseMutualTls()  // PoP is default
    .WithAzureRegion("eastus")
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Example 4: mTLS with Bearer Token

When you need mTLS transport but standard bearer token format.

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
        .UseMutualTls()
            .WithBearerToken()
    .WithAzureRegion("eastus")
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Example 5: Complex Configuration

Combining multiple features in a single fluent chain.

```csharp
var customClaims = new Dictionary<string, string>
{
    { "client_ip", "192.168.1.1" },
    { "device_id", "device-123" }
};

var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
        .SendCertificateChain()
        .WithAdditionalClaims(customClaims)
        .PartitionCacheBySerialNumber()
        .UseMutualTls()
            .WithProofOfPossession()
        .And()  // Continue with more certificate config if needed
    .WithAzureRegion("eastus")
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### Example 6: Claims Challenge at Request Time

Handling Conditional Access claims challenges.

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(certificate)
        .UseMutualTls()
    .WithAzureRegion("eastus")
    .Build();

try
{
    var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
}
catch (MsalUiRequiredException ex) when (!string.IsNullOrEmpty(ex.Claims))
{
    // Handle Conditional Access claims challenge
    var result = await app.AcquireTokenForClient(scopes)
        .WithClaims(ex.Claims)
        .ExecuteAsync();
}
```

## Migration Guide

### From: WithCertificate(cert, sendX5C)

**Before:**
```csharp
var app = builder.WithCertificate(certificate, sendX5C: true).Build();
```

**After:**
```csharp
var app = builder
    .WithCertificate(certificate)
        .SendCertificateChain()
    .Build();
```

### From: WithClientClaims

**Before:**
```csharp
var app = builder
    .WithClientClaims(certificate, claims, mergeWithDefaults: true, sendX5C: true)
    .Build();
```

**After:**
```csharp
var app = builder
    .WithCertificate(certificate)
        .WithAdditionalClaims(claims, mergeWithDefaults: true)
        .SendCertificateChain()
    .Build();
```

### From: WithMtlsProofOfPossession at Request Level

**Before:**
```csharp
var app = builder
    .WithCertificate(certificate)
    .WithAzureRegion(region)
    .Build();
    
var result = await app.AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

**After:**
```csharp
var app = builder
    .WithCertificate(certificate)
        .UseMutualTls()
    .WithAzureRegion(region)
    .Build();
    
var result = await app.AcquireTokenForClient(scopes)
    .ExecuteAsync();  // mTLS automatically applied
```

### From: Resource Provider Extension

**Before:**
```csharp
using Microsoft.Identity.Client.RP;

var app = builder
    .WithCertificate(certificate, sendX5C: true, associateTokensWithCertificateSerialNumber: true)
    .Build();
```

**After:**
```csharp
var app = builder
    .WithCertificate(certificate)
        .SendCertificateChain()
        .PartitionCacheBySerialNumber()
    .Build();
```

## Feature Comparison Matrix

| Feature | Old API | New API | Notes |
|---------|---------|---------|-------|
| Basic cert auth | `WithCertificate(cert)` | `WithCertificate(cert)` | Unchanged |
| Send X5C | `WithCertificate(cert, true)` | `.SendCertificateChain()` | More explicit |
| Custom claims | `WithClientClaims(cert, claims, merge)` | `.WithAdditionalClaims(claims, merge)` | Clearer naming |
| mTLS PoP | Request: `.WithMtlsProofOfPossession()` | Builder: `.UseMutualTls()` | Configuration moves to builder |
| mTLS Bearer | Not available | `.UseMutualTls().WithBearerToken()` | New feature |
| Cache partition | RP extension method | `.PartitionCacheBySerialNumber()` | Now built-in |
| Claims challenge | Request: `.WithClaims()` | Request: `.WithClaims()` | Unchanged |

## Alignment with PR #5399

This design complements the `AssertionResponse` pattern proposed in [PR #5399](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5399):

### Certificate-First Scenarios (This Spec)
When you have a certificate and want to configure how to use it:

```csharp
builder.WithCertificate(certificate)
    .UseMutualTls()
    .WithProofOfPossession()
```

### Assertion-First Scenarios (PR #5399)
When you have custom assertion logic or external providers:

```csharp
builder.WithClientAssertion(async (options, ct) => 
{
    return new AssertionResponse 
    { 
        Assertion = await GenerateJwtAsync(options, ct),
        TokenBindingCertificate = cert  // Optional, for mTLS binding
    };
})
```

Both approaches coexist and serve different developer mental models.

## Implementation Details

### Builder Classes Structure

```csharp
public sealed class CertificateAuthenticationBuilder
{
    private readonly ConfidentialClientApplicationBuilder _parentBuilder;
    private readonly X509Certificate2 _certificate;
    
    internal CertificateAuthenticationBuilder(
        ConfidentialClientApplicationBuilder parentBuilder,
        X509Certificate2 certificate)
    {
        _parentBuilder = parentBuilder ?? throw new ArgumentNullException(nameof(parentBuilder));
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
    }
    
    public CertificateAuthenticationBuilder SendCertificateChain()
    {
        _parentBuilder.Config.SendX5C = true;
        return this;
    }
    
    public CertificateAuthenticationBuilder WithAdditionalClaims(
        IDictionary<string, string> claims, 
        bool mergeWithDefaults = true)
    {
        if (claims == null || !claims.Any())
            throw new ArgumentException("Claims cannot be null or empty", nameof(claims));
            
        _parentBuilder.Config.ClientCredential = 
            new CertificateAndClaimsClientCredential(_certificate, claims, mergeWithDefaults);
        return this;
    }
    
    public CertificateAuthenticationBuilder PartitionCacheBySerialNumber()
    {
        _parentBuilder.Config.CertificateIdToAssociateWithToken = _certificate.SerialNumber;
        return this;
    }
    
    public MutualTlsBuilder UseMutualTls()
    {
        return new MutualTlsBuilder(_parentBuilder, _certificate);
    }
    
    public ConfidentialClientApplicationBuilder Build()
    {
        return _parentBuilder;
    }
}

public sealed class MutualTlsBuilder
{
    private readonly ConfidentialClientApplicationBuilder _parentBuilder;
    private readonly X509Certificate2 _certificate;
    
    internal MutualTlsBuilder(
        ConfidentialClientApplicationBuilder parentBuilder,
        X509Certificate2 certificate)
    {
        _parentBuilder = parentBuilder ?? throw new ArgumentNullException(nameof(parentBuilder));
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        
        // Enable mTLS by default
        _parentBuilder.Config.IsMtlsPopEnabledByCertificateConfiguration = true;
        _parentBuilder.Config.UseBearerTokenWithMtls = false; // PoP is default
    }
    
    public MutualTlsBuilder WithProofOfPossession()
    {
        _parentBuilder.Config.UseBearerTokenWithMtls = false;
        return this;
    }
    
    public MutualTlsBuilder WithBearerToken()
    {
        _parentBuilder.Config.UseBearerTokenWithMtls = true;
        return this;
    }
    
    public CertificateAuthenticationBuilder And()
    {
        return new CertificateAuthenticationBuilder(_parentBuilder, _certificate);
    }
    
    public ConfidentialClientApplicationBuilder Build()
    {
        return _parentBuilder;
    }
}
```

### Configuration Storage

```csharp
internal sealed class ApplicationConfiguration
{
    // Existing properties
    public IClientCredential ClientCredential { get; set; }
    public bool SendX5C { get; set; }
    public string CertificateIdToAssociateWithToken { get; set; }
    
    // New properties for fluent API
    public bool IsMtlsPopEnabledByCertificateConfiguration { get; set; }
    public bool UseBearerTokenWithMtls { get; set; }
}
```

### Auto-Apply Logic

```csharp
// In AcquireTokenForClientParameterBuilder.Create()
internal static AcquireTokenForClientParameterBuilder Create(
    IConfidentialClientApplicationExecutor executor,
    IEnumerable<string> scopes)
{
    var builder = new AcquireTokenForClientParameterBuilder(executor)
        .WithScopes(scopes);
    
    // Auto-apply mTLS if configured
    if (executor.ServiceBundle.Config.IsMtlsPopEnabledByCertificateConfiguration)
    {
        if (executor.ServiceBundle.Config.UseBearerTokenWithMtls)
        {
            builder.ApplyMtlsBearerAuthentication();
        }
        else
        {
            builder.WithMtlsProofOfPossession();
        }
    }
    
    return builder;
}
```

## Validation and Error Handling

### Certificate Validation

```csharp
public CertificateAuthenticationBuilder WithCertificate(X509Certificate2 certificate)
{
    if (certificate == null)
        throw new ArgumentNullException(nameof(certificate));
    
    if (!certificate.HasPrivateKey)
        throw new MsalClientException(
            MsalError.CertWithoutPrivateKey,
            "Certificate must have a private key for authentication. " +
            "Ensure the certificate includes the private key.");
    
    Config.ClientCredential = new CertificateClientCredential(certificate);
    return new CertificateAuthenticationBuilder(this, certificate);
}
```

### mTLS Validation

```csharp
// In AcquireTokenForClient validation
protected override void Validate()
{
    base.Validate();
    
    if (ServiceBundle.Config.IsMtlsPopEnabledByCertificateConfiguration)
    {
        // Check for Azure region only if the authority is AAD
        if (ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.Aad &&
            string.IsNullOrEmpty(ServiceBundle.Config.AzureRegion))
        {
            throw new MsalClientException(
                MsalError.MtlsPopWithoutRegion,
                "Mutual TLS requires an Azure region to be configured. " +
                "Use .WithAzureRegion(region) on the ConfidentialClientApplicationBuilder. " +
                "See https://aka.ms/msal-net-mtls for details.");
        }
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class CertificateFluentApiTests
{
    private X509Certificate2 _testCert;
    
    [TestInitialize]
    public void Setup()
    {
        _testCert = CertHelper.GetOrCreateTestCert();
    }
    
    [TestMethod]
    public void WithCertificate_ReturnsFluentBuilder()
    {
        var builder = ConfidentialClientApplicationBuilder.Create("client-id");
        var certBuilder = builder.WithCertificate(_testCert);
        
        Assert.IsInstanceOfType(certBuilder, typeof(CertificateAuthenticationBuilder));
    }
    
    [TestMethod]
    public void SendCertificateChain_SetsSendX5C()
    {
        var app = ConfidentialClientApplicationBuilder.Create("client-id")
            .WithCertificate(_testCert)
                .SendCertificateChain()
            .BuildConcrete();
        
        Assert.IsTrue(app.AppConfig.SendX5C);
    }
    
    [TestMethod]
    public void UseMutualTls_EnablesConfiguration()
    {
        var app = ConfidentialClientApplicationBuilder.Create("client-id")
            .WithCertificate(_testCert)
                .UseMutualTls()
            .WithAzureRegion("eastus")
            .BuildConcrete();
        
        Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
        Assert.IsFalse(app.AppConfig.UseBearerTokenWithMtls); // PoP is default
    }
    
    [TestMethod]
    public void UseMutualTls_WithBearerToken_SetsCorrectFlags()
    {
        var app = ConfidentialClientApplicationBuilder.Create("client-id")
            .WithCertificate(_testCert)
                .UseMutualTls()
                    .WithBearerToken()
            .WithAzureRegion("eastus")
            .BuildConcrete();
        
        Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
        Assert.IsTrue(app.AppConfig.UseBearerTokenWithMtls);
    }
    
    [TestMethod]
    public void ComplexConfiguration_AllOptionsWork()
    {
        var claims = new Dictionary<string, string> { { "test", "value" } };
        
        var app = ConfidentialClientApplicationBuilder.Create("client-id")
            .WithCertificate(_testCert)
                .SendCertificateChain()
                .WithAdditionalClaims(claims)
                .PartitionCacheBySerialNumber()
                .UseMutualTls()
                    .WithProofOfPossession()
                .And()
            .WithAzureRegion("eastus")
            .BuildConcrete();
        
        Assert.IsTrue(app.AppConfig.SendX5C);
        Assert.IsNotNull(app.AppConfig.ClientCredential);
        Assert.AreEqual(_testCert.SerialNumber, app.AppConfig.CertificateIdToAssociateWithToken);
        Assert.IsTrue(app.AppConfig.IsMtlsPopEnabledByCertificateConfiguration);
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class CertificateFluentApiIntegrationTests
{
    [TestMethod]
    public async Task AcquireToken_WithMutualTls_ReturnsPopToken()
    {
        using var httpManager = new MockHttpManager();
        httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(tokenType: "mtls_pop");
        
        var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
            .WithAuthority(TestConstants.AuthorityTenant)
            .WithCertificate(CertHelper.GetOrCreateTestCert())
                .UseMutualTls()
            .WithAzureRegion("eastus")
            .WithHttpManager(httpManager)
            .Build();
        
        var result = await app.AcquireTokenForClient(TestConstants.s_scope)
            .ExecuteAsync();
        
        Assert.AreEqual("PoP", result.TokenType);
        Assert.IsNotNull(result.BindingCertificate);
    }
}
```

## Performance Considerations

### No Runtime Overhead

- Builder objects created at configuration time only
- No additional allocations during token acquisition
- Same underlying implementation as current code
- No performance regression expected

### Memory Profile

- Builder instances are short-lived (GC Gen 0)
- No additional long-lived objects
- Same ApplicationConfiguration memory footprint

## Documentation Plan

### Required Documentation

1. **API Reference**: Complete XML documentation for all public methods
2. **Getting Started Guide**: Step-by-step certificate authentication
3. **mTLS Guide**: Comprehensive mutual TLS documentation
4. **Migration Guide**: Detailed migration from old APIs
5. **Best Practices**: Recommendations and patterns
6. **Troubleshooting**: Common issues and solutions

### Code Samples

All examples will be added to the samples repository:

- Basic certificate authentication
- SNI certificate with X5C
- mTLS with PoP tokens
- mTLS with Bearer tokens
- Resource provider scenarios
- Claims challenge handling
- Complex multi-feature configurations

## Success Criteria

### API Quality

- ✅ All methods have XML documentation
- ✅ IntelliSense provides helpful tooltips
- ✅ Compiler prevents invalid configurations
- ✅ Clear error messages for misconfigurations

### Developer Experience

- ✅ Developers can find the right API in <5 minutes
- ✅ First working code in <10 minutes
- ✅ Positive feedback from early adopters
- ✅ Reduced support tickets for certificate configuration

### Technical Quality

- ✅ 100% backward compatibility
- ✅ >90% test coverage
- ✅ No performance regression
- ✅ Passes security review
- ✅ Meets accessibility standards

## Timeline

### Phase 1: Implementation (Weeks 1-2)
- Implement fluent builder classes
- Add unit tests
- Update internal logic

### Phase 2: Testing (Week 3)
- Integration tests
- Performance testing
- Security review

### Phase 3: Documentation (Week 4)
- API documentation
- Migration guide
- Code samples

### Phase 4: Release (Week 5)
- Preview release for feedback
- Address community feedback
- Stable release

## Open Questions for Review

1. **Naming**: 
   - `UseMutualTls()` vs `EnableMutualTls()` vs `WithMutualTls()`?
   - `SendCertificateChain()` vs `IncludeCertificateChain()` vs `WithX5C()`?

2. **Default Behavior**:
   - Should mTLS default to PoP or Bearer?
   - Currently: PoP is default (more secure)

3. **Build() Method**:
   - Required at every level or optional?
   - Currently: Optional, allows flexible chaining

4. **Deprecation**:
   - Mark old APIs obsolete immediately or wait?
   - Recommendation: Wait for 1-2 releases to gather feedback

## Approvals

- [ ] API Review Board
- [ ] MSAL.NET Engineering Team
- [ ] Security Team
- [ ] Documentation Team
- [ ] Developer Experience Team

## References

- [Issue #5568 - Certificate API Consolidation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5568)
- [PR #5399 - Bound Client Assertion Spec](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5399)
- [mTLS PoP Design Document](../sni_mtls_pop_token_design.md)
- [RFC 8705 - OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
- [MSAL.NET Documentation](https://aka.ms/msal-net)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-06  
**Authors**: MSAL.NET Team  
**Status**: Proposed for Review
