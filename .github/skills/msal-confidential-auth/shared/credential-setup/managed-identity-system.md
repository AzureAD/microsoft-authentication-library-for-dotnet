# System-Assigned Managed Identity Setup

## Overview
System-assigned managed identities are automatically created and managed by Azure for your resource.

## Prerequisites
- Application running in Azure (App Service, Container, VM, etc.)
- Managed Identity enabled on the resource
- Service Principal has required permissions

## Enable Managed Identity
1. In Azure Portal, navigate to your resource
2. Settings → Identity → System Assigned
3. Toggle Status to "On"
4. Save

## Usage in Code
```csharp
var credential = new ManagedIdentityCredential();
```

## Grant Permissions
```powershell
# Grant API permissions via Azure CLI
az ad sp show --id <app-service-principal-id> --query objectId -o tsv | 
xargs -I {} az role assignment create --role "API Permissions" --assignee {}
```

## Object ID Discovery
```powershell
# Get the Service Principal Object ID
$spObjectId = (Get-AzADServicePrincipal -Filter "appId eq '$clientId'").Id
```

## When to Use
- **Native Azure resources** - App Services, Functions, Container Instances
- **Simplified management** - No credentials to manage
- **Zero-trust model** - Leverage Azure infrastructure security

## Limitations
- Only works within Azure
- Credentials cannot be extracted
