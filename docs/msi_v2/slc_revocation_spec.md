# Certificate Revocation Specification

## Overview

This document outlines the design and implementation details for **certificate revocation** in MSI V2 scenarios.

In the MSI v2 authentication flow, MSAL first obtains a credential (certificate) from IMDS and uses it to request a token from eSTS. In some cases, eSTS may respond with an error indicating that the certificate/attestation is no longer valid. When this occurs, **MSAL must obtain a new certificate** before retrying the token request with eSTS.

### certificate revocation

~~~mermaid
sequenceDiagram
    participant Application
    participant MSAL
    participant IMDS
    participant eSTS

    Application ->> MSAL: 1. Request Access Token
    MSAL ->> IMDS: 2. Request Credential (certificate)
    IMDS -->> MSAL: 3. Return credential (certificate)
    MSAL ->> eSTS: 4. Exchange credential for Access Token
    eSTS -->> MSAL: 5. Response (HTTP 200 / error)

    alt Token Revoked / Attestation invalid
        eSTS -->> MSAL: 5a. 401 invalid_client (AADSTS 1000610–1000614)
        MSAL ->> IMDS/eSTS: 6. Mint **new certificate** via /issuecredential?bypass_cache=true
        IMDS/eSTS -->> MSAL: 7. Return new certificate
        MSAL ->> eSTS: 8. Retry Access Token request with new certificate
        eSTS -->> MSAL: 9. Return new Access Token
    else Unspecified Credential Issue
        eSTS -->> MSAL: 5b. Return {"error": "invalid_client"}
        MSAL ->> IMDS: 6a. Mint new certificate via /issuecredential?bypass_cache=true
        IMDS -->> MSAL: 7a. Return new certificate
        MSAL ->> eSTS: 8a. Retry Access Token request with new certificate
        eSTS -->> MSAL: 9a. Return new Access Token
    end

    MSAL ->> Application: 10. Return Access Token
~~~

### claims challenge

~~~mermaid
sequenceDiagram
    participant Application
    participant MSAL
    participant IMDS
    participant eSTS
    participant Resource

    Application ->> MSAL: 1. Request Access Token
    MSAL ->> IMDS: 2. Request Credential (certificate)
    IMDS -->> MSAL: 3. Return certificate
    MSAL ->> eSTS: 4. Exchange certificate for Access Token
    eSTS -->> MSAL: 5. Return Access Token
    MSAL ->> Application: 6. Return Access Token

    Application ->> Resource: 7. Call API with Access Token
    Resource -->> Application: 8. 401 Unauthorized + WWW-Authenticate with claims
    Application ->> MSAL: 9. **Pass the claims** to MSAL
    MSAL ->> IMDS: 10. **Mint new certificate** via /issuecredential?bypass_cache=true
    IMDS -->> MSAL: 11. Return new certificate
    MSAL ->> eSTS: 12. Retry Access Token request **with claims**
    eSTS -->> MSAL: 13. Return new Access Token
    MSAL ->> Application: 14. Return new Access Token
~~~

> **Normative**: When `claims` are provided by the app (from a resource's 401 challenge), **MSAL MUST** mint a new certificate using `/issuecredential?bypass_cache=true` **before** calling eSTS with the `claims` parameter.

## Certificate Revocation Scenarios

- **Revoked/Invalid Certificate (MSAL-handled):** Token request fails due to certificate/attestation issues (AADSTS 1000610–1000614). **MSAL** re-mints the certificate via `/issuecredential?bypass_cache=true` and retries.
- **Unspecified Credential Issue (MSAL-handled):** eSTS returns `invalid_client` without suberror. **MSAL** treats as credential issue, re-mints certificate, retries.
- **Claims Challenge (App + MSAL):** Resource returns 401 with claims. **App** passes claims to MSAL. **MSAL** re-mints certificate (`bypass_cache=true`) and acquires token **with claims**.

## eSTS Response to Indicate Specific Error

| Scenario                       | eSTS/Resource Response                                                          |
|--------------------------------|----------------------------------------------------------------------------------|
| Revoked/Invalid Certificate    | `invalid_client` with AADSTS codes (see table below)                             |
| Unspecified Credential Issue   | `{ "error": "invalid_client" }`                                                  |

## ESTS error mapping for certificate creation/attestation failures

When certificate minting uses an **attestation token** and that token is invalid, eSTS returns **401 Unauthorized** with OAuth2 error `invalid_client` and one of the following AADSTS codes.

| AADSTS Code | ESTS `ErrorMapping ErrorCode`                               | HTTP | OAuth2 `error`   |
|-------------|--------------------------------------------------------------|------|------------------|
| 1000610     | `CertificateCreateAttestationTokenInvalidTimeRange`          | 401  | `invalid_client` |
| 1000611     | `CertificateCreateAttestationTokenInvalidIssuer`             | 401  | `invalid_client` |
| 1000612     | `CertificateCreateAttestationTokenInvalidClaimValue`         | 401  | `invalid_client` |
| 1000613     | `CertificateCreateAttestationTokenInvalidJkuHeader`          | 401  | `invalid_client` |
| 1000614     | `CertificateCreateAttestationTokenInvalidSignature`          | 401  | `invalid_client` |

### MSAL behavior for AADSTS1000610–1000614 (required)

For any of the above codes, **MSAL MUST**:

1. Call the certificate minting endpoint **`/issuecredential?bypass_cache=true`** to force a **new certificate** (ignore any cached/invalid state).
2. Replace the current certificate with the newly issued one.
3. **Retry** the token request with eSTS using the new certificate.
4. Emit telemetry (see below).

> These errors reflect invalid or stale certificate/attestation input; remediation is internal to MSAL. No developer action is required.

## Resource Token Rejection Scenario (claims challenge)

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

## MSAL Behavior Summary

- If `invalid_client` + AADSTS **1000610–1000614** → **MSAL auto-remediates** via `/issuecredential?bypass_cache=true`, then retries token.
- If `invalid_client` with **no** suberror/code → **MSAL** still re-mints certificate with `bypass_cache=true`, then retries.
- If **401 with claims** from a resource → **App passes claims** to MSAL; **MSAL** re-mints certificate (`bypass_cache=true`) and calls eSTS **with claims**.

### MSAL Pseudo Code Implementation

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

if (tokenResponse.error == "invalid_client")
{
    var aadsts = tokenResponse.error_codes?.FirstOrDefault();

    // Certificate/attestation validation failures
    if (aadsts is 1000610 or 1000611 or 1000612 or 1000613 or 1000614 || aadsts == null)
    {
        // Force a NEW certificate and retry
        currentCert = HttpClient.post("http://169.254.169.254/.../issuecredential?bypass_cache=true");

        tokenResponse = HttpClient.post(
            "https://ests-r/token",
            clientCredential: currentCert,
            claims: useClaims ? appProvidedClaims : null);
    }
}

// Success or surface error to caller
return tokenResponse;
~~~

## Acceptance Tests

### Test Scenarios and Expected Behavior

The following acceptance tests validate the behavior of MSAL when handling **certificate revocation** and **claims challenge** scenarios.

| **Test Case**                                   | **Description**                                                                                         | **Expected Outcome** |
|-------------------------------------------------|---------------------------------------------------------------------------------------------------------|----------------------|
| **AADSTS1000610–14 Auto-Remediation**           | eSTS returns 401 `invalid_client` with any of 1000610–1000614.                                          | MSAL calls `/issuecredential?bypass_cache=true`, obtains a new certificate, retries; success or deterministic failure surfaced. |
| **Unspecified Credential Issue**                 | eSTS returns `invalid_client` without codes.                                                            | MSAL forces new certificate (bypass cache) and retries. |
| **Claims Challenge Path**                        | Resource returns 401 with claims; app passes claims to MSAL.                                            | MSAL mints new certificate (`bypass_cache=true`), acquires token **with claims**. |
| **IMDS/IssueCredential Failure Path**            | `/issuecredential` fails.                                                                               | MSAL returns failure with diagnostics; no infinite retry. |
| **Telemetry Validation**                         | Verify counter/tags capture AADSTS code, `bypass_cache=true`, and `CredentialOutcome`.                  | Telemetry populated as specified. |

## Client-Side Telemetry

To improve observability and diagnostics of Managed Identity (MSI) scenarios within MSAL, we use a **telemetry counter** named `MsalMsiCounter`.

### Counter Name
- **`MsalMsiCounter`**

### Tags
Each time we record `MsalMsiCounter`, include:

1. **MsiSource** — `"AppService"`, `"CloudShell"`, `"AzureArc"`, `"ImdsV1"`, `"ImdsV2"`, `"ServiceFabric"`  
2. **TokenType** — `"Bearer"`, `"POP"`, `"mtls_pop"`  
3. **bypassCache** — `"true"` / `"false"`  
4. **CertType** — `"Platform"`, `"inMemory"`, `"UserProvided"`  
5. **CredentialOutcome** — `Not found` / `Retry Failed` / `Retry Succeeded` / `Success`  
6. **MsalVersion** — e.g., `"4.61.0"`  
7. **Platform** — e.g., `"net8.0-linux"`, `"net472-windows"`
