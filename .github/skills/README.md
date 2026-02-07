# GitHub Agent Skills for MSAL.NET

This directory contains **GitHub Agent Skills** that help AI assistants understand and implement MSAL.NET authentication patterns, particularly for mTLS Proof-of-Possession (PoP) flows.

## What Are Skills?

**GitHub Agent Skills** are an open standard for providing context and guidance to AI coding assistants like GitHub Copilot, Claude, ChatGPT, and others. They act as reusable "knowledge modules" that teach agents about specific technologies, patterns, and conventions in your codebase.

Skills use YAML frontmatter for metadata and Markdown for content, making them both machine-readable and human-friendly. When an AI assistant encounters a skill file, it uses that information to:
- Understand domain-specific terminology and conventions
- Generate code that follows project patterns
- Provide accurate guidance for complex scenarios
- Reference production-ready helper classes

**Learn More:**
- [GitHub Skills Documentation](https://github.com/features/copilot)
- [Skills Open Standard](https://github.com/skills-ai)

## Supported AI Assistants

These skills work with:
- **GitHub Copilot** (Chat, Inline, CLI)
- **Claude** (via Claude Desktop, API)
- **ChatGPT** (via file upload, plugins)
- **Other assistants** that support Markdown context

## How to Use Skills

### Option 1: Repository-Level (Automatic)
Skills in `.github/skills/` are automatically discovered by GitHub Copilot when working in this repository. No action needed!

### Option 2: User-Level (Personal Assistant)
Copy skill files to your personal assistant's context:
- **GitHub Copilot**: Reference skills with `@workspace` in chat
- **Claude Desktop**: Add to project context or conversation
- **ChatGPT**: Upload skill files for long-form assistance

### Option 3: Direct Chat Reference
Mention skills explicitly in your prompts:
```
@workspace "How do I implement mTLS PoP for Azure Key Vault?"
# References: .github/skills/msal-mtls-pop-vanilla/SKILL.md
```

## Skills vs. Instructions

**Skills** (this directory):
- **Purpose**: Teach patterns and provide production code
- **Audience**: AI assistants generating code
- **Format**: YAML frontmatter + Markdown
- **Examples**: Authentication flows, helper classes, domain terminology

**Instructions** (`.github/copilot-instructions.md`):
- **Purpose**: Set coding style, review standards, workflow preferences
- **Audience**: AI assistants reviewing or suggesting code
- **Format**: Plain Markdown
- **Examples**: "Use async/await", "Follow PEP 8", "Prefer composition over inheritance"

## Available Skills

### 1. mTLS PoP Guidance (`msal-mtls-pop-guidance/`)
**Shared terminology and conventions** for MSAL.NET's mTLS Proof-of-Possession flows.
- Defines "vanilla flow" vs. "FIC two-leg flow"
- Clarifies PoP token types (mTLS PoP, jwt-pop)
- Establishes reviewer expectations

**Use when:** Starting any mTLS PoP implementation or reviewing related code.

### 2. Vanilla mTLS PoP Flow (`msal-mtls-pop-vanilla/`)
**Direct PoP token acquisition** for calling protected resources (Azure Key Vault, Microsoft Graph, custom APIs).
- `SKILL.md`: Step-by-step guidance for vanilla flow
- `ResourceCaller.cs`: Production helper for HTTP calls with PoP tokens
- `MtlsPopTokenAcquirer.cs`: Simplified token acquisition wrapper

**Use when:** Acquiring mTLS PoP tokens for direct resource access (no delegation/exchange).

### 3. FIC Two-Leg Exchange (`msal-mtls-pop-fic-two-leg/`)
**Token exchange pattern** using assertions for cross-tenant scenarios or delegation.
- `SKILL.md`: Step-by-step guidance for two-leg assertion flow
- `FicAssertionProvider.cs`: Builds `ClientSignedAssertion` from Leg 1 token
- `FicLeg1Acquirer.cs`: Leg 1 token-exchange acquisition
- `FicLeg2Exchanger.cs`: Leg 2 assertion exchange with optional PoP

**Use when:** Implementing federated identity credential (FIC) flows, cross-tenant access, or service-to-service delegation.

## Helper Classes

Each skill includes **production-grade C# helper classes** that you can copy into your project. These classes:
- Encapsulate common mTLS PoP patterns
- Follow MSAL.NET conventions (`async`/`await`, `ConfigureAwait(false)`, `CancellationToken`)
- Handle error cases and edge conditions
- Are tested against real Azure environments
- Serve as reference implementations for custom scenarios

**Why C# files in a skills directory?**  
Following the pattern from [microsoft-identity-web](https://github.com/AzureAD/microsoft-identity-web), production helper classes make skills actionable. AI assistants can:
- Reference concrete code examples (not just prose)
- Copy-paste working patterns directly into user projects
- Understand MSAL.NET idioms through real implementations

## Testing

Skills and helper classes are based on verified test code in:
```
tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs
```

Key test methods:
- **Vanilla flow**: `Sni_Gets_Pop_Token_Successfully_TestAsync` (lines 36-84)
- **FIC two-leg flow**: `Sni_AssertionFlow_Uses_JwtPop_And_Succeeds_TestAsync` (lines 86-178)

## Troubleshooting

**AI assistant not finding skills?**
1. Ensure you're working in the repository root
2. Try `@workspace` mentions in GitHub Copilot Chat
3. Manually reference the skill file path in your prompt

**Helper classes not compiling?**
- Helper classes are **examples**, not shipped library code
- Add necessary `using` statements for your project
- Adjust namespaces to match your codebase
- Install required NuGet packages (`Microsoft.Identity.Client`, etc.)

**Need more help?**
- See [MSAL.NET Wiki](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki)
- Review [test code](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
- Open an issue with the `question` label

## Contributing

To add or improve skills:
1. Follow the existing structure (YAML frontmatter + Markdown)
2. Include `skill_name`, `version`, `description`, `applies_to`, and `tags` in frontmatter
3. Provide production-ready helper classes where applicable
4. Reference test code that validates the pattern
5. Use concise, "seasoned developer" tone (assume audience knows OAuth2/MSAL basics)

## Related Documentation

- [MSAL.NET Documentation](https://docs.microsoft.com/azure/active-directory/develop/msal-overview)
- [mTLS Proof-of-Possession Overview](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-mtls)
- [Client Credentials Flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow)
- [RFC 8705: OAuth 2.0 Mutual-TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
