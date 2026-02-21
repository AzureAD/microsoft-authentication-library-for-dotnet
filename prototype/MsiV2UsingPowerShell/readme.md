# MSI v2 (IMDS `/issuecredential`) – Quickstart + Sample

This folder contains a PowerShell sample that demonstrates the **Managed Identity v2** flow on **Azure VM / VMSS** using the **IMDS `/issuecredential`** endpoint.

## What’s MSI v2 (in one minute)

In **MSI v1**, IMDS returns an **access token** directly.

In **MSI v2**, IMDS returns a **client certificate** (bound to a key), and the client then uses **mTLS** to exchange that certificate for an access token from Entra STS.

High-level steps:

1. **Create a key** (best available: **KeyGuard/Credential Guard** on Windows).
2. (Optional but recommended for KeyGuard) **Attest** the key with **MAA** and get an attestation JWT.
3. Build a **CSR** (signed by the key) and call:
   - `GET /metadata/identity/getplatformmetadata`
   - `POST /metadata/identity/issuecredential` with `{ csr, attestation_token }`
4. IMDS returns:
   - `certificate` (x509 cert bytes)
   - `mtls_authentication_endpoint`
   - `tenant_id`
   - `client_id`
5. Call the **token endpoint** from IMDS over **mTLS**:
   - `POST {mtls_authentication_endpoint}/{tenant_id}/oauth2/v2.0/token`
   - `grant_type=client_credentials`
   - `client_id={client_id}`
   - `scope={resource}/.default`
   - `token_type=mtls_pop`
6. Use:
   - **TLS client certificate** + `Authorization: mtls_pop <token>` to call the target resource over mTLS.

✅ The returned access token contains `cnf.x5t#S256` and is **bound** to the certificate.

---

## Prereqs

- Windows VM/VMSS that supports MSI v2 (IMDS `/issuecredential`)
- PowerShell 7+ (must)
- Network access to IMDS (`169.254.169.254`) and to the mTLS token endpoint returned by IMDS
- **Native attestation DLL** available locally:
  - `AttestationClientLib.dll`

---

## Where to place the native DLL

Put `AttestationClientLib.dll` in **one** of these locations (the script searches in this order):

1. `.
ative\AttestationClientLib.dll` ✅ recommended
2. `.\AttestationClientLib.dll`
3. `%USERPROFILE%\Downloads\AttestationClientLib.dll`

Example:
```
C:\msiv2\
  get-msiv2-token.ps1
  native\
    AttestationClientLib.dll
```

---

## Running the PowerShell sample

From the folder containing the script:

```powershell
# Default: get Graph token and call mTLS Graph test endpoint
.\get-msiv2-token.ps1
```

Custom resource scope:

```powershell
.\get-msiv2-token.ps1 -Scope "https://management.azure.com/.default"
```

Call a different resource URL:

```powershell
.\get-msiv2-token.ps1 -ResourceUrl "https://mtlstb.graph.microsoft.com/v1.0/applications?`$top=5"
```

Disable extra logging:

```powershell
.\get-msiv2-token.ps1 -VerboseLogging:$false
```

> Note: You may get `403 Insufficient privileges` when calling Graph until the identity has the required Graph permissions/admin consent.

---

## Permissions note (Graph)

If your resource call is Graph (example: `GET /v1.0/applications`), you typically need app permissions such as:

- `Application.Read.All`

…and admin consent for the managed identity’s service principal.

---

## (Optional) .NET sample dependency

If you are building a .NET sample that uses the KeyGuard attestation package, include:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Azure.Security.KeyGuardAttestation" />
</ItemGroup>
```

And ensure the native DLL is deployed alongside the app output (or in a `native/` folder your app adds to PATH at runtime).

---

## Troubleshooting

### AADSTS392200: “Client certificate is missing from the request”
This means the TLS handshake did not present the client cert. Common causes:
- Cert is not bound to the private key (must attach KeyGuard key to cert).
- HTTP stack not configured for mTLS (use HttpClientHandler + ClientCertificates).

### 403 from Graph
Token + binding worked, but the identity does not have Graph permissions.
