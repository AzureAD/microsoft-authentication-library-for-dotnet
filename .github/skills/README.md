# GitHub Copilot Agent Skills for MSAL.NET

This directory contains GitHub Copilot Agent Skills that provide expert guidance for using Microsoft Authentication Library (MSAL) for .NET in various scenarios.

## What are GitHub Copilot Agent Skills?

GitHub Copilot Agent Skills are specialized knowledge modules that help Copilot provide more accurate and context-aware assistance for specific technologies and scenarios. Each skill contains:

- **SKILL.md**: Structured documentation with YAML frontmatter for Copilot integration
- **Helper Classes**: Production-ready C# code examples that follow MSAL.NET best practices
- **Examples**: Real-world scenarios with complete, tested code samples
- **Shared Resources**: Reusable patterns, credential setup guides, and troubleshooting content

## Available Skills

### Confidential Client Authentication (`msal-confidential-auth/`)

Comprehensive guidance for confidential client authentication flows with granularized, reusable credential setup patterns.

**Flows covered:**
- **[Authorization Code Flow](msal-confidential-auth/auth-code-flow/SKILL.md)** - Web applications with user sign-in
- **[On-Behalf-Of (OBO) Flow](msal-confidential-auth/obo-flow/SKILL.md)** - Multi-tier services acting on behalf of users
- **[Client Credentials Flow](msal-confidential-auth/client-credentials/SKILL.md)** - Service-to-service daemon applications

**Shared resources** (referenced by all skills for DRY principle):
- **[Certificate Setup](msal-confidential-auth/shared/credential-setup/certificate-setup.md)** - Load certificates from file, store, or Key Vault
- **[Certificate SNI Setup](msal-confidential-auth/shared/credential-setup/certificate-sni-setup.md)** - Subject Name Identifier configuration
- **[Federated Identity Credentials](msal-confidential-auth/shared/credential-setup/federated-identity-credentials.md)** - Keyless authentication with managed identities
- **[Token Caching Strategies](msal-confidential-auth/shared/patterns/token-caching-strategies.md)** - Cache management best practices
- **[Error Handling Patterns](msal-confidential-auth/shared/patterns/error-handling-patterns.md)** - Common error scenarios and solutions
- **[Troubleshooting Guide](msal-confidential-auth/shared/patterns/troubleshooting.md)** - Comprehensive troubleshooting

**When to use**: Implementing any confidential client authentication flow (auth code, OBO, client credentials) with standard credentials.

---

### mTLS Proof-of-Possession (PoP) Skills

Specialized skills for mTLS PoP authentication with Managed Identity and Confidential Client support. These skills reference the shared credential patterns from msal-confidential-auth for DRY compliance.

#### `msal-mtls-pop-guidance/`
Shared terminology, conventions, and patterns for mTLS PoP flows. Provides:
- Common terminology definitions (vanilla vs FIC two-leg)
- Authentication method comparison (MSI vs Confidential Client)
- MSI limitations (no `WithClientAssertion()` API for Leg 2)
- All 3 UAMI identifier types with real example IDs
- FIC valid combinations matrix (4 scenarios)
- Version requirements and reviewer expectations

**When to use**: Reference this when working with any mTLS PoP scenario to understand terminology and conventions.

#### `msal-mtls-pop-vanilla/`
Direct mTLS PoP token acquisition for target resources. Covers:
- System-Assigned Managed Identity (SAMI)
- User-Assigned Managed Identity (UAMI) - ClientId, ResourceId, ObjectId
- Confidential Client with certificate-based SNI
- Credential Guard attestation via `.WithAttestationSupport()`
- mTLS-specific endpoints (e.g., `https://mtlstb.graph.microsoft.com`)
- Self-contained Quick Start examples with inline HTTP calls
- Production helper classes: `VanillaMsiMtlsPop.cs`, `MtlsPopTokenAcquirer.cs`, `ResourceCaller.cs`

**When to use**: Acquire an mTLS PoP token directly for a target resource (Microsoft Graph, Azure Key Vault, custom APIs).

**References**: [Certificate Setup](msal-confidential-auth/shared/credential-setup/certificate-setup.md), [Certificate SNI Setup](msal-confidential-auth/shared/credential-setup/certificate-sni-setup.md)

#### `msal-mtls-pop-fic-two-leg/`
Federated Identity Credential (FIC) token exchange with assertions. Covers:
- Leg 1: MSI or Confidential Client → `api://AzureADTokenExchange` (PoP + Attestation)
- Leg 2: **Confidential Client ONLY** → Final resource (Bearer or mTLS PoP)
- Certificate binding requirement: ALL scenarios pass `TokenBindingCertificate` from Leg 1
- All 4 valid combinations (MSI/ConfApp × Bearer/PoP)
- Production helper classes: `FicLeg1Acquirer.cs`, `FicAssertionProvider.cs`, `FicLeg2Exchanger.cs`, `ResourceCaller.cs`

**When to use**: Workload identity federation scenarios requiring two-leg token exchange (Kubernetes, multi-tenant environments).

**References**: [Federated Identity Credentials Setup](msal-confidential-auth/shared/credential-setup/federated-identity-credentials.md), [Certificate Setup](msal-confidential-auth/shared/credential-setup/certificate-setup.md)

## Requirements

### By Skill Set

**Confidential Client Authentication:**
- MSAL.NET 4.61.0 or later
- .NET 6.0+ recommended

**mTLS PoP Skills:**
- MSAL.NET 4.82.1 or later (for `WithMtlsProofOfPossession()`, `WithAttestationSupport()`)
- Microsoft.Identity.Client.KeyAttestation NuGet package
- .NET 8.0 recommended

## Using These Skills

### In GitHub Copilot Chat
GitHub Copilot automatically discovers and uses these skills when you ask questions. Examples:

**Confidential Client:**
- "How do I implement authorization code flow with certificate?"
- "Show me OBO flow with managed identity"
- "What's the difference between standard cert and SNI?"

**mTLS PoP:**
- "How do I acquire an mTLS PoP token using Managed Identity?"
- "Show me FIC two-leg token exchange with MSI and Confidential Client"
- "What's the difference between SAMI and UAMI?"

### Direct Reference
Reference skills directly in your prompts:
```
@workspace Use the msal-mtls-pop-vanilla skill to implement token acquisition
@workspace Use the client-credentials skill to set up a daemon app
```

### Code Examples
Each skill includes production-ready C# helper classes following MSAL.NET conventions:
- Async/await with `ConfigureAwait(false)`
- `CancellationToken` support with defaults
- Full `IDisposable` implementation
- Input validation (`ArgumentNullException.ThrowIfNull`)
- Disposal checks (`ObjectDisposedException.ThrowIf`)

## Skill Structure

### Individual Skills
```
skill-name/
├── SKILL.md                  # Main documentation with YAML frontmatter
├── HelperClass1.cs           # Optional production helper class
└── HelperClass2.cs           # Optional production helper class
```

### Skill Sets with Shared Resources
```
skill-set-name/
├── README.md                 # Skill set overview
├── flow1/
│   └── SKILL.md              # Flow-specific documentation
├── flow2/
│   └── SKILL.md
└── shared/                   # Reusable patterns (DRY principle)
    ├── code-examples/        # Copy-paste code snippets
    ├── credential-setup/     # Setup guides by credential type
    └── patterns/             # Common patterns, troubleshooting
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
1. Follow the existing structure and naming conventions
2. Include complete YAML frontmatter in SKILL.md
3. Provide tested, production-ready code examples
4. Include troubleshooting sections for common issues
5. Document all requirements (NuGet packages, MSAL versions, frameworks)
6. Use real example IDs from E2E tests when applicable
7. Follow MSAL.NET coding conventions in all helper classes
8. **Follow DRY principle**: Reference shared patterns from msal-confidential-auth/shared/ instead of duplicating credential setup, error handling, or troubleshooting content

## Additional Resources

- [MSAL.NET Documentation](https://aka.ms/msal-net)
- [MSAL.NET GitHub Repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- [OAuth 2.0 Specification](https://tools.ietf.org/html/draft-ietf-oauth-v2-1-01)
- [Azure Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
- [mTLS PoP Integration Tests](../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [Managed Identity E2E Tests](../../tests/Microsoft.Identity.Test.E2e/)
