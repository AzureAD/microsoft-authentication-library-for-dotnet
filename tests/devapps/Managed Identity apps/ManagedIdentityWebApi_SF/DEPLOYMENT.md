# Deployment Guide – ManagedIdentityWebApi_SF

This guide covers deploying the `ManagedIdentityWebApi_SF` Service Fabric application to both a local development cluster and an Azure Service Fabric cluster.

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Service Fabric SDK](https://docs.microsoft.com/azure/service-fabric/service-fabric-get-started) (Windows only for local cluster)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for Azure deployment)
- PowerShell 5.1+ or PowerShell Core (for Service Fabric PowerShell module)
- Service Fabric PowerShell module: `Install-Module -Name Microsoft.ServiceFabric`

---

## Local Service Fabric Cluster

### Step 1 – Start the Local Cluster

Open the **Service Fabric Local Cluster Manager** from the system tray, or run:

```powershell
& "$env:ProgramFiles\Microsoft SDKs\Service Fabric\ClusterSetup\DevClusterSetup.ps1"
```

Wait until the cluster is healthy (all nodes show green in Service Fabric Explorer at `http://localhost:19080`).

### Step 2 – Build the Application

From the repository root:

```bash
dotnet publish "tests/devapps/Managed Identity apps/ManagedIdentityWebApi_SF/ManagedIdentityWebApi_SF.csproj" \
  -c Release -f net8.0 -o publish/ManagedIdentityWebApi_SF
```

### Step 3 – Package the Application

Copy the published output into the Service Fabric package layout:

```powershell
$pkg = "pkg\ManagedIdentityWebApi_SFType"
New-Item -ItemType Directory -Force "$pkg\ManagedIdentityWebApi_SFPkg\Code"
New-Item -ItemType Directory -Force "$pkg\ManagedIdentityWebApi_SFPkg\Config"

# Copy published binaries
Copy-Item publish\ManagedIdentityWebApi_SF\* "$pkg\ManagedIdentityWebApi_SFPkg\Code\" -Recurse

# Copy manifests and config
Copy-Item "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\PackageRoot\ServiceManifest.xml" `
          "$pkg\ManagedIdentityWebApi_SFPkg\ServiceManifest.xml"
Copy-Item "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\PackageRoot\ApplicationManifest.xml" `
          "$pkg\ApplicationManifest.xml"
Copy-Item "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\PackageRoot\Config\Settings.xml" `
          "$pkg\ManagedIdentityWebApi_SFPkg\Config\Settings.xml"
```

### Step 4 – Deploy to Local Cluster

```powershell
Import-Module "$env:ProgramFiles\Microsoft SDKs\Service Fabric\Tools\PSModule\ServiceFabricSDK\ServiceFabricSDK.psm1"

Connect-ServiceFabricCluster localhost:19000

Publish-NewServiceFabricApplication `
  -ApplicationPackagePath pkg\ManagedIdentityWebApi_SFType `
  -ApplicationName fabric:/ManagedIdentityWebApi_SF
```

### Step 5 – Verify Deployment

1. Open Service Fabric Explorer: `http://localhost:19080`
2. Navigate to **Applications → ManagedIdentityWebApi_SFType → fabric:/ManagedIdentityWebApi_SF**
3. Confirm the service status is **OK**
4. Test the endpoint:

```bash
curl "http://localhost:8454/appservice?resourceuri=https://management.azure.com/"
```

> **Note:** Managed Identity will not work on a local cluster because the IMDS endpoint is not available. Expect a network error response, which confirms the service is running correctly.

### Cleanup – Local Cluster

```powershell
Remove-ServiceFabricApplication -ApplicationName fabric:/ManagedIdentityWebApi_SF -Force
Unregister-ServiceFabricApplicationType -ApplicationTypeName ManagedIdentityWebApi_SFType `
  -ApplicationTypeVersion 1.0.0 -Force
```

---

## Azure Service Fabric Cluster

### Prerequisites for Azure Deployment

- An Azure Service Fabric cluster with **Managed Identity enabled**
- A VM Scale Set with a **system-assigned** or **user-assigned** managed identity
- The identity must have appropriate RBAC permissions on the target Azure resource

### Step 1 – Enable Managed Identity on the Cluster

If your cluster uses VM Scale Sets, enable managed identity:

```bash
# System-assigned
az vmss identity assign \
  --resource-group <resource-group> \
  --name <vmss-name>

# User-assigned
az vmss identity assign \
  --resource-group <resource-group> \
  --name <vmss-name> \
  --identities <managed-identity-resource-id>
```

### Step 2 – Build and Publish

```bash
dotnet publish "tests/devapps/Managed Identity apps/ManagedIdentityWebApi_SF/ManagedIdentityWebApi_SF.csproj" \
  -c Release -f net8.0 -o publish/ManagedIdentityWebApi_SF
```

### Step 3 – Package and Upload to Azure Storage

Service Fabric requires the application package to be uploaded to the cluster's image store or an Azure storage account.

```powershell
# Connect to the cluster (update endpoint and certificate thumbprint)
Connect-ServiceFabricCluster `
  -ConnectionEndpoint "<cluster-fqdn>:19000" `
  -X509Credential `
  -ServerCertThumbprint "<server-cert-thumbprint>" `
  -FindType FindByThumbprint `
  -FindValue "<client-cert-thumbprint>" `
  -StoreLocation CurrentUser `
  -StoreName My

# Copy the package to the cluster image store
Copy-ServiceFabricApplicationPackage `
  -ApplicationPackagePath pkg\ManagedIdentityWebApi_SFType `
  -ImageStoreConnectionString fabric:ImageStore `
  -ApplicationPackagePathInImageStore ManagedIdentityWebApi_SFType
```

### Step 4 – Register and Deploy

```powershell
# Register the application type
Register-ServiceFabricApplicationType -ApplicationPathInImageStore ManagedIdentityWebApi_SFType

# Create the application instance
New-ServiceFabricApplication `
  -ApplicationName fabric:/ManagedIdentityWebApi_SF `
  -ApplicationTypeName ManagedIdentityWebApi_SFType `
  -ApplicationTypeVersion 1.0.0
```

### Step 5 – Configure Networking

Ensure that the Azure Load Balancer allows inbound traffic on port **8454** (or the port defined in `ServiceManifest.xml`):

```bash
az network lb rule create \
  --resource-group <resource-group> \
  --lb-name <lb-name> \
  --name ManagedIdentityWebApiRule \
  --protocol tcp \
  --frontend-port 8454 \
  --backend-port 8454 \
  --frontend-ip-name LoadBalancerIPConfig \
  --backend-pool-name LoadBalancerBEAddressPool \
  --probe-name FabricGatewayProbe
```

### Step 6 – Verify Deployment

1. Open Service Fabric Explorer at `https://<cluster-fqdn>:19080`
2. Navigate to the application and confirm the health status is **OK**
3. Test the managed identity endpoint:

```bash
# System-assigned identity
curl "http://<cluster-ip>:8454/appservice?resourceuri=https://management.azure.com/"

# User-assigned identity
curl "http://<cluster-ip>:8454/appservice?resourceuri=https://management.azure.com/&userAssignedId=<client-id>"
```

Expected response: `Access token received. Token Source: IdentityProvider`

### Step 7 – Monitoring

View service logs in Service Fabric Explorer or query ETW traces:

```powershell
# Get service health
Get-ServiceFabricServiceHealth -ServiceName fabric:/ManagedIdentityWebApi_SF/ManagedIdentityWebApi_SF

# Get application health
Get-ServiceFabricApplicationHealth -ApplicationName fabric:/ManagedIdentityWebApi_SF
```

### Configuration Options

You can override the instance count at deployment time:

```powershell
New-ServiceFabricApplication `
  -ApplicationName fabric:/ManagedIdentityWebApi_SF `
  -ApplicationTypeName ManagedIdentityWebApi_SFType `
  -ApplicationTypeVersion 1.0.0 `
  -ApplicationParameter @{ "ManagedIdentityWebApi_SF_InstanceCount" = "3" }
```

### Cleanup – Azure Cluster

```powershell
Remove-ServiceFabricApplication -ApplicationName fabric:/ManagedIdentityWebApi_SF -Force

Unregister-ServiceFabricApplicationType `
  -ApplicationTypeName ManagedIdentityWebApi_SFType `
  -ApplicationTypeVersion 1.0.0 `
  -Force

Remove-ServiceFabricApplicationPackage `
  -ImageStoreConnectionString fabric:ImageStore `
  -ApplicationPackagePathInImageStore ManagedIdentityWebApi_SFType
```

---

## Updating the Application

To deploy a new version, update the `Version` attributes in `ServiceManifest.xml` and `ApplicationManifest.xml`, rebuild, repackage, and run:

```powershell
Copy-ServiceFabricApplicationPackage ...
Register-ServiceFabricApplicationType ...
Start-ServiceFabricApplicationUpgrade `
  -ApplicationName fabric:/ManagedIdentityWebApi_SF `
  -ApplicationTypeVersion <new-version> `
  -HealthCheckStableDurationSec 60 `
  -UpgradeDomainTimeoutSec 1200 `
  -UpgradeTimeout 3000 `
  -FailureAction Rollback `
  -Monitored
```
