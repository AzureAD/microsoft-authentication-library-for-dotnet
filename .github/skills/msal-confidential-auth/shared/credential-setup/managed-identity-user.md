# User-Assigned Managed Identity Setup

## Overview
User-assigned managed identities are created as standalone Azure resources and can be assigned to multiple services.

## Prerequisites
- User-assigned managed identity created in Azure
- Application running in Azure
- Managed Identity assigned to your resource
- Service Principal has required permissions

## Create User-Assigned Identity
```powershell
New-AzUserAssignedIdentity -ResourceGroupName "myRG" -Name "myIdentity"
```

## Assign to Resource
1. Navigate to your resource in Azure Portal
2. Settings → Identity → User Assigned
3. Click "Add"
4. Select your user-assigned identity
5. Save

## Usage in Code
```csharp
var userAssignedClientId = "00000000-0000-0000-0000-000000000000";
var credential = new ManagedIdentityCredential(userAssignedClientId);
```

## Grant Permissions
```powershell
$identityPrincipalId = (Get-AzUserAssignedIdentity -ResourceGroupName "myRG" -Name "myIdentity").PrincipalId
New-AzRoleAssignment -ObjectId $identityPrincipalId -RoleDefinitionName "Contributor" -Scope $resourceId
```

## When to Use
- **Multiple resources** - Share one identity across services
- **Flexible assignments** - Add/remove from resources easily
- **Explicit control** - Fine-grained management of identity lifecycle
- **Multi-team scenarios** - Different teams managing different identities

## Advantages over System-Assigned
- Reusable across multiple resources
- Better lifecycle management
- Explicit ownership and auditing
