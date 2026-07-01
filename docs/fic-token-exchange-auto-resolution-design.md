# Design: Auto-Resolution of Cloud-Specific FIC Token Exchange Audiences

FIC (Federated Identity Credential) and agentic authentication scenarios require a cloud-specific "token exchange audience" URI (e.g., `api://AzureADTokenExchange` for public cloud, `api://AzureADTokenExchangeUSGov` for US Government). Today, all three SDKs either hardcode the public cloud value or require customers to configure it manually, leading to silent misconfiguration failures that only surface deep in the token exchange flow.

This proposal adds automatic audience resolution to MSAL .NET's existing instance discovery metadata infrastructure, keyed by authority host. Since all FIC token acquisition in ID Web and MISE flows through MSAL, this single change can eliminate the configuration burden across the entire stack.

### Contents

- [Problem](#problem)
- [How the Token Exchange Audience Is Used Today](#how-the-token-exchange-audience-is-used-today)
- [Proposed Design](#proposed-design)
- [Open Questions](#open-questions)
- [Extensibility for Other Cloud-Specific Strings](#extensibility-for-other-cloud-specific-strings)
- [Test Strategy](#test-strategy)

---

## Problem

Currently, a customer using FIC must understand what the token exchange audience is and which cloud-specific endpoint to configure for the application. Some examples:

| Cloud | Required Audience |
|-------|-------------------|
| Public (Azure AD) | `api://AzureADTokenExchange` |
| US Government | `api://AzureADTokenExchangeUSGov` |
| China | `api://AzureADTokenExchangeChina` |
| Bleu (France) | `api://AzureADTokenExchangeFrance` |
| Delos (Germany) | `api://AzureADTokenExchangeGermany` |

**Why this is painful:**

1. A wrong-cloud audience (e.g., USGov audience used in public cloud) still leads to a successful response during the initial MI/client-credential call that acquires the assertion token. The failure only appears when ESTS validates the assertion's `aud` against the app's FIC entry and returns `AADSTS70022204: No matching federated identity record found for presented assertion audience`. This makes misconfiguration difficult to diagnose.

2. The same application code must select the correct audience at runtime depending on the deployment environment, but there is no SDK-provided mechanism to do so: the correct audience cannot be derived from instance discovery, OIDC metadata, or any other endpoint MSAL currently calls. It must be known at configuration time.

---

## How the Token Exchange Audience Is Used Today

### MSAL .NET (the token acquisition layer)

MSAL treats the audience as an opaque resource/scope string with no special handling. It enters via two code paths:

| Path | API | Input form | HTTP parameter | Target |
|------|-----|-----------|----------------|--------|
| Managed Identity | `AcquireTokenForManagedIdentity` | `"api://AzureADTokenExchange"` | `resource` (query param) | Local MI endpoint (IMDS, App Service, etc.) |
| Client Credentials | `AcquireTokenForClient` | `["api://AzureADTokenExchange/.default"]` | `scope` (POST body) | ESTS |

Key difference: the MI path strips `/.default` via `ScopeHelper.RemoveDefaultSuffixIfPresent()`; the client credentials path preserves it.

MSAL already maintains per-cloud metadata in `KnownMetadataProvider`: nine `InstanceDiscoveryMetadataEntry` instances mapping authority host aliases to `PreferredNetwork` and `PreferredCache`. This is the natural home for the audience mapping.

### ID Web (higher-level wrapper)

Two entry points, both flowing into MSAL:

- **`ManagedIdentityClientAssertion`**: Defaults to `"api://AzureADTokenExchange"` via `CertificatelessConstants.DefaultTokenExchangeUrl`, passed to MSAL's `AcquireTokenForManagedIdentity`.
- **`GetFicTokenAsync`**: Default parameter `scope = "api://AzureAdTokenExchange/.default"`, passed to MSAL's `AcquireTokenForClient`.

ID Web has no cloud-specific mapping infrastructure of its own.

### MISE (internal SDK)

- `UserFicClaimsPrincipalFactory` hardcodes `"api://AzureADTokenExchange"` inline and flows through ID Web's `ManagedIdentityClientAssertion` → MSAL.
- Has `CloudEnvironmentInformation` (maps authority URLs to cloud identifiers for 9 clouds), but it is in a separate assembly (`AttributeTokens`) and inaccessible from `UserFic`.
- Maintains a small number of **internal-only** audiences for certain clouds that cannot be stored in public GitHub repos.

### Key Observation

Every token exchange audience usage in ID Web and MISE flows through MSAL's `AcquireTokenForManagedIdentity` or `AcquireTokenForClient`. If MSAL resolves the correct audience, the entire stack benefits without per-SDK changes.

---

## Proposed Design

### Core Principle

**MSAL owns publicly known audiences. MISE owns internal-only audiences. ID Web delegates entirely.**

### MSAL .NET: Add `TokenExchangeAudience` to instance discovery metadata

Add a property to `InstanceDiscoveryMetadataEntry`:

```csharp
/// <summary>
/// Cloud-specific token exchange audience URI for FIC scenarios.
/// Null for clouds without a known token exchange application.
/// Not serialized, hardcoded entries only.
/// </summary>
public string TokenExchangeAudience { get; set; }
```

This will be set on any entry with a token exchange endpoint we can reference publicly, for example:

| Cloud Entry | Authority Aliases | `TokenExchangeAudience` |
|-------------|-------------------|------------------------|
| Public | `login.microsoftonline.com`, `login.windows.net`, `login.microsoft.com`, `sts.windows.net` | `api://AzureADTokenExchange` |
| USGov | `login.microsoftonline.us`, `login.usgovcloudapi.net` | `api://AzureADTokenExchangeUSGov` |
| China | `login.partner.microsoftonline.cn`, `login.chinacloudapi.cn` | `api://AzureADTokenExchangeChina` |
| Bleu | `login.sovcloud-identity.fr` | `api://AzureADTokenExchangeFrance` |
| Delos | `login.sovcloud-identity.de` | `api://AzureADTokenExchangeGermany` |

### MSAL .NET: Lookup API

```csharp
// On KnownMetadataProvider
public static bool TryGetTokenExchangeAudience(
    string authorityHost, out string tokenExchangeAudience)
```

Normalizes the host to lowercase, looks up the entry, and returns the audience if present.

### MSAL .NET: Auto-resolution integration point

**See [Open Questions](#open-questions).** We must decide whether MSAL overrides the customer-provided value with our internal mapping, or only resolves it if the value was not provided.

Regardless, the resolution must be applied at the point where the scope/resource enters the request pipeline, accounting for the `/.default` suffix difference between MI and client credentials paths.

### ID Web

- **If MSAL always overrides the configuration:** No code change required, ID Web can continue passing the public-cloud default and MSAL will simply override it. However, this behavior should still be cleaned up and properly logged to avoid a confusing developer experience.
- **If MSAL auto-resolves only if not set:** In order to support FIC in non-public clouds, ID Web must stop passing a default and instead rely on MSAL's auto-resolution.

### MISE

- **Public clouds:** Delegates to MSAL (no change needed for the common case).
- **Internal-only clouds:** MISE detects the cloud (via authority host or `CloudEnvironmentInformation`) and passes the audience explicitly, which MSAL uses as-is (explicit caller value takes precedence over auto-resolution).

---

## Open Questions

### 1. Override behavior: how should MSAL apply the resolved audience?

MSAL has several precedents for silently modifying caller-provided values:

| Existing behavior | Communication to caller |
|-------------------|------------------------|
| Authority host → `PreferredNetwork` rewrite (instance discovery) | `AuthenticationResultMetadata.TokenEndpoint` |
| Reserved scopes (`openid`, `profile`, `offline_access`) silently added | None |
| `MSAL_DISABLE_REGION` / `MSAL_FORCE_REGION` env vars override developer config | `RegionDetails` metadata |
| Client capabilities merged into claims | Logged in request params |

**Options for token exchange audience:**

| Option | Behavior | Pros | Cons |
|--------|----------|------|------|
| **1. Fill-in-the-blank** | Resolve only when caller passes `null`/sentinel | Zero breaking changes; clear opt-in | Requires changes in ID Web/MISE/any other MSAL customer as they currently always pass a value |
| **2. Silent override** | Always resolve from internal mapping, override caller value, log if different | Works without upstream changes; matches authority rewriting pattern | Surprising if customer intentionally uses a non-standard audience |

**Current leaning:** Option 1. Option 2 is simplest but removes control from the caller, and any new token exchange endpoint requires a new MSAL release and updates by every customer. Option 1 gives the most flexible developer experience (either they don't need to worry about it, or they explicitly tells MSAL what to use) and can be used as a workaround if MSAL isn't properly handling an existing endpoint or hasn't yet been updated to handle a new scenario.

### 2. How will MISE manage internal-only audiences?

| Option | Description | Trade-off |
|--------|-------------|-----------|
| **1. Selective override** | MISE passes explicit audience only for internal clouds; omits/passes null for public clouds (MSAL resolves) | Clean separation; no duplication; requires MISE to detect cloud |
| **2. Full self-contained mapping** | MISE maintains its own complete mapping (public + internal) and always passes explicitly | Fully decoupled from MSAL; duplicates public mappings |

**Current leaning:** Option 1. MISE ultimately relies on MSAL for token acquisition in scenarios where the token exchange endpoint is relevant, so like any other customer they only need to worry about scenarios where MSAL can't auto-resolve the correct endpoint.

### 3. `/.default` suffix handling

Auto-resolution must produce:
- `"api://AzureADTokenExchange{Suffix}"` for the MI path (which strips `/.default` anyway)
- `"api://AzureADTokenExchange{Suffix}/.default"` for the client credentials path

The stored `TokenExchangeAudience` is the **base URI without `/.default`**. The integration point must append `/.default` when appropriate based on which code path is active.

---

## Extensibility for Other Cloud-Specific Strings

### Pattern applicability

Adding `TokenExchangeAudience` to `InstanceDiscoveryMetadataEntry` establishes a pattern: **cloud-specific application-layer metadata stored alongside transport-layer metadata** (`PreferredNetwork`, `PreferredCache`). This could extend to other cloud-specific magic strings (e.g., Graph endpoints, first-party resource identifiers).

However, if we expect to have more and more of these cloud-specific properties then a dedicated abstraction class (`CloudConfiguration`) may be more maintainable than continuing to grow the instance discovery metadata entry.

### Other MSALs

We should have no issues with SDK consistency: our other MSALs have equivalent behavior to MSAL .NET's `KnownMetadataProvider`/`InstanceDiscoveryMetadataEntry` classes for storing cloud-specific metadata, and each recently received FIC support to match the support in MSAL .NET.

---

## Test Strategy

### What we can validate

| Layer | Approach |
|-------|----------|
| **Mapping correctness** | Unit tests: all known clouds, aliases, case-insensitivity, unknown hosts |
| **No interference** | Unit tests: network-fetched and user-provided entries remain `null` |
| **Wrong-audience behavior** | Integration tests: prove `AADSTS70022204` with mismatched audience |
| **Auto-resolution in MI path** | Mock MI endpoint; assert `resource` query param contains resolved audience |
| **Auto-resolution in CC path** | Mock ESTS; assert `scope` body param contains resolved audience + `/.default` |
| **Regionalized authorities** | Unit test: verify regional host still resolves to correct cloud |
| **End-to-end success** | Integration test: auto-resolved audience works in live public cloud |
| **Cross-repo** | Pack MSAL locally → consume in ID Web/MISE tests manually |

### Known gaps

- **No sovereign cloud test resources.** Lab tenant is public-cloud-only. Sovereign cloud audiences can only be verified at the mapping/string level, not end-to-end.
- **MI endpoint behavior is mocked.** Unit tests verify MSAL sends the correct `resource` parameter, but cannot confirm the MI infrastructure accepts it (that's an Azure fabric concern, not an SDK concern).
- **Cross-repo manual validation.** Will help catch issues in the initial implementation, but will not be a maintainable solution going forward.
