# Agent Identity APIs — Developer Experience Proposal

This document describes public APIs for MSAL .NET that natively support agent identity token acquisition, replacing the manual multi-step workarounds currently used by ID Web and Agents-for-net, and providing better support for FMI and FIC scenarios in general.

Two APIs at different abstraction levels:
- **`AcquireTokenForAgent`** — **Proposed** high-level composite API that chains all three legs of the FIC exchange into a single call (Section 1)
- **`AcquireTokenByUserFederatedIdentityCredential`** — **Implemented** (PR #5802, merged March 2026) lower-level primitive for the `user_fic` grant type, usable independently for custom FIC scenarios (Section 2)

These APIs work alongside the existing `AcquireTokenForClient`, which offers FMI support through the `WithFmiPath` parameter, in order to provide a simple set of APIs for any FIC, FMI, and agent identity scenarios.

---

## Table of Contents

1. [High-Level API: `AcquireTokenForAgent`](#1-high-level-api-acquiretokenforagent)
   - [1.1 `AcquireTokenForAgent` in `IConfidentialClientApplication`](#11-acquiretokenforagent-in-iconfidentialclientapplication)
   - [1.2 `AgentIdentity` — Required Input Model](#12-agentidentity--required-input-model)
   - [1.3 `AcquireTokenForAgent` — Entry Point Method](#13-acquiretokenforagent--entry-point-method)
   - [1.4 `AcquireTokenForAgentParameterBuilder` — Optional Configuration](#14-acquiretokenforagentparameterbuilder--optional-configuration)
   - [1.5 Return Value](#15-return-value)
   - [1.6 Credential Configuration](#16-credential-configuration)
   - [1.7 Caching Optimization](#17-caching-optimization)
   - [1.8 Key Design Decisions](#18-key-design-decisions)
2. [Lower-Level Primitive: `AcquireTokenByUserFederatedIdentityCredential` (Implemented)](#2-lower-level-primitive-acquiretokenbyuserfederatedidentitycredential-implemented)
   - [2.1 What Was Implemented (PR #5802)](#21-what-was-implemented-pr-5802)
   - [2.2 API Surface](#22-api-surface)
   - [2.3 Gaps vs. Original Design](#23-gaps-vs-original-design)
   - [2.4 Relationship to `AcquireTokenForAgent`](#24-relationship-to-acquiretokenforagent)
   - [2.5 Relationship to `WithFmiPath`](#25-relationship-to-withfmipath)
3. [Usage Examples](#3-usage-examples)
4. [Current Workarounds and How the Proposed APIs Replace Them](#4-current-workarounds-and-how-the-proposed-apis-replace-them)
   - [4.1 Agents-for-net](#41-agents-for-net-msalauthcs)
   - [4.2 ID Web](#42-id-web-agentuseridentitymsaladdincs)
   - [4.3 Summary of What Changes](#43-summary-of-what-changes)

---

## 1. High-Level API: `AcquireTokenForAgent`

Add a new `AcquireTokenForAgent` API to `IConfidentialClientApplication`, along with a new `AgentIdentity` model class to encapsulate the identity of the agent and target user. This is the recommended API for agent identity scenarios — it chains all three legs of the FIC exchange internally.

### 1.1 `AcquireTokenForAgent` in `IConfidentialClientApplication`

```csharp
public partial interface IConfidentialClientApplication : IClientApplicationBase
{
    // ... existing methods (AcquireTokenForClient, AcquireTokenOnBehalfOf, etc.) ...

    AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(
        IEnumerable<string> scopes,
        AgentIdentity agentIdentity);
}
```

### 1.2 `AgentIdentity` — Required Input Model

A new model class that encapsulates the identity of the agent and the target user. This parallels the existing `UserAssertion` model in OBO flows.

```csharp
public sealed class AgentIdentity
{
    // Construct for user-by-OID scenario
    public AgentIdentity(string agentApplicationId, Guid userObjectId);

    // Factory for user-by-UPN scenario
    public static AgentIdentity WithUsername(string agentApplicationId, string username);

    // Factory for app-only agent identity (no user — Legs 1-2 only)
    public static AgentIdentity AppOnly(string agentApplicationId);

    public string AgentApplicationId { get; }  // The agent app registration's client ID
    public Guid? UserObjectId { get; }         // User OID (if constructed with OID)
    public string Username { get; }            // User UPN (if constructed with Username)
}
```

**Parameters explained:**

| Parameter | Source | Purpose |
|-----------|--------|---------|
| `agentApplicationId` | The agent's own Entra ID app registration (not the blueprint) | Becomes the `fmi_path` in Leg 1 and the `client_id` for Legs 2-3 |
| `userObjectId` | The OID of the user the agent acts on behalf of (from incoming token claims) | Sent as `user_id` in the final `user_fic` grant |
| `username` | The UPN of the target user (alternative to OID) | Sent as `username` in the final `user_fic` grant |

**Three construction modes:**
- `new AgentIdentity(appId, oid)` — Agent acting as a user identified by OID (`Guid`; most common)
- `AgentIdentity.WithUsername(appId, upn)` — Agent acting as a user identified by UPN
- `AgentIdentity.AppOnly(appId)` — Agent acting as itself (no user impersonation)

### 1.3 `AcquireTokenForAgent` — Entry Point Method

The method on `IConfidentialClientApplication` (shown in [Section 1.1](#11-api-placement-on-iconfidentialclientapplication)) that kicks off the three-leg FIC exchange. The blueprint CCA provides its own credentials (cert/secret/federated); MSAL handles the rest internally.

```csharp
AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(
    IEnumerable<string> scopes,
    AgentIdentity agentIdentity);
```

**Required parameters** (in the method signature):
- `scopes` — The target downstream API scopes (e.g., `https://graph.microsoft.com/.default`). Only used in the final leg; MSAL uses `api://AzureAdTokenExchange/.default` internally for the intermediate legs.
- `agentIdentity` — The `AgentIdentity` describing the agent app and target user.

### 1.4 `AcquireTokenForAgentParameterBuilder` — Optional Configuration

Returns a fluent builder with optional `With*` modifiers, following MSAL's standard pattern:

```csharp
public sealed class AcquireTokenForAgentParameterBuilder
    : AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenForAgentParameterBuilder>
{
    // Force a fresh token acquisition, bypassing cached intermediate tokens
    public AcquireTokenForAgentParameterBuilder WithForceRefresh(bool forceRefresh);

    // Send X5C (public certificate) with the request — needed for SNI authentication
    public AcquireTokenForAgentParameterBuilder WithSendX5C(bool withSendX5C);

    // Include FMI identity attributes (returned as xms_attr claim in the token)
    public AcquireTokenForAgentParameterBuilder WithAttributes(string attributeJson);

    // Inherited: WithTenantId(string), WithCorrelationId(Guid),
    //           WithExtraQueryParameters(...), OnBeforeTokenRequest(...)
}
```

**What MSAL handles internally** (developers don't set these):
- The `api://AzureAdTokenExchange/.default` scope for intermediate legs
- The `fmi_path` parameter (derived from `AgentIdentity.AgentApplicationId`)
- Construction of internal CCAs for the agent app ID
- The `user_fic` grant type and all related body parameters
- Token chaining between the three legs
- Caching of intermediate tokens with proper cache keys (see [Section 1.7](#17-caching-optimization))

### 1.5 Return Value

Returns `Task<AuthenticationResult>` — the same result type as all other MSAL flows. Key properties:

| Property | Value |
|----------|-------|
| `AccessToken` | The final user-scoped (or app-scoped if `AppOnly`) agent identity token |
| `ExpiresOn` | Token expiration |
| `TenantId` | Tenant the token was issued for |
| `Scopes` | The granted scopes |
| `Account` | Account information (if user-scoped) |

### 1.6 Credential Configuration

The blueprint CCA's credentials authenticate Leg 1. MSAL derives all subsequent credentials internally — Leg 1's token becomes Leg 2's `client_assertion`, Leg 2's token becomes Leg 3's `user_federated_identity_credential`. Developers configure credentials **once** at CCA construction time using any standard `ConfidentialClientApplicationBuilder` method:

| Method | Use case |
|--------|----------|
| `.WithCertificate(cert)` | X.509 certificate (with optional SNI via `sendX5C`) |
| `.WithClientSecret(secret)` | Client secret |
| `.WithClientAssertion(Func<...>)` | Federated credential / managed identity assertion |

Credentials do not need to be on `AgentIdentity` or configurable per-request. Both ID Web and Agents-for-net create the blueprint CCA once and reuse it across all agent identity requests — only the `AgentIdentity` (agent app ID + user) and scopes vary per call.

For managed identity–based blueprints, use `WithClientAssertion` with a `ManagedIdentityClientAssertion` provider (the same pattern Agents-for-net uses today). `ManagedIdentityApplication` itself cannot be used as the entry point because the FIC exchange requires client credentials that MSI apps don't provide.

### 1.7 Caching Optimization

The three-leg exchange benefits from independent caching at each leg:

| Leg | Cache type | Varies by | Reusable across |
|-----|-----------|-----------|----------------|
| Leg 1 (FMI app token) | App cache | blueprint + agent app ID + tenant | All users of the same agent |
| Leg 2 (Instance token) | App cache | agent app ID + tenant | All users of the same agent |
| Leg 3 (User FIC token) | User cache | agent app ID + user + scopes | Only the same user + scopes |

Legs 1–2 can be served from cache when handling requests for different users of the same agent — only Leg 3 varies per user. Today, both packages construct fresh intermediate CCAs per request, losing this optimization. With MSAL owning the orchestration, it can cache and reuse intermediate tokens automatically.

### 1.8 Key Design Decisions

**Why a single method instead of exposing individual legs?**
Most callers never need intermediate tokens. A single `AcquireTokenForAgent` lets MSAL own orchestration and caching. Callers who need individual legs can use the existing `AcquireTokenForClient().WithFmiPath()` for FMI (Legs 1–2) and the now-implemented `AcquireTokenByUserFederatedIdentityCredential` for FIC (Leg 3).

**Why `AgentIdentity` as a model class instead of inline parameters?**
Inline parameters (e.g., `AcquireTokenForAgent(scopes, agentAppId, userId)`) can't cleanly express OID vs. UPN vs. AppOnly without method overload explosion. `AgentIdentity` parallels `UserAssertion` in OBO — a required, immutable input that describes the identity context.

**Why CCA-only (no `ManagedIdentityApplication`)?**
The FIC exchange requires client credentials for all three legs. `ManagedIdentityApplication` can't provide these. Blueprint apps that authenticate via MSI should use `ConfidentialClientApplicationBuilder.WithClientAssertion(...)` with a managed identity assertion provider (see [Section 1.6](#16-credential-configuration)).

---

## 2. Lower-Level Primitive: `AcquireTokenByUserFederatedIdentityCredential` (Implemented)

> **Status**: Implemented in MSAL .NET via PR [#5802](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5802) (commit `3d45a47`, merged March 18 2026). The sections below describe what shipped and note gaps relative to our original design.

### 2.1 What Was Implemented (PR #5802)

The `user_fic` grant type is now a first-class MSAL .NET API. The implementation:

- Added `IByUserFederatedIdentityCredential` interface with `AcquireTokenByUserFederatedIdentityCredential(scopes, username, assertion)`
- `IConfidentialClientApplication` inherits `IByUserFederatedIdentityCredential` (explicit interface implementation — requires cast)
- Added `AcquireTokenByUserFederatedIdentityCredentialParameterBuilder` (sealed, extends `AbstractConfidentialClientAcquireTokenParameterBuilder<T>`)
- Added `UserFederatedIdentityCredentialRequest` internal handler extending `RequestBase`
- Tokens stored in the **user token cache**
- OAuth2 constants: `OAuth2GrantType.UserFic`, `OAuth2Parameter.UserFederatedIdentityCredential`
- Telemetry: `ApiEvent.ApiIds.AcquireTokenByUserFederatedIdentityCredential`

The MSAL integration test (`Agentic.cs`) was updated to use the new API, replacing the previous `OnBeforeTokenRequest` hack.

### 2.2 API Surface

**Interface** (on a separate interface, following the `IByRefreshToken` pattern):

```csharp
public interface IByUserFederatedIdentityCredential
{
    AcquireTokenByUserFederatedIdentityCredentialParameterBuilder
        AcquireTokenByUserFederatedIdentityCredential(
            IEnumerable<string> scopes,
            string username,    // UPN — required
            string assertion);  // federated credential — required
}

// IConfidentialClientApplication inherits this interface
public partial interface IConfidentialClientApplication
    : IClientApplicationBase, IByUserFederatedIdentityCredential { ... }
```

**Usage** (requires cast due to explicit interface implementation):

```csharp
var result = await (cca as IByUserFederatedIdentityCredential)
    .AcquireTokenByUserFederatedIdentityCredential(
        new[] { "https://graph.microsoft.com/.default" },
        "user@contoso.com",
        federatedCredentialToken)
    .ExecuteAsync();
```

**Builder methods:**

| Method | Purpose |
|--------|---------|
| `WithForceRefresh(bool)` | Bypass cache and force a fresh token from Entra ID |
| `WithSendX5C(bool)` | Send X5C (public certificate chain) with the request |

**Protocol mapping** (what the handler sends):

```
POST /{tenantId}/oauth2/v2.0/token HTTP/1.1

grant_type=user_fic
client_id=<from CCA>
client_assertion=<from CCA credentials>
client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer
username=<username parameter>
user_federated_identity_credential=<assertion parameter>
scope=openid offline_access profile <requested scopes>
client_info=1
```

### 2.3 Gaps vs. Original Design

| Aspect | Our original design | What shipped | Resolution |
|--------|-------------------|--------------|------------|
| Method name | `AcquireTokenByFederatedCredential` | `AcquireTokenByUserFederatedIdentityCredential` | Naming only — no functional gap |
| User identification | `WithUserId(oid)` and `WithUsername(upn)` on builder (mutually exclusive) | `username` (UPN) required in method signature | **Resolved** — add `Guid userObjectId` overload (see below) |
| API placement | Direct method on `IConfidentialClientApplication` | Explicit interface `IByUserFederatedIdentityCredential` (requires cast) | Pattern difference — follows existing MSAL convention |
| `ForceRefresh` | On builder | On builder | ✅ Same |
| `SendX5C` | Not explicitly proposed | On builder | Bonus |
| User token cache | Yes | Yes | ✅ Same |

**OID gap resolution: `Guid` overload.** The shipped API only accepts a UPN via `string username`. To support OID-based user identification, we add a second overload that takes `Guid userObjectId` instead. Because `Guid` and `string` are different types, method overloading works naturally — the compiler resolves the correct overload at call time with no ambiguity.

This approach:
- Avoids clunky `WithUserId()` builder methods where `username` would be null/ignored
- Makes OID support available to **any** FIC consumer (not just `AcquireTokenForAgent`)
- Follows the precedent set by ID Web's `AgentIdentityExtension.WithAgentUserIdentity` overloads, which use the same `string` vs `Guid` disambiguation
- Is type-safe: Azure AD object IDs are always GUIDs, so `Guid` is semantically correct
- On the wire, the `Guid` overload sends `user_id` (formatted as `"D"` — lowercase with hyphens) instead of `username`

### 2.4 Relationship to `AcquireTokenForAgent`

`AcquireTokenForAgent` (proposed, Section 1) is a high-level composite that internally chains three primitives:

| Leg | Primitive | Status |
|-----|-----------|--------|
| **Leg 1** | `AcquireTokenForClient(exchangeScope).WithFmiPath(agentAppId)` | Exists |
| **Leg 2** | `AcquireTokenForClient(exchangeScope)` on CCA with `WithClientAssertion(leg1Token)` | Exists |
| **Leg 3** | `AcquireTokenByUserFederatedIdentityCredential(scopes, upn, leg2Token)` or `...(scopes, oid, leg2Token)` | **Implemented** (UPN); **Proposed** (`Guid` overload for OID) |

Most agent identity callers should use `AcquireTokenForAgent` and never touch the primitives directly. The implemented FIC primitive can be used standalone for custom FIC scenarios.

### 2.5 Relationship to `WithFmiPath`

`WithFmiPath` is the FMI primitive — it already exists and works well. `AcquireTokenByUserFederatedIdentityCredential` is the FIC primitive. Together with standard `AcquireTokenForClient` + `WithClientAssertion`, they form a complete toolkit for composing custom FMI/FIC flows.

---

## 3. Usage Examples

**Basic — agent acting as user (by OID):**
```csharp
IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
    .Create(blueprintClientId)
    .WithCertificate(certificate)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .Build();

var result = await cca
    .AcquireTokenForAgent(
        new[] { "https://graph.microsoft.com/.default" },
        new AgentIdentity(agentAppId, userObjectId))
    .ExecuteAsync(cancellationToken);

// result.AccessToken is a user-scoped token for the agent identity
```

**Agent acting as user (by UPN):**
```csharp
var result = await cca
    .AcquireTokenForAgent(
        new[] { "https://graph.microsoft.com/.default" },
        AgentIdentity.WithUsername(agentAppId, "user@contoso.com"))
    .ExecuteAsync();
```

**App-only agent identity (no user):**
```csharp
var result = await cca
    .AcquireTokenForAgent(
        new[] { "api://downstream-api/.default" },
        AgentIdentity.AppOnly(agentAppId))
    .ExecuteAsync();
```

**With optional parameters:**
```csharp
var result = await cca
    .AcquireTokenForAgent(
        new[] { "https://graph.microsoft.com/.default" },
        new AgentIdentity(agentAppId, userObjectId))
    .WithSendX5C(true)
    .WithAttributes("{\"sg1\":\"group-id\"}")
    .WithTenantId(differentTenantId)
    .WithForceRefresh(true)
    .ExecuteAsync(cancellationToken);
```

**FIC primitive — standalone federated credential exchange (using implemented API):**
```csharp
// CCA configured with credentials that authenticate the calling app
var cca = ConfidentialClientApplicationBuilder
    .Create(myAppId)
    .WithClientAssertion(myFederatedAssertionProvider)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .Build();

// Exchange a federated credential for a user-scoped token — by UPN (shipped)
var result = await (cca as IByUserFederatedIdentityCredential)
    .AcquireTokenByUserFederatedIdentityCredential(
        new[] { "https://graph.microsoft.com/.default" },
        "user@contoso.com",
        federatedCredentialToken)
    .ExecuteAsync();

// Exchange a federated credential for a user-scoped token — by OID (proposed Guid overload)
Guid userOid = Guid.Parse(incomingToken.Claims.First(c => c.Type == "oid").Value);
var result = await (cca as IByUserFederatedIdentityCredential)
    .AcquireTokenByUserFederatedIdentityCredential(
        new[] { "https://graph.microsoft.com/.default" },
        userOid,
        federatedCredentialToken)
    .ExecuteAsync();
```

**Manual FMI + FIC composition (custom flow with intermediate token inspection):**
```csharp
// Step 1: FMI-scoped token (existing WithFmiPath primitive)
var leg1 = await blueprintCca
    .AcquireTokenForClient(new[] { "api://AzureAdTokenExchange/.default" })
    .WithFmiPath(agentAppId)
    .ExecuteAsync();

// Step 2: Instance token (standard client_credentials)
var agentCca = ConfidentialClientApplicationBuilder
    .Create(agentAppId)
    .WithClientAssertion(() => Task.FromResult(leg1.AccessToken))
    .WithAuthority(authority)
    .Build();

var leg2 = await agentCca
    .AcquireTokenForClient(new[] { "api://AzureAdTokenExchange/.default" })
    .ExecuteAsync();

// Step 3: FIC exchange for user token (implemented API)
var userToken = await (agentCca as IByUserFederatedIdentityCredential)
    .AcquireTokenByUserFederatedIdentityCredential(
        new[] { "https://graph.microsoft.com/.default" },
        "user@contoso.com",
        leg2.AccessToken)
    .ExecuteAsync();
```

---

## 4. Current Workarounds and How the Proposed APIs Replace Them

Both ID Web and Agents-for-net independently implemented the same three-leg FIC protocol on top of MSAL .NET's extensibility hooks. The proposed `AcquireTokenForAgent` API would replace the full orchestration, and the now-implemented `AcquireTokenByUserFederatedIdentityCredential` already replaces the manual FIC hacks.

### 4.1 Agents-for-net (MsalAuth.cs)

**Current approach**: Three separate methods in `MsalAuth.cs`, each performing one leg of the exchange by directly calling MSAL APIs:

```
GetAgenticApplicationTokenAsync  →  AcquireTokenForClient + WithFmiPath
GetAgenticInstanceTokenAsync     →  new CCA(agentAppId, WithClientAssertion(leg1Token))
                                     → AcquireTokenForClient
GetAgenticUserTokenAsync         →  new CCA(agentAppId, WithClientAssertion(leg1Token))
                                     → AcquireTokenForClient + OnBeforeTokenRequest
                                       (rewrite grant_type=user_fic, inject user_id, inject instance token)
```

**What the package has to manage manually:**
- Constructing 2 additional CCAs (one per leg) with the agent app's client ID and previous leg's token as assertion
- Hardcoding `"api://AzureAdTokenExchange/.default"` as the FIC exchange scope
- Hardcoding `"user_fic"` as the grant type name and manually injecting it via `OnBeforeTokenRequest`
- Manually injecting `user_id` and `user_federated_identity_credential` body parameters
- Configuring logging, cache options, HTTP client factory on every intermediate CCA
- Managing the call sequence (Leg 1 → Leg 2 → Leg 3) and threading tokens between legs

**With the proposed API**, `GetAgenticUserTokenAsync` becomes:

```csharp
public async Task<string> GetAgenticUserTokenAsync(
    string tenantId, string agentAppInstanceId,
    string agenticUserId, IList<string> scopes, CancellationToken ct)
{
    var cca = (IConfidentialClientApplication)InnerCreateClientApplication(tenantId);

    var result = await cca
        .AcquireTokenForAgent(scopes, new AgentIdentity(agentAppInstanceId, Guid.Parse(agenticUserId)))
        .ExecuteAsync(ct);

    return result.AccessToken;
}
```

All three `IAgenticTokenProvider` methods collapse into a single call, and the separate `GetAgenticApplicationTokenAsync` / `GetAgenticInstanceTokenAsync` methods are no longer needed — MSAL handles the intermediate legs internally.

### 4.2 ID Web (AgentUserIdentityMsalAddIn.cs)

**Current approach**: ID Web repurposes the ROPC flow (`IByUsernameAndPassword`) on CCA, then uses a `MsalAuthenticationExtension` with `OnBeforeTokenRequestHandler` to completely rewrite the token request at the last moment:

```
IByUsernameAndPassword.AcquireTokenByUsernamePassword(scopes, username, password)
  → WithAuthenticationExtension(OnBeforeTokenRequestHandler = async (request) => {
        // Leg 1: Get FMI token via ITokenAcquirer.GetFicTokenAsync
        // Leg 2: Get instance token via new ITokenAcquirer with agent ClientId
        // Leg 3 body rewrite:
        //   - Set grant_type = "user_fic"
        //   - Set client_assertion = leg1Token
        //   - Set user_federated_identity_credential = leg2Token
        //   - Set user_id or username
        //   - Remove "password" and "client_secret" from body
    })
```

**What the package has to manage manually:**
- Using ROPC as a "shell" to carry the request, then stripping ROPC-specific parameters (`password`, `client_secret`) in the callback
- Performing Legs 1 and 2 inside an `OnBeforeTokenRequest` callback, which means token acquisition happens inside another token acquisition's request pipeline
- Creating separate `ITokenAcquirer` instances with merged options (agent's ClientId + blueprint's authority/credentials)
- Managing `MicrosoftEntraApplicationOptions` with `CustomSignedAssertion` credentials pointing to an `OidcIdpSignedAssertionProvider`
- Resolving the blueprint's authentication scheme and options through DI to create merged configurations
- Manually setting 6+ body parameters and removing 2 others

**With the proposed API**, the `AgentUserIdentityMsalAddIn` class and its `OnBeforeUserFicForAgentUserIdentityAsync` method would be replaced by a direct call in ID Web's `TokenAcquisition`:

```csharp
var result = await confidentialApp
    .AcquireTokenForAgent(scopes, new AgentIdentity(agentAppId, userObjectId))
    .WithTenantId(tenantId)
    .ExecuteAsync(cancellationToken);
```

The entire `AgentUserIdentityMsalAddIn.cs` file, the `OidcIdpSignedAssertionProvider` for FMI scenarios, the merged options logic in `TokenAcquisition.GetMergedOptions`, and the `ForAgentIdentity` extension method configuration could be significantly simplified or removed.

### 4.3 Summary of What Changes

| Concern | Currently owned by packages | With proposed APIs |
|---------|---------------------------|-------------------|
| Three-leg orchestration | Package code (60-80 lines each) | MSAL internal |
| FIC exchange scope (`api://AzureAdTokenExchange/.default`) | Hardcoded in package code | MSAL internal constant |
| `user_fic` grant type | Injected via `OnBeforeTokenRequest` | MSAL internal |
| `user_federated_identity_credential` parameter | Injected via `OnBeforeTokenRequest` | MSAL internal |
| Intermediate CCA construction | Package creates 2 extra CCAs per request | MSAL internal |
| Token chaining (Leg N output → Leg N+1 input) | Package manages manually | MSAL internal |
| Intermediate token caching | Inconsistent (Agents-for-net rebuilds CCAs; ID Web uses ITokenAcquirer) | MSAL caches all legs with proper keys |
| Protocol knowledge required by package | Must know FIC wire protocol details | Only needs `AgentIdentity(appId, userId)` |
| Blueprint credential management | Package configures the CCA | **Unchanged** — package still configures the blueprint CCA |
| Agent app ID / user ID | Package provides per-request | **Unchanged** — package passes via `AgentIdentity` |
| Target scopes | Package provides per-request | **Unchanged** — package passes as `scopes` parameter |
