# Managed Identity v2 (Attested TB) — Resilience & Caching Plan

## TL;DR
We reduce cold-start latency and dependency risk for MSI v2 by caching safe, long-lived artifacts, coordinating renewal across processes, and keeping the hot path in memory. **MAA is used only to (re)issue the binding certificate**; bound AT acquisition relies on that cert. Result: fewer failures, less churn, smoother CX.

---

## Problem
- Cold starts/reboots trigger extra external calls (MAA → IMDS → eSTS).
- OS certificate store I/O can contend under load.
- Multiple processes may race to re-issue binding certificates.
- We want resilience to **MAA** issues and predictable **cert renewal**.

---

## Solution (What’s Changing)
1. **Probe once** (link-local) to detect **MSI v2** → cache result **in-proc**.
2. Treat the **binding certificate** (from IMDS `/issuecredential`) as the **primary anchor** (~7-day validity); use it to get ATs.
3. **Proactive renewal at half-life (+ small jitter)** to rotate well before expiry.
4. **Single-writer coordination** so only one process issues/renews; others reuse the same cert.
5. **MAA token** is used **only** for issuance/renewal; short-lived cache to prevent attestations calls.

---

## Call Sequence (cold start)
```
Call 0 (local): Probe IMDS v2 → cache MSI source (V2/V1)
1       (local): Create KeyGuard key (per reboot)
2       (external): Get MAA token  // only for (re)issuing cert
3       (local): IMDS /issuecredential → binding cert + metadata
4       (external): eSTS-R → bound AT (mtls_pop/bearer) using client mTLS
5       (external): Call resource with bound AT + client mTLS
```
---

## Cache & Renewal Matrix

| Item | Scope | Where | TTL | Notes |
|---|---|---|---|---|
| **MSI v2 probe result** | Per process | In-proc static | Process lifetime | NO changes needed here |
| **MAA token** | Per **keyHandle** | small file cache | ≤ JWT `exp` (~8h) | Only for cert issuance; evict on reboot/policy change/attest fail; refresh half-life + jitter |
| **Binding cert + `/issuecredential` metadata** | Per **Managed Identity per user context** | Persisted (Win: `CurrentUser\My`; Linux: protected file/PEM) | ~7 days | Renew at **half-life + jitter**; Serialize issuance |
| **Access tokens (`bearer` or `mtls_pop`)** | Per audience | In memory | Service-configured | Reacquire after reboot (new key) |

---

## Invalidation Rules
- **Reboot** → Use **persisted binding cert** to fetch new ATs; re-attest on first demand on service failure.
- **Cert expiry** → re-issue.
- **MAA token expired** → re-attest and re-issue.

---

## Security
- Keys are **non-exportable** in **KeyGuard**; MSAL stores **handles**, not private keys.
- Persisted items are **user-scoped** and protected (DPAPI on Windows; restricted file perms/OS keyring on Linux).

---

  ## Why This Improves CX
- **MAA is out of the hot path**—steady-state calls rely on a **multi-day binding cert**.
- Different identities on the same VM, uses **cached MAA token**
- **No thundering herd**—single process renews certificate; others reuse.
- **Predictable renewals**—half-life + jitter prevents synchronized spikes.

---
