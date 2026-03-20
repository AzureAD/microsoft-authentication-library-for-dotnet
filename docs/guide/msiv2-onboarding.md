---
title: Configure Token Binding with Managed Identity (MSI) on a Trusted Launch VM or Confidential VM
description: Prepare a Windows Azure VM to use Token Binding with Managed Identity by enabling VBS/KeyGuard and installing required client components.
services: active-directory
author: GLJOHNS

ms.service: active-directory
ms.subservice: develop-1p
ms.topic: how-to
ms.date: 11/12/2025
ms.author: gljohns
ms.reviewer: nbhargava

#Customer intent: As an application owner, I want to enable Token Binding using a Managed Identity on an Azure Trusted Launch VM (TLVM) or Confidential VM (CVM) so my service can perform end-to-end (E2E) S2S Token Binding.
---

## What is Token Binding?

Token Binding (often implemented as PoP / mTLS tokens) makes access tokens harder to steal and replay:

- Instead of a **bearer** token that “anyone who has it can use”, Azure AD issues a token that is **cryptographically bound** to a private key.
- That key lives in a **VBS/KeyGuard‑protected container** on the VM, so only that VM (and that identity) can successfully use the token.
- Downstream services (for example, Key Vault) can then verify both the **identity** (MSI) and the **bound key** (via mTLS / PoP validation), enabling stronger end‑to‑end S2S security.

In this document, you’ll configure a Trusted Launch or Confidential VM so that its Managed Identity can request these **token‑bound** (PoP/mTLS) tokens and use them to call downstream services.

# Configure Token Binding with MSI on Trusted Launch or Confidential VMs

This guide shows how to prepare a Windows VM in Azure (Trusted Launch or Confidential) to support **Token Binding with Managed Identity (MSI)** and includes **sample code** to acquire token‑bound credentials.

> [!TIP]
> For the general **Managed identity with MSAL.NET** experience (without Token Binding or KeyGuard),
> see [Managed identity with MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/advanced/managed-identity).
> The rest of this article builds on that experience to show how to enable **Token Binding** on
> **Trusted Launch** and **Confidential** VMs.

You will:

1. Create a **Trusted Launch VM (TLVM)** or a **Confidential VM (CVM)** in the **Azure portal**.  
2. **Assign a Managed Identity** to the VM in the portal.  
3. **Enable Virtualization‑Based Security (VBS) and KeyGuard** inside the VM (PowerShell).  
4. Install **client SDK prerequisites** for E2E testing.  
5. Use **MSAL.NET** (Microsoft.Identity.Client **4.79.0 or later is recommended**) to request **PoP** tokens, over **mTLS**.

> [!IMPORTANT]
> These steps target **Windows** guests on Azure **Trusted Launch** or **Confidential** VMs.

---

## Prerequisites

- An Azure subscription and permissions to create/modify VMs and identities.  
- A supported Windows image (for example, *Windows Server 2022 Datacenter, Gen2*).  
- Local administrator access on the VM.  
- For CVMs: choose a **Confidential‑capable** VM size in a supported region (**Currently only supports Canary Regions: centraluseuap or eastus2euap**). 

> [!IMPORTANT]
> Your tenant must be allow‑listed for Token Binding to work. Email tokenbindingfeatureteam@service.microsoft.com to request allow‑listing for your tenant.

> [!NOTE]
> Token Binding works with either **system‑assigned (SAMI)** or **user‑assigned (UAMI)** managed identities. A **UAMI** is recommended when you need a stable client ID across redeployments or multiple VMs.

---

## 1. Create a Trusted Launch VM or Confidential VM (Azure portal)

1. Go to **[portal.azure.com](https://portal.azure.com)** → **Virtual machines** → **Create**.  
2. **Basics** tab: choose **Subscription**, **Resource group**, **Region**, and a **Windows Gen2** image (for example, *Windows Server 2022 Datacenter: Gen2*).  
3. **Administrator account**: specify credentials.  
4. Set **Security type** to **Trusted launch virtual machines** *or* **Confidential virtual machines**.  
   - For **Trusted launch**, ensure **Secure Boot** and **vTPM** are **On**.  
5. (Optional) **Management** tab → **Identity**: turn **System assigned** to **On**, or **Add** a **User‑assigned** identity. You can also do this after creation.  
6. Review + create → **Create**.

---

## 2. Assign a Managed Identity (Azure portal)

You can assign MI during creation (**Management → Identity**) or after deployment:

1. Open the VM → **Identity**.  
2. **System assigned**: set **Status** to **On** and **Save**.  
3. **User assigned** (recommended when you need a stable identity): select **+ Add** and choose your user‑assigned identity.  
4. Grant the identity appropriate roles on downstream services (for example, **Key Vault Secrets User** on your vault).

> [!IMPORTANT]
> For a user‑assigned managed identity, record the **client ID**, **object ID**, or **Azure resource ID**.  
> MSAL.NET lets you create the managed‑identity application with any of these identifiers (for example
> `ManagedIdentityId.WithUserAssignedClientId`, `WithUserAssignedObjectId`, or `WithUserAssignedResourceId`). 

---

## 3. Enable VBS and KeyGuard (inside the VM)

1. **RDP** into the VM with an administrator account.  
2. Open an **elevated** PowerShell/Command prompt.  
3. Run the following to enable **VBS** and **Hypervisor‑Enforced Code Integrity (HVCI)**:

```
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "EnableVirtualizationBasedSecurity" /t REG_DWORD /d 1 /f

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "RequirePlatformSecurityFeatures" /t REG_DWORD /d 1 /f

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "Locked" /t REG_DWORD /d 0 /f

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v "Enabled" /t REG_DWORD /d 1 /f

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v "Locked" /t REG_DWORD /d 0 /f
```

After running the commands, VBS typically shows **“Enabled and not running.”**

### Reboot to activate VBS

```powershell
Restart-Computer -Force
```

After the restart, VBS should show **“Running.”**

### Verify VBS/HVCI status

```powershell
(Get-CimInstance -ClassName Win32_ComputerSystem).HypervisorPresent
```
---

## 4. Client SDK setup (inside the VM)

Install the following components on the VM:

- **.NET 8 Runtime**  
  https://dotnet.microsoft.com/en-us/download/dotnet/8.0

- **Microsoft Visual C++ Redistributable**  
  https://aka.ms/vs/17/release/vc_redist.x64.exe

- **MSAL.NET libraries** (add these NuGet packages to your .NET project)  
  - [`Microsoft.Identity.Client`](https://www.nuget.org/packages/Microsoft.Identity.Client) (4.79.0 or later)  
  - [`Microsoft.Identity.Client.MtlsPop`](https://www.nuget.org/packages/Microsoft.Identity.Client.MtlsPop/4.79.2-preview or later)

- **Microsoft Azure Attestation Library (KeyGuard)** NuGet package  
  Internal feed: https://msazure.visualstudio.com/One/_artifacts/feed/Official/NuGet/Microsoft.Azure.Security.KeyGuardAttestation

> [!NOTE]
> The attestation library is required for Token Binding with MSI on Trusted Launch or Confidential VMs. Ensure you have bin placed the Native dlls in your application root.

In some cases these dependencies may already be installed. You can verify before installing:

```powershell
# Check installed .NET runtimes
dotnet --list-runtimes

# Check installed Microsoft Visual C++ x64 Redistributables
Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* |
  Where-Object { $_.DisplayName -like "Microsoft Visual C++*x64*" } |
  Select DisplayName, DisplayVersion
```

---

## 5. Code samples: MSI + Token Binding (C# / .NET 8)

> [!NOTE]
> The samples below extend the standard MSAL.NET managed identity pattern described in
> [Managed identity with MSAL.NET](https://learn.microsoft.com/en-us/entra/msal/dotnet/advanced/managed-identity) by adding
> **PoP/mTLS Token Binding** and **VBS/KeyGuard** requirements for Trusted Launch and Confidential VMs.

This section shows two patterns:

1. **Vanilla MSI** – request a PoP / mTLS-bound token directly using Managed Identity.
2. **FIC using Confidential Client** – use Managed Identity to bootstrap a confidential client and call a downstream resource (for example, Key Vault).

### Vanilla Managed Identity + Token Binding

If you only want to validate **Managed Identity + Token Binding issuance** from the TLVM/CVM (without calling a downstream resource yet), you can use the new **MSAL managed identity API** directly.

The code below:

- Builds a managed‑identity application (system‑assigned or user‑assigned)  
- Requests a **PoP / mTLS‑bound** token
- User Assigned Managed Identity (UAMI) shown; use `ManagedIdentityId.SystemAssigned` for SAMI

```csharp
IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId(clientIdOfUserAssignedManagedIdentity))
    .Build();

AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(resource)
    .WithMtlsProofOfPossession()
    .ExecuteAsync()
    .ConfigureAwait(false);
```

### FIC using Confidential Client (MSI bootstrap + downstream call)

This sample:
1. Gets a **Managed Identity** token using `ManagedIdentityApplication` (UAMI shown; use `ManagedIdentityId.SystemAssigned` for SAMI).  
2. Requests a **PoP** token bound to the HTTP method and URL (Key Vault secret read).  
3. Uses that MI token as a **client assertion** to create a **confidential client**.  
4. Calls Key Vault with the PoP token.

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.MtlsPop;

class Program
{
    private const string ResourceTenantId = "YOUR_RESOURCE_TENANT_ID";
    private const string AppClientId = "YOUR_APP_CLIENT_ID";
    private const string MiClientId = "YOUR_UAMI_CLIENT_ID"; // or use ManagedIdentityId.SystemAssigned
    private static readonly string Authority = $"https://login.microsoftonline.com/{ResourceTenantId}";
    private static readonly string KvScope = "https://vault.azure.net/.default";
    private static readonly Uri KvUri = new("https://<your-kv-name>.vault.azure.net/secrets/<secretName>?api-version=7.4");

    public static async Task Main()
    {
        // Build Managed Identity application to acquire MI token for client assertion.
        var miApp = ManagedIdentityApplicationBuilder
            .Create(ManagedIdentityId.WithUserAssignedClientId(MiClientId))
            .Build();

        // Dynamic client assertion delegate supplying both the assertion (JWT or MI token) and binding certificate.
        Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> assertionDelegate =
            async (opts, ct) =>
            {
                // Acquire MI token to use as the client assertion (audience should be AAD token endpoint or API://AzureADTokenExchange per your design).
                var miResult = await miApp
                    .AcquireTokenForManagedIdentity("api://AzureADTokenExchange")
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(ct)
                    .ConfigureAwait(false);

                // Return both the assertion value and the binding certificate.
                return new ClientSignedAssertion
                {
                    Assertion = miResult.AccessToken,
                    TokenBindingCertificate = miResult.BindingCertificate
                };
            };

        // Build confidential client with dynamic assertion.
        var cca = ConfidentialClientApplicationBuilder.Create(AppClientId)
            .WithAuthority(new Uri(Authority), validateAuthority: false)
            .WithAzureRegion("centraluseuap")
            .WithClientAssertion(assertionDelegate)
            .Build();

        var result = await cca
           .AcquireTokenForClient(new[] { KvScope })
           .WithMtlsProofOfPossession()
           .ExecuteAsync()
           .ConfigureAwait(false);

        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, KvUri);

        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("mtls_pop", result.AccessToken);

        var resp = await http.SendAsync(req).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        Console.WriteLine("Success: PoP + certificate-bound client assertion flow completed.");
    }
}
```

---

## (Optional) Quick checks

- **Verify MSI connectivity from the VM**
  ```powershell
  Invoke-RestMethod -Headers @{Metadata="true"} -Method GET `
    -Uri "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://management.azure.com/" |
    Select-Object -ExpandProperty access_token | ForEach-Object { $_.Length }
  ```

- **Re‑check VBS/HVCI after reboot**
  ```powershell
  (Get-CimInstance -ClassName Win32_ComputerSystem).HypervisorPresent
  ```

---

## Troubleshooting

- **VBS still not running after reboot**
  - Confirm the VM was created as **Trusted launch** or **Confidential** and (for TLVM) that **vTPM + Secure Boot** are enabled.
  - Ensure `bcdedit /enum` shows `hypervisorlaunchtype` set to `Auto`.
  - Check for incompatible kernel drivers; HVCI may block them.

- **KeyGuard/HVCI not running**
  - Re‑check the registry values above.
  - Ensure your Windows build/edition supports HVCI.
  - Update or replace incompatible kernel drivers that may block HVCI.

- **NuGet package not found or 401**
  - Add the correct Azure Artifacts feed and credentials.
  - Validate the package name: `Microsoft.Azure.Security.KeyGuardAttestation`.

- **MSI access to resources fails**
  - Verify the MI assignment on the VM and role assignments on target resources.
  - For user‑assigned MI, ensure your app uses the correct **client ID**.

---

## Related content

- Repo sample (ManagedIdentityAppVM): https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/tests/devapps/Managed%20Identity%20apps/ManagedIdentityAppVM/Program.cs  
- .NET 8 downloads: https://dotnet.microsoft.com/en-us/download/dotnet/8.0  
- VC++ Redistributable: https://aka.ms/vs/17/release/vc_redist.x64.exe  
- Microsoft Azure Attestation (KeyGuard) library (internal):  
  https://msazure.visualstudio.com/One/_artifacts/feed/Official/NuGet/Microsoft.Azure.Security.KeyGuardAttestation
