# MSAL.NET Shared Resources

Shared credential setup guides, code examples, and patterns referenced by all MSAL.NET skills (confidential client authentication, mTLS PoP, FIC two-leg, and others). Centralizing these resources follows the DRY principle: update once, referenced everywhere.

## Contents

### `credential-setup/` — Setup guides by credential type

| File | Description |
|------|-------------|
| [certificate-setup.md](credential-setup/certificate-setup.md) | Load certificates from file, Windows Certificate Store, or Azure Key Vault |
| [certificate-sni-setup.md](credential-setup/certificate-sni-setup.md) | Subject Name/Issuer (SNI) configuration with `sendX5c: true` |
| [federated-identity-credentials.md](credential-setup/federated-identity-credentials.md) | Keyless authentication using Federated Identity Credentials (FIC) in Azure Portal |

### `patterns/` — Common patterns and best practices

| File | Description |
|------|-------------|
| [token-caching-strategies.md](patterns/token-caching-strategies.md) | In-memory, distributed, and static cache strategies |
| [error-handling-patterns.md](patterns/error-handling-patterns.md) | Handling `MsalException`, retry logic, and common error codes |
| [troubleshooting.md](patterns/troubleshooting.md) | Comprehensive troubleshooting guide for all credential types |

### `code-examples/` — Copy-paste C# snippets

| File | Description |
|------|-------------|
| [with-certificate.cs](code-examples/with-certificate.cs) | Standard certificate authentication |
| [with-certificate-sni.cs](code-examples/with-certificate-sni.cs) | Certificate with SNI (`sendX5c: true`) |
| [with-federated-identity-credentials.cs](code-examples/with-federated-identity-credentials.cs) | FIC (keyless) authentication |

## Used By

These resources are referenced by the following skills:

| Skill | References |
|-------|-----------|
| [msal-confidential-auth/auth-code-flow](../msal-confidential-auth/auth-code-flow/SKILL.md) | credential-setup/, patterns/, code-examples/ |
| [msal-confidential-auth/obo-flow](../msal-confidential-auth/obo-flow/SKILL.md) | credential-setup/, patterns/, code-examples/ |
| [msal-confidential-auth/client-credentials](../msal-confidential-auth/client-credentials/SKILL.md) | credential-setup/, patterns/, code-examples/ |
| [msal-mtls-pop-guidance](../msal-mtls-pop-guidance/SKILL.md) | credential-setup/, patterns/ |
| [msal-mtls-pop-vanilla](../msal-mtls-pop-vanilla/SKILL.md) | credential-setup/, patterns/ |
| [msal-mtls-pop-fic-two-leg](../msal-mtls-pop-fic-two-leg/SKILL.md) | credential-setup/, patterns/ |
