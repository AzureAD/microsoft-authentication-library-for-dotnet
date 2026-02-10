# Client Credentials Flow Skill

## Overview
Client Credentials Flow is used for service-to-service authentication without user involvement. Ideal for daemon applications and background services.

## When to Use
- Service-to-service authentication
- Daemon/background applications
- Machine-to-machine communication
- No user context needed
- Automated processes

## Flow Steps
1. Service authenticates using client credentials (certificate or managed identity)
2. Service directly calls authorization endpoint with credentials
3. AAD validates credentials and returns access token
4. Token cached and used to access APIs as application identity

## Agent Actions

### Generate Code Snippet
Agent can show code for each credential type:
- Standard Certificate: [with-certificate.cs](../shared/code-examples/with-certificate.cs)
- Certificate with SNI: [with-certificate-sni.cs](../shared/code-examples/with-certificate-sni.cs)
- System-Assigned MI: [with-managed-identity-system.cs](../shared/code-examples/with-managed-identity-system.cs)
- User-Assigned MI: [with-managed-identity-user.cs](../shared/code-examples/with-managed-identity-user.cs)

### Setup Guidance
Reference appropriate credential setup:
- [Certificate Setup](../shared/credential-setup/certificate-setup.md)
- [Certificate with SNI](../shared/credential-setup/certificate-sni-setup.md)
- [System-Assigned Identity](../shared/credential-setup/managed-identity-system.md)
- [User-Assigned Identity](../shared/credential-setup/managed-identity-user.md)

### Example: Service with Certificate
```csharp
// Acquire token for service-to-service authentication
public class TokenAcquisitionService
{
    private readonly IConfidentialClientApplication _app;

    public TokenAcquisitionService(string clientId, X509Certificate2 cert)
    {
        // See: with-certificate.cs for credential setup
        _app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithCertificate(cert)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
            .Build();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var result = await _app.AcquireTokenForClient(
            new[] { "resource-uri" })
            .ExecuteAsync();

        return result.AccessToken;
    }
}
```

### Error Resolution
Refer to [Troubleshooting Guide](../shared/patterns/troubleshooting.md)

### Best Practices
- Use [Token Caching Strategies](../shared/patterns/token-caching-strategies.md) to optimize performance
- Implement [Error Handling Patterns](../shared/patterns/error-handling-patterns.md) 
- Monitor token acquisition metrics
- Rotate certificates periodically
- Use managed identity in Azure for automatic credential management

### Explain the Flow
1. **Credential Submission**: Service authenticates directly with AAD using certificate or MI
2. **No User Involved**: Authentication is machine-to-machine only
3. **Access Grant**: AAD validates credentials and issues access token
4. **Token Caching**: Token automatically cached for subsequent requests
5. **API Access**: Token used to call downstream APIs as application identity

### Decision Help
**Choose Client Credentials if:**
- Building daemon/background service
- Service-to-service authentication needed
- No user context involved
- Want simplest flow for automated processes

**Avoid if:**
- Need to access user-scoped resources
- User consent required
- Need refresh tokens for long-lived sessions
