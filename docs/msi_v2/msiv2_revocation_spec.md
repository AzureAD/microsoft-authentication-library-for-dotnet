# Certificate Revocation Specification

## Overview

This document outlines the design and implementation details for **certificate and token revocation** in MSI V2 scenarios.

In the MSI v2 authentication flow, MSAL obtains a credential (certificate) from IMDS and uses it to request a token from eSTS. In some cases, eSTS may respond with an error indicating that the certificate/attestation is no longer valid. When this occurs, **MSAL must obtain a new certificate** before retrying the token request with eSTS.

---

## Retry Policy (Normative)

There are two distinct “retry” concepts in this spec:

1. **Transport / HTTP retries (existing MSAL behavior)**  
   MSAL may retry transient network/service failures according to its existing retry logic (e.g., transient 5xx, timeouts), as applicable.

2. **Revocation remediation retry (this spec)**  
   For credential-related `invalid_client` errors as described below, **MSAL MUST retry remediation without an upper bound**:
   - mint a new certificate using `/issuecredential?bypass_cache=true`, then
   - retry the eSTS token request with the new certificate.

   This loop continues **until**:
   - the token request succeeds, or
   - MSAL encounters an error that is not classified as a credential/attestation remediation signal (i.e., not the `invalid_client` conditions defined in this spec), in which case MSAL MUST surface the error to the caller.

> **Note (Non-normative):** Implementations should consider adding backoff and/or jitter between remediation attempts to avoid tight loops under persistent failures.

---

## Certificate revocation (eSTS rejects the certificate during token acquisition)

> **Goal:** When eSTS indicates the certificate/attestation is invalid, MSAL continuously remints the certificate and retries until success or a non-remediable error occurs.

~~~mermaid
sequenceDiagram
    participant Application
    participant MSAL
    participant IMDS
    participant eSTS

    Application ->> MSAL: 1. Request Access Token

    Note over MSAL: (Optional) request CSR inputs/metadata if required by platform
    Note over MSAL: Create CSR for certificate minting request

    MSAL ->> IMDS: 2. Request Certificate (with CSR) via /issuecredential
    IMDS -->> MSAL: 3. Return Certificate

    MSAL ->> eSTS: 4. Exchange Certificate for Access Token
    eSTS -->> MSAL: 5. Response (HTTP 200 / error)

    alt eSTS returns invalid_client indicating credential/attestation failure
        Note over MSAL: Includes AADSTS 1000610–1000614 OR missing error_codes
        loop Unlimited remediation retries
            MSAL ->> IMDS: 6. Mint NEW certificate via /issuecredential?bypass_cache=true
            IMDS -->> MSAL: 7. Return new certificate
            MSAL ->> eSTS: 8. Retry Access Token request with new certificate
            eSTS -->> MSAL: 9. Return Access Token OR invalid_client (continue loop) OR other error (break loop and surface)
        end
    else other errors
        MSAL -->> Application: Surface error
    end

    MSAL ->> Application: 10. Return Access Token (if success)
~~~

---

## Claims challenge (Resource rejects the token while using it to gain access)

> **Normative**: When `claims` are provided by the app (from a resource's 401 challenge), **MSAL MUST** mint a new certificate using `/issuecredential?bypass_cache=true` **before** calling eSTS with the `claims` parameter.

~~~mermaid
sequenceDiagram
    participant Application
    participant MSAL
    participant IMDS
    participant eSTS
    participant Resource

    Application ->> MSAL: 1. Request Access Token

    Note over MSAL: (Optional) request CSR inputs/metadata if required by platform
    Note over MSAL: Create CSR for certificate minting request

    MSAL ->> IMDS: 2. Request Certificate (with CSR) via /issuecredential
    IMDS -->> MSAL: 3. Return Certificate
    MSAL ->> eSTS: 4. Exchange Certificate for Access Token
    eSTS -->> MSAL: 5. Return Access Token
    MSAL ->> Application: 6. Return Access Token

    Application ->> Resource: 7. Call API with Access Token
    Resource -->> Application: 8. 401 Unauthorized + WWW-Authenticate with claims

    Application ->> MSAL: 9. Pass claims to MSAL

    Note over MSAL: Claims present => force fresh certificate before token call
    MSAL ->> IMDS: 10. Mint NEW certificate via /issuecredential?bypass_cache=true
    IMDS -->> MSAL: 11. Return new Certificate

    MSAL ->> eSTS: 12. Retry Access Token request with claims + new certificate
    eSTS -->> MSAL: 13. Return new Access Token
    MSAL ->> Application: 14. Return new Access Token
~~~

---

## Certificate Revocation Scenarios

- **Revoked/Invalid Certificate (MSAL-handled):** Token request fails due to certificate/attestation issues (AADSTS 1000610–1000614). **MSAL** re-mints the certificate via `/issuecredential?bypass_cache=true` and retries until success or a non-remediable error occurs.
- **Unspecified Credential Issue (MSAL-handled):** eSTS returns `invalid_client` without suberror / error code. **MSAL** treats this as a credential issue, re-mints certificate, retries until success or a non-remediable error occurs.

---

## eSTS Response to Indicate Specific Error

| Scenario                       | eSTS/Resource Response                                                          |
|--------------------------------|----------------------------------------------------------------------------------|
| Revoked/Invalid Certificate    | `invalid_client` with AADSTS codes (see table below)                             |
| Unspecified Credential Issue   | `{ "error": "invalid_client" }` (no `error_codes`)                               |

---

## ESTS error mapping for certificate creation/attestation failures

When certificate minting uses an **attestation token** and that token is invalid, eSTS returns **401 Unauthorized** with OAuth2 error `invalid_client` and one of the following AADSTS codes.

| AADSTS Code | ESTS `ErrorMapping ErrorCode`                               | HTTP | OAuth2 `error`   |
|-------------|--------------------------------------------------------------|------|------------------|
| 1000610     | `CertificateCreateAttestationTokenInvalidTimeRange`          | 401  | `invalid_client` |
| 1000611     | `CertificateCreateAttestationTokenInvalidIssuer`             | 401  | `invalid_client` |
| 1000612     | `CertificateCreateAttestationTokenInvalidClaimValue`         | 401  | `invalid_client` |
| 1000613     | `CertificateCreateAttestationTokenInvalidJkuHeader`          | 401  | `invalid_client` |
| 1000614     | `CertificateCreateAttestationTokenInvalidSignature`          | 401  | `invalid_client` |

---

## MSAL behavior for AADSTS1000610–1000614 (required)

For any of the above codes, **MSAL MUST**:

1. Call the certificate minting endpoint **`/issuecredential?bypass_cache=true`** to force a **new certificate** (ignore any cached/invalid state).
2. Replace the current certificate with the newly issued one.
3. **Retry** the token request with eSTS using the new certificate, repeating remediation **without an upper bound** while the error remains in the remediable set.
4. Emit telemetry (see below).

> These errors reflect invalid or stale certificate/attestation input; remediation is internal to MSAL. No developer action is required.

---

## Token Revocation Scenarios

- **Claims Challenge (App + MSAL):** Resource returns 401 with claims. **App** passes claims to MSAL. **MSAL** re-mints certificate (`bypass_cache=true`) and acquires token **with claims**.

A resource may reject a token due to policy/claims requirements. When this occurs:

- The resource responds with **401** and a `WWW-Authenticate` header containing **claims**.
- The **application** extracts those claims and **passes them to MSAL**.
- **MSAL MUST** call `/issuecredential?bypass_cache=true` to mint a **new certificate**, then request a new token from eSTS **including the claims**.

**Illustrative eSTS payload (for reference only)**
~~~json
{
  "error": "invalid_client",
  "error_description": "AADSTS1000613: The attestation token contains invalid Jku header. The value must be a URL with a domain name that matches the token issuer.",
  "error_codes": [1000613]
}
~~~

---

## MSAL Behavior Summary

- If `invalid_client` + AADSTS **1000610–1000614** → **MSAL auto-remediates** via `/issuecredential?bypass_cache=true`, then retries token, repeating remediation **without an upper bound** while the error remains in the remediable set.
- If `invalid_client` with **no** `error_codes` → **MSAL** still re-mints certificate with `bypass_cache=true`, then retries token, repeating remediation **without an upper bound** while the error remains in the remediable set.
- If **401 with claims** from a resource → **App passes claims** to MSAL; **MSAL** re-mints certificate (`bypass_cache=true`) and calls eSTS **with claims**.

---

## MSAL Pseudo Code Implementation

~~~csharp
// Inputs: scope/resource; optional 'appProvidedClaims' from a prior 401 challenge.
var useClaims = !string.IsNullOrEmpty(appProvidedClaims);

// If claims are present, preemptively force a fresh cert before token call
if (useClaims)
{
    currentCert = HttpClient.post("http://169.254.169.254/.../issuecredential?bypass_cache=true");
}

// First attempt
var tokenResponse = HttpClient.post(
    "https://ests-r/token",
    clientCredential: currentCert,
    claims: useClaims ? appProvidedClaims : null);

// Unlimited remediation loop for credential/attestation failures
while (tokenResponse.error == "invalid_client")
{
    var aadsts = tokenResponse.error_codes?.FirstOrDefault();

    // Credential/attestation validation failures (treat missing error_codes as credential issue)
    if (aadsts is 1000610 or 1000611 or 1000612 or 1000613 or 1000614 || aadsts == null)
    {
        // Remediation: Force a NEW certificate and retry again
        currentCert = HttpClient.post("http://169.254.169.254/.../issuecredential?bypass_cache=true");

        tokenResponse = HttpClient.post(
            "https://ests-r/token",
            clientCredential: currentCert,
            claims: useClaims ? appProvidedClaims : null);

        // Continue loop if still invalid_client with remediable codes
        continue;
    }

    // Not remediable => break and surface
    break;
}

// Success or surface error to caller
return tokenResponse;
~~~

---

## Acceptance Tests

### Test Scenarios and Expected Behavior

| **Test Case**                                            | **Description**                                                                                                 | **Expected Outcome** |
|----------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|----------------------|
| **AADSTS1000610–14 Auto-Remediation (Eventually Succeeds)** | eSTS returns 401 `invalid_client` with any of 1000610–1000614 for N attempts. MSAL forces new cert via `/issuecredential?bypass_cache=true` and retries until success. | Token acquisition succeeds after retries. (`CredentialOutcome=Retry Succeeded`) |
| **AADSTS1000610–14 Auto-Remediation (Never Succeeds)**   | eSTS continuously returns a remediable `invalid_client` (same or varying codes). MSAL keeps reminting and retrying. | Continues retrying (per spec). Implementations may add backoff/jitter to avoid tight loops. |
| **Unspecified Credential Issue (Eventually Succeeds)**   | eSTS returns `invalid_client` without `error_codes` for N attempts. MSAL forces new certificate and retries until success. | Token acquisition succeeds after retries. (`CredentialOutcome=Retry Succeeded`) |
| **Claims Challenge Path**                                | Resource 401 with claims; app supplies claims; MSAL re-mints cert (`bypass_cache=true`) and retries with claims. | New token with claims. (`CredentialOutcome=Success`) |
| **IMDS/IssueCredential Failure Path**                    | `/issuecredential` call fails (network / service / malformed).                                                  | Failure returned; transport retry behavior is per existing MSAL retry logic. |
| **Telemetry Validation**                                 | Validate tags (MsiSource, TokenType, bypassCache, KeyType, CredentialOutcome) across above scenarios.            | Telemetry populated exactly as specified. |

---

## Client-Side Telemetry

To improve observability and diagnostics of Managed Identity (MSI) scenarios within MSAL, we use a **telemetry counter** named `MsalMsiCounter`.

### Counter Name
- **`MsalMsiCounter`**

### Tags
Each time we record `MsalMsiCounter`, include:

1. **MsiSource** — `"AppService"`, `"CloudShell"`, `"AzureArc"`, `"Imds"`, `"ImdsV2"`, `"ServiceFabric"`
2. **TokenType** — `"Bearer"`, `"mtls_pop"`
3. **bypassCache** — `"true"` / `"false"`
4. **KeyType** — `"InMemory"`, `"Hardware"`, `"KeyGuard"`
5. **CredentialOutcome** — `Not found` / `Retry Failed` / `Retry Succeeded` / `Success`
