# GitHub Copilot Agent Skills for MSAL.NET

This directory contains GitHub Copilot Agent Skills that provide expert guidance for using Microsoft Authentication Library (MSAL) for .NET in various authentication scenarios.

## What are GitHub Copilot Agent Skills?

GitHub Copilot Agent Skills are specialized knowledge modules that help Copilot provide more accurate and context-aware assistance for specific technologies and scenarios. Each skill contains:

- **SKILL.md**: Structured documentation with YAML frontmatter for Copilot integration
- **Helper Classes**: Production-ready C# code examples that follow MSAL.NET best practices
- **Examples**: Real-world scenarios with complete, tested code samples
- **Shared Resources**: Reusable patterns, credential setup guides, and troubleshooting content (DRY principle)

## Available Skills

### 1. Confidential Client Authentication (`msal-confidential-auth/`)

A comprehensive skill set for confidential client authentication patterns in MSAL.NET, covering three core flows with granularized, reusable credential setup patterns.

#### Authentication Flows

- **[Authorization Code Flow](msal-confidential-auth/auth-code-flow/SKILL.md)** - Web applications with user sign-in
- **[On-Behalf-Of (OBO) Flow](msal-confidential-auth/obo-flow/SKILL.md)** - Multi-tier services acting on behalf of users  
- **[Client Credentials Flow](msal-confidential-auth/client-credentials/SKILL.md)** - Service-to-service daemon applications

#### Shared Resources (DRY Principle)

All skills reference these granularized patterns:

**Credential Setup:**
- [Certificate Setup](msal-shared/credential-setup/certificate-setup.md) - Load from file, store, or Key Vault
- [Certificate SNI Setup](msal-shared/credential-setup/certificate-sni-setup.md) - Subject Name Identifier configuration
- [Federated Identity Credentials](msal-shared/credential-setup/federated-identity-credentials.md) - Keyless authentication

**Patterns & Best Practices:**
- [Token Caching Strategies](msal-shared/patterns/token-caching-strategies.md) - Cache management
- [Error Handling Patterns](msal-shared/patterns/error-handling-patterns.md) - Common error scenarios
- [Troubleshooting Guide](msal-shared/patterns/troubleshooting.md) - Comprehensive troubleshooting

**Code Examples:**
- [with-certificate.cs](msal-shared/code-examples/with-certificate.cs) - Standard certificate authentication
- [with-certificate-sni.cs](msal-shared/code-examples/with-certificate-sni.cs) - Certificate with SNI
- [with-federated-identity-credentials.cs](msal-shared/code-examples/with-federated-identity-credentials.cs) - FIC authentication

#### Agent Capabilities

For each flow, agents can help with:

1. **Generate Code Snippet** - Show code for [flow] with [credential type]
2. **Setup Guidance** - When do I set up [credential type]?
3. **Error Resolution** - I'm getting [error], what's the solution?
4. **Best Practices** - What are the best practices for [scenario]?
5. **Explain the Flow** - Explain how [flow] works
6. **Decision Help** - Which flow should I use for [scenario]?
7. **Validate Code** - Review and validate MSAL implementation for correctness

#### When to Use

- **Web Application with User Sign-In** → Authorization Code Flow
- **Multi-Tier Service (API calling another API)** → On-Behalf-Of Flow
- **Daemon/Background Service** → Client Credentials Flow

---

### 2. mTLS Proof-of-Possession (PoP) Skills

Specialized skills for mTLS PoP authentication with Managed Identity and Confidential Client support. These skills **reference the shared patterns** from `msal-shared/` for DRY compliance.

#### `msal-mtls-pop-guidance/`

**Shared terminology, conventions, and patterns** for mTLS PoP flows.

**Provides:**
- Common terminology definitions (vanilla vs FIC two-leg)
- Authentication method comparison (MSI vs Confidential Client)
- MSI limitations (no `WithClientAssertion()` API - cannot perform FIC Leg 2)
- All 3 UAMI identifier types with real example IDs from PR #5726
- FIC valid combinations matrix (4 scenarios: MSI/ConfApp × Bearer/PoP)
- Version requirements (MSAL.NET 4.82.1+) and reviewer expectations

**When to use:** Reference this when working with any mTLS PoP scenario to understand terminology and conventions.

**📄 [View Skill](msal-mtls-pop-guidance/SKILL.md)**

---

#### `msal-mtls-pop-vanilla/`

**Direct mTLS PoP token acquisition** for target resources (single-step, no token exchange).

**Covers:**
- System-Assigned Managed Identity (SAMI) - Azure environments only
- User-Assigned Managed Identity (UAMI) - By ClientId, ResourceId, or ObjectId
- Confidential Client with certificate (SNI) - Works anywhere
- Credential Guard attestation via `.WithAttestationSupport()`
- mTLS-specific endpoints (e.g., `https://mtlstb.graph.microsoft.com`)
- Self-contained Quick Start examples with complete inline HTTP calls
- Null-safe certificate handling with proper checks

**Helper Classes:**
- `VanillaMsiMtlsPop.cs` - MSI implementation (SAMI + all 3 UAMI ID types)
- `MtlsPopTokenAcquirer.cs` - Generic token acquisition with attestation
- `ResourceCaller.cs` - HTTP client configuration and API calls with mTLS binding

**When to use:** Acquire an mTLS PoP token directly for a target resource (Microsoft Graph, Azure Key Vault, Azure Storage, custom APIs).

**References:** [Certificate Setup](msal-shared/credential-setup/certificate-setup.md), [Certificate SNI Setup](msal-shared/credential-setup/certificate-sni-setup.md), [Troubleshooting](msal-shared/patterns/troubleshooting.md)

**📄 [View Skill](msal-mtls-pop-vanilla/SKILL.md)**

---

#### `msal-mtls-pop-fic-two-leg/`

**Federated Identity Credential (FIC) token exchange** with assertions (two-step pattern).

**Covers:**
- **Leg 1:** MSI or Confidential Client → `api://AzureADTokenExchange` (with PoP + Attestation)
- **Leg 2:** Confidential Client ONLY → Final resource (Bearer or mTLS PoP)
- Certificate binding requirement: ALL scenarios pass `TokenBindingCertificate` from Leg 1
- All 4 valid combinations (MSI/ConfApp Leg 1 × Bearer/PoP Leg 2)
- Region specification: All Leg 2 Confidential Client apps include `.WithAzureRegion()`

**Helper Classes:**
- `FicLeg1Acquirer.cs` - Leg 1 token acquisition (MSI or Confidential Client)
- `FicAssertionProvider.cs` - Constructs `ClientSignedAssertion` from Leg 1 token
- `FicLeg2Exchanger.cs` - Leg 2 token exchange (Confidential Client only)
- `ResourceCaller.cs` - HTTP client configuration for final resource calls

**When to use:** Workload identity federation scenarios requiring two-leg token exchange (Kubernetes workload identity, multi-tenant authentication chains, cross-tenant scenarios).

**References:** [Federated Identity Credentials Setup](msal-shared/credential-setup/federated-identity-credentials.md), [Certificate Setup](msal-shared/credential-setup/certificate-setup.md), [Troubleshooting](msal-shared/patterns/troubleshooting.md)

**📄 [View Skill](msal-mtls-pop-fic-two-leg/SKILL.md)**

---

## Quick Start Guide

### Choose Your Scenario

| Scenario | Skill to Use |
|----------|--------------|
| Web app with user sign-in | [Authorization Code Flow](msal-confidential-auth/auth-code-flow/SKILL.md) |
| API acting on behalf of user | [On-Behalf-Of Flow](msal-confidential-auth/obo-flow/SKILL.md) |
| Daemon/background service | [Client Credentials Flow](msal-confidential-auth/client-credentials/SKILL.md) |
| Direct mTLS PoP token (MSI/SNI) | [Vanilla mTLS PoP](msal-mtls-pop-vanilla/SKILL.md) |
| FIC token exchange with mTLS PoP | [FIC Two-Leg mTLS PoP](msal-mtls-pop-fic-two-leg/SKILL.md) |

### Choose Your Credential Type

| Credential Type | Setup Guide |
|-----------------|-------------|
| Standard Certificate | [Certificate Setup](msal-shared/credential-setup/certificate-setup.md) |
| Certificate with SNI | [Certificate SNI Setup](msal-shared/credential-setup/certificate-sni-setup.md) |
| Federated Identity Credentials | [FIC Setup](msal-shared/credential-setup/federated-identity-credentials.md) |
| System-Assigned Managed Identity | [Vanilla mTLS PoP](msal-mtls-pop-vanilla/SKILL.md) (Azure only) |
| User-Assigned Managed Identity | [Vanilla mTLS PoP](msal-mtls-pop-vanilla/SKILL.md) (3 ID types) |

## Requirements

### By Skill Set

**Confidential Client Authentication:**
- MSAL.NET 4.61.0 or later
- .NET 6.0+ recommended
- Appropriate credential type configured (certificate, FIC, etc.)

**mTLS PoP Skills:**
- MSAL.NET 4.82.1 or later (for `WithMtlsProofOfPossession()`, `WithAttestationSupport()`)
- Microsoft.Identity.Client.KeyAttestation NuGet package
- .NET 8.0 recommended
- Azure environment for MSI (SAMI/UAMI) or certificate for local/on-premises

## Using These Skills

### In GitHub Copilot Chat

GitHub Copilot automatically discovers and uses these skills when you ask questions. Simply ask natural language questions:

**Confidential Client Examples:**
- "How do I implement authorization code flow with certificate?"
- "Show me OBO flow with managed identity"
- "What's the difference between standard cert and SNI?"
- "How do I set up federated identity credentials?"

**mTLS PoP Examples:**
- "How do I acquire an mTLS PoP token using Managed Identity?"
- "Show me FIC two-leg token exchange with MSI and Confidential Client"
- "What's the difference between SAMI and UAMI?"
- "How do I call Microsoft Graph with mTLS PoP?"

### Direct Skill Reference

Reference specific skills in your prompts for targeted assistance:
```
@workspace Use the msal-mtls-pop-vanilla skill to implement token acquisition
@workspace Use the client-credentials skill to set up a daemon app
@workspace Use the auth-code-flow skill for my web application
```

### Code Examples

Each skill includes production-ready C# helper classes following MSAL.NET conventions:
- Async/await with `ConfigureAwait(false)`
- `CancellationToken` support with defaults
- Full `IDisposable` implementation
- Input validation (`ArgumentNullException.ThrowIfNull`)
- Disposal checks (`ObjectDisposedException.ThrowIf`)

## Architecture & Design

### DRY Principle

Skills follow the **Don't Repeat Yourself (DRY)** principle:
- **Shared patterns** live in `msal-shared/` (single source of truth)
- **Individual skills** reference shared patterns via links (no duplication)
- **Updates** to credential setup, error handling, or troubleshooting happen once
- **Composition** is easy - mix and match patterns from multiple skills

### Skill Structure

#### Individual Skills
```
skill-name/
├── SKILL.md                  # Main documentation with YAML frontmatter
├── HelperClass1.cs           # Optional production helper class
└── HelperClass2.cs           # Optional production helper class
```

#### Skill Sets with Shared Resources
```
.github/skills/
├── msal-shared/                  # Shared resources for all skill sets (DRY)
│   ├── code-examples/            # Copy-paste code snippets
│   ├── credential-setup/         # Setup guides by credential type
│   └── patterns/                 # Common patterns, troubleshooting
├── skill-set-name/
│   ├── flow1/
│   │   └── SKILL.md              # Flow-specific documentation
│   └── flow2/
│       └── SKILL.md
└── individual-skill-name/
    ├── SKILL.md                  # Main documentation with YAML frontmatter
    └── HelperClass.cs            # Optional production helper class
```

### YAML Frontmatter Format

Each SKILL.md begins with YAML frontmatter for Copilot integration:

```yaml
---
skill_name: unique-skill-identifier
version: 1.0
description: Brief description of what this skill covers
applies_to:
  - Area/Feature this skill applies to
tags:
  - Relevant
  - Search
  - Keywords
---
```

## Contributing

When adding new skills:

1. **Follow structure** - Use existing naming conventions and directory structure
2. **Include YAML frontmatter** - Every SKILL.md needs complete frontmatter
3. **Provide tested examples** - All code must be production-ready and tested
4. **Add troubleshooting** - Include common issues and solutions
5. **Document requirements** - List NuGet packages, MSAL versions, target frameworks
6. **Use real IDs** - When applicable, use example IDs from E2E tests
7. **Follow conventions** - Adhere to MSAL.NET coding conventions in helper classes
8. **Reference shared patterns** - Link to `msal-shared/` instead of duplicating content (DRY principle)
9. **Update catalog** - Add new skills to this README for discoverability

## Additional Resources

### MSAL.NET Documentation
- [MSAL.NET Official Documentation](https://aka.ms/msal-net)
- [MSAL.NET GitHub Repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)

### OAuth & Identity Standards
- [OAuth 2.0 Specification](https://tools.ietf.org/html/draft-ietf-oauth-v2-1-01)
- [OAuth 2.0 Authorization Code Flow](https://tools.ietf.org/html/draft-ietf-oauth-v2-1-01)
- [Azure Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)

### Test References
- [mTLS PoP Integration Tests](../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [Managed Identity E2E Tests](../../tests/Microsoft.Identity.Test.E2e/)
