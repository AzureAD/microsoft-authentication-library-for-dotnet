# AI Agent Instructions for MSAL.NET

## Repository Overview

This repository contains the Microsoft Authentication Library for .NET
(MSAL.NET). It enables .NET applications to acquire tokens from the Microsoft
identity platform for work and school accounts, personal Microsoft accounts,
and Azure AD B2C.

Keep changes small, focused, backward compatible, and consistent with nearby
code. Authentication, token caching, protocol, and public API changes require
particular care because MSAL.NET is a widely consumed security library.

## Authoritative Guidance

Before changing code, read and follow:

- `.github/copilot-instructions.md`
- all Markdown files under `.clinerules/`
- the relevant skill under `.github/skills/` when the task concerns an
  authentication flow or mTLS Proof-of-Possession
- `CONTRIBUTING.md` for contribution and design-proposal expectations, as well as build and test instructions

More specific instructions override this file.

## Repository Layout

- `src/client/Microsoft.Identity.Client/`: core MSAL.NET library
- `src/client/Microsoft.Identity.Client.Broker/`: broker integration
- `src/client/Microsoft.Identity.Client.Desktop/`: desktop platform support
- `src/client/Microsoft.Identity.Client.Extensions.Msal/`: token cache
  extensions, for public client scenarios
- `src/client/Microsoft.Identity.Client.KeyAttestation/`: key attestation
  support, for mTLS POP support
- `tests/Microsoft.Identity.Test.Unit/`: main unit test project
- `tests/Microsoft.Identity.Test.Integration.*`: integration test projects
- `tests/Microsoft.Identity.Test.Common/`: shared test infrastructure
- `tests/CacheCompat/`: cross-library cache compatibility tests
- `tests/devapps/`: development and manual test applications
- `tests/Microsoft.Identity.Test.Performance/`: performance coverage
- `build/`: CI templates, run settings, and build support
- `docs/`: repository documentation
- `tools/`: development tools

`LibsAndSamples.sln` is the full solution. `LibsAndSamples.sdk.slnf` is the
smaller SDK-style solution filter and is usually faster for local work.

## Building and Testing

This is a standard .net project and standard dotnet tooling applies. The only complexity is that this is a multi-target SDK which explicitly targets: 

- .NET Framework and .NET
- Netstandard 2.0
- mobile target frameworks 

Note that the regular build ignores the mobile target frameworks. The CI builds those. 

Unit tests use MSTest SDK v4 and NSubstitute. Follow nearby naming patterns and
use `// Arrange`, `// Act`, and `// Assert` comments. Prefer deterministic unit
tests that mock the HTTP layer. Integration tests are slow, require protected
Azure resources, and should be reserved for mainline scenarios.

## Coding Requirements

- Follow `.editorconfig`; do not make style-only or drive-by refactors.
- Preserve existing file headers, line endings, nullability, and local patterns.
- Use async APIs consistently and retain `ConfigureAwait(false)` in library
  code.
- Validate inputs at method boundaries and throw specific exception types.
- Never log or expose tokens, secrets, credentials, claims, or PII.

See .github/instructions folder for more rules.