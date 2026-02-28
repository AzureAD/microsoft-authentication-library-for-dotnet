# ManagedIdentityWebApi_SF

A Service Fabric stateless service that tests MSAL's `ManagedIdentityApplication` for token acquisition in Service Fabric environments.

## Overview

This project provides an ASP.NET Core Web API hosted as a Service Fabric stateless service. It demonstrates how to use the Microsoft Authentication Library (MSAL) to acquire tokens via Managed Identity when running inside Azure Service Fabric.

## Architecture

```
ManagedIdentityWebApi_SF (Service Fabric Stateless Service)
├── Program.cs                   - Service Fabric host entry point
├── ManagedIdentityWebApiService.cs - Stateless service with Kestrel listener
├── ServiceEventSource.cs        - ETW event tracing for Service Fabric
├── Controllers/
│   └── AppServiceController.cs  - REST API controller for token acquisition
└── PackageRoot/
    ├── ServiceManifest.xml       - Service type and endpoint definitions
    ├── ApplicationManifest.xml   - Application type and default services
    └── Config/
        └── Settings.xml          - Service configuration settings
```

The service uses:
- **MSAL `ManagedIdentityApplicationBuilder`** to acquire Azure AD tokens via Managed Identity
- **Service Fabric Kestrel integration** to host ASP.NET Core inside the SF runtime
- **ETW tracing** via `ServiceEventSource` for diagnostics in Service Fabric tooling

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Service Fabric SDK](https://docs.microsoft.com/azure/service-fabric/service-fabric-get-started) (for local cluster deployment)
- Visual Studio 2022 with the Azure development and Service Fabric workloads (recommended)
- An Azure Service Fabric cluster with Managed Identity enabled (for Azure deployment)

## Quick Start

### Local Development (without Service Fabric)

You can run the Web API directly without a Service Fabric cluster for initial testing:

```bash
cd "tests/devapps/Managed Identity apps/ManagedIdentityWebApi_SF"
dotnet run
```

> **Note:** Managed Identity token acquisition requires an Azure environment. For local testing, you will receive an error from the MSAL endpoint, which is expected.

### Deploy to Local Service Fabric Cluster

See [DEPLOYMENT.md](DEPLOYMENT.md#local-service-fabric-cluster) for step-by-step instructions.

### Deploy to Azure Service Fabric Cluster

See [DEPLOYMENT.md](DEPLOYMENT.md#azure-service-fabric-cluster) for step-by-step instructions.

## API Documentation

### GET /appservice

Acquires a Managed Identity token from Azure AD.

**Query Parameters**

| Parameter       | Required | Description                                               |
|-----------------|----------|-----------------------------------------------------------|
| `resourceuri`   | Yes      | The resource URI to acquire a token for (e.g., `https://management.azure.com/`) |
| `userAssignedId`| No       | Client ID of a user-assigned managed identity. Omit for system-assigned. |

**Example Requests**

System-assigned managed identity:
```http
GET /appservice?resourceuri=https://management.azure.com/
```

User-assigned managed identity:
```http
GET /appservice?resourceuri=https://management.azure.com/&userAssignedId=<client-id>
```

**Example Response**

Success:
```
Access token received. Token Source: IdentityProvider
```

Cached:
```
Access token received. Token Source: Cache
```

Error (MSAL exception as JSON):
```json
{"error_code":"managed_identity_unreachable_network","error_description":"..."}
```

## Testing

After deploying the service, verify token acquisition works:

```bash
# System-assigned identity
curl "http://<node-ip>:8454/appservice?resourceuri=https://management.azure.com/"

# User-assigned identity
curl "http://<node-ip>:8454/appservice?resourceuri=https://management.azure.com/&userAssignedId=<client-id>"
```

Expected output: `Access token received. Token Source: IdentityProvider` on first call, `Token Source: Cache` on subsequent calls within the token lifetime.

## Troubleshooting

| Issue | Likely Cause | Resolution |
|-------|-------------|------------|
| `managed_identity_unreachable_network` | IMDS endpoint not available | Ensure the Service Fabric node has network access to `169.254.169.254` |
| `managed_identity_failed_response` | Managed identity not configured | Assign a system-assigned or user-assigned managed identity to the VM Scale Set backing the SF cluster |
| Service fails to start | Port 8454 in use | Update the port in `PackageRoot/ServiceManifest.xml` |
| `ServiceHostInitializationFailed` | Service type registration failure | Check Service Fabric event logs and the ETW trace |

## Related Projects

- [ManagedIdentityWebApi](../ManagedIdentityWebApi) – Plain ASP.NET Core version (App Service / Container Apps)
- [ManagedIdentityAppVM](../ManagedIdentityAppVM) – Console app for VM environments
- [MSIHelperService](../MSIHelperService) – Helper service for E2E managed identity tests
