# Probe Logic for VM/VMSS Credential Endpoint

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

## 5. Document Purpose
This document provides a **step-by-step breakdown** of the **[MSI V2 authentication probe](https://microsoft.sharepoint.com/:w:/t/AzureMSI/EUOAjN2q-hBNptrwi1ZolLgBsAYYmm_qRKXsoY62D2oiAg?e=hSAVOl)**, ensuring:
- **Correct detection of the `/credential` endpoint.**
- **Proper handling of IMDS restarts and failures.**
- **Secure token acquisition for accessing Azure resources.**
