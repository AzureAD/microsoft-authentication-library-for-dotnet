# Managed Identity + mTLS PoP Demo (SAMI & UAMI)

A compact console demo that shows **Azure Managed Identity** issuing an **mTLS PoP** token and then
uses the **binding certificate** to call an **mTLS-protected** Graph test endpoint.

> **mTLS target**: `https://mtlstb.graph.microsoft.com/v1.0/applications`  
> **Output**: one selected property from the `applications` collection (default: `displayName`).

---

## Prerequisites (read first)

- **Azure subscription (allow listed for feature)** and one of:
  - An **Azure VM/VMSS** with **System‑Assigned Managed Identity** enabled
  *(For UAMI, note the **clientId** you plan to use.)*
- **Target resource access** (e.g., Graph test slice) permitting the MI to call it.
- **.NET 8 SDK** on your dev machine.
- **NuGet access to the internal IDDP feed** (Azure DevOps **PAT** with Packaging **Read**).

> New to Managed Identity? In the Azure Portal, enable **System assigned** on your VM/App and save; for **User assigned**, create the identity and **assign** it to the compute resource.

---

## What this demo highlights

- **System Assigned (SAMI)** and **User Assigned (UAMI)** Managed Identity.
- **IDP vs Cache** acquisition paths (force-refresh vs cache-only).
- **mTLS PoP bound proof**: verifies `cnf.x5t#S256` in the token matches the SHA-256 of
  the binding certificate used in the TLS handshake.
- **mTLS call** with a success panel (status, latency, HTTP version, response size).
- **Toggleable MSAL logging** (off by default) and a **full-token view** (press `F`).

> **Sensitive**: If you enable full-token view, treat tokens like passwords—avoid screenshots, copying, or sharing.

---

## Required packages (IDDP feed)

This demo consumes **preview builds** from the **IDDP** Azure Artifacts feed.

- **Core MSAL**  
  *Package Details*: Azure Artifacts → Microsoft.Identity.Client → `4.77.0-msi-v2-package1`  
  Link: https://identitydivision.visualstudio.com/Engineering/_artifacts/feed/IDDP/NuGet/Microsoft.Identity.Client/overview/4.77.0-msi-v2-package1

- **mTLS PoP helper**  
  *Package Details*: Azure Artifacts → Microsoft.Identity.Client.MtlsPop → `4.77.0-msi-v2-package1-preview`  
  Link: https://identitydivision.visualstudio.com/Engineering/_artifacts/feed/IDDP/NuGet/Microsoft.Identity.Client.MtlsPop/overview/4.77.0-msi-v2-package1-preview 

**NuGet source (IDDP feed)**  
`https://pkgs.dev.azure.com/identitydivision/Engineering/_packaging/IDDP/nuget/v3/index.json`

> You need an Azure DevOps **PAT** with at least **Packaging: Read** scope to restore from this feed.

### Configure the feed (dotnet CLI)

```bash
# 1) Add the IDDP feed (one-time)
dotnet nuget add source "https://pkgs.dev.azure.com/identitydivision/Engineering/_packaging/IDDP/nuget/v3/index.json"     --name IDDP --username azdo --password <YOUR_AZDO_PAT> --store-password-in-clear-text

# 2) Verify
dotnet nuget list source

# 3) Restore
dotnet restore
```

> **Security note**: prefer using a **machine-scoped** token store (e.g., Windows Credential Manager) where available,
and always treat your PAT like a secret.

### Optional: `nuget.config` snippet

```xml
<configuration>
  <packageSources>
    <add key="IDDP" value="https://pkgs.dev.azure.com/identitydivision/Engineering/_packaging/IDDP/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

---

## Build & Run

```bash
dotnet build -c Release
dotnet run  -c Release --project MsiV2DemoApp
```

### Menu quick map

```
Acquire Tokens
  1   SAMI → Token from IDP (force refresh)
  1a  SAMI → Token from Cache
  2   UAMI → Token from IDP (force refresh)
  2a  UAMI → Token from Cache

Call Resource
  3   SAMI token + cert → Call resource (mTLS)
  4   UAMI token + cert → Call resource (mTLS)

Display & Toggles
  F   Toggle Full Token view (ON/OFF)
  L   Toggle MSAL logging (ON/OFF; default OFF)

Settings
  set-uami     Change UAMI client id
  set-resource Change resource URL (defaults to mTLStb applications endpoint)
  set-prop     Change single property shown from 'value[]' (default: displayName)

System
  C/cls/clear  Clear screen
  M            Maximize console (Windows)
  Q            Quit
```

---

## Side‑by‑side demo (App1 + App2)

**Goal:** Show that the **binding certificate** minted by Managed Identity is **reused** across processes, so
a second app can present the same cert over mTLS **without** re-provisioning it.

1. **Start App1** and acquire a token (e.g., `2` for UAMI **IDP**) to trigger IMDS to mint the binding certificate.  
   You’ll see the cert subject like `CN=<clientId>, DC=<tenant>` and a thumbprint.
2. **Call the resource** from App1 (`4`). Confirm the green **mTLS call SUCCESS** panel and the item list.
3. **Start App2** (a second instance of the same console).  
   - Acquire a token via cache mode where applicable (`1a`/`2a`) or IDP if needed.  
   - Call the resource (`3`/`4`).  
   You’ll notice:
   - The **same binding certificate thumbprint** is selected from the store.
   - No extra **certificate provisioning** or **MAA interactions** are required.
   - Token acquisition can be **cache**-sourced to avoid an IDP round-trip.
4. Both apps now **present the same cert** during the TLS handshake and can call the mTLS endpoint in parallel.

> Tip: If you want App2 to *only* reuse the certificate while you manually pass a token, you can copy the token from App1
(`F` to show full token) and adapt App2 to accept a bearer input—this is optional and not required for the standard demo.

---

## Environment toggles (DEV only)

- `ACCEPT_ANY_SERVER_CERT=1` — bypass server TLS validation (lab only).  
- `UAMI_CLIENT_ID` — pre-set a UAMI client id for convenience.  
- `MSI_MTLS_RESOURCE_URL` — override the resource URL (defaults to mTLStb applications).  
- `MSI_DEMO_PROPERTY` — property to print from `value[]` (default `displayName`).  
- `MSI_MTLS_TEST_CERT_THUMBPRINT` / `MSI_MTLS_TEST_CERT_SUBJECT` — force using a specific cert from the store.

> **Never** use `ACCEPT_ANY_SERVER_CERT` outside of test environments.

---

## Troubleshooting

- **No managed identity available**: ensure you’re on an Azure VM/VMSS with MI enabled.  
- **403/401 at the resource**: verify the MI has access to the target endpoint and the token `aud` is correct.  
- **Unicode icons look odd**: your console font may not support them; the app auto-falls back to ASCII.  
- **Maximize not working**: depends on the host terminal; best-effort on classic Windows Console.

---

## Credits

Built for an internal demo to showcase **Managed Identity + mTLS PoP** with a friendly console UX.
Includes a simple verification that `cnf.x5t#S256` matches the binding certificate’s SHA‑256.
