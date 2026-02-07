# GitHub Agent Skills for MSAL.NET

This directory contains GitHub Agent Skills that help developers implement authentication and authorization patterns using Microsoft Authentication Library for .NET (MSAL.NET).

## What are GitHub Agent Skills?

GitHub Agent Skills are structured documentation and code examples designed to work with GitHub Copilot and other AI-powered development tools. Each skill provides:

- **Structured Documentation**: Markdown files with YAML frontmatter for AI agents to understand the skill's purpose
- **Production-Ready Code**: Helper classes and examples that can be directly used in applications
- **Best Practices**: Guidance on security, performance, and maintainability

## Available Skills

### mTLS PoP (Mutual TLS Proof of Possession)

Three complementary skills for implementing mTLS Proof of Possession authentication:

1. **msal-mtls-pop-vanilla** - Direct token acquisition using mTLS PoP
   - System-Assigned Managed Identity (SAMI)
   - User-Assigned Managed Identity (UAMI)
   - Confidential Client with certificates
   - Helper classes: `MtlsPopTokenAcquirer`, `ResourceCaller`

2. **msal-mtls-pop-fic-two-leg** - FIC (Federated Identity Credential) two-leg flow
   - Leg 1: Acquire assertion token
   - Leg 2: Exchange assertion for access token with mTLS PoP
   - All 4 scenarios: SAMI/UAMI + Vanilla/FIC
   - Helper classes: `FicLeg1Acquirer`, `FicLeg2Exchanger`

3. **msal-mtls-pop-guidance** - High-level guidance and troubleshooting
   - When to use vanilla vs FIC two-leg flow
   - Common pitfalls and solutions
   - Security considerations

## Prerequisites

All mTLS PoP skills require:

- **MSAL.NET 4.82.1 or higher** (critical for mTLS PoP APIs)
- .NET 6.0 or higher (net8.0 recommended)

## How to Use These Skills

### With GitHub Copilot

GitHub Copilot Chat will automatically discover and use these skills when you ask questions about MSAL.NET authentication patterns.

Example prompts:
- "Help me implement mTLS PoP with Managed Identity"
- "How do I use the FIC two-leg flow for token acquisition?"
- "What's the difference between vanilla and FIC flows for mTLS PoP?"

### As Documentation

Each skill's `SKILL.md` file contains comprehensive documentation that can be read directly:

- Code examples with all required namespaces
- Step-by-step implementation guides
- Troubleshooting sections for common issues

### As Production Code

Helper classes (`.cs` files) follow MSAL.NET conventions and can be copied directly into your projects:

- Full async/await support with `ConfigureAwait(false)`
- Proper disposal patterns (`IDisposable`)
- Input validation and error handling
- CancellationToken support

## Skill Structure

Each skill directory contains:

```
skill-name/
├── SKILL.md              # Main documentation with YAML frontmatter
├── HelperClass1.cs       # Optional: Production-ready helper class
└── HelperClass2.cs       # Optional: Additional helper classes
```

### YAML Frontmatter

Each `SKILL.md` starts with YAML metadata:

```yaml
---
skill_name: skill-identifier
version: 1.0.0
description: Brief description of the skill
applies_to:
  - language: csharp
  - framework: dotnet
tags:
  - msal
  - authentication
  - mtls-pop
---
```

## Contributing

When adding or updating skills:

1. **Follow Conventions**: Use the same structure and patterns as existing skills
2. **Test Code Examples**: Ensure all code compiles and works correctly
3. **Include Troubleshooting**: Add common issues and solutions
4. **Update Prerequisites**: Specify exact version requirements
5. **Add Complete Namespaces**: Include all required `using` statements

## Additional Resources

- [MSAL.NET Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki)
- [mTLS PoP Design Document](../../docs/sni_mtls_pop_token_design.md)
- [MSAL.NET Samples](../../tests/Microsoft.Identity.Test.Integration.netcore/)

## Support

For issues or questions about these skills:

1. Check the troubleshooting section in each `SKILL.md`
2. Review the test files in `tests/Microsoft.Identity.Test.Integration.netcore/`
3. Open an issue in the MSAL.NET repository
