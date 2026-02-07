---
skill_name: "MSAL.NET mTLS PoP - Shared Guidance"
version: "1.0"
description: "Canonical definitions, terminology rules, and reviewer expectations for mTLS Proof-of-Possession flows in MSAL.NET"
applies_to:
  - "**/*mtls*/**"
  - "**/*pop*/**"
  - "**/ClientCredentialsMtlsPopTests.cs"
  - "**/ManagedIdentity*/**"
tags:
  - "msal"
  - "mtls-pop"
  - "authentication"
  - "terminology"
  - "conventions"
---

# MSAL.NET mTLS PoP - Shared Guidance

This skill provides foundational guidance applicable to all mTLS Proof-of-Possession flows in MSAL.NET. Use this as the first reference when implementing, documenting, or reviewing any mTLS PoP code.

## Canonical Flow Definitions

### Vanilla Flow (No Legs)
**Definition**: A single-step token acquisition where the application directly requests an mTLS PoP token for the target resource.

**Characteristics**:
- No token exchange involved
- Single `AcquireTokenForClient()` call
- Certificate provided via `WithCertificate()` at app level
- Token bound directly to the target resource (Graph, Key Vault, custom API)

**Flow Diagram**:
```
Application → MSAL → Azure AD → mTLS PoP Token (for target resource)
```

**Example Scenarios**:
- Calling Microsoft Graph with mTLS PoP
- Accessing Azure Key Vault with certificate-bound tokens
- Invoking custom APIs that require mTLS PoP

### FIC Two-Leg Flow
**Definition**: A two-step token exchange pattern where an assertion token is first acquired, then exchanged for an mTLS PoP token for the target resource.

**Characteristics**:
- **Leg 1**: Acquire assertion token for `api://AzureADTokenExchange`
- **Leg 2**: Exchange assertion for target resource token using `WithClientAssertion()`
- Certificate binding applied during the exchange (jwt-pop)
- Enables cross-tenant and delegation scenarios

**Flow Diagram**:
```
Leg 1: Application → MSAL → Azure AD → Assertion Token (api://AzureADTokenExchange)
Leg 2: Application → MSAL (with assertion + cert) → Azure AD → mTLS PoP Token (for target resource)
```

**Example Scenarios**:
- Federated Identity Credentials (FIC) token exchange
- Cross-tenant service-to-service authentication
- Service chaining with delegated credentials

## Terminology Rules

### ALWAYS Use These Terms

| Term | Meaning | Example |
|------|---------|---------|
| **Vanilla** | Direct token acquisition without exchange | "The vanilla flow acquires a token in a single step" |
| **FIC** or **FIC two-leg** | Federated Identity Credential exchange pattern | "The FIC two-leg flow exchanges an assertion for a target token" |
| **Leg 1** | First step in FIC flow (assertion acquisition) | "Leg 1 acquires a token for api://AzureADTokenExchange" |
| **Leg 2** | Second step in FIC flow (token exchange) | "Leg 2 exchanges the assertion for the target resource token" |
| **Assertion** | Token used as credential in token exchange | "The assertion is provided via WithClientAssertion()" |
| **mTLS PoP** | Mutual TLS Proof-of-Possession | "Acquire an mTLS PoP token" |
| **jwt-pop** | JWT Proof-of-Possession (assertion binding) | "client_assertion_type uses jwt-pop for certificate binding" |
| **MSI** | Managed System Identity | "MSI flow uses system-assigned identity" |
| **SNI** | Service Named Identity (app-registered identity) | "SNI flow uses WithCertificate() at app level" |

### NEVER Use These Anti-Patterns

❌ **Mixing vanilla and FIC concepts**  
Example: "The vanilla flow's first leg..." (vanilla has no legs)  
✅ Correct: "The vanilla flow directly acquires..." OR "The FIC flow's first leg..."

❌ **"Scopes" for consumption**  
Example: "Call the API with the scopes"  
✅ Correct: "Call the API with the access token" or "Request token for resource"

❌ **Ambiguous "PoP" without context**  
Example: "Configure PoP"  
✅ Correct: "Configure mTLS PoP" or "Enable Proof-of-Possession with mTLS"

❌ **Logging secrets/tokens**  
Example: `Console.WriteLine(authResult.AccessToken)`  
✅ Correct: `Console.WriteLine($"Token acquired, expires: {authResult.ExpiresOn}")`

### Resource vs. Scopes Distinction

**When acquiring tokens** (developer perspective):
- Use "scopes" in code: `new[] { "https://vault.azure.net/.default" }`
- Use "resource" in documentation: "Acquire a token for the Key Vault resource"

**When consuming tokens** (calling APIs):
- Use "access token" or "mTLS PoP token"
- Use "resource" for the endpoint: "Call the Key Vault resource with the token"
- **NEVER** use "scopes" when describing API calls

## Code Conventions

### Certificate Handling
```csharp
// ✅ Good: Use existing certificate, don't dispose
X509Certificate2 cert = CertificateHelper.FindCertificateByName(certName);
// Certificate is managed by the store, don't dispose

// ❌ Bad: Creating disposable certificate
using (var cert = new X509Certificate2(certPath, password))
{
    // Cert disposed too early, MSAL may fail
}
```

### Region Strings
Always use **real Azure region strings** from [Azure region list](https://learn.microsoft.com/azure/reliability/availability-zones-service-support):

```csharp
// ✅ Good: Real regions
.WithAzureRegion("westus3")
.WithAzureRegion("eastus2")
.WithAzureRegion("westeurope")

// ❌ Bad: Placeholder regions
.WithAzureRegion("region-placeholder")
.WithAzureRegion("your-region-here")
```

**Available regions include**: `westus3`, `eastus`, `eastus2`, `westus`, `westus2`, `centralus`, `northeurope`, `westeurope`, `southeastasia`, `japaneast`, `uksouth`, `australiaeast`, and many more.

### Logging Best Practices
```csharp
// ✅ Good: Log metadata, not secrets
Console.WriteLine($"Token acquired for {authResult.Scopes.FirstOrDefault()}");
Console.WriteLine($"Expires: {authResult.ExpiresOn}");
Console.WriteLine($"Token type: {authResult.TokenType}");

// ❌ Bad: Logging sensitive data
Console.WriteLine($"Access token: {authResult.AccessToken}");
Console.WriteLine($"Certificate private key: {cert.PrivateKey}");
```

### Assertion Provider Pattern
```csharp
// ✅ Good: Return ClientSignedAssertion with TokenBindingCertificate for jwt-pop
.WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
{
    return Task.FromResult(new ClientSignedAssertion
    {
        Assertion = assertionToken,           // From Leg 1
        TokenBindingCertificate = certificate  // Binds for jwt-pop
    });
})

// ❌ Bad: Missing TokenBindingCertificate (no jwt-pop binding)
.WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
{
    return Task.FromResult(new ClientSignedAssertion
    {
        Assertion = assertionToken
        // Missing certificate binding
    });
})
```

## Documentation Style

### Structure Guidelines
1. **Start with "When to Use"** - Clear decision criteria
2. **Provide complete examples** - No pseudo-code or placeholders
3. **Include verification steps** - How to validate success
4. **Reference real tests** - Link to actual test implementations
5. **Add troubleshooting** - Common issues and solutions

### Code Example Template
```csharp
// Context comment explaining the scenario
X509Certificate2 cert = CertificateHelper.FindCertificateByName("cert-name");

// Configuration with real values
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
    .Create("app-id-guid")
    .WithAuthority("https://login.microsoftonline.com/tenant-id-guid")
    .WithAzureRegion("westus3")  // Real region
    .WithCertificate(cert, true)
    .Build();

// Token acquisition with verification
AuthenticationResult result = await app
    .AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Success criteria
Assert.AreEqual("mtls_pop", result.TokenType);
Assert.IsNotNull(result.BindingCertificate);
```

### Checklist Format
Use checklists for pre-PR verification:

```markdown
## Pre-PR Verification Checklist

- [ ] Code compiles without warnings
- [ ] Real region strings (no placeholders)
- [ ] No secrets/tokens logged
- [ ] Consistent terminology (vanilla vs. FIC)
- [ ] Certificate binding configured correctly
- [ ] Success assertions validate token type
- [ ] Documentation uses "resource" (not "scopes" for consumption)
```

## Reviewer Expectations

### Code Review Focus Areas

**For Vanilla Flow PRs**:
1. Verify single `AcquireTokenForClient()` call (no legs)
2. Check `WithCertificate()` at app level
3. Confirm `.WithMtlsProofOfPossession()` at request level
4. Validate `TokenType == "mtls_pop"`
5. Ensure `BindingCertificate` assertion

**For FIC Two-Leg Flow PRs**:
1. Verify Leg 1 targets `api://AzureADTokenExchange`
2. Check Leg 2 uses `WithClientAssertion()`
3. Confirm `TokenBindingCertificate` in assertion
4. Validate `client_assertion_type == "urn:ietf:params:oauth:client-assertion-type:jwt-pop"`
5. Ensure regional mTLS endpoint usage (`mtlsauth.microsoft.com`)

### Documentation Review Focus Areas

**Terminology Consistency**:
- [ ] "Vanilla" or "FIC two-leg" used correctly (not mixed)
- [ ] "Resource" for API endpoints, "scopes" only for token acquisition
- [ ] "mTLS PoP" or "Proof-of-Possession" (not ambiguous "PoP")
- [ ] No secret/token logging in examples

**Code Quality**:
- [ ] Real region strings (not placeholders)
- [ ] Complete examples (not pseudo-code)
- [ ] Proper certificate handling (no premature disposal)
- [ ] Success criteria clearly defined

**Structural Elements**:
- [ ] "When to Use" section present
- [ ] Clear flow diagram or description
- [ ] Verification checklist included
- [ ] References to actual test files

## Common Pitfalls

### 1. Mixing Flow Types
❌ **Problem**: "The vanilla flow's first leg acquires an assertion..."  
✅ **Solution**: Vanilla has no legs. Either:
- "The vanilla flow directly acquires a token for the resource"
- "The FIC two-leg flow's first leg acquires an assertion"

### 2. Certificate Disposal Issues
❌ **Problem**: `using (var cert = ...) { ... }` causes MSAL to fail  
✅ **Solution**: Use certificate from store, don't wrap in `using` statement

### 3. Missing jwt-pop Binding
❌ **Problem**: Assertion without `TokenBindingCertificate`  
✅ **Solution**: Always include certificate in `ClientSignedAssertion` for FIC flow

### 4. Placeholder Values
❌ **Problem**: `"your-region-here"`, `"your-app-id"`, `"replace-me"`  
✅ **Solution**: Use real values or clearly marked test constants

### 5. Scope Confusion
❌ **Problem**: "Call the API with the scopes"  
✅ **Solution**: "Call the API with the access token" (scopes are for acquisition, not consumption)

## Pre-PR Verification Checklist

Before submitting any mTLS PoP PR:

### Code Validation
- [ ] Code compiles and runs without errors
- [ ] All tests pass locally
- [ ] No compiler warnings introduced
- [ ] Certificate handling follows best practices
- [ ] Real Azure region strings used (no placeholders)

### Security Review
- [ ] No secrets, tokens, or private keys logged
- [ ] Certificate private keys not exposed
- [ ] Sensitive data properly sanitized in logs
- [ ] Token validation implemented correctly

### Terminology Compliance
- [ ] "Vanilla" vs "FIC" used correctly (not mixed)
- [ ] "Resource" vs "scopes" distinction maintained
- [ ] "mTLS PoP" or full "Proof-of-Possession" (not ambiguous "PoP")
- [ ] No anti-patterns from "NEVER Use" list

### Documentation Quality
- [ ] "When to Use" guidance provided
- [ ] Complete code examples (no pseudo-code)
- [ ] Flow diagrams or clear descriptions included
- [ ] References to actual test files included
- [ ] Verification steps documented

### Testing Coverage
- [ ] Unit tests for new functionality
- [ ] Integration tests for token flows
- [ ] Success assertions validate TokenType
- [ ] BindingCertificate validation included
- [ ] Error handling tested

## References

### Test Files
- Vanilla flow implementation: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 36-84)
- FIC two-leg flow implementation: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 86-178)

### Standards
- [RFC 8705 - OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
- [Azure Regions List](https://learn.microsoft.com/azure/reliability/availability-zones-service-support)

### Related Skills
- [Vanilla Flow Skill](../msal-mtls-pop-vanilla/SKILL.md) - Direct token acquisition patterns
- [FIC Two-Leg Flow Skill](../msal-mtls-pop-fic-two-leg/SKILL.md) - Token exchange patterns

---

**Version**: 1.0  
**Last Updated**: 2026-02-07  
**Maintainers**: MSAL.NET Team
