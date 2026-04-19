# MSI v2 mTLS PoP — Cross-Language Design & Constraints

## Overview

MSI v2 introduces mTLS Proof-of-Possession (PoP) tokens — access tokens bound to a client certificate via the `cnf` claim. The flow requires MSAL to:

1. Generate an RSA key pair
2. Build a CSR and submit it to IMDS to get a binding certificate
3. Optionally attest the key via MAA (Microsoft Azure Attestation)
4. Call the mTLS token endpoint with the binding certificate in the TLS handshake
5. Return the bound token + certificate to the caller

This creates two cross-cutting challenges across all MSAL languages:

- **X.509 certificate and key handling** — how does each language/platform access non-exportable keys and present them in TLS handshakes?
- **HttpClient customization** — higher-level SDKs customize MSAL's HTTP transport for retry, telemetry, proxy, etc. How does the mTLS certificate flow through that customization?

---

## Part 1: X.509 Certificate, Key Types, and TLS Constraints

### The Core Problem

The mTLS handshake requires the client to present a certificate **and** prove possession of the private key during the TLS negotiation. How the private key is stored and accessed varies by platform and security tier:

**Key Hierarchy (highest to lowest security):**

1. **KeyGuard (VBS/Credential Guard)** — Key lives in a virtualization-isolated enclave. Non-exportable. Attested via MAA. Windows only.
2. **Hardware (TPM/KSP)** — Key backed by TPM hardware. Non-exportable. No attestation in current flow.
3. **In-Memory (software)** — Key lives in process memory. Exportable. No attestation. Cross-platform.

### The TLS Stack Matters

Whether the private key can be used for mTLS depends on the TLS stack:

**SChannel (Windows native)** — Can access non-exportable CNG keys (KeyGuard, TPM) natively via the Windows certificate store. .NET's `HttpClient` uses SChannel on Windows, so `X509Certificate2` with a CNG key reference "just works."

**OpenSSL** — Requires the private key as exportable bytes (PEM/DER). Cannot access non-exportable CNG keys. Python, Node.js, Go, and Java on Linux all use OpenSSL (or similar) for TLS.

This means:

- **Exportable keys (in-memory)** work everywhere — the key bytes can be passed to any TLS stack.
- **Non-exportable keys (KeyGuard, TPM)** only work with SChannel — which means Windows + a language runtime that uses SChannel.

### Resource Calls After Token Acquisition

After acquiring the `mtls_pop` token, the caller must present the **same binding certificate** when calling the resource (e.g., Key Vault, Graph). This is a second mTLS handshake, entirely outside MSAL.

For non-exportable keys, the caller also needs a way to do this mTLS call using SChannel, not OpenSSL. In Python, this led to the `mtls_http_request()` helper — a WinHTTP/SChannel bridge via ctypes.

For exportable keys, the caller can use standard HTTP libraries (`requests`, `http.client`, etc.) with the cert + key PEM files.

### Additional Resource Call Requirements

Discovered during Python E2E testing:

- **`x-ms-tokenboundauth: true` header** — Required by Azure Key Vault. Triggers the server to request the client certificate via TLS renegotiation.
- **HTTP/1.1** — TLS renegotiation (for client cert request) is forbidden in HTTP/2.
- **TLS 1.2** — TLS renegotiation works reliably in TLS 1.2. TLS 1.3 uses post-handshake authentication which may not be fully supported by all stacks.

---

## Part 2: Language Status

### MSAL .NET — Delivered

**Key approach:** KeyGuard (primary) → Hardware (fallback) → In-Memory (fallback). All three tiers implemented. KeyGuard keys are attested via MAA.

**TLS:** SChannel via `HttpClient`. Non-exportable keys work natively. `X509Certificate2` wraps both the cert and the CNG key reference.

**Caller experience:**
```csharp
var result = await app.AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()
    .ExecuteAsync();

// Resource call — standard HttpClient
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);
var client = new HttpClient(handler);
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(result.TokenType, result.AccessToken);
var response = await client.GetAsync(mtlsResourceUrl);
```

**Status:** ✅ Complete. Token acquisition, attestation, cert binding, resource calls all working.

---

### MSAL Python — Two POC Approaches

#### Approach 1: KeyGuard (PR [#904](https://github.com/AzureAD/microsoft-authentication-library-for-python/pull/904))

**Key approach:** KeyGuard via NCrypt/CNG ctypes bindings. Non-exportable. Attested via MAA.

**TLS:** OpenSSL cannot access KeyGuard keys. Solution: `mtls_http_request()` helper that uses WinHTTP/SChannel via ctypes for both token acquisition and resource calls.

**Caller experience:**
```python
from msal.msi_v2 import mtls_http_request

result = client.acquire_token_for_client(
    resource="https://vault.azure.net",
    mtls_proof_of_possession=True,
    with_attestation_support=True,
)

cert_der = base64.b64decode(result["cert_der_b64"])
resp = mtls_http_request(
    "GET", resource_url, cert_der,
    headers={"Authorization": f"{result['token_type']} {result['access_token']}",
             "x-ms-tokenboundauth": "true"})
```

> **TBD:** The exact developer experience for the platform mTLS helper (mtls_http_request), including whether it is exposed directly by MSAL or through a shared companion package used by both MSAL and calling applications, will be finalized in a future design update.

**Tradeoffs:**
- ✅ Highest security — matches .NET's KeyGuard path
- ❌ Windows only
- ❌ Requires WinHTTP/SChannel ctypes bridge (~500+ LOC)
- ❌ Caller must use `mtls_http_request()` helper — standard `requests` won't work
- ❌ High complexity (NCrypt, Crypt32, WinHTTP, manual DER encoding)

**Status:** ⏳ POC complete. Token acquisition and cert binding verified. AKV E2E blocked on service-side issue.

---

#### Approach 2: In-Memory Key (PR [#905](https://github.com/AzureAD/microsoft-authentication-library-for-python/pull/905))

**Key approach:** In-memory software RSA key. Exportable. No attestation.

**TLS:** Standard OpenSSL. Key bytes can be passed to `requests` as cert + key PEM files.

**Caller experience:**
```python
result = client.acquire_token_for_client(
    resource="https://vault.azure.net",
    mtls_proof_of_possession=True,
)

# Standard requests — no helper needed
resp = requests.get(
    resource_url,
    cert=(cert_path, key_path),
    headers={"Authorization": f"{result['token_type']} {result['access_token']}",
             "x-ms-tokenboundauth": "true"})
```

**Tradeoffs:**
- ✅ Cross-platform (Windows, Linux, macOS)
- ✅ No helper needed — standard `requests`/`httpx` work
- ✅ Simple implementation (~200 LOC vs 500+ for KeyGuard)
- ✅ CSR built with `cryptography` library (~20 LOC vs manual DER)
- ❌ Lower security — key in process memory, exportable, no attestation

**Status:** ⏳ Design document. Not yet implemented.

---

### MSAL Java — TBD

**Key considerations:**
- Java's `KeyStore` can hold non-exportable keys via PKCS#11 or Windows-MY provider
- `SSLContext` with custom `KeyManager` can present CNG-backed certs on Windows
- On Linux, in-memory key path needed
- Java's `HttpClient` (JDK 11+) supports custom `SSLContext`

**Status:** ❓ Not started.

---

### MSAL Node — TBD

**Key considerations:**
- Node.js uses OpenSSL for TLS — same constraint as Python
- `tls.createSecureContext()` accepts key as PEM/Buffer — needs exportable key
- In-memory key path likely needed
- For non-exportable keys on Windows, would need native addon (N-API) calling SChannel

**Status:** ❓ Not started.

---

### MSAL Go — TBD

**Key considerations:**
- Go's `crypto/tls` uses its own TLS implementation (not OpenSSL)
- `tls.Config.Certificates` accepts `tls.Certificate` with `PrivateKey` interface
- Could implement `crypto.Signer` backed by CNG/NCrypt via cgo on Windows
- On Linux, in-memory key path needed

**Status:** ❓ Not started.

---

## Part 3: HttpClient / HTTP Transport Customization

### The Problem

Higher-level SDKs customize MSAL's HTTP transport to inject retry logic, telemetry, proxy settings, and connection pooling. With mTLS, the binding certificate must be present in the TLS handshake for the token endpoint call — but the certificate is internal to MSAL, discovered at runtime from IMDS.

This is a common challenge across languages. The specifics vary by language but the pattern is the same: the SDK provides a custom HTTP client/transport, and MSAL needs to somehow get the binding certificate into that transport.

### MSAL .NET — `IMsalMtlsHttpClientFactory`

.NET solved this with a new interface extending the existing `IMsalHttpClientFactory`:

```csharp
public interface IMsalMtlsHttpClientFactory : IMsalHttpClientFactory
{
    HttpClient GetHttpClient(X509Certificate2 x509Certificate2);
}
```

MSAL checks at runtime: if the factory implements `IMsalMtlsHttpClientFactory`, MSAL calls `GetHttpClient(cert)` passing the binding certificate. If it only implements `IMsalHttpClientFactory`, MSAL calls `GetHttpClient()` and the cert is not passed — mTLS fails silently.

**Tested scenarios (on Azure VM):**

| Factory Interface | Cert from SDK? | Works? | Dev Experience |
|-------------------|---------------|--------|----------------|
| `IMsalHttpClientFactory` | No | ❌ | N/A |
| `IMsalHttpClientFactory` | Yes (baked from prior AuthResult) | ✅ | Poor — two-pass flow |
| `IMsalMtlsHttpClientFactory` | No | ✅ | **Good** — single pass |
| `IMsalMtlsHttpClientFactory` | Yes (also adds) | ✅ | Unnecessary |

**Recommendation for .NET SDKs:** Implement `IMsalMtlsHttpClientFactory`. MSAL passes the cert at call time. No two-pass flow needed.

**Known gap:** Scenario 1 fails silently — no warning at configuration time.

**Proposed improvement:** A static `ManagedIdentityApplication.GetBindingCertificateAsync()` API that performs only the IMDS leg and returns the certificate. SDKs can then configure their transport explicitly without needing the callback interface.

Full details: PR [#5935](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5935)

---

### MSAL Python

Python's HTTP transport customization varies by approach:

**KeyGuard approach:** MSAL uses its own WinHTTP/SChannel bridge for the mTLS call. The SDK's custom `requests.Session` or HTTP transport is bypassed for the token endpoint call. The SDK cannot inject retry/telemetry into this specific call.

**In-memory approach:** MSAL can use standard `requests` with cert + key PEM for the mTLS call. The SDK can provide a custom `requests.Session` and MSAL can attach the cert. More natural integration with Python HTTP patterns.

**Status:** Transport customization story TBD for both approaches.

---

### MSAL Java, Node, Go

HTTP transport customization patterns for mTLS TBD. The core challenge is the same: how does the SDK's custom transport get the binding certificate that MSAL discovers at runtime?

---

## Summary

| Language | Key Approach | Attestation | TLS Stack | mTLS Helper Needed | HttpClient Customization | Status |
|----------|-------------|-------------|-----------|-------------------|-------------------------|--------|
| .NET | KeyGuard → HW → InMemory | ✅ MAA | SChannel | No | `IMsalMtlsHttpClientFactory` | ✅ Delivered |
| Python (KeyGuard) | KeyGuard | ✅ MAA | WinHTTP/SChannel (ctypes) | Yes (`mtls_http_request`) | TBD | ⏳ POC |
| Python (InMemory) | In-Memory | ❌ | OpenSSL | No | TBD | ⏳ Design |
| Java | TBD | TBD | TBD | TBD | TBD | ❓ Not started |
| Node | TBD | TBD | OpenSSL | TBD | TBD | ❓ Not started |
| Go | TBD | TBD | Go TLS | TBD | TBD | ❓ Not started |
