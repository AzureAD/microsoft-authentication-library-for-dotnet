# GitHub Copilot Agent Skills for MSAL.NET

This directory contains GitHub Copilot Agent Skills that provide expert guidance for using Microsoft Authentication Library (MSAL) for .NET in various scenarios.

## What are GitHub Copilot Agent Skills?

GitHub Copilot Agent Skills are specialized knowledge modules that help Copilot provide more accurate and context-aware assistance for specific technologies and scenarios. Each skill contains:

- **SKILL.md**: Structured documentation with YAML frontmatter for Copilot integration
- **Helper Classes**: Production-ready C# code examples that follow MSAL.NET best practices
- **Examples**: Real-world scenarios with complete, tested code samples

## Available Skills

### msal-mtls-pop-guidance
Shared terminology, conventions, and patterns for mTLS Proof-of-Possession (PoP) flows in MSAL.NET. This skill provides:
- Common terminology definitions
- Authentication method comparison (MSI vs Confidential Client)
- Flow pattern explanations (vanilla vs FIC two-leg)
- Reviewer expectations and best practices

**When to use**: Reference this skill when working with any mTLS PoP scenario to understand terminology and conventions.

### msal-mtls-pop-vanilla
Direct mTLS PoP token acquisition for target resources using Managed Identity (MSI) or Confidential Client authentication. This skill covers:
- System-Assigned Managed Identity (SAMI)
- User-Assigned Managed Identity (UAMI) - all 3 ID types
- Confidential Client with certificate-based SNI
- Credential Guard attestation support via `.WithAttestationSupport()`
- Production-ready helper classes for token acquisition and resource calls

**When to use**: When you need to acquire an mTLS PoP token directly for a target resource (e.g., Microsoft Graph, Azure Key Vault, custom APIs).

### msal-mtls-pop-fic-two-leg
Federated Identity Credential (FIC) token exchange pattern using assertions. This skill covers:
- Leg 1: MSI or Confidential Client → `api://AzureADTokenExchange` with PoP + Attestation
- Leg 2: **Confidential Client ONLY** (MSI cannot use WithClientAssertion)
- All 4 valid combinations (Bearer/PoP final tokens with MSI/ConfApp Leg 1)
- Production-ready helper classes for assertion building and token exchange

**When to use**: When implementing workload identity federation scenarios that require a two-leg token exchange pattern, typically in Kubernetes or multi-tenant environments.

## Requirements

All skills in this directory require:
- **MSAL.NET 4.82.1 or later** - Earlier versions lack required APIs
- **Microsoft.Identity.Client.KeyAttestation** NuGet package for attestation support
- **.NET 8.0 recommended** - All examples target net8.0 LTS

## Using These Skills

### In GitHub Copilot Chat
GitHub Copilot automatically discovers and uses these skills when you ask questions related to MSAL.NET mTLS PoP flows. Simply ask natural language questions like:
- "How do I acquire an mTLS PoP token using Managed Identity?"
- "Show me how to implement FIC two-leg token exchange"
- "What's the difference between SAMI and UAMI?"

### Direct Reference
You can also reference skills directly in your prompts:
```
@workspace /skills Use the msal-mtls-pop-vanilla skill to help me implement token acquisition
```

### Code Examples
Each skill includes production-ready C# helper classes that you can copy directly into your project. These classes follow MSAL.NET conventions:
- Async/await with `ConfigureAwait(false)`
- Proper `CancellationToken` support
- Full `IDisposable` implementation
- Input validation with `ArgumentNullException.ThrowIfNull`
- Disposal checks with `ObjectDisposedException.ThrowIf`

## Skill Structure

Each skill follows this structure:

```
skill-name/
├── SKILL.md                  # Main documentation with YAML frontmatter
├── HelperClass1.cs           # Optional production helper class
├── HelperClass2.cs           # Optional production helper class
└── ...
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

## Additional Resources

- [MSAL.NET Documentation](https://aka.ms/msal-net)
- [mTLS PoP Integration Tests](../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- [Managed Identity E2E Tests](../../tests/Microsoft.Identity.Test.E2e/)
- [MSAL.NET GitHub Repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
