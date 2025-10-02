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

---

## 4. Error Handling
- If store entry is corrupt → remove it and fetch new cert.  
- If IMDS call fails → bubble up error to caller.  

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
