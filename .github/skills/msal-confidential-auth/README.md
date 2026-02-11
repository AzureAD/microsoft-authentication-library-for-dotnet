# MSAL.NET Confidential Client Authentication Skill

A GitHub Agent Skill for Microsoft Authentication Library (MSAL) for .NET confidential client scenarios.

## About This Skill

This skill enables GitHub Copilot Agents to provide guided assistance with implementing confidential client authentication patterns in MSAL.NET, covering three core authentication flows:

- **Authorization Code Flow** - Web applications with user sign-in
- **On-Behalf-Of (OBO) Flow** - Multi-tier services acting on behalf of users
- **Client Credentials Flow** - Service-to-service daemon applications

With support for multiple credential types:
- Standard certificates
- Certificates with SNI (Subject Name Identifier)
- System-assigned managed identities
- User-assigned managed identities

## Structure

```
msal-confidential-auth/
├── auth-code-flow/           # Web app authentication
│   └── SKILL.md              # Auth code flow definition
├── obo-flow/                 # Multi-tier authentication
│   └── SKILL.md              # OBO flow definition
├── client-credentials/       # Daemon/service authentication
│   └── SKILL.md              # Client credentials flow definition
└── shared/                   # Shared resources for all flows
    ├── code-examples/        # Reusable code snippets
    │   ├── with-certificate.cs
    │   ├── with-certificate-sni.cs
    │   └── with-federated-identity-credentials.cs
    ├── credential-setup/     # Setup guides for each credential type
    │   ├── certificate-setup.md
    │   ├── certificate-sni-setup.md
    │   └── federated-identity-credentials.md
    └── patterns/             # Common patterns and best practices
        ├── token-caching-strategies.md
        ├── error-handling-patterns.md
        └── troubleshooting.md
```

## Agent Capabilities

For each flow, agents can help with:

1. **Generate Code Snippet** - Show code for [flow] with [credential type]
2. **Setup Guidance** - When do I set up [credential type]?
3. **Error Resolution** - I'm getting [error], what's the solution?
4. **Best Practices** - What are the best practices for [scenario]?
5. **Explain the Flow** - Explain how [flow] works
6. **Decision Help** - Which flow should I use for [scenario]?
7. **Validate Code** - Review and validate MSAL implementation for correctness and best practices

## Getting Started

### Choose Your Authentication Flow

- **Web Application** → See [Authorization Code Flow](auth-code-flow/SKILL.md)
- **Multi-Tier Service** → See [On-Behalf-Of Flow](obo-flow/SKILL.md)
- **Daemon Service** → See [Client Credentials Flow](client-credentials/SKILL.md)

### Choose Your Credential Type

- **Standard Certificate** → [Certificate Setup](shared/credential-setup/certificate-setup.md)
- **Certificate with SNI** → [Certificate with SNI Setup](shared/credential-setup/certificate-sni-setup.md)
- **Federated Identity Credentials** → [Federated Identity Credentials Setup](shared/credential-setup/federated-identity-credentials.md)

### Common Challenges

- Token Caching → [Token Caching Strategies](shared/patterns/token-caching-strategies.md)
- Error Handling → [Error Handling Patterns](shared/patterns/error-handling-patterns.md)
- Troubleshooting → [Troubleshooting Guide](shared/patterns/troubleshooting.md)

## References

- [MSAL.NET Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-overview)
- [OAuth 2.0 Authorization Code Flow](https://tools.ietf.org/html/draft-ietf-oauth-v2-1-01)
- [Azure Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
