# Certificate Handling on Windows for MSI v2 / PoP

## Overview
In the MSI v2 flow, the client must present a certificate over mTLS.  
The certificate itself is the **credential** for this flow.  

Currently, the certificate is stored only in memory.  
To reduce repeated calls to IMDS and enable reuse across requests,  
the certificate should be **rooted (persisted)** in the Windows certificate store.

## 1. Certificate Store Location
- Use the **Windows Certificate Store**.
- Scope: **CurrentUser\My** store (per-user, not global).
- This provides:
  - Secure key isolation (KeyGuard).
  - OS-enforced ACLs instead of manual file permissions.

---

## 2. Certificate Lifecycle

1. **Check Cert Store**  
   - Look in `CurrentUser\My` for an MSI certificate.  
   - Identify by subject and issuer.
   - If found and not expired → load it.  

2. **No Valid Certificate**  
   - Generate a key pair (RSA) in KeyGuard (for POP).
   - Get an attestation token for the key. 
   - Create CSR in memory.  
   - Call **IMDS issuecredential** endpoint.  
   - Receive certificate from IMDS.  

3. **Persist the Certificate**  
   - Store the new certificate (with private key) in `CurrentUser\My`.  
   - Mark the key as **non-exportable**.  (uses KeyGuard keys - already non-exportable)

4. **Use Certificate**  
   - Load cert from store as needed.  
   - Use it for mTLS handshake.  

---

## 3. Expiration and Renewal
- Certificates are short-lived (7 days).  
- Always check expiration before use.  
- If expired or close to expiry:  
  - Remove stale cert from store.  
  - Acquire and persist a new cert from IMDS.  

### Proactive Renewal: Start renewal at half the certificate lifetime (typically, 3.5 days) to avoid expiry during active sessions.

**This is calculated based on the certificate’s NotAfter property (expiration date).**
---

## 4. Error Handling
- If store entry is corrupt → remove it and fetch new cert.  
- If IMDS call fails → bubble up error to caller.  

---

# Certificate Store Location in Linux 
- Linux has no OS-backed cert store like Windows.  
- Define a **per-user store** under the home directory:  ~/.config/msal/certs/

- File naming convention:  
- `msal_mtls.pfx` (PKCS#12) or  
- `msal_mtls.pem` (PEM with key + cert).  
- Permissions:  
- Directory = `700` (only the user can read/write/execute).  
- File = `600` (only the user can read/write).  

---

## 5. Security Considerations
- Private keys must be generated in **CNG/KeyGuard** with `ExportPolicy = None`.  
- Keys should **never** be exported or persisted outside the Windows certificate store.  
- `CurrentUser\My` is scoped to the signed-in user.  
- For service scenarios, use `LocalMachine\My` with proper ACLs.

---

## ✅ Summary
- Always **check Windows cert store first** (`CurrentUser\My`).  
- If no valid cert, **request one from IMDS**.  
- **Persist it** back into the store securely.  
- **Refresh on expiration**.  
