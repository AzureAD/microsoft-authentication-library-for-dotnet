# Agent Identity — Required SDK Components Reference

This document explains each component needed to implement agent identity (FMI/FIC) support in an MSAL SDK. For every component, we describe **what it does**, **how MSAL .NET implements it**, and **where to find the code**. Use this as a translation guide when implementing in Java, Python, Go, or JS/Node.

> **Source branch**: `avdunn/agent-identity-apis` on `microsoft-authentication-library-for-dotnet` ([PR #5883](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5883))

---

## Table of Contents

1. [Client Credentials Flow (Legs 1, 2)](#1-client-credentials-flow-legs-1-2)
2. [`fmi_path` Body Parameter (Leg 1)](#2-fmi_path-body-parameter-leg-1)
3. [Cache Key Isolation (`ext_cache_key`)](#3-cache-key-isolation-ext_cache_key)
4. [Callable/Dynamic Client Assertions (Legs 2, 3)](#4-callabledynamic-client-assertions-legs-2-3)
5. [Assertion Context — `fmi_path` in Assertion Callback (Leg 1)](#5-assertion-context--fmi_path-in-assertion-callback-leg-1)
6. [`user_fic` Grant Type (Leg 3)](#6-user_fic-grant-type-leg-3)
7. [`user_federated_identity_credential` Body Parameter (Leg 3)](#7-user_federated_identity_credential-body-parameter-leg-3)
8. [`user_id` / `username` Body Parameters (Leg 3)](#8-user_id--username-body-parameters-leg-3)
9. [`AgentIdentity` Model](#9-agentidentity-model)
10. [Composite `AcquireTokenForAgent` API](#10-composite-acquiretokenforagent-api)
11. [Primitive `AcquireTokenByUserFederatedIdentityCredential` API](#11-primitive-acquiretokenbyuserfederatedidentitycredential-api)

---

## 1. Client Credentials Flow (Legs 1, 2)

### What It Does

The standard `client_credentials` OAuth2 grant. The blueprint app uses this grant in **Leg 1** (with `fmi_path` added to the body) to get an FMI-scoped token. The internal agent CCA uses it again in **Leg 2** (with Leg 1's token as the `client_assertion`) to get an instance token. Both legs target `scope=api://AzureADTokenExchange/.default`.

### MSAL .NET Reference

**Existing infrastructure** — no new code needed for this component. The `ClientCredentialRequest` handler already exists.

| Item | File | Key Code |
|------|------|----------|
| Public API | `IConfidentialClientApplication.cs` | `AcquireTokenForClient(IEnumerable<string> scopes)` |
| Builder | `AcquireTokenForClientParameterBuilder.cs` | Returns builder with `.WithFmiPath()`, `.WithForceRefresh()`, etc. |
| Parameters | `AcquireTokenForClientParameters.cs` | `ForceRefresh`, `SendX5C` |
| Request handler | `Internal/Requests/ClientCredentialRequest.cs` | `GetBodyParameters()` sets `grant_type=client_credentials`, `scope`, `client_info` |
| Body injection | `OAuth2/TokenClient.cs` | Calls `AddBodyParamsAndHeaders()`, which adds credential params + `fmi_path` if set |
| Executor | `Executors/ConfidentialClientExecutor.cs` | Routes to `ClientCredentialRequest` |

**`GetBodyParameters()` in `ClientCredentialRequest`:**
```csharp
private Dictionary<string, string> GetBodyParameters()
{
    var dict = new Dictionary<string, string>
    {
        [OAuth2Parameter.GrantType] = OAuth2GrantType.ClientCredentials,
        [OAuth2Parameter.Scope] = AuthenticationRequestParameters.Scope.AsSingleString(),
        [OAuth2Parameter.ClientInfo] = "2"
    };
    return dict;
}
```
Note: `fmi_path` is NOT set here — it's added separately by `TokenClient` from `AuthenticationRequestParameters.FmiPathSuffix` (see [§2](#2-fmi_path-body-parameter-leg-1)).

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `Flow2_Token_From_CertTest` | `FmiIntegrationTests.cs` (integration) | Client credentials + `WithFmiPath` acquires a real FMI token from Entra ID |
| `Flow3_FmiCredential_From_AnotherFmiCredential` | `FmiIntegrationTests.cs` (integration) | Client credentials where the credential itself is an FMI token (chained credentials — Leg 1 → Leg 2 pattern) |
| `AgentGetsAppTokenForGraphTest` | `Agentic.cs` (integration) | App-only agent flow: CCA with assertion callback acquires a Graph token via client credentials |
| `AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Composite API exercises Legs 1-2 internally; verifies Leg 1+2 are cached and reused for second user |

### Translation Notes

Every MSAL already has `client_credentials` support. No new implementation needed for this component itself — it's the foundation the other components build on.

---

## 2. `fmi_path` Body Parameter (Leg 1)

### What It Does

Tells Entra ID to scope the `client_credentials` token to a specific agent identity. Sent as `fmi_path=<agentAppId>` in the POST body alongside the standard client credentials grant. Only used in Leg 1.

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| OAuth2 constant | `OAuth2/OAuthConstants.cs` | `public const string FmiPath = "fmi_path";` |
| Builder method | `AcquireTokenForClientParameterBuilder.cs` | `WithFmiPath(string pathSuffix)` — stores value in `CommonParameters.FmiPathSuffix` |
| Plumbing | `AcquireTokenCommonParameters.cs` → `AuthenticationRequestParameters.cs` | `FmiPathSuffix` property flows through |
| Body injection | `OAuth2/TokenClient.cs` | `if (!string.IsNullOrEmpty(_requestParams.FmiPathSuffix)) _oAuth2Client.AddBodyParameter(OAuth2Parameter.FmiPath, _requestParams.FmiPathSuffix);` |
| Cache registration | `AcquireTokenForClientParameterBuilder.WithFmiPath()` | Calls `WithAdditionalCacheKeyComponents` with key `"fmi_path"` |

**`WithFmiPath` implementation:**
```csharp
public AcquireTokenForClientParameterBuilder WithFmiPath(string pathSuffix)
{
    var cacheKey = new SortedList<string, Func<CancellationToken, Task<string>>>
    {
        { OAuth2Parameter.FmiPath, (CancellationToken ct) => Task.FromResult(pathSuffix) }
    };
    this.WithAdditionalCacheKeyComponents(cacheKey);
    CommonParameters.FmiPathSuffix = pathSuffix;
    return this;
}
```

**Data flow**: `WithFmiPath("agentId")` → `CommonParameters.FmiPathSuffix = "agentId"` → `TokenClient.AddBodyParameter("fmi_path", "agentId")` in the HTTP POST body.

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `Flow1_Credential_From_Cert` | `FmiIntegrationTests.cs` (integration) | `WithFmiPath("SomeFmiPath/FmiCredentialPath")` sends `fmi_path` in POST body; result token's `sub` claim contains the FMI path |
| `Flow2_Token_From_CertTest` | `FmiIntegrationTests.cs` (integration) | Same `fmi_path` used with a different scope (web API vs exchange); verifies body injection works for resource tokens |
| `Flow5_FmiToken_From_FmiCred` | `FmiIntegrationTests.cs` (integration) | `fmi_path` used on a CCA whose own credential is itself an FMI credential (chained FMI) |
| `Flow6_Token_withAttributeTest` | `FmiIntegrationTests.cs` (integration) | `WithFmiPath` combined with `WithAttributes`; verifies both land in the POST body |

### Translation Notes

SDKs that already have `fmi_path` support (Python, Go) already cover this. Java and JS need to add:
- A way to inject `fmi_path` into the client credentials POST body
- Corresponding cache key differentiation (see [§3](#3-cache-key-isolation-ext_cache_key))

---

## 3. Cache Key Isolation (`ext_cache_key`)

### What It Does

Prevents tokens acquired with different `fmi_path` values (or other non-standard body params) from colliding in the token cache. Without this, a token acquired for `fmi_path=agentA` could be returned for a request targeting `fmi_path=agentB`.

### MSAL .NET Reference

MSAL .NET uses a **sorted key-value concatenation → SHA-256 → Base64URL** algorithm to compute an extended cache key hash.

| Item | File | Key Code |
|------|------|----------|
| Deferred components storage | `AcquireTokenCommonParameters.cs` | `SortedList<string, Func<CancellationToken, Task<string>>> CacheKeyComponents` |
| Resolve at request time | `ApplicationBase.cs` | `InitializeCacheKeyComponentsAsync()` — invokes each `Func` to get the values |
| Hash computation | `Utils/CoreHelpers.cs` | `ComputeAccessTokenExtCacheKey(SortedList<string, string>)` |
| Access token cache item | `Cache/Items/MsalAccessTokenCacheItem.cs` | `InitCacheKey()` — appends the hash and switches credential type to `"AccessToken_Extended"` |
| App token cache key | `Cache/CacheKeyFactory.cs` | `GetAppTokenCacheItemKey()` — includes the hash in the app cache partition key |

**Hash algorithm (`CoreHelpers.ComputeAccessTokenExtCacheKey`):**
```csharp
internal static string ComputeAccessTokenExtCacheKey(SortedList<string, string> cacheKeyComponents)
{
    if (cacheKeyComponents == null || !cacheKeyComponents.Any())
        return string.Empty;

    StringBuilder stringBuilder = new();
    foreach (var component in cacheKeyComponents)
    {
        stringBuilder.Append(component.Key);
        stringBuilder.Append(component.Value);
    }

    using (SHA256 hash = SHA256.Create())
    {
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        return Base64UrlHelpers.Encode(hashBytes);
    }
}
```

**How the hash enters the cache key (`MsalAccessTokenCacheItem.InitCacheKey`):**
```csharp
if (AdditionalCacheKeyComponents != null)
{
    _credentialDescriptor = StorageJsonValues.CredentialTypeAccessTokenExtended; // "AccessToken_Extended"
    _extraKeyParts = new[] { CoreHelpers.ComputeAccessTokenExtCacheKey(AdditionalCacheKeyComponents) };
}

CacheKey = MsalCacheKeys.GetCredentialKey(
    HomeAccountId, Environment, _credentialDescriptor,
    ClientId, TenantId, ScopeString, _extraKeyParts);
```

**Cache key format**: `{homeAccountId}-{environment}-{credentialType}-{clientId}-{tenantId}-{scopes}[-{hash}]`

When `AdditionalCacheKeyComponents` are present:
- Credential type changes from `"AccessToken"` → `"AccessToken_Extended"`
- The SHA-256 hash is appended as an extra segment

### Cross-SDK Comparison

| SDK | Algorithm | Credential Type Switch | Implemented? |
|-----|-----------|----------------------|-------------|
| .NET | SHA-256 of sorted key+value pairs, Base64URL encoded | `"AccessToken"` → `"AccessToken_Extended"` | ✅ |
| Go | SHA-256 of sorted key+value pairs, Base64URL encoded | `"AccessToken"` → `"atext"` | ✅ |
| Python | SHA-256 of sorted key+value pairs, Base64URL encoded | `"AccessToken"` → `"atext"` | ✅ |
| Java | — | — | ❌ |
| JS | — | — | ❌ |

> Go and Python use `"atext"` as the extended credential type shorthand. .NET uses the full `"AccessToken_Extended"`. The hash algorithm is identical across all three.

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `Flow1_Credential_From_Cert` | `FmiIntegrationTests.cs` (integration) | Asserts the **internal cache key** contains `atext` (extended credential type) and the expected SHA-256 hash of `fmi_path`; verifies the **external cache key** (partition key) also includes the hash |
| `Flow2_Token_From_CertTest` | `FmiIntegrationTests.cs` (integration) | Same cache key structure validation with a different scope — confirms the hash is scope-independent (derived from `fmi_path` only) |
| `Flow3_FmiCredential_From_AnotherFmiCredential` | `FmiIntegrationTests.cs` (integration) | Different `fmi_path` value → different hash → different cache key; validates isolation between FMI paths |
| `Flow6_Token_withAttributeTest` | `FmiIntegrationTests.cs` (integration) | `WithAttributes` combined with `WithFmiPath` — both contribute to cache key components; hash includes both |

The FMI integration tests are the best reference for cache key behavior because `AssertResults()` validates the exact internal and external cache key strings, including the `atext` credential type and SHA-256 hash.

### Translation Notes

Java and JS need to implement:
1. A mechanism for non-standard body params to contribute to the cache key (e.g., a `SortedMap<String,String>` of extra components)
2. The SHA-256 + Base64URL hash of sorted key+value concatenation
3. A credential type switch to differentiate extended tokens from regular ones
4. Serialization/deserialization of the additional cache key components

---

## 4. Callable/Dynamic Client Assertions (Legs 2, 3)

### What It Does

The assertion callback allows MSAL to delegate credential acquisition to an external function. In the agent flow, the previous leg's token becomes the next leg's credential:
- **Leg 2**: The agent CCA's assertion callback calls Leg 1 (`blueprint.AcquireTokenForClient + WithFmiPath`) to obtain the FMI token, then returns it as the `client_assertion`
- **Leg 3**: The agent CCA reuses the same assertion callback (Leg 1 is cached), providing the FMI token as `client_assertion` alongside the `user_fic` grant

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| Builder method | `ConfidentialClientApplicationBuilder.cs` | `WithClientAssertion(Func<AssertionRequestOptions, Task<string>> assertionProvider)` |
| Options model | `AppConfig/AssertionRequestOptions.cs` | Properties: `ClientID`, `TokenEndpoint`, `TenantId`, `Authority`, `Claims`, `ClientCapabilities`, `CancellationToken`, `ClientAssertionFmiPath` |
| Credential wrapper | `Internal/ClientCredential/ClientAssertionStringDelegateCredential.cs` | Wraps the `Func`, builds `AssertionRequestOptions`, invokes callback |
| Invocation site | `ClientAssertionStringDelegateCredential.AddConfidentialClientParametersAsync()` | Builds `AssertionRequestOptions` with context from the current request, then calls the delegate |

**How the agent CCA's assertion callback is set up (in `AgentTokenRequest.BuildAgentCca`):**
```csharp
var builder = ConfidentialClientApplicationBuilder
    .Create(agentAppId)
    .WithAuthority(authority)
    .WithExperimentalFeatures(true)
    .WithClientAssertion(async (AssertionRequestOptions opts) =>
    {
        // Leg 1: Acquire FMI credential from the Blueprint CCA
        string fmiPath = opts.ClientAssertionFmiPath ?? agentAppId;
        var result = await blueprint
            .AcquireTokenForClient(new[] { TokenExchangeScope })
            .WithFmiPath(fmiPath)
            .ExecuteAsync()
            .ConfigureAwait(false);
        return result.AccessToken;
    });
```

**Context flow to callback:**  
`WithFmiPathForClientAssertion(agentAppId)` on the Leg 2 request → `CommonParameters.ClientAssertionFmiPath` → `AuthenticationRequestParameters.ClientAssertionFmiPath` → credential class builds `AssertionRequestOptions { ClientAssertionFmiPath = "agentAppId" }` → callback receives it via `opts.ClientAssertionFmiPath`.

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `Flow3_FmiCredential_From_AnotherFmiCredential` | `FmiIntegrationTests.cs` (integration) | CCA constructed with `WithClientAssertion((options) => GetFmiCredentialFromRma(options))` — the assertion callback acquires an FMI credential from a parent RMA |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | Assertion callback receives `AssertionRequestOptions`, asserts `a.ClientAssertionFmiPath == AgentIdentity`, then calls Leg 1 with the FMI path |
| `AgentGetsAppTokenForGraphTest` | `Agentic.cs` (integration) | Assertion callback `(AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity)` — dynamic assertion used as the agent CCA's credential |
| `AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Verifies Leg 1 (FMI credential) is cached: second user only triggers 1 HTTP call (Leg 3) instead of 3, proving the assertion callback result was reused |

### Translation Notes

Key requirements for each SDK:
1. **Assertion callback must receive context** — at minimum: `clientId`, `tokenEndpoint`, and `fmiPath`. Without `fmiPath`, the callback can't build the correct Leg 1 request.
2. **Callback must be async** — Leg 1 is a network operation.
3. **Result must be cacheable** — the callback is invoked every time MSAL needs a fresh assertion, so the callback itself should delegate to a cache-aware method (like `AcquireTokenForClient` which has built-in caching).

| SDK | Current Assertion Callback | Context Available | Needs Work |
|-----|---------------------------|-------------------|------------|
| .NET | `Func<AssertionRequestOptions, Task<string>>` | `ClientID`, `TokenEndpoint`, `TenantId`, `Authority`, `Claims`, `ClientCapabilities`, `ClientAssertionFmiPath` | ✅ Complete |
| Go | `func(context.Context, AssertionRequestOptions) (string, error)` | `ClientID`, `TokenEndpoint` | ⚠️ Needs `FMIPath` |
| Python | `callable()` (zero-arg) | None | ❌ Needs context |
| Java | `Callable<String>` (zero-arg) | None | ❌ Needs context |
| JS | `(config: {clientId, tokenEndpoint?}) => Promise<string>` | `clientId`, `tokenEndpoint` | ⚠️ Needs `fmiPath` |

---

## 5. Assertion Context — `fmi_path` in Assertion Callback (Leg 1)

### What It Does

When the agent CCA needs a fresh `client_assertion` (i.e., Leg 1's FMI token), MSAL passes contextual information to the assertion callback so the callback can make the correct Leg 1 request. The critical piece is `ClientAssertionFmiPath` — without it, the callback doesn't know which agent app to request the FMI token for.

### MSAL .NET Reference

This is a sub-component of [§4](#4-callabledynamic-client-assertions-legs-2-3). The key mechanism is `WithFmiPathForClientAssertion`, which is distinct from `WithFmiPath`:

| Aspect | `WithFmiPath` | `WithFmiPathForClientAssertion` |
|--------|--------------|-------------------------------|
| Purpose | Adds `fmi_path` to the **token request body** | Passes `fmi_path` to the **assertion callback** |
| Storage | `CommonParameters.FmiPathSuffix` | `CommonParameters.ClientAssertionFmiPath` |
| Body injection | Yes — `TokenClient` adds `fmi_path` to POST | No — callback decides what to do with it |
| Cache key name | `"fmi_path"` | `"credential_fmi_path"` |
| Available on | `AcquireTokenForClientParameterBuilder` | Any builder (extension method) |

| Item | File | Key Code |
|------|------|----------|
| Extension method | `Extensibility/AbstractConfidentialClientAcquireTokenParameterBuilderExtension.cs` | `WithFmiPathForClientAssertion<T>(this ..., string fmiPath)` |
| Options property | `AppConfig/AssertionRequestOptions.cs` | `public string ClientAssertionFmiPath { get; set; }` |
| Population | `Internal/ClientCredential/ClientAssertionStringDelegateCredential.cs` | Sets `AssertionRequestOptions.ClientAssertionFmiPath = p.ClientAssertionFmiPath` before invoking callback |

**Usage in `AgentTokenRequest`:**
```csharp
// Leg 2: The WithFmiPathForClientAssertion tells the assertion callback which FMI path to use
var assertionResult = await PropagateOuterRequestParameters(
        agentCca.AcquireTokenForClient(new[] { TokenExchangeScope }))
    .WithFmiPathForClientAssertion(agentAppId)  // ← this flows to the callback
    .ExecuteAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | Uses `WithFmiPathForClientAssertion(AgentIdentity)` on the assertion app's `AcquireTokenForClient` call; assertion callback asserts `a.ClientAssertionFmiPath == AgentIdentity` — proving the FMI path flows through to the callback |
| `AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Composite API internally uses `WithFmiPathForClientAssertion`; the mock setup verifies the correct number of HTTP calls (Leg 1 cached via assertion context) |

### Translation Notes

This is only needed if the SDK supports the composite `AcquireTokenForAgent` API (where MSAL internally manages the multi-leg flow). For the primitive APIs, the caller manually manages the legs and passes tokens between them.

For SDKs implementing the composite API:
- The assertion callback context must include the `fmi_path` value
- The composite handler must set this context before Leg 2's token request

---

## 6. `user_fic` Grant Type (Leg 3)

### What It Does

A **non-standard** OAuth2 grant type that exchanges a federated identity credential (from Leg 2) for a user-scoped token. This is the core of the agent-acting-as-user scenario. Sent as `grant_type=user_fic` in the POST body.

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| OAuth2 constant | `OAuth2/OAuthConstants.cs` | `public const string UserFic = "user_fic";` (in `OAuth2GrantType` class) |
| Request handler | `Internal/Requests/UserFederatedIdentityCredentialRequest.cs` | Sets `dict[OAuth2Parameter.GrantType] = OAuth2GrantType.UserFic` in `GetAdditionalBodyParameters()` |
| Builder | `AcquireTokenByUserFederatedIdentityCredentialParameterBuilder.cs` | Entry point for standalone Leg 3 |
| Parameters | `AcquireTokenByUserFederatedIdentityCredentialParameters.cs` | `Username`, `UserObjectId`, `Assertion`, `SendX5C`, `ForceRefresh` |

**`GetAdditionalBodyParameters` in `UserFederatedIdentityCredentialRequest`:**
```csharp
private Dictionary<string, string> GetAdditionalBodyParameters(string assertion)
{
    var dict = new Dictionary<string, string>
    {
        [OAuth2Parameter.GrantType] = OAuth2GrantType.UserFic,
        [OAuth2Parameter.UserFederatedIdentityCredential] = assertion
    };

    if (_userFicParameters.UserObjectId.HasValue)
    {
        dict[OAuth2Parameter.UserId] = _userFicParameters.UserObjectId.Value.ToString("D");
    }
    else
    {
        dict[OAuth2Parameter.Username] = _userFicParameters.Username;
    }

    ISet<string> unionScope = new HashSet<string>
    {
        OAuth2Value.ScopeOpenId, OAuth2Value.ScopeOfflineAccess, OAuth2Value.ScopeProfile
    };
    unionScope.UnionWith(AuthenticationRequestParameters.Scope);
    dict[OAuth2Parameter.Scope] = unionScope.AsSingleString();
    dict[OAuth2Parameter.ClientInfo] = "1";

    return dict;
}
```

**Key behaviors:**
- Scope is **augmented** with `openid`, `offline_access`, `profile` (same as auth code flow, not client credentials)
- `client_info=1` is sent (same as user-based flows) to enable account/cache-key resolution
- User is identified by **either** `user_id` (OID) or `username` (UPN), never both
- Tokens are stored in the **user token cache** (not app cache)

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenByUserFic_SendsCorrectOAuth2Parameters_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Mock expects `grant_type=user_fic`, `username`, `user_federated_identity_credential` in POST body |
| `AcquireTokenByUserFic_TokenIsStoredInUserCache_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Verifies `result.Account` is not null and `GetAccountsAsync()` returns accounts — proving user cache storage |
| `AcquireTokenByUserFic_WithForceRefresh_CallsIdentityProvider_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Two calls: first from IdP, second with `WithForceRefresh(true)` bypasses cache and hits IdP again; both mock handlers consumed |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | Full end-to-end: Leg 1 (FMI) → Leg 2 (instance token via `WithFmiPathForClientAssertion`) → Leg 3 (`user_fic` via `AcquireTokenByUserFederatedIdentityCredential`) → `AcquireTokenSilent` returns from cache |

### Translation Notes

Every non-.NET MSAL needs:
1. A new grant type constant `"user_fic"`
2. A new request handler/supplier that builds the POST body with the above parameters
3. Proper scope augmentation (add the OIDC scopes)
4. `client_info=1` so the response includes account information
5. Results cached in the user token cache, not the app cache

---

## 7. `user_federated_identity_credential` Body Parameter (Leg 3)

### What It Does

Carries the instance token (T2, from Leg 2) in the Leg 3 POST body. This is the federated credential that proves the agent's identity to Entra ID.

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| OAuth2 constant | `OAuth2/OAuthConstants.cs` | `public const string UserFederatedIdentityCredential = "user_federated_identity_credential";` |
| Body injection | `UserFederatedIdentityCredentialRequest.cs` | `dict[OAuth2Parameter.UserFederatedIdentityCredential] = assertion;` |
| Parameter source | `AcquireTokenByUserFederatedIdentityCredentialParameters.cs` | `public string Assertion { get; set; }` |
| Public API signature | `IByUserFederatedIdentityCredential.cs` | `...(IEnumerable<string> scopes, string username, string assertion)` — `assertion` parameter |

**Data flow**: Caller passes `assertion` (T2) → stored in `Parameters.Assertion` → `UserFederatedIdentityCredentialRequest.GetAdditionalBodyParameters(assertion)` → POST body `user_federated_identity_credential=<T2>`.

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenByUserFic_SendsCorrectOAuth2Parameters_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Mock's `ExpectedPostData` includes `{ OAuth2Parameter.UserFederatedIdentityCredential, FakeAssertion }` — verifies the assertion is sent in the POST body |
| `AcquireTokenByUserFic_WithOid_SendsUserIdParameter_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Same assertion body param verified alongside the OID-based user identification |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | Leg 2's access token is passed as the `assertion` parameter to `AcquireTokenByUserFederatedIdentityCredential`, which sends it as the `user_federated_identity_credential` body param |

### Translation Notes

Straightforward string parameter. Just needs:
- A constant for the parameter name
- Inclusion in the POST body alongside the `user_fic` grant type

---

## 8. `user_id` / `username` Body Parameters (Leg 3)

### What It Does

Identifies the target user in the Leg 3 `user_fic` request. **Exactly one** must be present:
- `user_id` — the user's Object ID (GUID), the immutable identifier (preferred)
- `username` — the user's UPN (string), mutable but human-readable

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| OAuth2 constants | `OAuth2/OAuthConstants.cs` | `public const string UserId = "user_id";` and `public const string Username = "username";` |
| Mutual exclusion | `UserFederatedIdentityCredentialRequest.cs` | `if (UserObjectId.HasValue) { dict["user_id"] = ... } else { dict["username"] = ... }` |
| OID parameter type | `AcquireTokenByUserFederatedIdentityCredentialParameters.cs` | `public Guid? UserObjectId { get; set; }` |
| UPN parameter type | Same file | `public string Username { get; set; }` |
| Public API overloads | `IByUserFederatedIdentityCredential.cs` | `...(scopes, string username, assertion)` vs `...(scopes, Guid userObjectId, assertion)` |

**CCS routing header support** (`UserFederatedIdentityCredentialRequest.GetCcsHeader`):
```csharp
protected override KeyValuePair<string, string>? GetCcsHeader(...)
{
    if (_userFicParameters.UserObjectId.HasValue)
    {
        string ccsHint = CoreHelpers.GetCcsClientInfoHint(
            _userFicParameters.UserObjectId.Value.ToString("D"),
            AuthenticationRequestParameters.Authority.TenantId);
        if (!string.IsNullOrEmpty(ccsHint))
            return new KeyValuePair<string, string>(Constants.CcsRoutingHintHeader, ccsHint);
        return null;
    }
    return GetCcsUpnHeader(_userFicParameters.Username);
}
```

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenByUserFic_WithOid_SendsUserIdParameter_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | OID overload sends `user_id` and explicitly asserts `username` is **NOT** in the POST body (`UnExpectedPostData`) |
| `AcquireTokenByUserFic_WithUpn_SendsUsernameParameter_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | UPN overload sends `username` and explicitly asserts `user_id` is **NOT** in the POST body — mutual exclusion |
| `AcquireTokenByUserFic_TwoUpns_SilentReturnsCorrectToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Two different UPN users → two separate accounts → `AcquireTokenSilent` returns correct token per user |
| `AcquireTokenByUserFic_TwoOids_SilentReturnsCorrectToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Two different users looked up by `HomeAccountId.ObjectId` — validates OID-based account matching in cache |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | Uses UPN (`UserUpn`) for user identification in the full end-to-end flow |

### Translation Notes

Two design considerations:
1. **Type safety** — .NET uses `Guid` for OID to prevent accidental confusion with UPN strings. Other languages should use their equivalent (`UUID` in Java/Python/Go, or explicit string validation in JS).
2. **CCS routing** — this is MSAL-internal routing optimization. Not strictly required for correctness, but improves performance by routing to the right datacenter.

---

## 9. `AgentIdentity` Model

### What It Does

Encapsulates the agent application ID and user identifier into a single immutable object. Parallels `UserAssertion` (used in OBO) as the "who" of the request. Supports three modes:
- **by OID**: `new AgentIdentity(agentAppId, userObjectId)` — recommended
- **by UPN**: `AgentIdentity.WithUsername(agentAppId, upn)`
- **app-only**: `AgentIdentity.AppOnly(agentAppId)` — Legs 1-2 only, no user

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| Model class | `AgentIdentity.cs` (root of `Microsoft.Identity.Client`) | Sealed class with private base constructor |
| Properties | Same file | `AgentApplicationId` (string), `UserObjectId` (Guid?), `Username` (string) |
| Internal helper | Same file | `HasUserIdentifier` — `UserObjectId.HasValue \|\| !string.IsNullOrEmpty(Username)` |
| Usage | `AgentTokenRequest.ExecuteAsync()` | Checks `agentIdentity.HasUserIdentifier` to decide app-only vs user flow |

**Full constructor/factory pattern:**
```csharp
public sealed class AgentIdentity
{
    private AgentIdentity(string agentApplicationId) { /* validates, sets AgentApplicationId */ }

    // By OID (recommended)
    public AgentIdentity(string agentApplicationId, Guid userObjectId) : this(agentApplicationId)
    {
        // validates Guid.Empty, sets UserObjectId
    }

    // By UPN
    public static AgentIdentity WithUsername(string agentApplicationId, string username) { ... }

    // App-only (no user, Legs 1-2 only)
    public static AgentIdentity AppOnly(string agentApplicationId) { ... }

    public string AgentApplicationId { get; }
    public Guid? UserObjectId { get; private set; }
    public string Username { get; private set; }
    internal bool HasUserIdentifier => UserObjectId.HasValue || !string.IsNullOrEmpty(Username);
}
```

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Uses `AgentIdentity.WithUsername(AgentAppId, upn)` — validates UPN factory |
| `AcquireTokenForAgent_WithPreCancelledToken_ThrowsOperationCanceledException_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Uses `AgentIdentity.WithUsername(...)` — validates model construction with UPN |
| `AcquireTokenByUserFic_NullUsername_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Null validation on username parameter (covers the input validation pattern) |
| `AcquireTokenByUserFic_NullAssertion_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Null validation on assertion parameter |
| `AcquireTokenByUserFic_EmptyAssertion_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Empty string validation on assertion parameter |

Note: The `AgentIdentity` model doesn't have its own dedicated test class — its validation is tested indirectly through the builder and composite API tests. A dedicated test for `AgentIdentity` construction (null/empty `agentApplicationId`, `Guid.Empty` rejection, `AppOnly` factory) would be useful.

### Translation Notes

Each SDK should create an equivalent model adapted to language idioms:
- **Java**: Immutable class with private constructor + public constructor for OID + static factory methods for UPN and app-only
- **Python**: Dataclass or named constructor pattern; Python idiom might use `@classmethod` for factories
- **Go**: Struct with `NewAgentIdentity(appId, userOID)`, `NewAgentIdentityWithUsername(appId, upn)`, `NewAppOnlyAgentIdentity(appId)` constructors
- **JS/Node**: Class or interface with static factory methods

---

## 10. Composite `AcquireTokenForAgent` API

### What It Does

Single-call API that orchestrates the full three-leg FMI/FIC token exchange. The developer passes scopes and an `AgentIdentity`; MSAL handles Legs 1-3 internally, including caching intermediate tokens.

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| Interface method | `IConfidentialClientApplication.cs` | `AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(IEnumerable<string> scopes, AgentIdentity agentIdentity)` |
| CCA implementation | `ConfidentialClientApplication.cs` | Delegates to `AcquireTokenForAgentParameterBuilder.Create(executor, scopes, agentIdentity)` |
| Builder | `AcquireTokenForAgentParameterBuilder.cs` | `WithForceRefresh()`, `WithSendX5C()`, `ExecuteInternalAsync()` → executor |
| Parameters | `AcquireTokenForAgentParameters.cs` | `AgentIdentity`, `ForceRefresh`, inherits `SendX5C` |
| Executor overload | `IConfidentialClientApplicationExecutor.cs` | `ExecuteAsync(common, agentParams, ct)` |
| Executor wiring | `ConfidentialClientExecutor.cs` | Creates `AgentTokenRequest` handler, passes `_confidentialClientApplication` as blueprint |
| **Core orchestrator** | `Internal/Requests/AgentTokenRequest.cs` | Three-leg orchestration (331 lines) |
| Agent CCA cache | `ConfidentialClientApplication.cs` | `ConcurrentDictionary<string, IConfidentialClientApplication> AgentCcaCache` |
| Telemetry API ID | `TelemetryCore/Internal/Events/ApiEvent.cs` | `AcquireTokenForAgent = 1020` |

**Orchestration flow in `AgentTokenRequest.ExecuteAsync`:**
```
1. GetOrCreateAgentCca(agentAppId, authority)
   └─ Retrieves cached agent CCA or builds new one (BuildAgentCca)
      └─ Agent CCA's assertion callback = Leg 1 (blueprint.AcquireTokenForClient + WithFmiPath)

2. If app-only (no user identifier):
   └─ agentCca.AcquireTokenForClient(callerScopes) → return

3. If user identity:
   a. TryAcquireTokenSilentFromAgentCacheAsync (unless ForceRefresh)
      └─ GetAccountsAsync → FindMatchingAccount(by OID or UPN) → AcquireTokenSilent
      └─ If cache hit → return cached token
   b. Leg 2: agentCca.AcquireTokenForClient(TokenExchangeScope).WithFmiPathForClientAssertion(agentAppId)
      └─ Agent CCA's assertion callback fires → Leg 1 (FMI token from blueprint, cached)
      └─ Returns assertion token (T2)
   c. Leg 3: agentCca.AcquireTokenByUserFederatedIdentityCredential(callerScopes, user, T2)
      └─ user_fic grant, result stored in agent CCA's user token cache
      └─ Return final token
```

**Agent CCA caching**: The agent CCA is stored in `ConfidentialClientApplication.AgentCcaCache` (a `ConcurrentDictionary<string, IConfidentialClientApplication>`) keyed by `"agent_" + agentAppId`. This ensures:
- The agent CCA's in-memory token cache (both app and user) persists across calls
- Leg 1 (FMI token) and Leg 2 (instance token) are cached automatically
- Different agent app IDs get separate CCAs with separate caches

**Blueprint config propagation** (`AgentTokenRequest.PropagateBlueprintConfig`):
The agent CCA inherits the blueprint's HTTP factory, logging, telemetry identity, and instance discovery settings so that agent requests behave consistently with the developer's original configuration.

**Per-request parameter propagation** (`AgentTokenRequest.PropagateOuterRequestParameters`):
Each inner request (Leg 2, Leg 3, Silent) inherits the outer request's correlation ID, claims/client capabilities, tenant override, and extra query parameters.

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenForAgent_TwoUpns_CacheReturnsCorrectUserToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | **Core composite test**: User 1 triggers 3 HTTP calls (Leg 1 + Leg 2 + Leg 3). User 2 triggers only 1 call (Leg 3 — Legs 1+2 cached). User 1 again returns from cache (0 calls). Validates internal CCA caching, per-user token isolation, and silent retrieval. |
| `AcquireTokenForAgent_WithPreCancelledToken_ThrowsOperationCanceledException_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Pre-cancelled `CancellationToken` propagates through `AgentTokenRequest.ExecuteAsync` → inner `AcquireTokenForClient` → `TokenClient.SendTokenRequestAsync` without making any HTTP calls |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | End-to-end with real Entra ID: manual three-leg flow using the primitive APIs (`WithFmiPath`, `WithFmiPathForClientAssertion`, `AcquireTokenByUserFederatedIdentityCredential`), then `AcquireTokenSilent` returns from cache |
| `AgentGetsAppTokenForGraphTest` | `Agentic.cs` (integration) | App-only agent flow: CCA with assertion callback (Leg 1) → `AcquireTokenForClient` for Graph (Leg 2) |

The unit test (`TwoUpns`) is the most important for validating the composite orchestration because it precisely controls HTTP calls via `MockHttpManager` and verifies the exact number of network requests per user.

### Translation Notes

This is the most complex component. Key design decisions for non-.NET SDKs:
1. **Internal CCA caching** — the agent CCA must be reused across calls for the same agent app ID to benefit from cached intermediate tokens
2. **Silent check before Leg 2+3** — for user-scoped tokens, check the user cache first to avoid unnecessary network calls
3. **Config propagation** — the agent CCA should inherit the blueprint's HTTP/logging/telemetry settings
4. **Request parameter propagation** — correlation ID, claims, tenant override should flow to inner requests

---

## 11. Primitive `AcquireTokenByUserFederatedIdentityCredential` API

### What It Does

Standalone API for just Leg 3 — the `user_fic` grant type exchange. The caller provides the scopes, user identifier (OID or UPN), and the assertion (T2 token from Leg 2). Used by callers who manage Legs 1-2 themselves.

### MSAL .NET Reference

| Item | File | Key Code |
|------|------|----------|
| Interface | `IByUserFederatedIdentityCredential.cs` | Two overloads: `...(scopes, string username, assertion)` and `...(scopes, Guid userObjectId, assertion)` |
| Interface inheritance | `IConfidentialClientApplication.cs` | `IConfidentialClientApplication : ... , IByUserFederatedIdentityCredential` |
| CCA implementation | `ConfidentialClientApplication.cs` | Explicit interface implementation for both overloads |
| Builder | `AcquireTokenByUserFederatedIdentityCredentialParameterBuilder.cs` | `WithForceRefresh()`, `WithSendX5C()`, separate `Create` overloads for string UPN vs Guid OID |
| Parameters | `AcquireTokenByUserFederatedIdentityCredentialParameters.cs` | `Username`, `UserObjectId (Guid?)`, `Assertion`, `SendX5C`, `ForceRefresh` |
| Request handler | `UserFederatedIdentityCredentialRequest.cs` | Builds POST body with `grant_type=user_fic`, user identifier, assertion, augmented scopes |
| Executor overload | `ConfidentialClientExecutor.cs` | Routes to `UserFederatedIdentityCredentialRequest` |

**Relationship to composite API**: The composite `AcquireTokenForAgent` handler (`AgentTokenRequest`) calls this same primitive API internally for Leg 3:
```csharp
// In AgentTokenRequest.ExecuteAsync, Leg 3:
return await ((IByUserFederatedIdentityCredential)agentCca)
    .AcquireTokenByUserFederatedIdentityCredential(
        AuthenticationRequestParameters.Scope,
        agentIdentity.UserObjectId.Value,
        assertion)
    .ExecuteAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Key Tests

| Test | File | What It Validates |
|------|------|-------------------|
| `AcquireTokenByUserFic_SendsCorrectOAuth2Parameters_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | UPN overload: validates `grant_type=user_fic`, `username`, `user_federated_identity_credential` in POST body |
| `AcquireTokenByUserFic_WithOid_SendsUserIdParameter_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | OID overload: validates `user_id` in POST body, `username` absent |
| `AcquireTokenByUserFic_WithUpn_SendsUsernameParameter_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | UPN overload: validates `username` in POST body, `user_id` absent |
| `AcquireTokenByUserFic_TokenIsStoredInUserCache_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Token stored in user cache: `result.Account` not null, `GetAccountsAsync()` returns accounts |
| `AcquireTokenByUserFic_WithForceRefresh_CallsIdentityProvider_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | `WithForceRefresh(true)` bypasses user cache, hits IdP a second time |
| `AcquireTokenByUserFic_TwoUpns_SilentReturnsCorrectToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Multi-user cache isolation: two UPN users, `AcquireTokenSilent` returns correct token per account |
| `AcquireTokenByUserFic_TwoOids_SilentReturnsCorrectToken_Async` | `UserFederatedIdentityCredentialTests.cs` (unit) | Multi-user cache isolation by OID: lookup via `HomeAccountId.ObjectId` returns correct token |
| `AcquireTokenByUserFic_NullUsername_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Input validation: null username |
| `AcquireTokenByUserFic_NullAssertion_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Input validation: null assertion |
| `AcquireTokenByUserFic_EmptyAssertion_ThrowsArgumentNullException` | `UserFederatedIdentityCredentialTests.cs` (unit) | Input validation: empty assertion |
| `AgentUserIdentityGetsTokenForGraphTest` | `Agentic.cs` (integration) | End-to-end primitive flow against real Entra ID: Leg 1 → Leg 2 → `AcquireTokenByUserFederatedIdentityCredential` → silent retrieval |

### Translation Notes

This primitive API is simpler than the composite:
- No multi-leg orchestration
- No internal CCA caching
- Just a new grant type + body parameters + user token cache storage

Each SDK needs:
1. A new method on the CCA (or equivalent) that accepts `(scopes, userIdentifier, assertion)`
2. A new request handler/supplier for the `user_fic` grant
3. Results stored in the user token cache

---

## Appendix: File Inventory

All source files in MSAL .NET that were added or modified for agent identity support:

### New Files
| File | Purpose |
|------|---------|
| `AgentIdentity.cs` | Public model class |
| `ApiConfig/AcquireTokenForAgentParameterBuilder.cs` | Composite API builder |
| `ApiConfig/Parameters/AcquireTokenForAgentParameters.cs` | Composite API internal parameters |
| `Internal/Requests/AgentTokenRequest.cs` | Three-leg orchestration handler |

### Modified Files
| File | Change |
|------|--------|
| `IConfidentialClientApplication.cs` | Added `AcquireTokenForAgent` method |
| `ConfidentialClientApplication.cs` | Implemented `AcquireTokenForAgent`, added `AgentCcaCache`, added OID overload for `AcquireTokenByUserFederatedIdentityCredential` |
| `IByUserFederatedIdentityCredential.cs` | Added `Guid userObjectId` overload |
| `ApiConfig/AcquireTokenByUserFederatedIdentityCredentialParameterBuilder.cs` | Added `Guid` constructor + `Create` overload |
| `ApiConfig/Parameters/AcquireTokenByUserFederatedIdentityCredentialParameters.cs` | Added `UserObjectId (Guid?)`, added logging |
| `ApiConfig/Executors/IConfidentialClientApplicationExecutor.cs` | Added `ExecuteAsync` overload for agent params |
| `ApiConfig/Executors/ConfidentialClientExecutor.cs` | Implemented agent executor, creates `AgentTokenRequest` |
| `Internal/Requests/UserFederatedIdentityCredentialRequest.cs` | Added OID support (`user_id` param), CCS routing for OID |
| `OAuth2/OAuthConstants.cs` | Added `UserId = "user_id"` constant |
| `TelemetryCore/Internal/Events/ApiEvent.cs` | Added `AcquireTokenForAgent = 1020` API ID |
| `PublicApi/*/PublicAPI.Unshipped.txt` (6 TFMs) | 18 new API entries each |

### Pre-existing Files (not modified, but important reference)
| File | Relevance |
|------|-----------|
| `OAuth2/OAuthConstants.cs` | `FmiPath`, `UserFederatedIdentityCredential`, `UserFic` constants (already shipped) |
| `ApiConfig/AcquireTokenForClientParameterBuilder.cs` | `WithFmiPath()` implementation |
| `OAuth2/TokenClient.cs` | `fmi_path` body injection |
| `Utils/CoreHelpers.cs` | `ComputeAccessTokenExtCacheKey()` hash algorithm |
| `Cache/Items/MsalAccessTokenCacheItem.cs` | `InitCacheKey()` with ext cache key logic |
| `AppConfig/AssertionRequestOptions.cs` | Assertion callback context model |
| `Extensibility/AbstractConfidentialClientAcquireTokenParameterBuilderExtension.cs` | `WithFmiPathForClientAssertion()` |
| `Internal/ClientCredential/ClientAssertionStringDelegateCredential.cs` | Assertion callback invocation |

