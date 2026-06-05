# Federated Identity Credentials (FIC) Setup

## Overview
Federated Identity Credentials (FIC) is a modern, keyless authentication method that establishes a trust relationship between your application and an Azure Managed Identity. Instead of using certificates or secrets, the managed identity issues a federated token that your application exchanges for access tokens.

## Benefits
- **No credential management** - No certificates to rotate or secrets to secure
- **Zero-trust compatible** - Leverages Azure's identity infrastructure
- **Audit trail** - Full identity tracking through managed identity
- **Automatic renewal** - No manual credential rotation needed

## Prerequisites
- Application registered in Azure AD/Entra ID
- Azure Managed Identity (system or user-assigned)
- Managed Identity with appropriate permissions for target resources

## Azure Portal Configuration

### 1. Create or Identify Managed Identity
```powershell
# Create user-assigned managed identity
az identity create --resource-group <resource-group> --name <identity-name>

# Get the client ID
az identity show --resource-group <resource-group> --name <identity-name> --query clientId -o tsv

# Get the principal ID (object ID)
az identity show --resource-group <resource-group> --name <identity-name> --query principalId -o tsv

# Get the tenant ID
az identity show --resource-group <resource-group> --name <identity-name> --query tenantId -o tsv
```

### 2. Configure Federated Credentials
Navigate to **App Registration** → **Certificates & secrets** → **Federated credentials** → **Add credential**

**Configuration for Managed Identity:**
| Property | Value |
|----------|-------|
| **Federated credential scenario** | Other Issuer |
| **Issuer** | `https://login.microsoftonline.com/{tenantID}/v2.0` (use MI tenant ID) |
| **Subject identifier** | Principal ID/Object ID of managed identity (GUID format) |
| **Name** | Descriptive name (e.g., "MI-FIC-Production") |
| **Audience** | Cloud-specific token exchange URI (e.g., `api://AzureADTokenExchange` for Public cloud). See [Cloud-specific token exchange audiences](#cloud-specific-token-exchange-audiences) below. |

### 3. Grant Permissions to Managed Identity
Assign the managed identity permissions on target resources:
```powershell
# Example: Grant contributor role on resource group
az role assignment create --assignee <principalId> --role "Contributor" --scope <resource-id>
```

## User-Assigned Managed Identity (Primary Pattern)

Use client ID for the best user experience:
```csharp
var miClientId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
var miApplication = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId(miClientId))
    .Build();
```

To retrieve user-assigned MI client ID:
```powershell
az identity show --resource-group <rg> --name <name> --query clientId -o tsv
```

## System-Assigned Managed Identity (Alternative)

For resources with system-assigned identity:
```csharp
var miApplication = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();
```

System-assigned MI is automatically created with the resource and has no configurable ID.

## Alternative Identifiers (Advanced)

For non-standard scenarios, you can also use:

**Object ID**
```csharp
ManagedIdentityId.WithUserAssignedObjectId(objectId)
```
Use when: Managing identities across tenants or when principal ID is more convenient

**Resource ID**
```csharp
ManagedIdentityId.WithUserAssignedResourceId(resourceId)
```
Use when: Working with Azure Resource Manager or infrastructure-as-code

See [ManagedIdentityId API documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.client.appconfig.managedidentityid?view=msal-dotnet-latest) for details.

## Cloud-specific token exchange audiences

The FIC `audience` value is **cloud-specific**. ESTS validates the audience of the incoming managed-identity assertion against the AAD Token Exchange app URI for the cloud where the token exchange is performed. Using the wrong value results in error `7002206 UserFederatedIdentityTokenAudienceMustBeTokenExchange` — *"Forbidden token audience. Token audience must match the cloud-specific AAD Token Exchange App Uri."*

| Cloud                | Audience (Resource URI)              |
|----------------------|--------------------------------------|
| Public (commercial)  | `api://AzureADTokenExchange`         |
| US Gov (Fairfax)     | `api://AzureADTokenExchangeUSGov`    |
| China (Mooncake)     | `api://AzureADTokenExchangeChina`    |
| France (Bleu)        | `api://AzureADTokenExchangeFrance`   |
| Germany (Delos)      | `api://AzureADTokenExchangeGermany`  |

> MSAL .NET has authority/instance discovery support for Bleu and Delos (see `KnownMetadataProvider`, added in v4.82.0). Use the audience that matches the cloud where the **token exchange** is performed, not necessarily the cloud of the target resource.

## Common Issues

### "Issuer does not match"
- Verify issuer URL uses the **managed identity's tenant ID**, not the application tenant
- Format: `https://login.microsoftonline.com/{MI_TENANT_ID}/v2.0`

### "Subject identifier not found"
- Ensure subject is the **principal ID** (Object ID) of the managed identity
- Must be in GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

### "Audience not recognized" / `UserFederatedIdentityTokenAudienceMustBeTokenExchange` (error 7002206)
- The audience must match the **cloud-specific** AAD Token Exchange URI — see the [Cloud-specific token exchange audiences](#cloud-specific-token-exchange-audiences) table.
- `api://AzureADTokenExchange` is correct **only** for the Public (commercial) cloud.
- For US Gov use `api://AzureADTokenExchangeUSGov`; for China (Mooncake) use `api://AzureADTokenExchangeChina`; for France (Bleu) use `api://AzureADTokenExchangeFrance`; for Germany (Delos) use `api://AzureADTokenExchangeGermany`.
- Verify the assertion's `aud` claim before token exchange — ESTS will reject mismatches even if the issuer and subject are valid.

## Migration from Certificates to FIC

If migrating from certificate-based auth:
1. Set up managed identity and FIC (keep existing certificate as backup)
2. Deploy updated application code using FIC
3. Monitor and validate authentication works
4. Remove certificate from Azure AD once stable

## References
- [Federated Identity Credentials Microsoft Docs](https://learn.microsoft.com/en-us/entra/identity-platform/workload-identity-federation-create-trust)
- [ManagedIdentityId API Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.client.appconfig.managedidentityid?view=msal-dotnet-latest)
- [Token cache serialization](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal)
