# Authorization Code Flow Skill

## Overview
Authorization Code Flow is used by web applications to authenticate users and obtain access tokens on their behalf.

## When to Use
- Web applications with server-side backend
- Need to access user-scoped APIs
- User sign-in required
- Refresh tokens needed

## Flow Steps
1. Redirect user to AAD login page
2. User logs in and consents to permissions
3. AAD returns authorization code to callback URL
4. Server exchanges code for token using confidential credentials
5. Token cached and used to access APIs

## Agent Actions

### Generate Code Snippet
Agent can show code snippets for each credential type:
- Standard Certificate: [with-certificate.cs](../shared/code-examples/with-certificate.cs)
- Certificate with SNI: [with-certificate-sni.cs](../shared/code-examples/with-certificate-sni.cs)
- Federated Identity Credentials: [with-federated-identity-credentials.cs](../shared/code-examples/with-federated-identity-credentials.cs)

### Setup Guidance
Reference appropriate credential setup:
- [Certificate Setup](../shared/credential-setup/certificate-setup.md)
- [Certificate with SNI](../shared/credential-setup/certificate-sni-setup.md)
- [Federated Identity Credentials](../shared/credential-setup/federated-identity-credentials.md)

### Example: Web Application with Certificate
```csharp
// In controller's callback method
[HttpGet("auth/callback")]
public async Task HandleCallback(string code, string state)
{
    // See: with-certificate.cs for credential setup
    var app = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithCertificate(cert)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
        .WithRedirectUri("https://myapp.com/auth/callback")
        .Build();

    var result = await app.AcquireTokenByAuthorizationCode(
        new[] { "scope-uri" },
        code)
        .ExecuteAsync();

    // Result contains AccessToken, RefreshToken, ExpiresOn
}
```

### Error Resolution
Refer to [Troubleshooting Guide](../shared/patterns/troubleshooting.md)

### Best Practices
- Use [Token Caching Strategies](../shared/patterns/token-caching-strategies.md) for optimal token acquisition
- Implement [Error Handling Patterns](../shared/patterns/error-handling-patterns.md)
- Store refresh tokens securely
- Use PKCE for native clients
- For advanced caching options including distributed caches for multi-instance deployments, see [Token cache serialization documentation](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal)

### Explain the Flow
1. **Initiation**: Redirect to `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize?client_id=...&redirect_uri=...`
2. **User Action**: User logs in and grants consent
3. **Code Reception**: AAD sends authorization code to redirect URI
4. **Token Exchange**: Server uses code + client credentials to get token
5. **Token Usage**: Token cached and used for API calls

### Decision Help
**Choose Auth Code Flow if:**
- Building web application with server backend
- Need to access user resources with user consent
- Want to maintain long-lived sessions (using refresh tokens)

**Avoid if:**
- Building single-page app (use implicit/hybrid instead)
- Don't have secure backend for credentials
