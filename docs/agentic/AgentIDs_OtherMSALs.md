# Agent Identity Support: Non-.NET MSAL SDKs

This document covers agent identity (FMI/FIC) support across the non-.NET MSAL libraries — Java, Python, Go, and JS/Node. For each SDK, we identify what already exists, what's missing, and propose APIs consistent with the MSAL .NET design while respecting each library's idioms.

> **Reference**: The target design is defined in `AgentIDs_APIDesign_Full.md` (MSAL .NET) and `AgentIDs_DevEx_2.md` (developer experience).

---

## Table of Contents

1. [Overview of Agent Identity Flow](#1-overview-of-agent-identity-flow)
2. [Cross-SDK Summary](#2-cross-sdk-summary)
3. [MSAL Java](#3-msal-java)
4. [MSAL Python](#4-msal-python)
5. [MSAL Go](#5-msal-go)
6. [MSAL JS (Node)](#6-msal-js-node)

---

## 1. Overview of Agent Identity Flow

### The Protocol

Agent identities are Entra ID constructs that allow a "blueprint" application to spawn child agent identities that can act autonomously or on behalf of users. The protocol requires a three-leg token exchange using non-standard OAuth2 extensions:

**Leg 1 — FMI Application Token (Blueprint → Agent FIC)**
- Blueprint app authenticates with its own credentials (cert/secret/federated)
- Sends `fmi_path=<agentAppId>` in the POST body
- Scope: `api://AzureAdTokenExchange/.default`
- Grant type: `client_credentials`
- Result: T1 (an FMI-scoped FIC token)

**Leg 2 — Instance Token (Agent Instance FIC)**
- A new confidential client is constructed with `client_id=agentAppId` and `client_assertion=T1`
- Standard `client_credentials` grant
- Scope: `api://AzureAdTokenExchange/.default`
- Result: T2 (an agent instance token)

**Leg 3 — User FIC Token (Agent-Acting-As-User)**
- Same agent confidential client (`client_id=agentAppId`, `client_assertion=T1`)
- Non-standard grant type: `user_fic`
- Sends `user_federated_identity_credential=T2` and `user_id=<oid>` or `username=<upn>`
- Scope: caller-provided target API scopes (e.g., `https://graph.microsoft.com/.default`)
- Result: Final user-scoped agent identity token

For **app-only** agent identity (no user), only Legs 1–2 are performed.

### Required SDK Components

| Component | Used In | Purpose |
|-----------|---------|---------|
| **Client credentials flow** | Legs 1, 2 | Standard `client_credentials` grant |
| **`fmi_path` body parameter** | Leg 1 | Tells Entra to scope the token to a specific agent identity |
| **Callable/dynamic client assertions** | Legs 2, 3 | Previous leg's token becomes the next leg's credential |
| **`user_fic` grant type** | Leg 3 | Non-standard grant for user-scoped agent identity tokens |
| **`user_federated_identity_credential` body param** | Leg 3 | Carries the instance token (T2) |
| **`user_id` / `username` body params** | Leg 3 | Identifies the target user |
| **Cache key including `fmi_path` / agent app ID** | Legs 1, 2, 3 | Prevents tokens for different agents from colliding |
| **Assertion context (fmi_path in assertion callback)** | Leg 1 (optional) | Some assertion providers need to include fmi_path in the signed assertion |

### Target API Design (from MSAL .NET)

MSAL .NET defines two API levels:

**High-level composite — `AcquireTokenForAgent`**:
```csharp
var result = await cca
    .AcquireTokenForAgent(scopes, new AgentIdentity(agentAppId, userObjectId))
    .ExecuteAsync();
```

**Low-level primitive — `AcquireTokenByUserFederatedIdentityCredential`**:
```csharp
var result = await ((IByUserFederatedIdentityCredential)cca)
    .AcquireTokenByUserFederatedIdentityCredential(scopes, username, assertion)
    .ExecuteAsync();
```

**Key model — `AgentIdentity`**:
- `new AgentIdentity(agentAppId, userObjectId)` — by OID (most common)
- `AgentIdentity.WithUsername(agentAppId, upn)` — by UPN
- `AgentIdentity.AppOnly(agentAppId)` — app-only (Legs 1-2 only)

Each non-.NET MSAL should offer equivalents of both API levels, adapted to the language/SDK idioms.

---

## 2. Cross-SDK Summary

### Readiness at a Glance

| Component | MSAL .NET (ref) | Java | Python | Go | JS/Node |
|-----------|:---:|:---:|:---:|:---:|:---:|
| `fmi_path` body parameter | ✅ | ❌ | ✅ | ✅ | ❌ |
| Cache isolation (`ext_cache_key`) | ✅ | ❌ | ✅ | ✅ | ❌ |
| `user_fic` grant type | ✅ | ❌ | ❌ | ❌ | ❌ |
| Assertion callback with context | ✅ | ❌ | ❌ | ⚠️ partial | ⚠️ partial |
| ROPC on CCA | ✅ | ❌ | ✅ | ✅ | ✅ |
| `AgentIdentity` model | ✅ | ❌ | ❌ | ❌ | ❌ |
| Composite `AcquireTokenForAgent` | ✅ | ❌ | ❌ | ❌ | ❌ |
| Primitive `AcquireTokenByUserFIC` | ✅ | ❌ | ❌ | ❌ | ❌ |

### Work Needed Per SDK

**MSAL Go** — **Medium effort.** Leg 1 is fully functional (`WithFMIPath()` + `ext_cache_key` cache isolation). Needs: `user_fic` grant type, `AcquireTokenByUserFederatedIdentityCredential` method, `AcquireTokenForAgent` composite, `AgentIdentity` model, and `FMIPath` added to `AssertionRequestOptions`.

**MSAL Python** — **Medium effort.** Leg 1 is fully functional (`fmi_path` named parameter + `ext_cache_key` cache isolation). Needs: `user_fic` grant type, `acquire_token_by_user_federated_identity_credential` method, `acquire_token_for_agent` composite, `AgentIdentity` model, and assertion context support.

**MSAL Java** — **Large effort.** No agent identity infrastructure exists. Needs everything: `fmi_path` parameter, `ext_cache_key` cache isolation, `user_fic` grant type, assertion context (`AssertionProvider`), `AgentIdentity` model, both new API methods, and new `*Parameters`/`*Request`/`*Supplier` classes for each flow.

**MSAL JS/Node** — **Large effort.** No agent identity infrastructure exists, and `extraParameters` is explicitly `Omit`-ed from `ClientCredentialRequest` and `OnBehalfOfRequest`, blocking even workarounds. Needs everything Java needs, plus `ext_cache_key` support spanning both `msal-common` and `msal-node` packages.

### Shared Gaps (All SDKs)

Every non-.NET MSAL is missing the same core items:
- **`user_fic` grant type** — the non-standard grant for Leg 3
- **`AgentIdentity` model class** — with by-OID, by-UPN, and app-only constructors
- **`AcquireTokenForAgent` composite API** — single-call three-leg orchestration
- **`AcquireTokenByUserFederatedIdentityCredential` primitive API** — Leg 3 only

---

## 3. MSAL Java

### 3.1 Current State

| Capability | Status | Details |
|-----------|--------|---------|
| Client credentials flow | ✅ | `cca.acquireToken(ClientCredentialParameters)` → `CompletableFuture<IAuthenticationResult>` |
| OBO flow | ✅ | `cca.acquireToken(OnBehalfOfParameters)` with `UserAssertion` |
| Callable client assertions | ✅ | `ClientCredentialFactory.createFromCallback(Callable<String>)` |
| Per-request credential override | ✅ | `ClientCredentialParameters.builder(scopes).clientCredential(override)` |
| `extraQueryParameters` (POST body) | ⚠️ Deprecated | Despite the name, merges into POST body. Deprecated. |
| `fmi_path` body parameter | ❌ | No supported way to add `fmi_path` to the request body |
| `ext_cache_key` / cache isolation | ❌ | AT key: `homeAccountId-environment-credentialType-clientId-realm-target`. No fmi_path component. |
| Assertion context | ❌ | `Callable<String>` takes no arguments — no access to fmi_path, token endpoint, or scopes |
| `user_fic` grant type | ❌ | `GrantConstants` only has standard OAuth2 grants |
| ROPC on CCA | ❌ | `UserNamePasswordParameters` only accepted by `PublicClientApplication` |

**Key internal details:**
- `ClientCredentialRequest` → `AcquireTokenByClientCredentialSupplier` → `TokenRequestExecutor.executeTokenRequest()`. The `OAuthAuthorizationGrant` holds a `Map<String, String>` that becomes the POST body.
- `extraQueryParameters` merges into the POST body (last-writer-wins) and *could* inject `fmi_path` on the wire, but is deprecated and doesn't affect cache keying.
- `Callable<String>` assertion callbacks have no context — the assertion provider cannot know the fmi_path, token endpoint, or scopes for the current request.

### 3.2 Gaps

| Gap | Impact |
|-----|--------|
| No `fmi_path` parameter | Cannot perform Leg 1 |
| No `ext_cache_key` / cache isolation | Tokens for different agents collide in cache |
| No assertion context | Assertion providers can't include fmi_path in signed assertions (OIDC-FIC) |
| No `user_fic` grant type | Cannot perform Leg 3 |
| No `user_federated_identity_credential` / `user_id` params | Cannot build Leg 3 body |
| No ROPC on CCA | Cannot use ROPC-based workaround (dedicated flow is preferred regardless) |

### 3.3 Proposed API

#### High-Level: `acquireToken(AgentIdentityParameters)`

```java
AgentIdentityParameters params = AgentIdentityParameters
    .builder(scopes, agentIdentity)
    .tenant(tenantId)
    .build();

CompletableFuture<IAuthenticationResult> result = cca.acquireToken(params);
```

**`AgentIdentity` model:**

```java
public class AgentIdentity {
    public AgentIdentity(String agentApplicationId, UUID userObjectId);
    public static AgentIdentity withUsername(String agentApplicationId, String username);
    public static AgentIdentity appOnly(String agentApplicationId);

    public String agentApplicationId();
    public UUID userObjectId();       // null if UPN or app-only
    public String username();         // null if OID or app-only
}
```

#### Low-Level: `acquireToken(UserFederatedIdentityCredentialParameters)`

```java
UserFederatedIdentityCredentialParameters params =
    UserFederatedIdentityCredentialParameters
        .builder(scopes, "user@contoso.com", federatedCredentialToken)
        .build();

// OR by OID
UserFederatedIdentityCredentialParameters params =
    UserFederatedIdentityCredentialParameters
        .builder(scopes, userObjectId, federatedCredentialToken)
        .build();

CompletableFuture<IAuthenticationResult> result = cca.acquireToken(params);
```

#### Supporting Changes

**`fmi_path` on client credentials** (for manual Leg 1):
```java
ClientCredentialParameters.builder(scopes).fmiPath(agentAppId).build();
```

**Assertion context** (new functional interface, backward-compatible):
```java
@FunctionalInterface
public interface AssertionProvider {
    String createAssertion(AssertionRequestContext context);
}

public class AssertionRequestContext {
    public String tokenEndpoint();
    public String clientAssertionFmiPath();
    public Set<String> scopes();
}

// New factory method (existing Callable<String> overload preserved)
ClientCredentialFactory.createFromCallback(AssertionProvider provider);
```

### 3.4 Implementation Notes

**New internal classes:**
- `AgentIdentityRequest` + `AcquireTokenByAgentIdentitySupplier` — three-leg orchestration
- `UserFederatedIdentityCredentialRequest` + `AcquireTokenByUserFederatedIdentityCredentialSupplier` — `user_fic` grant

**Three-leg orchestration (`AcquireTokenByAgentIdentitySupplier.execute()`):**
1. Leg 1 via internal `ClientCredentialRequest` with `fmi_path` on the grant
2. Leg 2 by constructing an internal CCA with `agentAppId` and `WithClientAssertion(() -> leg1Token)`
3. Leg 3 via `UserFederatedIdentityCredentialRequest` on the agent CCA

The orchestration chains `CompletableFuture`s via `.thenCompose()`. Internal CCAs should reuse the blueprint's `ServiceBundle` (HTTP client, telemetry) where possible.

**Cache keying:**
- `AccessTokenCacheEntity.getKey()` needs an `ext_cache_key` component (SHA-256 hash, matching Go/Python algorithm)
- Credential type switches from `"AccessToken"` → `"atext"` when `ext_cache_key` is present
- Cache lookup must filter on `ext_cache_key`; entries with it must not match queries without it
- `user_fic` tokens are user-scoped and should include the user identifier in the cache key

**New constants:** `GrantConstants.USER_FIC`, `PublicApi.ACQUIRE_TOKEN_FOR_AGENT`, `PublicApi.ACQUIRE_TOKEN_BY_USER_FEDERATED_IDENTITY_CREDENTIAL`

### 3.5 Effort Summary

| Change Area | Scope |
|-------------|-------|
| `AgentIdentity` model class | Small |
| `AgentIdentityParameters` + builder | Medium |
| `UserFederatedIdentityCredentialParameters` + builder | Medium |
| `IConfidentialClientApplication` — add 2 methods | Small (breaking interface change) |
| `AgentIdentityRequest` + `Supplier` (3-leg orchestration) | Large |
| `UserFederatedIdentityCredentialRequest` + `Supplier` | Medium |
| `fmi_path` on `ClientCredentialParameters` + wire to POST body | Small |
| `AssertionRequestContext` + `AssertionProvider` | Medium |
| Cache keying (`ext_cache_key`, `AccessTokenCacheEntity`, `TokenCache`) | Medium |
| Grant constants + `PublicApi` enum | Small |
| **Overall** | **Large** |

---

## 4. MSAL Python

### 4.1 Current State

| Capability | Status | Details |
|-----------|--------|---------|
| Client credentials flow | ✅ | `cca.acquire_token_for_client(scopes)` → `dict` |
| `fmi_path` body parameter | ✅ | `acquire_token_for_client(scopes, fmi_path="agentAppId")` — first-class named parameter |
| `ext_cache_key` / cache isolation | ✅ | `_compute_ext_cache_key(data)` hashes non-standard body params via SHA-256. AT uses `"atext"` credential type when present. Cache search isolates entries with/without `ext_cache_key`. |
| OBO flow | ✅ | `cca.acquire_token_on_behalf_of(user_assertion, scopes)` |
| ROPC on CCA | ✅ | `acquire_token_by_username_password()` on base `ClientApplication`; deprecation warning skipped for CCA |
| Callable client assertions | ✅ | `client_assertion` can be a `callable` (zero-arg), invoked lazily |
| `data={}` POST body override | ✅ | All `acquire_token_*` methods accept `data={}` via `**kwargs`; merges last into POST body (user data prevails) |
| Assertion context | ❌ | Callable receives zero arguments — no access to fmi_path, token endpoint, or scopes |
| `user_fic` grant type | ❌ | No dedicated method or grant type |
| `AgentIdentity` model / composite API | ❌ | No three-leg orchestration |

**Key internal details:**
- `acquire_token_for_client(scopes, fmi_path="...")` validates the type, injects into `kwargs["data"]["fmi_path"]`, then calls `_acquire_token_silent_with_error()` which computes `ext_cache_key` from the data dict. On cache miss, the data flows through to `client.obtain_token_for_client()` and the POST body. On response, `TokenCache.add()` stores `ext_cache_key` on the AT entry.
- The `data={}` override mechanism means developers *can* technically perform Legs 2-3 today by overriding `grant_type` and injecting body params, but this is unsupported, fragile, and bypasses proper caching.
- Callable assertions are wrapped in `AutoRefresher` for caching, but the regeneration callable takes no context arguments.

### 4.2 Gaps

| Gap | Impact |
|-----|--------|
| No assertion context | Assertion providers can't include fmi_path in signed assertions (OIDC-FIC) |
| No `user_fic` grant type | Cannot perform Leg 3 with a proper API |
| No `user_fic` cache awareness | Tokens from a `data={}` workaround wouldn't be cached correctly |
| No `AgentIdentity` model / composite API | No single-call three-leg orchestration |

### 4.3 Proposed API

#### High-Level: `acquire_token_for_agent`

```python
result = cca.acquire_token_for_agent(
    scopes,
    agent_identity,
    claims_challenge=None,
)
```

**`AgentIdentity` model:**

```python
class AgentIdentity:
    def __init__(self, agent_application_id, user_object_id=None):
        """By OID (if user_object_id provided) or app-only (if None)."""

    @classmethod
    def with_username(cls, agent_application_id, username):
        """By UPN."""

    @classmethod
    def app_only(cls, agent_application_id):
        """App-only (Legs 1-2 only)."""

    @property
    def is_app_only(self):
        return self.user_object_id is None and self.username is None
```

#### Low-Level: `acquire_token_by_user_federated_identity_credential`

```python
result = cca.acquire_token_by_user_federated_identity_credential(
    scopes,
    assertion,                      # the federated credential token (T2)
    username=None,                  # one of username or user_object_id required
    user_object_id=None,
    claims_challenge=None,
)
```

#### Supporting Changes

**Assertion context** (backward-compatible — detection via `inspect.signature` or try/except):
```python
class AssertionContext:
    def __init__(self, token_endpoint, fmi_path=None, scopes=None):
        self.token_endpoint = token_endpoint
        self.fmi_path = fmi_path
        self.scopes = scopes

# Zero-arg callables: called as client_assertion()
# One-arg callables: called as client_assertion(context)
```

### 4.4 Implementation Notes

**`acquire_token_for_agent` orchestration:**
1. **Leg 1**: `self.acquire_token_for_client(["api://AzureAdTokenExchange/.default"], fmi_path=agent_identity.agent_application_id)` — already works with cache isolation.
2. **Leg 2**: Construct internal CCA with `client_id=agent_app_id`, `client_assertion=lambda: t1`. Call `agent_cca.acquire_token_for_client(["api://AzureAdTokenExchange/.default"])`.
3. **Leg 3** (if not app-only): `agent_cca.acquire_token_by_user_federated_identity_credential(scopes, assertion=t2, ...)`.

The internal CCA reuses the blueprint's `http_client` and authority but gets its own cache.

**`acquire_token_by_user_federated_identity_credential`:**
Add `obtain_token_by_user_fic()` on `oauth2cli.Client` delegating to `_obtain_token("user_fic", data={...})`. The data dict includes `user_federated_identity_credential`, and `user_id` or `username`.

**Cache behavior for `user_fic` tokens:**
The response is user-scoped. `TokenCache.add()` should handle this automatically if the server returns `client_info`. If not, application-level code may need to synthesize the cache event.

### 4.5 Effort Summary

| Change Area | Scope |
|-------------|-------|
| `AgentIdentity` model class | Small |
| `acquire_token_for_agent()` on CCA | Medium |
| `acquire_token_by_user_federated_identity_credential()` on CCA | Small–Medium |
| `obtain_token_by_user_fic()` on `oauth2cli.Client` | Small |
| `AssertionContext` + backward-compat detection | Small |
| Cache handling for `user_fic` tokens | Small |
| `fmi_path` on `acquire_token_for_client` | ✅ Already done |
| `ext_cache_key` cache isolation | ✅ Already done |
| **Overall** | **Medium** |

---

## 5. MSAL Go

### 5.1 Current State

| Capability | Status | Details |
|-----------|--------|---------|
| Client credentials flow | ✅ | `cca.AcquireTokenByCredential(ctx, scopes, opts...)` → `(AuthResult, error)` |
| `fmi_path` body parameter | ✅ | `WithFMIPath(path)` option — sets `fmi_path` in POST body AND `cacheKeyComponents` for cache isolation |
| `ext_cache_key` / cache isolation | ✅ | `CacheExtKeyGenerator()` hashes `CacheKeyComponents` (SHA-256 + base64url). AT key uses `"atext"` credential type when `ExtCacheKey` present. Cache read correctly isolates. |
| OBO flow | ✅ | `cca.AcquireTokenOnBehalfOf(ctx, userAssertion, scopes, opts...)` |
| ROPC on CCA | ✅ | `cca.AcquireTokenByUsernamePassword(ctx, scopes, username, password, opts...)` |
| Assertion callbacks | ✅ partial | `NewCredFromAssertionCallback(func(ctx, AssertionRequestOptions) (string, error))` — receives `ClientID` + `TokenEndpoint`, but NOT `fmi_path` |
| Token provider callback | ✅ | `NewCredFromTokenProvider(...)` — bypasses MSAL HTTP layer entirely |
| Extra body params | ✅ | `ExtraBodyParameters map[string]string` on `AuthParams`; called via `addExtraBodyParameters()` in credential flows |
| `fmi_path` in assertion context | ❌ | `AssertionRequestOptions` lacks `FMIPath` field |
| `user_fic` grant type | ❌ | Not in `grant` package |
| Extra body params on ROPC | ❌ | `FromUsernamePassword()` does not call `addExtraBodyParameters()` |
| `AgentIdentity` model / composite API | ❌ | No three-leg orchestration |

**Key internal details:**
- `WithFMIPath("agentAppId")` sets both `extraBodyParameters["fmi_path"]` and `cacheKeyComponents["fmi_path"]`. The `addExtraBodyParameters()` function iterates the map and sets values on the URL form. Called from `FromAssertion()` and `FromClientSecret()`, but NOT from `FromUsernamePassword()`.
- `CacheExtKeyGenerator()` sorts keys, concatenates `key+value` pairs, SHA-256 hashes, base64url encodes. This is the reference algorithm that Python replicates.
- `AssertionRequestOptions` has `ClientID` + `TokenEndpoint` — more context than Java/Python's zero-arg callbacks, but still no `fmi_path`.

### 5.2 Gaps

| Gap | Impact |
|-----|--------|
| No `fmi_path` in assertion context | Assertion providers can't include fmi_path in signed assertions (OIDC-FIC) |
| No `user_fic` grant type | Cannot perform Leg 3 |
| No `user_federated_identity_credential` / `user_id` params | Cannot build Leg 3 body |
| No `AgentIdentity` model / composite API | No single-call three-leg orchestration |

### 5.3 Proposed API

#### High-Level: `AcquireTokenForAgent`

```go
func (cca Client) AcquireTokenForAgent(
    ctx context.Context,
    scopes []string,
    agentIdentity AgentIdentity,
    opts ...AcquireTokenForAgentOption,
) (AuthResult, error)
```

**`AgentIdentity` model:**

```go
type AgentIdentity struct {
    agentAppID   string
    userObjectID string
    username     string
}

func NewAgentIdentity(agentAppID, userObjectID string) AgentIdentity
func NewAgentIdentityByUsername(agentAppID, username string) AgentIdentity
func NewAppOnlyAgentIdentity(agentAppID string) AgentIdentity
func (a AgentIdentity) IsAppOnly() bool
```

#### Low-Level: `AcquireTokenByUserFederatedIdentityCredential`

```go
func (cca Client) AcquireTokenByUserFederatedIdentityCredential(
    ctx context.Context,
    scopes []string,
    assertion string,
    opts ...AcquireByUserFICOption,
) (AuthResult, error)
```

**User identification via options (exactly one required):**
```go
func WithUserObjectID(oid string) AcquireByUserFICOption
func WithUserFICUsername(username string) AcquireByUserFICOption
```

#### Supporting Changes

**Assertion context** — add `FMIPath` to existing struct (backward-compatible):
```go
type AssertionRequestOptions struct {
    ClientID      string
    TokenEndpoint string
    FMIPath       string  // NEW
}
```

### 5.4 Implementation Notes

**New code path through Go's layers:**
1. `confidential.Client.AcquireTokenForAgent()` / `AcquireTokenByUserFederatedIdentityCredential()`
2. `oauth.Client.UserFederatedIdentityCredential(ctx, authParams)`
3. `accesstokens.Client.FromUserFederatedIdentityCredential(ctx, ap, cred)` — builds POST body with `grant_type=user_fic`, `user_federated_identity_credential`, `user_id`/`username`
4. New grant constant: `grant.UserFIC = "user_fic"`

**Three-leg orchestration:**
1. **Leg 1**: `cca.AcquireTokenByCredential(ctx, ficScopes, WithFMIPath(agentAppID))` — already works.
2. **Leg 2**: Ephemeral CCA with `agentAppID` as client ID and `NewCredFromAssertionCallback(func(...) { return leg1Token, nil })`.
3. **Leg 3** (if not app-only): `agentCCA.AcquireTokenByUserFederatedIdentityCredential(ctx, scopes, leg2Token, WithUserObjectID(oid))`.

Internal CCAs reuse the blueprint's HTTP client via `WithHTTPClient()`.

**Cache behavior for `user_fic` tokens:**
User-scoped — `HomeAccountID` derived from response's `client_info`. Existing `base.AuthResultFromToken()` should handle this automatically if the server returns `client_info`.

**Assertion context enhancement:** `FromAssertion()` populates `AssertionRequestOptions.FMIPath` from `authParameters.ExtraBodyParameters["fmi_path"]` before calling the callback.

### 5.5 Effort Summary

| Change Area | Scope |
|-------------|-------|
| `AgentIdentity` type + constructors | Small |
| `AcquireTokenForAgent()` on CCA | Medium |
| `AcquireTokenByUserFederatedIdentityCredential()` on CCA | Small–Medium |
| `FromUserFederatedIdentityCredential()` on `accesstokens.Client` | Small |
| `UserFIC` grant constant | Trivial |
| `FMIPath` on `AssertionRequestOptions` | Small |
| `WithUserObjectID()` / `WithUserFICUsername()` options | Small |
| `WithFMIPath()` on `AcquireTokenByCredential` | ✅ Already done |
| `ext_cache_key` cache isolation | ✅ Already done |
| **Overall** | **Medium** |

---

## 6. MSAL JS (Node)

> **Scope note:** Agent identity is a confidential client concern. `ConfidentialClientApplication` exists only in `msal-node`, not `msal-browser`. Shared types (cache entities, grant constants) live in `msal-common`.

### 6.1 Current State

| Capability | Status | Details |
|-----------|--------|---------|
| Client credentials flow | ✅ | `cca.acquireTokenByClientCredential(request)` → `Promise<AuthenticationResult \| null>` |
| OBO flow | ✅ | `cca.acquireTokenOnBehalfOf(request)` |
| ROPC on CCA | ✅ | `cca.acquireTokenByUsernamePassword(request)` — on base `ClientApplication` |
| Client assertion callback | ✅ | `ClientAssertionCallback = (config: ClientAssertionConfig) => Promise<string>` — receives `{clientId, tokenEndpoint?}` |
| Per-request assertion | ✅ | `ClientCredentialRequest.clientAssertion` — string or callback |
| App token provider | ✅ | `IAppTokenProvider` — bypasses MSAL HTTP layer |
| `extraParameters` on `BaseAuthRequest` | ✅ | Adds arbitrary key-value pairs to POST body |
| `fmi_path` body parameter | ❌ | Not available on any request type |
| `extraParameters` on ClientCredential | ❌ **Blocked** | `CommonClientCredentialRequest = Omit<BaseAuthRequest, "extraQueryParameters" \| "extraParameters">` |
| `extraParameters` on OBO | ❌ **Blocked** | `CommonOnBehalfOfRequest` — same `Omit` pattern |
| `ext_cache_key` / cache isolation | ❌ | AT key: `homeAccountId-environment-credentialType-clientId-realm-target-scheme`. No extended key. |
| `user_fic` grant type | ❌ | Not in `GrantType` enum |
| Assertion context with `fmi_path` | ❌ | `ClientAssertionConfig` has `{clientId, tokenEndpoint?}` only |

**Key internal details:**
- `ClientCredentialClient.createTokenRequestBody()` builds a `Map<string, string>` that becomes the POST body. There is no call to `addExtraParameters()` and no extension point to inject additional body params.
- TypeScript's `Omit<BaseAuthRequest, "extraQueryParameters" | "extraParameters">` structurally removes these fields from `CommonClientCredentialRequest` and `CommonOnBehalfOfRequest` — the compiler rejects them, and the runtime doesn't read them.
- `generateCredentialKey()` in `CacheHelpers.ts` builds `homeAccountId-environment-credentialType-clientId-realm-target-scheme`. No `ext_cache_key` or `"atext"` credential type concept.
- `ClientAssertionConfig` provides `{clientId, tokenEndpoint?}` — `tokenEndpoint` is explicitly `undefined` for per-request assertions in the `ClientCredentialClient` flow.

### 6.2 Gaps

| Gap | Impact |
|-----|--------|
| No `fmi_path` parameter | Cannot perform Leg 1; `extraParameters` is blocked on `ClientCredentialRequest` |
| No `ext_cache_key` / cache isolation | Tokens for different agents collide in cache |
| `extraParameters` blocked on ClientCredential and OBO | Cannot even workaround via existing extensibility |
| No `user_fic` grant type | Cannot perform Leg 3 |
| No assertion context with `fmi_path` | Assertion providers can't include fmi_path in signed assertions |
| No `AgentIdentity` model / composite API | No three-leg orchestration |

### 6.3 Proposed API

#### High-Level: `acquireTokenForAgent`

```typescript
async acquireTokenForAgent(
    request: AgentIdentityRequest
): Promise<AuthenticationResult | null>;

export type AgentIdentityRequest = {
    agentIdentity: AgentIdentity;
    scopes: Array<string>;
    claims?: string;
    azureRegion?: AzureRegion;
    skipCache?: boolean;
};
```

**`AgentIdentity` model:**

```typescript
export class AgentIdentity {
    readonly agentApplicationId: string;
    readonly userObjectId?: string;
    readonly username?: string;

    constructor(agentApplicationId: string, userObjectId: string);
    static withUsername(agentApplicationId: string, username: string): AgentIdentity;
    static appOnly(agentApplicationId: string): AgentIdentity;
    get isAppOnly(): boolean;
}
```

#### Low-Level: `acquireTokenByUserFederatedIdentityCredential`

```typescript
async acquireTokenByUserFederatedIdentityCredential(
    request: UserFederatedIdentityCredentialRequest
): Promise<AuthenticationResult | null>;

export type UserFederatedIdentityCredentialRequest = {
    scopes: Array<string>;
    assertion: string;
    claims?: string;
} & (
    | { userObjectId: string; username?: never }
    | { username: string; userObjectId?: never }
);
```

#### Supporting Changes

**`fmiPath` on client credentials** — add as typed field (recommended over un-`Omit`-ing `extraParameters`):
```typescript
export type CommonClientCredentialRequest = Omit<
    BaseAuthRequest,
    "extraQueryParameters" | "extraParameters"
> & {
    skipCache?: boolean;
    azureRegion?: AzureRegion;
    clientAssertion?: ClientAssertion;
    fmiPath?: string;           // NEW
};
```

**Assertion context:**
```typescript
export type ClientAssertionConfig = {
    clientId: string;
    tokenEndpoint?: string;
    fmiPath?: string;          // NEW
};
```

**Cache key (`ext_cache_key`)** — add to `AccessTokenEntity` and update `generateCredentialKey()`:
```typescript
// AccessTokenEntity gets new optional field
extCacheKey?: string;

// generateCredentialKey switches to "atext" when extCacheKey present
// and appends the hash — matching Go/Python algorithm (SHA-256 + base64url)
```

### 6.4 Implementation Notes

**Changes span two packages:**
- `msal-common`: `AccessTokenEntity.extCacheKey`, `generateCredentialKey()`, `GrantType.USER_FIC`, `ClientAssertionConfig.fmiPath`
- `msal-node`: New client class(es), request types, `ConfidentialClientApplication` methods, `fmiPath` on `CommonClientCredentialRequest`

**New internal client:**
`UserFederatedIdentityCredentialClient` (parallel to `ClientCredentialClient`) handles the `user_fic` grant, or an `AgentIdentityClient` that orchestrates all three legs.

**Three-leg orchestration:**
1. **Leg 1**: `this.acquireTokenByClientCredential({ scopes: ficScopes, fmiPath: agentAppId })` — requires `fmiPath` addition first.
2. **Leg 2**: Ephemeral CCA with `clientId: agentAppId` and `clientAssertion: leg1Token`.
3. **Leg 3** (if not app-only): `agentCCA.acquireTokenByUserFederatedIdentityCredential({ scopes, assertion: leg2Token, userObjectId })`.

Internal CCA construction is heavier than in Python/Go (more constructor setup). May need an internal factory that shares resources.

**Cache isolation (`ext_cache_key`):**
Implement SHA-256 hashing in `msal-common` utilities (Node.js `crypto` module). `CacheManager` filtering must enforce isolation: entries with `ext_cache_key` don't match lookups without it.

### 6.5 Effort Summary

| Change Area | Scope |
|-------------|-------|
| `AgentIdentity` type/class | Small |
| `AgentIdentityRequest` + `UserFederatedIdentityCredentialRequest` types | Small |
| `acquireTokenForAgent()` on CCA | Medium |
| `acquireTokenByUserFederatedIdentityCredential()` on CCA | Medium |
| `UserFederatedIdentityCredentialClient` internal class | Medium |
| `fmiPath` on `CommonClientCredentialRequest` + wire through | Small |
| `ClientCredentialClient.createTokenRequestBody()` — add `fmiPath` | Small |
| `GrantType.USER_FIC` constant | Trivial |
| `ClientAssertionConfig.fmiPath` | Small |
| `ext_cache_key` on `AccessTokenEntity` + `generateCredentialKey()` | Medium |
| Cache filter/lookup with `ext_cache_key` isolation | Medium |
| SHA-256 `ext_cache_key` generator utility | Small |
| `IConfidentialClientApplication` — add 2 methods | Small |
| **Overall** | **Large** |
