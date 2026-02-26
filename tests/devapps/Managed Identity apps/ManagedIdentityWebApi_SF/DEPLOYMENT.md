# Deployment runbook: **ManagedIdentityWebApi_SF** on **Azure Service Fabric Managed Cluster (SFMC)**

This runbook deploys the sample Service Fabric app **ManagedIdentityWebApi_SF** (MSAL .NET repo) onto an **Azure Service Fabric Managed Cluster** and validates **Managed Identity** token acquisition via MSAL.

---

## What you get at the end

- A Service Fabric Managed Cluster you can access with **Service Fabric Explorer (SFX)**.
- The app deployed as `fabric:/ManagedIdentityWebApi_SF`.
- Public test endpoint:

  `http://<clusterFqdn>:8454/appservice?resourceuri=<RESOURCE>&userAssignedId=<UAMI_CLIENT_ID>`

- A successful response like: `Access token received. Token Source: IdentityProvider`

---

## Variables (fill these in)

> Your environment (example values you already used are included under **Example**).

| Item | Placeholder | Example |
|---|---|---|
| Resource group | `<rg>` | `rg-sfmc-msal` |
| Region | `<region>` | `westus` |
| Cluster name | `<clusterName>` | `sfmc-client` |
| Cluster FQDN | `<clusterFqdn>` | `sfmc-client.westus.cloudapp.azure.com` |
| Cluster management endpoint | `<clusterFqdn>:19000` | `sfmc-client.westus.cloudapp.azure.com:19000` |
| SFX endpoint | `https://<clusterFqdn>:19080` | `https://sfmc-client.westus.cloudapp.azure.com:19080` |
| App port | `8454` | `8454` |
| User Assigned MI name | `<uamiName>` | `uami-sfmc-msal` |
| UAMI Client ID | `<uamiClientId>` | *(copy from UAMI Overview)* |
| Client cert thumbprint | `<clientTp>` | `E2D76BC85A0A850AE7D7A3D6B4A1995713FE8B40` |
| Server cert thumbprint | `<serverTp>` | `52DFB221CD9114A8A4C5035A2999C83D97FB003` *(format doesn’t matter)* |

---

# Part A — Azure setup (Portal)

## A1) Create the Service Fabric Managed Cluster (SFMC)

If you already have the cluster, skip to **A2**.

1. **Create resource group**
   - Azure Portal → **Resource groups** → **Create**
   - Name: `<rg>` (example: `rg-sfmc-msal`)
   - Region: `<region>` (example: `westus`)

2. **Create Key Vault**
   - Azure Portal → **Key vaults** → **Create**
   - Put it in the same RG/region.

3. **Create certificate in Key Vault**
   - Key Vault → **Certificates** → **Generate/Import**
   - Create a self-signed certificate (example CN: `sfmcclient`)
   - Download PFX and import it into your Windows cert store (**CurrentUser\My**).

4. **Create Service Fabric Managed Cluster**
   - Create resource → **Service Fabric Managed Cluster**
   - Choose **Basic** SKU for a test cluster
   - Provide admin username/password
   - Choose the **Key Vault + certificate**
   - Finish creation (this can take a while).

5. **Open Service Fabric Explorer**
   - Cluster resource → find SFX link or open: `https://<clusterFqdn>:19080`
   - Select your client certificate when prompted.

---

## A2) Confirm the managed resource group exists

SFMC creates a **managed resource group** that starts with something like:

- `SFC_<guid>`

In that RG you’ll see resources like:

- Load Balancer (example: `LB-sfmc-client`)
- VM Scale Set (example: `nodetype1`)
- NSG (example: `SF-NSG`)
- Public IP (example: `PublicIP-sfmc-client`)
- VNet

This is expected.

---

## A3) Create a User Assigned Managed Identity (UAMI)

If you already created `uami-sfmc-msal`, skip to **A4**.

1. Azure Portal → search **Managed identities**
2. **Create** → **User assigned**
3. Name: `<uamiName>` (example: `uami-sfmc-msal`)
4. After creation, open it and copy:
   - **Client ID** → save as `<uamiClientId>`
   - **Resource ID** (for template-based assignment if needed)

---

## A4) Assign the UAMI to the compute (node type / VMSS)

You can do this in **two ways**. The fastest for troubleshooting is **VMSS Identity** (B). The “managed” method is template-based (A).

### Option A (managed method): update node type `vmManagedIdentity` via template

1. Open your **Service Fabric managed cluster** (not the `SFC_...` RG)
2. **Export template** → **Deploy** → **Edit template**
3. Find the node type resource:
   - Type: `Microsoft.ServiceFabric/managedClusters/nodeTypes` (or `.../nodetypes`)
4. Ensure the node type `apiVersion` is **2021-05-01 or newer** (example: `2022-01-01`).
5. Add inside `properties`:

```json
"vmManagedIdentity": {
  "userAssignedIdentities": [
    "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', '<uamiName>')]"
  ]
}
```

6. Save and **Deploy**.

### Option B (fastest): assign UAMI directly to the VM Scale Set in the managed RG

1. Go to managed RG: `SFC_<guid>`
2. Open the VM Scale Set: **`nodetype1`**
3. Left menu → **Identity**
4. Tab: **User assigned**
5. **Add** → select your UAMI (`<uamiName>`) → **Save**

✅ This is the step that fixed your `IMDS: invalid_request, Identity not found`.

---

## A5) Open port **8454** to the internet (LB + NSG)

You must do BOTH:
- A Load Balancer rule (frontend 8454 → backend 8454)
- An NSG inbound allow rule for 8454

### Option A (managed way): add managed cluster `loadBalancingRules`

1. Open your **managed cluster** resource (`sfmc-client`)
2. **Export template** → **Deploy** → **Edit template**
3. Find resource type: `Microsoft.ServiceFabric/managedClusters`
4. Inside `properties`, add:

```json
"loadBalancingRules": [
  {
    "frontendPort": 8454,
    "backendPort": 8454,
    "probeProtocol": "tcp",
    "protocol": "tcp"
  }
]
```

5. Deploy.

### Option B (fast way): manually add LB rule + NSG rule in managed RG

1. Managed RG (`SFC_<guid>`) → open LB: `LB-<clusterName>`
2. **Load balancing rules** → **Add**
   - Name: `App8454`
   - Protocol: TCP
   - Frontend port: 8454
   - Backend port: 8454
   - Backend pool: `LoadBalancerBEAddressPool`
   - Health probe: create TCP probe on port 8454
3. Managed RG → open NSG: `SF-NSG`
4. **Inbound security rules** → **Add**
   - Destination port: 8454
   - Protocol: TCP
   - Action: Allow
   - Priority: choose an unused one (e.g. 3002)
   - Source: *My IP* (recommended) or Any (test)

---

# Part B — Local machine prerequisites (Windows)

## B1) Install tooling

You need these on the machine you deploy from:

- Git
- .NET SDK 8.x
- PowerShell 5.1+ or PowerShell 7+
- **Service Fabric Runtime + SDK** (for native DLLs used by ServiceFabric PowerShell)

### Quick validation commands

```powershell
# Module existence
Get-Module -ListAvailable ServiceFabric*
Get-Command Connect-ServiceFabricCluster

# Native DLL presence (typical path)
Test-Path "C:\Program Files\Microsoft Service Fabric\bin\Fabric\FabricCommon.dll"
```

If `FabricCommon.dll` is missing, install:
- Service Fabric Runtime (EXE)
- Service Fabric SDK (MSI)

> You already fixed the error: **Unable to load DLL FabricCommon.dll** by installing runtime+SDK.

---

# Part C — Build + package the app

## C1) Get the PR code

```powershell
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
git fetch origin pull/5782/head:pr-5782
git checkout pr-5782
```

App directory:

`tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF`

---

## C2) Publish

From repo root:

```powershell
dotnet publish "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\ManagedIdentityWebApi_SF.csproj" `
  -c Release -f net8.0 -r win-x64 --self-contained true `
  -o "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\publish\ManagedIdentityWebApi_SF"
```

Sanity check:

```powershell
Get-ChildItem "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF\publish\ManagedIdentityWebApi_SF" | Select Name
```

You should see `ManagedIdentityWebApi_SF.exe`.

---

## C3) Create Service Fabric package

> Important: run this **from the app folder**:
>
> `tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF`
>
> If you run it from elsewhere, update paths accordingly.

```powershell
cd "tests\devapps\Managed Identity apps\ManagedIdentityWebApi_SF"

$pkg    = ".\pkg\ManagedIdentityWebApi_SFType"
$svcPkg = "$pkg\ManagedIdentityWebApi_SFPkg"

New-Item -ItemType Directory -Force "$svcPkg\Code"   | Out-Null
New-Item -ItemType Directory -Force "$svcPkg\Config" | Out-Null

Copy-Item ".\publish\ManagedIdentityWebApi_SF\*" "$svcPkg\Code\" -Recurse -Force

Copy-Item ".\PackageRoot\ServiceManifest.xml"     "$svcPkg\ServiceManifest.xml" -Force
Copy-Item ".\PackageRoot\ApplicationManifest.xml" "$pkg\ApplicationManifest.xml" -Force
Copy-Item ".\PackageRoot\Config\Settings.xml"     "$svcPkg\Config\Settings.xml" -Force
```

Verify:

```powershell
Get-ChildItem ".\pkg\ManagedIdentityWebApi_SFType" -Recurse | Select FullName
```

---

# Part D — Connect to the cluster and deploy

## D1) Connect (PowerShell)

```powershell
Import-Module ServiceFabric -Force

# Ensure SF native runtime DLLs can be found
$sfBin = "C:\Program Files\Microsoft Service Fabric\bin\Fabric\Fabric.Code"
if ($env:PATH -notlike "*$sfBin*") { $env:PATH = "$sfBin;$env:PATH" }

$endpoint = "<clusterFqdn>:19000"
$serverTp = "<serverTp>"
$clientTp = "<clientTp>"

Connect-ServiceFabricCluster -ConnectionEndpoint $endpoint -X509Credential `
  -ServerCertThumbprint $serverTp `
  -FindType FindByThumbprint -FindValue $clientTp `
  -StoreLocation CurrentUser -StoreName My

Get-ServiceFabricClusterHealth
```

✅ If you see `Cluster health OK`, you are connected.

---

## D2) Deploy / upgrade the application

Run from the app folder that contains `.\pkg\ManagedIdentityWebApi_SFType`:

```powershell
$appPackagePath = (Resolve-Path ".\pkg\ManagedIdentityWebApi_SFType").Path
$imageStorePath = "ManagedIdentityWebApi_SFType_$(Get-Date -Format 'yyyyMMdd_HHmmss')"

Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $appPackagePath `
  -ImageStoreConnectionString "fabric:ImageStore" `
  -ApplicationPackagePathInImageStore $imageStorePath

Register-ServiceFabricApplicationType -ApplicationPathInImageStore $imageStorePath

[xml]$am = Get-Content (Join-Path $appPackagePath "ApplicationManifest.xml")
$appTypeName    = $am.ApplicationManifest.ApplicationTypeName
$appTypeVersion = $am.ApplicationManifest.ApplicationTypeVersion
$appName        = "fabric:/ManagedIdentityWebApi_SF"

if (-not (Get-ServiceFabricApplication -ApplicationName $appName -ErrorAction SilentlyContinue)) {
  New-ServiceFabricApplication -ApplicationName $appName `
    -ApplicationTypeName $appTypeName `
    -ApplicationTypeVersion $appTypeVersion
} else {
  Start-ServiceFabricApplicationUpgrade -ApplicationName $appName `
    -ApplicationTypeVersion $appTypeVersion `
    -UpgradeMode Monitored
}

Get-ServiceFabricApplication
Get-ServiceFabricService -ApplicationName $appName
```

---

# Part E — Verify + test

## E1) Verify in Service Fabric Explorer

Open:

- `https://<clusterFqdn>:19080`

Confirm:
- Application `fabric:/ManagedIdentityWebApi_SF` exists and is healthy.

You may also see the internal node endpoint in SFX like:

- `http://10.0.0.4:8454`

That confirms the service is listening inside the cluster.

---

## E2) Test from your machine (public endpoint)

First check the port is reachable:

```powershell
Test-NetConnection <clusterFqdn> -Port 8454
```

Then call the service:

```text
http://<clusterFqdn>:8454/appservice?resourceuri=https%3A%2F%2Fmanagement.azure.com%2F&userAssignedId=<uamiClientId>
```

Example (your environment):

```text
http://sfmc-client.westus.cloudapp.azure.com:8454/appservice?resourceuri=https%3A%2F%2Fmanagement.azure.com%2F&userAssignedId=<UAMI_CLIENT_ID>
```

Expected:
- `Access token received. Token Source: IdentityProvider`

---

# Troubleshooting quick map

## 1) `Unable to load DLL FabricCommon.dll`
- Install Service Fabric Runtime + SDK
- Ensure `C:\Program Files\Microsoft Service Fabric\bin\Fabric\Fabric.Code` is on PATH (or add it in-session like in D1)

## 2) `CertificateNotMatched`
- Your server thumbprint != client thumbprint
- Get **server** thumbprint from cluster certificate (browser cert viewer or cluster JSON)
- Use **client** thumbprint from `Cert:\CurrentUser\My`

## 3) Can’t connect to `:19000`
- LB/NSG must allow 19000 (SFMC usually creates this)
- Verify LB rules include FabricTcpGateway and NSG allows it

## 4) App deploy succeeded but `:8454` times out
- LB rule for 8454 missing OR NSG inbound for 8454 missing
- Fix Part A5

## 5) App reachable but MI fails: `IMDS invalid_request Identity not found`
- The UAMI is not assigned to the node type / VMSS
- Fix Part A4 (Option B is fastest)

---

# Cleanup (optional)

## Remove the app

```powershell
$appName = "fabric:/ManagedIdentityWebApi_SF"
Remove-ServiceFabricApplication -ApplicationName $appName -Force
```

## Unregister application type (optional)

```powershell
# List types
Get-ServiceFabricApplicationType

# Unregister (update name/version accordingly)
Unregister-ServiceFabricApplicationType -ApplicationTypeName "ManagedIdentityWebApi_SFType" -ApplicationTypeVersion "1.0.0" -Force
```

## Close the port
- Remove LB rule + NSG inbound rule for 8454 (if you opened it manually), or remove it from `loadBalancingRules` and redeploy.

## Remove the UAMI assignment
- VMSS → Identity → User assigned → remove UAMI (if you assigned directly)

---

## Notes / good practices

- The service binds to a **fixed port 8454**. Do not schedule multiple instances on the same node (port conflict).
- For security, prefer restricting NSG source IP to your own instead of `Any`.
- This is a test app; don’t leave it publicly open in production.

