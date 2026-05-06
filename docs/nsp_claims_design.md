# WithClientClaims API Design

## Background

Azure Redis Cache operates in a Backing resource VM/VMSS and uses MSAL with Managed Identity credentials to acquire tokens from ESTS. The Redis team has requested that MSAL support sending NSP (Network Security Perimeter) claims to IMDS, so that the resulting tokens contain the NSP claim required to access NSP-protected resources.

This document proposes a new `WithClientClaims()` API to support this scenario in a consistent, safe, and harmonized way across all MSAL auth flows.

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

MSIv2 uses a different protocol from MSIv1. It acquires an mTLS binding certificate from IMDS, then makes a POST directly to an ESTS token endpoint (`/oauth2/v2.0/token`). Claims are not forwarded in that POST request today.

> **Open question for IMDS team**: Does the MSIv2 ESTS endpoint accept a `claims` body parameter the same way the standard ESTS token endpoint does?

## Proposed API: `WithClientClaims(string claimsJson)`

Add `WithClientClaims(string claimsJson)` across the MSI, client credentials, and FIC request builders.

### Distinction from `WithClaims()`

| API | Who originates | Cache behavior | Use case |
|---|---|---|---|
| `WithClaims()` | Server (ESTS / web API challenge) | Bypasses cache | CAE, MFA step-up |
| `WithClientClaims()` | Client application | Cached, keyed on claims value | NSP, Step-Up |

### Key Behaviors

1. **Does not bypass the cache.** Tokens are cached and keyed on the claims value. Different claims values produce separate cache entries.

2. **Transport-agnostic API.** MSAL routes the claims to the correct location per flow:
   - MSIv1: query parameter to IMDS
   - MSIv2: body parameter in the ESTS POST request
   - Cert-based / FIC: body parameter sent to ESTS

3. **MSAL owns the JSON merge.** If a server-issued claims challenge (e.g., CAE) arrives while `WithClientClaims` is set, MSAL merges the two claims objects using the existing `ClaimsHelper` infrastructure. This infrastructure already performs JSON merging for cert-based flows today.

4. **Stable claims only.** Callers should avoid dynamic values (timestamps, nonces) in the claims string — each unique claims value creates a distinct cache entry, and frequently changing values will create an unbounded cache.

### Handling Dynamic Claims

If dynamic claims truly cannot be avoided, the following options are available (each with tradeoffs):

| Option | Description | Risk |
|---|---|---|
| `IncludeInCacheKey: false` via `WithExtraQueryParameters` | Claims sent with the request but excluded from the cache key | Cached token may not satisfy the current claims requirement — incorrect for security-sensitive claims |
| `WithClaims()` (existing) | Always bypass the cache | Hits IMDS on every call; will cause throttling for high-throughput workloads like Redis |
| Disable internal cache (`CacheOptions.DisableInternalCacheOptions`) | Caller manages their own cache externally | Maximum flexibility, maximum complexity |
| Caller normalizes claims | Strip dynamic fields before passing to `WithClientClaims`; send dynamic parts separately via `WithExtraQueryParameters` with `IncludeInCacheKey: false` | Requires caller to understand the claims structure |

For the NSP use case specifically, the claims represent a network security perimeter identifier, which is stable per workload deployment. Dynamic values are not expected to be an issue here.

## Open Questions

1. **API shape**: Is `WithClientClaims` the right name and signature for all teams involved?

2. **MSIv2 protocol** *(for IMDS team)*: Does the MSIv2 ESTS endpoint accept a `claims` body parameter? This cannot be confirmed from the MSAL code alone.

3. **MSIv1 claims param name**: Should the NSP claim be sent as `claims` (OIDC standard) or under a different query parameter name specific to IMDS?

4. **Rollout scope**: Implement for all flows in one PR, or start with MSIv1 and extend MSIv2/cert/FIC in follow-ups?

## Related

- `WithClaims()` — `AcquireTokenForManagedIdentityParameterBuilder.cs`
- `WithExtraClientAssertionClaims()` — `AbstractConfidentialClientAcquireTokenParameterBuilderExtension.cs`
- `ClaimsHelper.GetMergedClaimsAndClientCapabilities()` — `ClaimsHelper.cs`
- `ManagedIdentitySourceExtensions.SupportsClaimsAndCapabilities()` — `ManagedIdentitySourceExtensions.cs`
- `CacheKeyFactory.GetAppTokenCacheItemKey()` — `CacheKeyFactory.cs`
