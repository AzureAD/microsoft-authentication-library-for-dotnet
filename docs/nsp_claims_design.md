# WithClientClaims API Design

## Background

Azure Redis Cache operates in a Backing resource VM/VMSS and uses MSAL with Managed Identity credentials to acquire tokens from ESTS. The Redis team has requested that MSAL support sending NSP (Network Security Perimeter) claims to IMDS, so that the resulting tokens contain the NSP claim required to access NSP-protected resources.

This document proposes a new `WithClientClaims()` API to support this scenario in a consistent, safe, and harmonized way across all MSAL auth flows.

## Scope and Initial Rollout

> **Important:** This feature is initially scoped to a specific scenario. The IMDS/MIRP team will gate it in the service layer.

- **First consumer**: Azure Redis Cache
- **Initial service-side scope**: MIRP will enable this only for **delegated identities** in Redis Cache
- **General availability**: Depends on IMDS/MIRP team's rollout plan; do not assume this is a general-purpose NSP claims mechanism at this time

## Problem Statement

The existing `WithClaims()` API in MSAL is designed for **server-issued** claims challenges — situations where ESTS or a downstream web API rejects a token and asks the client to re-authenticate with specific claims (e.g., CAE, MFA step-up). Its behavior is:

- Claims bypass the token cache on every call
- Intended to be used reactively, in response to a 401/challenge

This is the wrong model for NSP claims, where:

- The claims are **known upfront** by the client application
- Tokens **should be cached** per claims value to avoid hammering IMDS
- The claims are **stable** (not dynamic/time-bound)

Additionally, the current behavior of `WithClaims()` for MSIv1 (IMDS) is broken in a subtle way: it bypasses the cache but **never actually forwards the claims value to IMDS**. The claim is silently dropped.

## State of Each Auth Flow Today

### Cert-based (`client_credentials`) and FIC

These flows are in the best shape. Claims are already sent to ESTS as a body parameter via `TokenClient`. MSAL already has a `ClaimsHelper` class that JSON-merges user-supplied claims with client capabilities before sending.

`WithExtraClientAssertionClaims` already exists as an API that puts client-originated claims inside the signed JWT assertion and correctly includes them in the cache key. The main gap is that `WithClaims()` still bypasses the cache, so for NSP-style client-originated claims that should be cached, callers have no clean option today.

### MSIv1 (IMDS)

This is the biggest gap. `WithClaims()` bypasses the cache, but it **never actually sends the claims value to IMDS** as a query parameter. Claims forwarding in the MSI pipeline is currently only enabled for Service Fabric — IMDS is not wired up. The current behavior for MSIv1 is: cache bypassed, claims silently dropped.

For the NSP scenario, claims need to be sent to IMDS as a query parameter **and** the resulting token needs to be cached (not bypassed).

### MSIv2 (IMDS v2)

MSIv2 uses a different protocol from MSIv1. It acquires an mTLS binding certificate from IMDS, then makes a POST directly to an ESTS token endpoint (`/oauth2/v2.0/token`). The MSIv2 design for `WithClientClaims` is not finalized — the IMDS team is still working on it. See the **ETAs** section.

## Proposed API: `WithClientClaims(string claimsJson)`

Add `WithClientClaims(string claimsJson)` across the MSI, client credentials, and FIC request builders.

### Naming note: coexistence with the existing obsolete `WithClientClaims`

`ConfidentialClientApplicationBuilder` already has an **obsolete, app-level** `WithClientClaims(X509Certificate2, IDictionary<string,string>, ...)` that signs extra claims into the client assertion JWT. The new API described here is a **request-level** method on `AcquireTokenForManagedIdentityParameterBuilder` and `AcquireTokenForClientParameterBuilder` that takes a JSON string. The two APIs are on different classes with different signatures and coexist without ambiguity. The obsolete app-level overload remains for backward compatibility and is unaffected by this change.

### Distinction from `WithClaims()`

| API | Who originates | Cache behavior | Use case |
|---|---|---|---|
| `WithClaims()` | Server (ESTS / web API challenge) | Bypasses cache | CAE, MFA step-up |
| `WithClientClaims()` | Client application | Cached, keyed on claims value | NSP, Step-Up |

### Key Behaviors

1. **Does not bypass the cache.** Tokens are cached and keyed on the claims value. Different claims values produce separate cache entries.

2. **Transport-agnostic API.** MSAL routes the claims to the correct location per flow:
   - MSIv1: percent-encoded query parameter (`claims=...`) to IMDS
   - MSIv2: body parameter in the ESTS POST request *(design pending IMDS team confirmation)*
   - Cert-based / FIC: `claims` body parameter sent to ESTS — **not** embedded in the client assertion JWT

3. **CCA: claims go in the request body, not the JWT.** For confidential client flows, `WithClientClaims` sends the NSP claim as a standard ESTS `claims` body parameter. It is **not** placed inside the signed client assertion JWT. The existing `WithExtraClientAssertionClaims` API (separate, unrelated) handles the JWT-embedding path. These two APIs are distinct and serve different purposes.

4. **MSAL owns the JSON merge.** If a server-issued claims challenge (e.g., CAE) arrives while `WithClientClaims` is set, MSAL merges the two claims objects using the existing `ClaimsHelper` infrastructure. This infrastructure already performs JSON merging for cert-based flows today.

5. **Stable claims only.** Callers should avoid dynamic values (timestamps, nonces) in the claims string — each unique claims value creates a distinct cache entry, and frequently changing values will create an unbounded cache.

### MSIv1 claim restriction

MSIv1 (IMDS v1) only accepts a single custom claim: `xms_az_nwperimid`. Any other claim key in the JSON causes IMDS to return HTTP 400 Bad Request with no diagnostic detail.

To avoid this silent failure, MSAL validates the claims JSON upfront for MSIv1 requests. If any top-level key other than `xms_az_nwperimid` is present, MSAL throws `MsalClientException(MsalError.InvalidRequest)` before making any network call.

### Handling Dynamic Claims

If dynamic claims truly cannot be avoided, the following options are available (each with tradeoffs):

| Option | Description | Risk |
|---|---|---|
| `IncludeInCacheKey: false` via `WithExtraQueryParameters` | Claims sent with the request but excluded from the cache key | Cached token may not satisfy the current claims requirement — incorrect for security-sensitive claims |
| `WithClaims()` (existing) | Always bypass the cache | Hits IMDS on every call; will cause throttling for high-throughput workloads like Redis |
| Disable internal cache (`CacheOptions.DisableInternalCacheOptions`) | Caller manages their own cache externally | Maximum flexibility, maximum complexity |
| Caller normalizes claims | Strip dynamic fields before passing to `WithClientClaims`; send dynamic parts separately via `WithExtraQueryParameters` with `IncludeInCacheKey: false` | Requires caller to understand the claims structure |

For the NSP use case specifically, the claims represent a network security perimeter identifier, which is stable per workload deployment. Dynamic values are not expected to be an issue here.

### Why the API is request-level, not app-level

`WithClientClaims` is intentionally placed on the request builder, not the application builder, to support scenarios where claims change at runtime — for example, when an admin toggles NSP enforcement mode, the NSP SDK vends updated claims and the workload needs MSAL to acquire a new token scoped to those claims. If claims were baked into the application object, the caller would have to destroy and recreate the `ManagedIdentityApplication` on every enforcement change.

Typical NSP usage:

```csharp
// nspContext is updated by the NSP SDK when enforcement mode changes.
// Each distinct claims value maps to its own cache entry.
string currentNspClaims = nspContext.GetCurrentClaimsJson();

AuthenticationResult result = await miApp
    .AcquireTokenForManagedIdentity("https://management.azure.com/")
    .WithClientClaims(currentNspClaims)
    .ExecuteAsync(cancellationToken)
    .ConfigureAwait(false);
```

The per-request placement means the caller doesn't need to recreate the app when claims update — a new request with new claims produces a new cache entry automatically.

## ETAs

| Flow | Owner | Status | ETA |
|------|-------|--------|-----|
| CCA (cert-based / FIC) | Robbie | ✅ Done — included in POC PR | — |
| MSIv1 (IMDS v1) | Raghu | In progress | Canary by June 30 |
| MSIv2 (IMDS v2) | TBD | Blocked — IMDS team design pending | Q2/Q3 |

## E2E Testing Plan

E2E testing requires the Redis Cache team's help because this feature is gated in MIRP for Redis Cache delegated identities only.

- **MSI flows**: Requires a test VM with Managed Identity configured inside an NSP. The Redis Cache team will coordinate access to a suitable test environment.
- **CCA flow**: Can be tested with an existing MSAL test tenant app registration. Verify that the `claims` body parameter is forwarded to ESTS and the returned token contains `xms_az_nwperimid`.
- **Status**: Nidhi is confirming test environment availability with the internal team and the Redis Cache team.

## Resolved Questions

| # | Question | Resolution |
|---|----------|------------|
| 1 | Is `WithClientClaims` the right name? | Yes — agreed across teams |
| 2 | CCA: request body or client assertion JWT? | **Request body only.** Claims are sent as the ESTS `claims` body parameter. They are not embedded in the signed client assertion JWT. |
| 3 | MSIv1 claims param name | `claims` query parameter (OIDC standard), percent-encoded |
| 4 | Rollout scope | MSIv1 first; MSIv2 and CCA follow once MSIv2 design is ready from IMDS team |

## Open Questions

1. **MSIv2 protocol** *(for IMDS team)*: What additional changes are needed in the `/issuecredential` request body to signal that custom claims are in use? The IMDS team is designing this; MSAL implementation will follow once the contract is confirmed.

2. **E2E test environment**: What test VM and tenant are available? *(Nidhi coordinating with internal team and Redis Cache)*

## Related

- `WithClaims()` — `AcquireTokenForManagedIdentityParameterBuilder.cs`
- `WithExtraClientAssertionClaims()` — `AbstractConfidentialClientAcquireTokenParameterBuilderExtension.cs`
- `ClaimsHelper.GetMergedClaimsAndClientCapabilities()` — `ClaimsHelper.cs`
- `ManagedIdentitySourceExtensions.SupportsClaimsAndCapabilities()` — `ManagedIdentitySourceExtensions.cs`
- `CacheKeyFactory.GetAppTokenCacheItemKey()` — `CacheKeyFactory.cs`
- POC implementation — PR #5999

