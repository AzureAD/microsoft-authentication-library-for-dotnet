# MTLSPoP for Managed Identity — Developer Guide

> **Audience**: Engineers new to MSAL.NET or the mTLS Proof-of-Possession feature.  
> **Related docs**: [Design doc](sni_mtls_pop_token_design.md) · [Skills guide](mtls_pop_skills_guide.md) · [Architecture diagram](mtlspop_architecture.md)

---

## What problem does this solve?

A normal **Bearer token** is like a physical key: if someone steals it, they can use it to unlock anything until it expires. Bearer tokens travel as plain strings in HTTP headers, and if intercepted, they can be replayed by an attacker.

**Proof-of-Possession (PoP)** fixes this by *binding* the token to a cryptographic key. A PoP token is only valid when presented by whoever holds the matching private key — a stolen token is useless without it.

**mTLS PoP** (Mutual TLS Proof-of-Possession) takes this further: the token is bound to an X.509 certificate, and the connection to the resource server *must* use mTLS with that same certificate. This satisfies [RFC 8705](https://datatracker.ietf.org/doc/html/rfc8705).

For **Managed Identity** specifically, MSAL handles the certificate lifecycle entirely — your application code never touches a private key.

---

## When to use it

Use `WithMtlsProofOfPossession()` when:

- Your workload runs on an **Azure VM or VMSS** with Managed Identity enabled.
- Your environment supports **IMDSv2** (required — see [constraints](#constraints)).
- You want stronger token security than Bearer tokens.
- You need to integrate with services that require `token_type=mtls_pop`.

---

## Quick-start example

```csharp
using Microsoft.Identity.Client;

// Build the Managed Identity application (system-assigned identity)
var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

// Acquire a token with mTLS PoP
AuthenticationResult result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Use the token in HTTP calls
// IMPORTANT: your HttpClient must also be configured for mTLS
// using result.BindingCertificate as the client certificate.
Console.WriteLine($"Token type: {result.TokenType}");       // "mtls_pop"
Console.WriteLine($"Cert thumbprint: {result.BindingCertificate.Thumbprint}");
```

> **Key point**: You must configure your `HttpClient` handler with `result.BindingCertificate` 
> as the client certificate when calling the downstream resource. The token alone is not enough — 
> the resource server will verify the mTLS connection matches the bound certificate.

---

## How it works — step by step

Here is what happens inside MSAL when you call `WithMtlsProofOfPossession()`:

### Step 1 — Platform check

MSAL immediately validates that:
- You are running on **Windows** (Linux is not yet supported)
- You are on **.NET Core / .NET 5+** (not .NET Framework 4.6.2)

If either check fails, a `MsalClientException` is thrown before any network calls are made.

### Step 2 — Token cache check

MSAL checks its in-memory token cache. If a valid `mtls_pop` token already exists for this resource, it returns it immediately (no network calls at all).

### Step 3 — Get platform metadata from IMDSv2

If no cached token exists, MSAL calls the Azure Instance Metadata Service (IMDS) running locally on the VM:

```
GET http://169.254.169.254/metadata/identity/getplatformmetadata?cred-api-version=2.0
Header: Metadata: true
```

The response (`CsrMetadata`) contains:
- `clientId` — the Managed Identity's app registration GUID
- `tenantId` — the Azure tenant
- `cuId` — the Compute Unit ID (uniquely identifies this VM/VMSS instance)
- `attestationEndpoint` — used only for KeyGuard attestation (advanced)

> **IMDSv2 vs IMDSv1**: mTLS PoP only works with **IMDSv2**. If this endpoint returns 404, it means the VM only supports IMDSv1 and mTLS PoP is not available. MSAL throws `MtlsPopTokenNotSupportedinImdsV1`.

### Step 4 — Get or create the binding certificate

MSAL checks a **two-tier certificate cache**:

1. **In-memory cache** (fastest — checked first on every call)
2. **Windows certificate store** (survives process restarts)

If both miss, MSAL mints a fresh certificate:

1. **Creates a cryptographic key** via `IManagedIdentityKeyProvider` (must be KeyGuard type for mTLS PoP).
2. **Generates a Certificate Signing Request (CSR)** embedding the `clientId`, `tenantId`, and `cuId`.
3. **POSTs the CSR** to IMDS to get it signed:

    ```
    POST http://169.254.169.254/metadata/identity/issuecredential?cred-api-version=2.0
    Header: Metadata: true
    Body: { "csr": "<raw base64 — PEM headers stripped>", "attestation_token": "<MAA JWT>" }
    // attestation_token is omitted entirely when WithAttestationSupport() is not used
    ```

4. **Receives a signed X.509 certificate** along with the `mtlsAuthenticationEndpoint` (the regional STS URL) and the canonical `clientId` to use.
5. **Attaches the private key** from step 1 to the certificate.
6. **Stores the result** in both cache tiers.

The cached object (`MtlsBindingInfo`) contains: the certificate, the STS endpoint, and the client ID.

### Step 5 — Acquire the token over mTLS

MSAL posts a client credentials request to the **regional Entra STS endpoint**, using the binding certificate for the mTLS connection:

```
POST https://{region}.mtlsauth.microsoft.com/{tenantId}/oauth2/v2.0/token
[TLS client certificate = binding certificate from step 4]

Body:
  client_id    = <canonical MI clientId>
  grant_type   = client_credentials
  scope        = https://management.azure.com/.default
  token_type   = mtls_pop        ← This is what makes it a PoP token
```

ESTS validates the certificate during the TLS handshake and uses it to bind the issued token. The response contains an `access_token` with `token_type=mtls_pop`.

### Step 6 — Return result

MSAL:
- Caches the token in its token cache (keyed by resource)
- Sets `AuthenticationResult.BindingCertificate` to the certificate
- Returns the `AuthenticationResult` to your application

---

## Key classes (where to look in the code)

| Class | File | What it does |
|---|---|---|
| `ManagedIdentityPopExtensions` | `ManagedIdentity/ManagedIdentityPopExtensions.cs` | Public `.WithMtlsProofOfPossession()` extension method |
| `ManagedIdentityClient` | `ManagedIdentity/ManagedIdentityClient.cs` | Detects the MI source; routes mTLS PoP requests to IMDSv2; holds the runtime binding certificate |
| `ManagedIdentityAuthRequest` | `Internal/Requests/ManagedIdentityAuthRequest.cs` | Orchestrates cache lookup → token fetch → caching |
| `ImdsV2ManagedIdentitySource` | `ManagedIdentity/V2/ImdsV2ManagedIdentitySource.cs` | The full CSR → certificate → token flow |
| `MtlsPopAuthenticationOperation` | `AuthScheme/PoP/MtlsPopAuthenticationOperation.cs` | Sets `token_type=mtls_pop` on the request; populates `BindingCertificate` on the result |
| `MtlsPopParametersInitializer` | `ApiConfig/Parameters/MtlsPopParametersInitializer.cs` | Validates and initializes PoP parameters (used in the CCA path) |
| `MtlsBindingCache` | `ManagedIdentity/V2/MtlsCertificateCache.cs` | Two-tier cert cache (memory + Windows store). Note: the class is named `MtlsBindingCache`; it lives in the file `MtlsCertificateCache.cs`. |
| `Csr` | `ManagedIdentity/V2/Csr.cs` | Generates the Certificate Signing Request |

---

## Architecture diagram

The following shows the full sequence of calls for a cache-miss scenario:

```
Your App
  │
  │  AcquireTokenForManagedIdentity(resource).WithMtlsProofOfPossession()
  ▼
ManagedIdentityAuthRequest                          ← Checks MSAL token cache
  │
  │  Cache miss → call MI source
  ▼
ManagedIdentityClient                               ← Routes to ImdsV2
  │
  ▼
ImdsV2ManagedIdentitySource
  │
  ├─[1]→ IMDS /getplatformmetadata                 ← GET CSR metadata
  │       ← { clientId, tenantId, cuId, attestationEndpoint }
  │
  ├─[2]→ MtlsBindingCache (memory + Windows)       ← Check cert cache
  │       │ Cache miss:
  │       ├─[3]→ IManagedIdentityKeyProvider        ← Get/create KeyGuard RSA key
  │       ├─[4]→ Csr.Generate()                    ← Build CSR from key + metadata
  │       └─[5]→ IMDS /issuecredential             ← POST CSR, get signed cert
  │               ← { certificate, mtlsAuthEndpoint, clientId, tenantId }
  │
  └─[6]→ STS /oauth2/v2.0/token  (🔒 mTLS)        ← POST token request
          token_type = mtls_pop
          ← { access_token, token_type="mtls_pop" }
  │
  ▼
ManagedIdentityAuthRequest
  │  Sets AuthenticationScheme = MtlsPopAuthenticationOperation
  │  Caches the token
  │
  ▼
Your App ← AuthenticationResult { AccessToken, BindingCertificate, TokenType="mtls_pop" }
```

---

## Using the token

After calling `ExecuteAsync()`, you get back an `AuthenticationResult`. To make an authenticated API call:

```csharp
// 1. Get the token
var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// 2. Configure HttpClientHandler with the binding certificate
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

// 3. Use it for the downstream call
using var client = new HttpClient(handler);

// Use CreateAuthorizationHeader() — it automatically applies the correct
// scheme prefix ("mtls_pop") from the AuthenticationResult.
client.DefaultRequestHeaders.Add("Authorization", result.CreateAuthorizationHeader());

var response = await client.GetAsync("https://management.azure.com/subscriptions");
```

---

## Optional: KeyGuard attestation (MAA)

For higher security scenarios, MSAL supports **Credential Guard attestation** via the `Microsoft.Identity.Client.KeyAttestation` package. This provides hardware-level proof that the private key lives inside a VBS-protected enclave.

### What is MAA?

**MAA (Microsoft Azure Attestation)** is an external Azure service that verifies a KeyGuard RSA key is genuinely protected by [Virtualization-Based Security (VBS)](https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity) on the VM. When called, it returns a **JWT (the MAA token)** that cryptographically proves the key is hardware-backed.

Without this, IMDS still issues a certificate — it just can't verify the key is hardware-protected.

### How it fits in the flow

```
[Normal flow]    Step 4: GetOrCreateKey → KeyGuard RSA key
                          │
[+ Attestation]           ├─→ AttestationClientLib.dll (native)
                          │     Collects TPM logs + VBS evidence
                          │
                          ├─→ POST {csrMetadata.attestationEndpoint}/attest/keyguard
                          │     (MAA service — Azure Attestation)
                          │     ← MAA JWT (proves key is hardware-backed)
                          │
                 Step 5: POST /metadata/identity/issuecredential
                          Body: { csr: "<raw base64>", attestation_token: "<MAA JWT>" }
                          // attestation_token omitted when WithAttestationSupport() not used
                                                  ▲
                                     null / omitted without WithAttestationSupport()
```

### Usage

```csharp
// Requires the Microsoft.Identity.Client.KeyAttestation NuGet package
var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()    // ← obtains MAA token; chains onto WithMtlsProofOfPossession()
    .ExecuteAsync();
```

When attestation is configured:
- The key type must be `KeyGuard` (hardware-backed, requires VBS/Credential Guard enabled on the VM)
- `AttestationClientLib.dll` (a native Windows DLL) calls the MAA endpoint from `CsrMetadata.AttestationEndpoint`
- The MAA JWT is embedded in the `/issuecredential` request body as `attestation_token`
- The token cache partitions attested vs non-attested tokens (`att=1` vs `att=0`) — they are not interchangeable

---

## Constraints

| Constraint | Detail |
|---|---|
| **OS** | Windows only. Throws `MtlsNotSupportedForManagedIdentity` on Linux/Mac. |
| **Framework** | .NET Core / .NET 5+ only. Not supported on .NET Framework 4.6.2. |
| **IMDS version** | Requires IMDSv2. If the VM only has IMDSv1, throws `MtlsPopTokenNotSupportedinImdsV1`. |
| **Key type** | KeyGuard RSA key required. Throws error code `mtls_pop_requires_keyguard` if not available (hardcoded string — not yet a constant in `MsalError`). |
| **Mixed usage** | Once IMDSv1 is used in a process while IMDSv2 is cached, switching to IMDSv2 PoP in the same process is blocked (preview behavior). Throws `CannotSwitchBetweenImdsVersionsForPreview`. |
| **Experimental** | This feature is in preview. Not all regions and environments may be supported. |

---

## Common errors

| Error code | What it means | How to fix |
|---|---|---|
| `MtlsNotSupportedForManagedIdentity` | You're on a non-Windows OS or .NET 4.6.2 | Run on Windows with .NET 5+ |
| `MtlsPopTokenNotSupportedinImdsV1` | Your VM only supports IMDSv1 | Ensure the VM image supports IMDSv2 |
| `mtls_pop_requires_keyguard` | The managed identity key is not a KeyGuard key (hardcoded string — not yet a constant in `MsalError`) | Use a VM/VMSS with KeyGuard support enabled |
| `MtlsCertificateNotProvided` | (CCA path) No certificate was found for binding | Pass a certificate via `.WithCertificate(cert, sendX5C: true)` |
| `MtlsPopWithoutRegion` | (CCA path) Azure region not set | Add `.WithAzureRegion("region")` to the app builder |
| `CannotSwitchBetweenImdsVersionsForPreview` | Mixed IMDSv1/v2 usage in same process | Use a single IMDS version per process; restart the app |

---

## Difference from SNI certificate (Confidential Client) flow

There are **two** MTLSPoP flows in MSAL:

| Feature | **Managed Identity (IMDSv2)** | **Confidential Client (SNI cert)** |
|---|---|---|
| **Who provides the cert?** | IMDS mints it automatically | You provide it via `.WithCertificate()` |
| **API entry point** | `AcquireTokenForManagedIdentity().WithMtlsProofOfPossession()` | `AcquireTokenForClient().WithMtlsProofOfPossession()` |
| **Region required?** | No (endpoint comes from IMDS response) | Yes — `.WithAzureRegion("region")` required |
| **Windows only?** | Yes | No — cross-platform |
| **CSR flow?** | Yes — multi-step: metadata → CSR → cert issuance | No — cert already in hand |
| **Key classes** | `ImdsV2ManagedIdentitySource`, `MtlsBindingCache` | `MtlsPopParametersInitializer`, `CertificateClientCredential` |

---

## Tests

If you need to understand expected behavior, start here:

- **Unit tests**: `tests/Microsoft.Identity.Test.Unit/ManagedIdentityTests/ImdsV2Tests.cs`
- **Public API tests**: `tests/Microsoft.Identity.Test.Unit/PublicApiTests/MtlsPopTests.cs`
- **Integration tests**: `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`

---

## References

- [RFC 8705 — OAuth 2.0 Mutual TLS Client Authentication](https://datatracker.ietf.org/doc/html/rfc8705)
- [Design doc: SNI mTLS PoP token design](sni_mtls_pop_token_design.md)
- [Copilot prompts guide for mTLS PoP](mtls_pop_skills_guide.md)
