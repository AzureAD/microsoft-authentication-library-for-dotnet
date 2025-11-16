# Managed Identity v2 (Attested TB) — Resilience & Caching Plan

## TL;DR

We reduce cold-start latency and dependency risk for MSI v2 by:

- treating the **binding certificate from IMDS `/issuecredential`** as the long-lived credential for bound ATs,
- caching safe artifacts (MAA token, binding cert, ATs),
- renewing at **half-life with jitter**, and
- using a **single writer per managed identity per user** to avoid thundering herds.

All lifetimes (MAA tokens, binding certs, ATs) come from **MAA / IMDS / eSTS**; nothing is hardcoded.  
If a cached artifact is missing, invalid, or corrupted, we treat it as a **cache miss** and re-acquire via the normal flow.

---

## Behavior Summary

1. **IMDS probe (per process)**  
   - On first MSI use in a process, we probe IMDS to detect **MSI v2 vs v1**.  
   - The result is cached **in that process only** (no cross-process state).

2. **Binding cert as the long-lived credential**  
   - IMDS `/issuecredential` returns a **binding certificate + metadata**.  
   - This cert is the **credential we use to get bound ATs** (mtls_pop/bearer).  
   - Its validity window comes from IMDS (e.g., cert `notBefore` / `notAfter`); we do **not** assume “7 days” or any fixed value.

3. **Renewal timing (half‑life + jitter)**

For any artifact whose expiry comes from the service (MAA, IMDS, eSTS), we:

- treat the time between “when we obtained it” and “when it expires” as its **lifetime**;
- plan to renew it **around halfway through that lifetime** (half‑life);
- add a small, per‑process **random jitter** around that halfway point so different processes don’t all renew at the same time; and
- always enforce a small safety buffer so renewal completes **before** expiry (for example, at least a few minutes before in the worst case).

**Binding certificate vs. others**

- For the **binding certificate**, we additionally guarantee that it is rotated **at least 24 hours before the certificate’s expiry time**. 
- Other artifacts (MAA token, access tokens) simply follow the **half‑life + jitter** rule with the normal safety buffer.


4. **Caches and how they are shared**

- **MAA token (file cache, shared across processes)**  
  - The MAA token is stored in a small per‑user file cache so that all MSAL processes for that user on the same machine can reuse it.  
  - Access to this cache is coordinated so that only one process at a time writes or refreshes the token; other processes read the latest complete value from the file.

- **Binding certificate (persisted in certificate store)**  
  - The binding certificate returned by IMDS `/issuecredential` is persisted in the OS certificate store, scoped per user and per managed identity.  
  - When the certificate is renewed, updates to the store entry are coordinated so that only one process at a time replaces it; other processes continue to read the stored certificate.

- **Access tokens (in‑memory MSAL cache)**  
  - Access tokens remain in MSAL’s existing in‑memory cache, scoped to a single process.  
  - There is no new cross‑process sharing for ATs: each process uses its own in‑memory cache and reacquires bound ATs as needed using the shared binding certificate.


5. **Caches**

   | Item | Scope | Stored as | TTL source | Behavior |
   |---|---|---|---|---|
   | **MSI v2 probe result** | Per process | In-memory | Process lifetime | First MSI call in a process probes IMDS and caches v2/v1/none. If the probe fails, that process falls back to MSI v1 behavior. New processes probe again. |
   | **MAA token** | Per key / identity context | Per-user file cache (shared across processes) | JWT `exp` from MAA | Used **only** for `/issuecredential`. Stored in a small per-user file cache so all MSAL processes for that user on the same machine can reuse it. When it needs to be refreshed, processes coordinate so that only one process updates the file; others read the latest complete value. Renewed at half-life with per-process jitter (always before `exp`). If missing, expired, invalid, or attestation/policy/key errors occur, we discard and get a new token next time. |
   | **Binding cert + `/issuecredential` metadata** | Per managed identity per user | User certificate store (plus metadata) | Cert / metadata from IMDS | Long-lived credential for bound ATs. Persisted in the user’s certificate store so all processes for that user can read the same cert. The cert is renewed at roughly half-life with per-process jitter, but in all cases rotation completes **at least 24 hours before the certificate’s expiry** (where lifetime allows). When renewal happens, only one process at a time updates the stored certificate and metadata; others continue to read the existing entry. If the cert or metadata is missing, invalid, or rejected by IMDS/eSTS (expired, not yet valid, binding mismatch, etc.), we discard it and re-issue via MAA → `/issuecredential`. |
   | **Access tokens (bearer / mtls_pop)** | Per (audience, managed identity, binding-cert thumbprint) | In-memory per process | `exp` from eSTS | Regular MSAL token cache, unchanged by this design. Tokens are cached per process in memory. Never reused past `exp`. On 401/403 or invalid token errors, we drop the token and reacquire with the **current** binding cert. Rotating the binding cert changes the thumbprint, so tokens for the old thumbprint are naturally not reused. |

6. **Failure & recovery**

   - **Lost / deleted cache files** (MAA token or binding cert metadata):  
     - treated as a cache miss → we obtain a new MAA token and/or re‑issue the binding cert on the next call, with only one process updating the shared cache or cert store entry at a time.
   - **Corrupted or invalid entries** (cannot parse, cert not usable, token fails validation):  
     - treated as a cache miss → we discard the bad entry and re-acquire using the normal MAA → IMDS → eSTS flow.
   - **MAA policy / key rotation**:  
     - we don’t poll for changes; we infer them from MAA/IMDS/eSTS errors that clearly indicate attestation/policy/key issues;  
     - on such errors we drop the affected MAA token (and binding cert if needed) and perform a **fresh attestation** on next demand.
   - **Reboot**:  
     - we try the persisted binding cert first; if it is valid and accepted by eSTS, we reuse it and reacquire ATs;  
     - if it fails locally or at eSTS, we treat it as invalid and re-run MAA → `/issuecredential` to get a new cert.

7. **Retries**

   - **MAA**  
     - Calls go through **MAA Native**, which implements its own retry and backoff.  
     - MSAL does **not** control per-call retry policy for MAA and does not add an extra retry layer on top. We only apply the cache invalidation rules above when a MAA call ultimately fails or succeeds.
   - **IMDS and eSTS**  
     - Use the existing MSAL HTTP retry pipeline (bounded retries, exponential backoff, jitter) for transient failures (network, certain 5xx/429, etc.).  
     - No retries for permanent 4xx that indicate bad input or policy violations.  
     - If all retries fail, we surface the error and do not overwrite previously valid cache entries.

8. **Security & isolation (high level)**  

   - Private keys stay in the platform key store (e.g., KeyGuard); MSAL only deals with **handles/evidence**, not raw keys.  
   - Persisted artifacts (MAA tokens, binding certs, metadata) are:
     - scoped to the **current user** and **managed identity**, and
     - stored in per-user secure locations with restricted permissions.  
   - Deleting these artifacts is safe; it just forces a clean re-attestation and re-issuance on next use.

---

## Why This Improves CX

- **MAA is out of the hot path**: steady-state uses cached binding certs and ATs; MAA is only needed to (re)issue certs.
- **No thundering herd**: renew at half-life with per-process jitter, and shared caches (file for MAA token, cert store for binding cert) ensure that only one process refreshes them at a time while others reuse the result.
- **Predictable behavior**: missing/corrupt/expired artifacts always behave like cache misses with a well-defined recovery path.
- **No hidden hardcoded lifetimes**: we always use the lifetimes returned by MAA, IMDS, and eSTS; the only additional rule is that binding certs are rotated at least 24 hours before their expiry.
