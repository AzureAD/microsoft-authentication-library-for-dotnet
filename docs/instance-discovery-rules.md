# Instance Discovery Rules for MSAL

## Purpose

Instance discovery resolves an authority host (e.g., `login.microsoftonline.com`) to its **metadata**: preferred network host, preferred cache host, and a list of aliases. This metadata enables SSO across aliased environments and ensures tokens are sent to the correct endpoint.

This document describes the rules implemented in MSAL.NET to aid reimplementation in other MSAL libraries.

---

## 1. Core Data Model

Instance discovery produces a single entry per authority environment:

```
InstanceDiscoveryMetadataEntry:
  preferred_network: string   # host to use for token requests
  preferred_cache:   string   # host to use as cache key
  aliases:           string[] # all equivalent hosts (used for SSO, cache lookups)
```

A successful network response returns an array of these entries:

```json
{
  "tenant_discovery_endpoint": "https://login.microsoftonline.com/{tenant}/.well-known/openid-configuration",
  "metadata": [
    {
      "preferred_network": "login.microsoftonline.com",
      "preferred_cache": "login.windows.net",
      "aliases": ["login.microsoftonline.com", "login.windows.net", "login.microsoft.com", "sts.windows.net"]
    }
  ]
}
```

---

## 2. Applicability

| Authority Type | Instance Discovery Supported |
|---|---|
| AAD (Entra ID) | ✅ Yes |
| B2C | ❌ No — use self-entry |
| ADFS | ❌ No — use self-entry |
| CIAM / dSTS | ❌ No — use self-entry |

A **self-entry** means: `preferred_network = preferred_cache = aliases = [configured_authority_host]`.

**Rule:** If the authority type does not support instance discovery, skip all providers and immediately return a self-entry.

---

## 3. Known Environments (Hardcoded)

MSAL maintains a static, hardcoded list of known cloud environments and their metadata. This avoids network calls for common clouds.

| Cloud | Aliases | Preferred Network | Preferred Cache |
|---|---|---|---|
| **Public** | `login.microsoftonline.com`, `login.windows.net`, `login.microsoft.com`, `sts.windows.net` | `login.microsoftonline.com` | `login.windows.net` |
| **China** | `login.partner.microsoftonline.cn`, `login.chinacloudapi.cn` | `login.partner.microsoftonline.cn` | `login.partner.microsoftonline.cn` |
| **US Gov** | `login.microsoftonline.us`, `login.usgovcloudapi.net` | `login.microsoftonline.us` | `login.microsoftonline.us` |
| **Germany (legacy)** | `login.microsoftonline.de` | `login.microsoftonline.de` | `login.microsoftonline.de` |
| **US (login-us)** | `login-us.microsoftonline.com` | `login-us.microsoftonline.com` | `login-us.microsoftonline.com` |
| **PPE** | `login.windows-ppe.net`, `sts.windows-ppe.net`, `login.microsoft-ppe.com` | `login.windows-ppe.net` | `login.windows-ppe.net` |
| **Bleu (FR)** | `login.sovcloud-identity.fr` | `login.sovcloud-identity.fr` | `login.sovcloud-identity.fr` |
| **Delos (DE)** | `login.sovcloud-identity.de` | `login.sovcloud-identity.de` | `login.sovcloud-identity.de` |
| **GovSG** | `login.sovcloud-identity.sg` | `login.sovcloud-identity.sg` | `login.sovcloud-identity.sg` |

**Known environment check for the known metadata provider**: The known metadata provider is only usable when **all** environments already present in the token cache are themselves known. If any cached environment is unknown, the known provider must be skipped (because the network may return richer alias data).

---

## 4. User-Supplied Overrides

MSAL provides two APIs for callers to override the default instance discovery behavior.

### 4.1 User-Supplied Instance Metadata (`WithInstanceDiscoveryMetadata(string json)`)

Callers can supply a complete pre-fetched instance discovery response as a JSON string (matching the format returned by the AAD instance discovery endpoint).

**Effect**: When this is set, the user-supplied metadata **completely short-circuits all other providers**. The `UserMetadataProvider` is consulted first in every resolution flow (§5). If the authority environment is found in the user-supplied metadata, that entry is used. If the environment is **not** found, an exception is thrown immediately (fail-fast) rather than falling back to the network.

**Use case**: Microservice/service environments where a shared discovery cache is pre-fetched and distributed out-of-band.

### 4.2 User-Supplied Discovery URI (`WithInstanceDiscoveryMetadata(Uri instanceDiscoveryUri)`)

Callers can supply a custom URI to use as the instance discovery endpoint instead of the AAD default.

**Effect**: When this is set, the network call in §5 goes to this URI instead of computing the host based on the authority (see §5 Discovery Endpoint Selection). The user-supplied URI completely replaces the default endpoint — both for known and unknown cloud environments.

**Use case**: Organizations that operate their own instance discovery service.

---

## 5. Discovery Endpoint Selection

When a network call is needed, MSAL must choose which host to call:

| Condition | Discovery Endpoint Used |
|---|---|
| User supplied a discovery URI (`WithInstanceDiscoveryMetadata(Uri)`) | The user-supplied URI (verbatim, no modification) |
| Known environment (e.g., `login.microsoftonline.com`) | Same host: `https://{authority_host}/common/discovery/instance` |
| Unknown environment (e.g., `login.microsoft.new`) | Fallback to default trusted host: `https://login.microsoftonline.com/common/discovery/instance` |

The query parameters are:
```
GET https://{discovery_host}/common/discovery/instance
    ?api-version=1.1
    &authorization_endpoint=https://{authority_host}/{tenant}/oauth2/v2.0/authorize
```

---

## 6. Provider Resolution Order

### 6.1 Full Flow (GetMetadataEntryAsync — used during token acquisition)

Providers are consulted in strict priority order. The first non-null result wins.

```
0. [User-supplied metadata provider (WithInstanceDiscoveryMetadata(string))]
   → If set: return entry for authority, or THROW if not found. STOP.

1. [If instance discovery is disabled (WithInstanceDiscovery(false))]
   → Check region discovery provider (regions are not affected by the disable flag)
   → If still null, return self-entry. STOP.

2. Region discovery provider

3. Network call (FetchNetworkMetadataOrFallback)
   → Endpoint: per §5 (user-supplied URI overrides default host selection)
   → On success: cache all entries by alias in the network cache
   → On "invalid_instance" error: see §7
   → On any other error (404, 502, network failure, etc.): see §8

4. [If still null] Log warning, create self-entry, cache it in network cache
```

### 6.2 Cache-Preferring Flow (GetMetadataEntryTryAvoidNetworkAsync — used during token acquisition with cache check)

This flow tries to avoid network calls when possible:

```
0. [User-supplied metadata provider (WithInstanceDiscoveryMetadata(string))]
   → If set: return entry for authority, or THROW if not found. STOP.

1. Region discovery provider

2. [If instance discovery is disabled] → return self-entry

3. Network cache (static, populated from prior network calls)

4. Known metadata provider (hardcoded, but only if all cached environments are known)

5. Full flow (§6.1)

6. [If still null] Return self-entry
```

### 6.3 Offline Flow (GetMetadataEntryAvoidNetwork — used for GetAccounts/AcquireTokenSilent)

No network calls are ever made:

```
1. [If instance discovery is enabled]:
   a. Network cache
   b. Known metadata provider (with null existing environments → always usable)

2. [If still null] Return self-entry
```

---

## 7. Error Handling: `invalid_instance`

When the discovery endpoint returns an `invalid_instance` error (the AAD server-side error `AADSTS50049`), this means the authority genuinely does not exist.

| `ValidateAuthority` setting | Behavior |
|---|---|
| `true` (default) | **Throw** `MsalServiceException` with error code `invalid_instance`. Do NOT cache. |
| `false` | Return a self-entry. Continue without validation. |

---

## 8. Error Handling: All Other Errors (404, 502, network failures, etc.)

When instance discovery fails with any error other than `invalid_instance`:

1. **Try the known metadata provider** for the authority host (with empty `existingEnvironmentsInCache` to force lookup).
2. If the known provider has no entry, **create a self-entry**.
3. **Cache the result** in the network cache so that subsequent calls do NOT retry the network call.
4. Return the entry.

**Critical rule**: The fallback entry MUST be cached. Without caching, every token request retries the failing network call, causing performance degradation and unnecessary traffic. (See [issue #5804](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5804).)

---

## 9. Caching Rules

### 9.1 Network Cache

- **Scope**: Static / process-wide (shared across all app instances in the same process).
- **Key**: Authority host string (case-sensitive).
- **Population**: 
  - On successful network response: cache each entry keyed by each of its aliases.
  - On network failure (non-`invalid_instance`): cache the fallback entry keyed by the authority host.
- **Eviction**: None. Entries persist for the process lifetime.
- **Thread safety**: Must be thread-safe (concurrent dictionary or equivalent).

### 9.2 Known Metadata Cache

- **Scope**: Static / compiled into the library.
- **Immutable**: Never modified at runtime.
- **Guard**: Only usable when all environments in the token cache are themselves known environments.

---

## 10. Interaction with Regions

- Regional discovery (e.g., `centralus.login.microsoft.com`) runs independently of instance discovery.
- Even when instance discovery is disabled (`WithInstanceDiscovery(false)`), region discovery still runs.
- Regional metadata takes precedence when available (checked before the network call).

---

## 11. Interaction with Authority Validation

Authority validation is a separate step that runs **after** instance discovery:

1. Instance discovery resolves metadata (preferred network, aliases).
2. If `ValidateAuthority` is true AND instance discovery is enabled:
   - Validate the authority against the OIDC endpoint.
   - Cache successful validations by environment.

---

## 12. Validation Test Matrix

The following test scenarios should be implemented in any MSAL that supports instance discovery. Tests use HTTP mocking (no real network calls).

### T1: Known Cloud — Discovery Happens on Same Host

**Setup**: Authority = `https://login.microsoftonline.com/tenant` (or any known cloud host).  
**Expected**:
- Instance discovery GET request goes to `https://login.microsoftonline.com/common/discovery/instance`.
- Token request goes to `https://login.microsoftonline.com/tenant/oauth2/v2.0/token`.
- Token is acquired successfully.

**Test hosts to cover**: `login.microsoftonline.com`, `login.microsoftonline.us`, `login.microsoftonline.de`, `login.partner.microsoftonline.cn`, `login.sovcloud-identity.fr`, `login.sovcloud-identity.de`, `login.sovcloud-identity.sg`.

### T2: Instance Discovery Disabled — No Network Discovery Call

**Setup**: Authority = any (known or unknown), `WithInstanceDiscovery(false)`.  
**Expected**:
- No GET request to the discovery endpoint.
- Token request goes directly to the configured authority.
- Token is acquired successfully.

### T3: Unknown Cloud — Discovery Falls Back to Default Trusted Host

**Setup**: Authority = `https://unknown.host.example/tenant` (not in known list).  
**Expected**:
- Instance discovery GET request goes to `https://login.microsoftonline.com/common/discovery/instance` (the default trusted host).
- If discovery succeeds: metadata is cached, token request uses the resolved preferred network.
- If discovery fails (e.g., 404): fallback entry is created and cached, token request goes to the original authority.

### T4: Discovery Failure (404/502) — Fallback Is Cached, No Retry

**Setup**: Authority = unknown host. Discovery endpoint returns 404 or 502.  
**Expected**:
- First `AcquireTokenForClient`: discovery fails, fallback entry created and cached, token acquired from IdP.
- Second `AcquireTokenForClient` (same scope): token served from cache, no network calls.
- Third `AcquireTokenForClient` (different scope): token acquired from IdP, NO discovery call (fallback is cached).

**HTTP mocks (in order)**:
1. GET `https://login.microsoftonline.com/common/discovery/instance` → 404 (or 502)
2. POST token → 200 (success)
3. POST token → 200 (success) — for the different-scope call

If the SDK makes an unexpected discovery call, the mock framework should fail.

### T5: Discovery Failure (Network Error / HttpRequestException) — Fallback, No Retry

**Setup**: Authority = unknown host. Discovery endpoint throws a network-level exception.  
**Expected**: Same as T4 — fallback is cached, subsequent calls don't retry.

### T6: Discovery Failure with `invalid_instance` — Throw (ValidateAuthority=true)

**Setup**: Authority = unknown host. Discovery endpoint returns `invalid_instance` error. `ValidateAuthority` = true (default).  
**Expected**:
- `MsalServiceException` is thrown with error code `invalid_instance`.
- The known metadata provider is NOT consulted as a fallback.

### T7: Discovery Failure with `invalid_instance` — Proceed (ValidateAuthority=false)

**Setup**: Authority = unknown host. Discovery endpoint returns `invalid_instance`. `ValidateAuthority` = false.  
**Expected**:
- No exception thrown.
- A self-entry is returned (authority used as-is).
- Token is acquired successfully.

### T8: Known Metadata — Used When All Cache Environments Are Known

**Setup**: Authority = `login.microsoftonline.com`. Token cache contains entries only for known environments.  
**Expected**:
- Known metadata provider returns the hardcoded entry.
- No network discovery call.

### T9: Known Metadata Bypassed — Unknown Environment in Cache

**Setup**: Authority = `login.microsoftonline.com`. Token cache contains entries for both known and unknown environments.  
**Expected**:
- Known metadata provider is bypassed (returns null).
- Network discovery call is made.

### T10: B2C / ADFS — Instance Discovery Skipped

**Setup**: Authority = B2C or ADFS authority.  
**Expected**:
- No instance discovery call.
- Self-entry is returned.

### T11: Airgapped Cloud with Regions — Discovery Disabled

**Setup**: Authority = unknown host. `WithInstanceDiscovery(false)`. `WithAzureRegion("centralus")`.  
**Expected**:
- No instance discovery call.
- Region discovery still runs.
- Token request goes to the regionalized endpoint.

### T12: Airgapped Cloud with Regions — Discovery Enabled but Fails

**Setup**: Authority = unknown host. `WithAzureRegion("centralus")`. Instance discovery call throws (e.g., `HttpRequestException`).  
**Expected**:
- Instance discovery failure is swallowed.
- Token request still succeeds using the regionalized endpoint.
- No retry of instance discovery on subsequent calls.

---

## 13. Implementation Checklist

- [ ] Hardcode the known cloud metadata table (§3).
- [ ] Implement support for user-supplied instance metadata (`WithInstanceDiscoveryMetadata(string)`) (§4.1).
- [ ] Implement support for user-supplied discovery URI (`WithInstanceDiscoveryMetadata(Uri)`) (§4.2).
- [ ] Implement discovery endpoint host selection (§5).
- [ ] Implement the three resolution flows (§6.1, §6.2, §6.3).
- [ ] Handle `invalid_instance` separately from other errors (§7 vs §8).
- [ ] Cache fallback entries on non-`invalid_instance` failures (§8 — critical).
- [ ] Use a process-wide static cache for network results (§9.1).
- [ ] Guard known metadata usage by checking all cached environments are known (§9.2).
- [ ] Support `WithInstanceDiscovery(false)` to disable network discovery.
- [ ] Ensure region discovery is independent of instance discovery toggle (§10).
- [ ] Self-entry: `preferred_network = preferred_cache = aliases = [authority_host]` (§2).
- [ ] Implement all tests T1–T12 in §12.
