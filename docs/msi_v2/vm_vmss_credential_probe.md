# Probe Logic for VM/VMSS Credential Endpoint

```mermaid
sequenceDiagram
    participant SDK
    participant IMDS

    SDK ->> IMDS: 1. Send probe request (POST `/credential` with `.` body, no headers)
    IMDS -->> SDK: 2. Response (HTTP 400 Bad Request / HTTP 500 Internal Server Error / other)

    alt `/credential` endpoint available
        IMDS -->> SDK: 3. Return HTTP 400 Bad Request
        SDK ->> SDK: 4. Confirm `/credential` endpoint exists
    else IMDS is restarting
        IMDS -->> SDK: 3a. Return HTTP 500 Internal Server Error
        SDK ->> SDK: 4a. Check `Server` header for IMDS presence
        alt IMDS header unavailable (No "IMDS/" in `Server` header)
            SDK ->> SDK: 5. fallback to IMDS
        else IMDS header available
            SDK ->> SDK: 5a. Proceed with IMDS V2 Source
        end
    end
```

## 1. Probe Request
To determine if the **MSI V2 `/credential` endpoint** is available in IMDS for **VM/VMSS**, send a **POST request** with the following criteria:
- **No headers**
- **A single period (`.`) as the request body**

### Example Probe Request (PowerShell)
```powershell
Invoke-WebRequest -Uri 'http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0' `
    -Method POST `
    -Body '.' `
    -UseBasicParsing
```

---

## 2. Expected IMDS Response (When Available)
If the `/credential` endpoint exists but the request lacks required metadata headers, **IMDS** responds with **HTTP 400 Bad Request**:

```
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8

{"error":"invalid_request","error_description":"Required metadata header not specified"}
```

### Interpretation:
- Receiving `400 Bad Request` confirms that **IMDS supports the `/credential` endpoint**.
- The SDK can now proceed to use `/credential` for token acquisition.

---

## 3. Handling IMDS Restart Scenario
If **IMDS is restarting**, the **WireServer (a proxy to IMDS)** may return **HTTP 500 Internal Server Error** instead:

```
HTTP/1.1 500 Internal Server Error
Content-Type: text/plain; charset=utf-8
```

- In this case, the `Server` header **does not contain "IMDS/"**.
- This indicates that **IMDS is temporarily unavailable**.
- The SDK **should implement a retry mechanism** with an **exponential backoff strategy**.

---

## 4. Probe Logic Summary

| **Step** | **Action** | **Expected Response** | **Handling** |
|----------|-----------|----------------------|--------------|
| **1️⃣** | Send a **POST request** to `/metadata/identity/credential` **without headers**, using `.` as the body. | | |
| **2️⃣** | **Check HTTP response status.** | | |
| **3️⃣** | If **400 Bad Request**, the `/credential` endpoint **is available**. | Proceed with token acquisition. |
| **4️⃣** | If **500 Internal Server Error**, check the **Server** header. | `"Microsoft-IIS/10.0"` (no `IMDS/`) | Retry the request (IMDS might be restarting). |
| **5️⃣** | If the response does not match the above cases, treat it as **unexpected behavior**. | | Log the issue or fallback to IMDS `/token` if applicable. |

MSAL will use it's existing retry logic to handle the IMDS restart scenario and other retryable errors.

---

## Client-Side Telemetry

To improve observability and diagnostics of Managed Identity (MSI) scenarios within MSAL, we propose introducing a **new telemetry counter** named `MsalMsiCounter`. This counter will be incremented (or otherwise recorded) whenever MSI token acquisition activities occur, capturing the most relevant context in the form of tags.

### Counter Name
- **`MsalMsiCounter`**

### Tags
Each time we increment `MsalMsiCounter`, we include the following tags:

1. **MsiSource**  
   Describes which MSI path or resource is used.  
   - Possible values: `"AppService"`, `"CloudShell"`, `"AzureArc"`, `"ImdsV1"`, `"ImdsV2"`, `"ServiceFabric"`

2. **TokenType**  
   Specifies the type of token being requested or used.  
   - Possible values: `"Bearer"`, `"POP"`, `"mtls_pop"`

3. **bypassCache**  
   Indicates whether the MSAL cache was intentionally bypassed.  
   - Possible values: `"true"`, `"false"`

4. **CertType**  
   Identifies which certificate was used during the MSI V2 flow.  
   - Possible values: `"Platform"`, `"inMemory"`, `"UserProvided"`

5. **CredentialOutcome**  
   If using the `/credential` endpoint (ImdsV2) log the outcome.  
   - Not found 
   - Retry Failed
   - Retry Succeeded
   - Success

6. **MsalVersion**  
   The MSAL library version in use.  
   - Example: `"4.51.2"`

7. **Platform**  
   The runtime/OS environment.  
   - Examples: `"net6.0-linux"`, `"net472-windows"`

---

## 6. Document Purpose
This document provides a **step-by-step breakdown** of the **[MSI V2 authentication probe](https://microsoft.sharepoint.com/:w:/t/AzureMSI/EUOAjN2q-hBNptrwi1ZolLgBsAYYmm_qRKXsoY62D2oiAg?e=hSAVOl)**, ensuring:
- **Correct detection of the `/credential` endpoint.**
- **Proper handling of IMDS restarts and failures.**
- **Secure token acquisition for accessing Azure resources.**
