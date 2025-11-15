# Managed Identity v2 (Attested TB) — Resilience & Caching Plan

## TL;DR

We reduce cold-start latency and dependency risk for MSI v2 by caching safe, long-lived artifacts, coordinating renewal across processes, and keeping the hot path in memory. **MAA is used only to (re)issue the binding certificate**; bound access tokens (ATs) are obtained using that certificate. If the binding cert or its cache is lost or invalid, we recover by re-attesting and re-issuing. No expirations are hardcoded; we always use the values returned by services.

---

## Problem

- Cold starts/reboots trigger extra external calls (MAA → IMDS → eSTS).
- Accessing persisted certificates can contend under load.
- Multiple processes may race to re-issue binding certificates.
- We want resilience to **MAA** issues and predictable **cert renewal** while:
  - avoiding thundering herds, and
  - not hardcoding any lifetimes.

---

## Solution (What’s Changing)

1. **Probe IMDS once per process** to detect **MSI v2** and cache that result in memory for the life of the process.
2. Use the **binding certificate** returned by IMDS `/issuecredential` as the long‑lived credential for bound AT requests:
   - its lifetime comes from the cert / metadata returned by IMDS (no hardcoded duration).
3. **Proactively renew** the binding cert and the MAA token when roughly **half** of their lifetime has elapsed, with a **small random offset** per process, and always **well before** their actual expiry.
4. Use **single‑writer coordination per managed identity (per user context)** on each machine so that only one process issues/renews the binding cert and MAA token; other processes reuse the same artifacts.
5. Use the **MAA token only** for issuing/renewing the binding certificate:
   - cache it for up to its JWT `exp`,
   - refresh it at half‑life + jitter,
   - and evict it on attestation/policy failures.

---

## Call Sequence (cold start)

Cold start / first bound call for a given managed identity + user context:

```text
0: Probe IMDS to detect MSI v2 vs v1 and cache the result in the process.
1: Ensure a KeyGuard key / handle exists for this reboot.
2: Call MAA to obtain an attestation token (using KeyGuard evidence).
3: Call IMDS `/issuecredential` with the MAA token → returns binding certificate + metadata.
4: Call eSTS to request a bound AT (mtls_pop or bearer) using client mTLS with the binding certificate.
5: Call the resource using the bound AT and client mTLS.
