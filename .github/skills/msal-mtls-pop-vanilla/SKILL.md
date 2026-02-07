---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: Direct token acquisition using mTLS Proof of Possession (vanilla flow) with MSAL.NET for Managed Identity and Confidential Client scenarios
applies_to:
  - language: csharp
  - framework: dotnet
tags:
  - msal
  - authentication
  - mtls-pop
  - managed-identity
  - confidential-client
  - security
---

# mTLS PoP Vanilla Flow - Direct Token Acquisition

This skill covers **direct token acquisition** using mTLS Proof of Possession (PoP) with MSAL.NET. This is also called the "vanilla flow" because it directly acquires tokens without intermediate assertion exchanges.

## Prerequisites

- **MSAL.NET 4.82.1 or higher** (required for `WithMtlsProofOfPossession()` and `BindingCertificate`)
- .NET 6.0 or higher (net8.0 recommended)

### Update MSAL NuGet Package

If you're using an older version, update immediately:

```bash
dotnet add package Microsoft.Identity.Client --version 4.82.1
# or to get the latest version
dotnet package update Microsoft.Identity.Client
```

### Version Check

If you see errors like:
- "`WithMtlsProofOfPossession()` method not found"
- "`BindingCertificate` property missing on AuthenticationResult"
- "`ManagedIdentityId` not found"

**Your MSAL package is outdated.** Update to 4.82.1+ immediately.

## What is the Vanilla Flow?

The vanilla flow is the simplest way to get an mTLS PoP token:

1. Configure your token acquirer with `.WithMtlsProofOfPossession()`
2. Call `AcquireTokenAsync()` with the resource URI
3. Receive an `AuthenticationResult` with a PoP token and binding certificate
4. Use the token to call protected resources

**No intermediate legs or assertion exchanges** - just direct token acquisition.

## Supported Identity Types

### System-Assigned Managed Identity (SAMI)

**Azure-only**: Runs on App Service, VMs, Functions, Container Instances, AKS, etc.

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;  // ← CRITICAL for ManagedIdentityId

namespace MtlsPopSamiGraph
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // System-Assigned Managed Identity (SAMI)
                var acquirer = MtlsPopTokenAcquirer.CreateForManagedIdentity(
                    ManagedIdentityId.SystemAssigned);
                
                var authResult = await acquirer.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Token type: {authResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {authResult.BindingCertificate?.Thumbprint}");
                
                using var caller = new ResourceCaller(authResult);
                string meJson = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/me");
                Console.WriteLine($"Graph /me response: {meJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### User-Assigned Managed Identity (UAMI)

UAMI can be specified by **Client ID**, **Resource ID**, or **Object ID**:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;  // ← CRITICAL for ManagedIdentityId

namespace MtlsPopUamiGraph
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // User-Assigned by Client ID
                var uamiClientId = ManagedIdentityId.WithUserAssignedClientId(
                    "6325cd32-9911-41f3-819c-416cdf9104e7");
                
                // Or by Resource ID
                var uamiResourceId = ManagedIdentityId.WithUserAssignedResourceId(
                    "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami");
                
                // Or by Object ID
                var uamiObjectId = ManagedIdentityId.WithUserAssignedObjectId(
                    "ecb2ad92-3e30-4505-b79f-ac640d069f24");
                
                // Use one of the above
                var acquirer = MtlsPopTokenAcquirer.CreateForManagedIdentity(uamiClientId);
                
                var authResult = await acquirer.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Token type: {authResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {authResult.BindingCertificate?.Thumbprint}");
                
                using var caller = new ResourceCaller(authResult);
                string meJson = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/me");
                Console.WriteLine($"Graph /me response: {meJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

### Confidential Client with Certificate

Works anywhere (local, Azure, on-premises):

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MtlsPopConfidentialClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Load your application certificate
                var cert = new X509Certificate2("path/to/cert.pfx", "password");
                
                var acquirer = MtlsPopTokenAcquirer.CreateForConfidentialClient(
                    clientId: "your-client-id",
                    tenantId: "your-tenant-id",
                    certificate: cert,
                    region: "westus3"); // Optional: Azure region for regional endpoints
                
                var authResult = await acquirer.AcquireTokenAsync(
                    "https://graph.microsoft.com",
                    usePoP: true);
                
                Console.WriteLine($"Token type: {authResult.TokenType}");
                Console.WriteLine($"Binding cert thumbprint: {authResult.BindingCertificate?.Thumbprint}");
                
                using var caller = new ResourceCaller(authResult);
                string meJson = await caller.CallResourceAsync("https://graph.microsoft.com/v1.0/me");
                Console.WriteLine($"Graph /me response: {meJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

## Helper Classes

This skill provides two production-ready helper classes:

### MtlsPopTokenAcquirer

Simplifies token acquisition with mTLS PoP:

```csharp
// For Managed Identity
var acquirer = MtlsPopTokenAcquirer.CreateForManagedIdentity(
    ManagedIdentityId.SystemAssigned);

// For Confidential Client
var acquirer = MtlsPopTokenAcquirer.CreateForConfidentialClient(
    clientId: "your-client-id",
    tenantId: "your-tenant-id",
    certificate: cert);

// Acquire token
var authResult = await acquirer.AcquireTokenAsync(
    "https://graph.microsoft.com",
    usePoP: true);
```

### ResourceCaller

Makes HTTP calls using the acquired token:

```csharp
using var caller = new ResourceCaller(authResult);
string response = await caller.CallResourceAsync(
    "https://graph.microsoft.com/v1.0/me");
```

## Local Testing

**Important**: System-Assigned Managed Identity (SAMI) only works in Azure services (App Service, VM, Functions, Container Instances, AKS, etc.).

For local testing, use:
- **User-Assigned Managed Identity (UAMI)** via Azure CLI login
- **Confidential Client** with certificate - works anywhere

## Troubleshooting

### "`ManagedIdentityId` is not defined"
**Solution:** Add `using Microsoft.Identity.Client.AppConfig;`

### "`WithMtlsProofOfPossession()` method not found"
**Solution:** Upgrade MSAL: `dotnet package update Microsoft.Identity.Client`

Verify you have version 4.82.1 or higher:
```bash
dotnet list package | grep Microsoft.Identity.Client
```

### "`BindingCertificate` property is null"
**Solution:** Ensure `.WithMtlsProofOfPossession()` was called before token acquisition and that `usePoP: true` was passed to `AcquireTokenAsync()`

### "Timeout calling IMDS endpoint" (local machine)
**Solution:** You're using SAMI outside Azure. Switch to UAMI or Confidential Client for local testing.

### "Unable to get UAMI token"
**Possible causes:**
- UAMI doesn't exist in the subscription
- Current user lacks `Managed Identity Operator` role
- UAMI not assigned to your compute resource
- Try UAMI by different ID type (ClientId vs ResourceId vs ObjectId)

### "Certificate not found" or "Access denied"
For Confidential Client:
- Verify certificate path and password
- Ensure certificate has private key
- Check certificate is not expired
- On Windows, ensure user has access to private key in certificate store

## Security Considerations

1. **Certificate Management**: Binding certificates are ephemeral and automatically managed by MSAL
2. **Token Lifetime**: PoP tokens are short-lived; acquire new tokens as needed
3. **Network Security**: mTLS PoP requires TLS 1.2+ with proper certificate validation
4. **Private Key Protection**: Never log or expose private keys or certificates

## Performance Tips

1. **Reuse TokenAcquirer**: Create once, use multiple times
2. **Cache Tokens**: MSAL handles token caching automatically
3. **Parallel Calls**: Use HttpClient best practices for concurrent requests
4. **Regional Endpoints**: Specify `region` parameter for lower latency (Confidential Client only)

## Related Skills

- **msal-mtls-pop-fic-two-leg** - FIC (Federated Identity Credential) two-leg flow for assertion-based token exchange
- **msal-mtls-pop-guidance** - High-level guidance on choosing between vanilla and FIC flows

## Additional Resources

- [MSAL.NET Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki)
- [mTLS PoP Design Document](../../../docs/sni_mtls_pop_token_design.md)
- [Test Examples](../../../tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs)
