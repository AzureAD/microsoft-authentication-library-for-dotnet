# GitHub Copilot Agent Skills for MSAL.NET

This directory contains Agent Skills that provide guidance for implementing and documenting mTLS Proof-of-Possession (PoP) flows in MSAL.NET.

## What are Agent Skills?

Agent Skills are structured documentation files that help GitHub Copilot and other AI agents understand domain-specific patterns, conventions, and best practices. They enable consistent code generation, documentation, and code reviews by providing:

- **Canonical flow definitions** - Clear specifications for authentication patterns
- **Code templates** - Ready-to-use examples with real API signatures
- **Terminology guidelines** - Consistent language across documentation and code
- **Reviewer checklists** - Quality gates for PRs and documentation

## Available Skills

| Skill | Description | Use When |
|-------|-------------|----------|
| [msal-mtls-pop-guidance](./msal-mtls-pop-guidance/SKILL.md) | Shared guidance covering terminology, conventions, and reviewer expectations | Writing or reviewing any mTLS PoP documentation or code |
| [msal-mtls-pop-vanilla](./msal-mtls-pop-vanilla/SKILL.md) | Direct PoP token acquisition (no token exchange) | Implementing direct resource calls with MSI or SNI |
| [msal-mtls-pop-fic-two-leg](./msal-mtls-pop-fic-two-leg/SKILL.md) | FIC two-leg exchange pattern using assertions | Implementing token exchange for cross-tenant or delegation scenarios |

## mTLS PoP Flow Taxonomy

### Vanilla Flow (No Legs)
**What**: Direct token acquisition with mTLS PoP for a target resource  
**When**: Application directly calls Graph, Key Vault, or custom APIs  
**How**: Single `AcquireTokenForClient()` call with `.WithMtlsProofOfPossession()`  
**Example**: App → MSAL → Azure AD → mTLS PoP token for `https://vault.azure.net`

### FIC Two-Leg Flow
**What**: Token exchange pattern using assertions for delegated access  
**When**: Cross-tenant scenarios, service chaining, or federated identity credentials  
**How**: 
1. **Leg 1**: Acquire assertion token for `api://AzureADTokenExchange`
2. **Leg 2**: Exchange assertion for target resource token using `WithClientAssertion()`

**Example**: 
- Leg 1: App → MSAL → Azure AD → assertion token
- Leg 2: App → MSAL (with assertion) → Azure AD → mTLS PoP token for target resource

## Folder Layout

```
.github/skills/
├── README.md                          # This file - registry and overview
├── msal-mtls-pop-guidance/
│   └── SKILL.md                       # Shared guidance and conventions
├── msal-mtls-pop-vanilla/
│   └── SKILL.md                       # Vanilla flow patterns (MSI, SNI)
└── msal-mtls-pop-fic-two-leg/
    └── SKILL.md                       # FIC two-leg exchange patterns
```

## How to Use These Skills

### For Code Generation
When generating mTLS PoP code:
1. Start with [msal-mtls-pop-guidance](./msal-mtls-pop-guidance/SKILL.md) to understand terminology
2. Choose the appropriate flow skill based on your scenario
3. Follow the code templates and adapt to your needs

### For Documentation
When writing mTLS PoP documentation:
1. Review [msal-mtls-pop-guidance](./msal-mtls-pop-guidance/SKILL.md) for terminology rules
2. Reference the relevant flow skill for technical accuracy
3. Use the pre-PR checklist before submitting

### For Code Reviews
When reviewing mTLS PoP PRs:
1. Check [msal-mtls-pop-guidance](./msal-mtls-pop-guidance/SKILL.md) for reviewer expectations
2. Verify code follows the patterns in the flow-specific skill
3. Ensure documentation uses consistent terminology

## YAML Frontmatter for Agents

Each skill file includes YAML frontmatter with metadata:

```yaml
---
skill_name: "MSAL.NET mTLS PoP - [Flow Name]"
version: "1.0"
description: "Brief description"
applies_to:
  - "Relevant file patterns"
tags:
  - "msal"
  - "mtls-pop"
  - "authentication"
---
```

This structured metadata helps AI agents:
- Identify when to apply the skill
- Understand the scope and purpose
- Cross-reference related skills

## Testing Reference Files

The skills reference actual test implementations:

- **Vanilla Flow**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 36-84)
  - Test method: `Sni_Gets_Pop_Token_Successfully_TestAsync`
  
- **FIC Two-Leg Flow**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` (lines 86-178)
  - Test method: `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync`

## Contributing

When adding new skills:
1. Follow the existing structure and YAML frontmatter format
2. Include concrete code examples from actual tests
3. Provide clear "when to use" guidance
4. Add the skill to the registry table above
5. Update this README with any new taxonomy

## Related Resources

- [Issue #5712](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5712) - Feature Request: Add GitHub Agent Skill support
- [PR #5726](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5726) - Add IMDSv2 E2E tests for mTLS PoP
- [MSAL.NET Documentation](https://learn.microsoft.com/azure/active-directory/develop/msal-net-client-assertions)
- [RFC 8705 - OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
