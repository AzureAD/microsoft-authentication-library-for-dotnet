# On-Behalf-Of (OBO) Flow Skill

## Overview
OBO (On-Behalf-Of) Flow enables a web API to act on behalf of an authenticated user to access downstream APIs. The web API receives a user token, validates it, and exchanges it for a token to call another API while maintaining the user's identity and context.

## When to Use
- Web APIs receiving user tokens from clients
- Need to access downstream APIs on behalf of authenticated users
- Multi-tier applications with user context propagation
- User authorization context must flow through service chain

## Flow Steps
1. Client calls web API with user access token in Authorization header
2. Web API validates the incoming token
3. Web API exchanges user token for new token scoped for downstream API
4. Web API calls downstream API on behalf of user

## Important: Token Types
⚠️ **Always pass an access token, NOT an ID token** to `AcquireTokenOnBehalfOf()`  
ID tokens are for authentication; access tokens are for authorization and API access.

## Agent Actions

### Generate Code Snippet
Agent can show code for each credential type:
- Standard Certificate: [with-certificate.cs](../shared/code-examples/with-certificate.cs)
- Certificate with SNI: [with-certificate-sni.cs](../shared/code-examples/with-certificate-sni.cs)
- Federated Identity Credentials: [with-federated-identity-credentials.cs](../shared/code-examples/with-federated-identity-credentials.cs)

### Setup Guidance
Reference appropriate credential setup:
- [Certificate Setup](../shared/credential-setup/certificate-setup.md)
- [Certificate with SNI](../shared/credential-setup/certificate-sni-setup.md)
- [Federated Identity Credentials](../shared/credential-setup/federated-identity-credentials.md)

### Example: Web API with Certificate
```csharp
// In web API controller receiving user token
[HttpGet("api/data")]
public async Task<IActionResult> GetData()
{
    // Extract access token from Authorization header
    var authHeader = Request.Headers["Authorization"].ToString();
    var userToken = authHeader.Replace("Bearer ", "");
    
    // See: with-certificate.cs for credential setup
    var app = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithCertificate(cert)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
        .Build();

    // Create UserAssertion with access token (not ID token)
    var userAssertion = new UserAssertion(userToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");
    
    var result = await app.AcquireTokenOnBehalfOf(
        new[] { "scope-uri" },
        userAssertion)
        .ExecuteAsync();

    // Use result.AccessToken to call downstream API
    return Ok(result.AccessToken);
}
```

### Error Resolution
Refer to [Troubleshooting Guide](../shared/patterns/troubleshooting.md)

**Common OBO errors:**
- `MsalUiRequiredException`: MFA or conditional access required—requires client re-authentication
- Invalid token: Verify access token (not ID token) is passed

### Best Practices
- Use [Token Caching Strategies](../shared/patterns/token-caching-strategies.md) for optimal session-based token caching
- Implement [Error Handling Patterns](../shared/patterns/error-handling-patterns.md)
- Always validate incoming token before using in OBO
- Extract `tid` claim from user token for guest user scenarios—use tenant-specific authority, not /common
- For multi-instance deployments and advanced caching, see [Token cache serialization documentation](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal)

### Explain the Flow
1. **Token Reception**: Web API receives user's access token from client
2. **Token Validation**: Web API validates token signature and claims
3. **Token Exchange**: Web API calls `AcquireTokenOnBehalfOf()` with user's token + client credentials
4. **Scoped Token**: AAD returns new token scoped for downstream API
5. **Downstream Call**: Web API calls downstream service with new token

### Decision Help
**Choose OBO if:**
- Building multi-tier web API architecture
- Receiving user tokens in web API
- Need to maintain user context through service chain
- Authenticating with downstream APIs on behalf of user

**Avoid if:**
- Direct client-to-API communication (use Auth Code Flow)
- Service-to-service with no user context (use Client Credentials)

