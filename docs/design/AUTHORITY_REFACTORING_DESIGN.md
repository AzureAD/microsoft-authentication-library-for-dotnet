# Authority Refactoring Design Document

**Status:** Proposed  
**Authors:** MSAL.NET Team  
**Last Updated:** 2026-02-26

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Motivation and Goals](#motivation-and-goals)
3. [Current Architecture](#current-architecture)
4. [Proposed Architecture](#proposed-architecture)
5. [Authority Detection and Normalization](#authority-detection-and-normalization)
6. [Integration Points](#integration-points)
7. [Test Strategy](#test-strategy)
8. [Backwards Compatibility](#backwards-compatibility)
9. [Extensibility Example](#extensibility-example)
10. [Rollout Plan](#rollout-plan)
11. [Risk Analysis](#risk-analysis)
12. [Success Criteria](#success-criteria)
13. [Appendices](#appendices)

---

## Executive Summary

The current authority handling in MSAL.NET is distributed across multiple classes (`Authority`, `AuthorityInfo`, `AuthorityInfoHelper`, `AuthorityManager`) with type-detection and creation logic duplicated or scattered throughout. As new authority types have been added (ADFS, B2C, DSTS, CIAM, Generic), the surface area has grown organically, making it increasingly difficult to:

- Add a new authority type without touching multiple files
- Reason about the full lifecycle of an authority (detection → normalization → validation → resolution)
- Write isolated unit tests for individual pipeline steps

This document proposes a **Registry + Pipeline** architecture that centralizes authority metadata in an `AuthorityRegistry`, drives all creation through a deterministic 6-step `AuthorityCreationPipeline`, and exposes clean extension points for new authority types.

The refactoring is designed to produce **zero functional regressions**: every observable behavior (token endpoints, tenant handling, environment override, MSA passthrough, error messages) is preserved and validated by a comprehensive characterization test suite.

---

## Motivation and Goals

### Problems with the Current Design

| Problem | Location | Impact |
|---|---|---|
| Type detection duplicated | `AuthorityInfo` + `Authority.CreateAuthority` | Risk of divergence when new types are added |
| Authority normalization mixed with validation | `AuthorityManager` | Hard to test normalization in isolation |
| No single source of truth for "what is an AAD authority?" | Spread across 4+ files | Onboarding cost, maintenance risk |
| Adding a new authority type requires changes in 5+ files | Entire `Instance/` subtree | High blast radius for new cloud types |
| Instance discovery and authority validation tightly coupled | `AuthorityManager` | Cannot mock one without the other |

### Goals

1. **Single registry** — one place (`AuthorityRegistry`) that maps an authority type to its detection predicate, factory, validator, and resolver.
2. **Deterministic pipeline** — all authority creation flows through a documented 6-step `AuthorityCreationPipeline`.
3. **Zero regressions** — characterization tests capture every observable behavior before the first line of production code changes.
4. **Additive extensibility** — adding a new authority type requires changes only in the registry registration, not in the pipeline or callers.
5. **Improved testability** — each pipeline step is independently unit-testable.

---

## Current Architecture

See [AUTHORITY_REFACTORING_ARCHITECTURE.md](AUTHORITY_REFACTORING_ARCHITECTURE.md) for detailed diagrams.

### Key Classes

| Class | Responsibility |
|---|---|
| `AuthorityInfo` | Immutable data object: canonical URI, type enum, validation flag |
| `AuthorityInfo.AuthorityInfoHelper` | Nested class handling config-vs-request authority merging and environment overrides |
| `Authority` (abstract) | Abstract base class; houses factory methods `CreateAuthority()` and `CreateAuthorityForRequestAsync()` |
| `AadAuthority` | AAD-specific endpoint construction and tenant handling |
| `B2CAuthority` | B2C user-flow authority; extends `AadAuthority` |
| `AdfsAuthority` | ADFS authority with WebFinger-based validation |
| `DstsAuthority` | DSTS (Distributed Token Service) authority |
| `CiamAuthority` | CIAM (Customer Identity Access Management) authority; extends `AadAuthority` |
| `GenericAuthority` | Pass-through for custom / OIDC-compliant endpoints |
| `AuthorityManager` | Per-request orchestrator: instance discovery, validation, environment resolution |
| `AadAuthorityValidator` | Validates AAD authority against instance discovery metadata |
| `AdfsAuthorityValidator` | Validates ADFS authority via WebFinger |
| `NullAuthorityValidator` | No-op validator for types that do not require network validation |

### Current Creation Flow

```
User calls PublicClientApplicationBuilder.WithAuthority(uri)
  └── AuthorityInfo.FromAuthorityUri(uri)
        ├── Normalize URI
        ├── Detect type (switch on host/path heuristics)
        └── new AuthorityInfo(type, canonicalUri, ...)
  
At request time:
  AuthorityManager.GetRequestAuthorityAsync()
    ├── AuthorityInfoHelper.BuildAuthorityForRequest()
    │     ├── Merge config-level vs request-level authority
    │     ├── Apply environment override (multi-cloud)
    │     └── Validate host consistency
    ├── RunInstanceDiscoveryAsync()
    │     └── Fetch cloud metadata / preferred network / aliases
    └── Authority.CreateAuthority(mergedInfo)
          └── switch(type) → new AadAuthority / B2CAuthority / ...
```

### Pain Points Illustrated

The type-detection logic in `AuthorityInfo` (~line 418–443) and the factory switch in `Authority.CreateAuthority` are the two most fragile points. Any new authority type must be added to both independently, with no compile-time enforcement that they stay in sync.

---

## Proposed Architecture

### Overview

```
AuthorityRegistry  ──registers──►  AuthorityRegistration[]
                                        │
                                        ▼
                              AuthorityCreationPipeline
                                   (6 steps)
                                        │
                                        ▼
                                  Authority instance
```

### AuthorityRegistration

Each authority type is described by a single `AuthorityRegistration` record:

```csharp
public sealed class AuthorityRegistration
{
    /// <summary>Unique identifier for this authority type.</summary>
    public AuthorityType Type { get; init; }

    /// <summary>
    /// Returns true if the given raw authority URI belongs to this type.
    /// Evaluated in registration order; first match wins.
    /// </summary>
    public Func<Uri, bool> DetectionPredicate { get; init; }

    /// <summary>Constructs the concrete Authority subclass from AuthorityInfo.</summary>
    public Func<AuthorityInfo, Authority> Factory { get; init; }

    /// <summary>
    /// Validates that the authority is known/trusted.
    /// May perform network calls (e.g., WebFinger for ADFS).
    /// </summary>
    public IAuthorityValidator Validator { get; init; }

    /// <summary>
    /// Resolves the preferred canonical URI after instance discovery
    /// (e.g., maps to regional endpoint for AAD).
    /// </summary>
    public IAuthorityResolver Resolver { get; init; }
}
```

### AuthorityRegistry

```csharp
public static class AuthorityRegistry
{
    private static readonly IReadOnlyList<AuthorityRegistration> s_registrations;

    static AuthorityRegistry()
    {
        s_registrations = new[]
        {
            DstsAuthorityRegistration.Create(),
            B2CAuthorityRegistration.Create(),
            AdfsAuthorityRegistration.Create(),
            CiamAuthorityRegistration.Create(),
            AadAuthorityRegistration.Create(),   // must be near-last: broad match
            GenericAuthorityRegistration.Create() // catch-all
        };
    }

    public static AuthorityRegistration Detect(Uri authorityUri)
    {
        foreach (var reg in s_registrations)
        {
            if (reg.DetectionPredicate(authorityUri))
                return reg;
        }
        throw new MsalClientException(
            MsalError.InvalidAuthority,
            $"No registered authority type matches '{authorityUri}'.");
    }

    public static AuthorityRegistration Get(AuthorityType type)
        => s_registrations.Single(r => r.Type == type);
}
```

### AuthorityCreationPipeline (6-Step)

| Step | Name | Description |
|---|---|---|
| 1 | **Parse** | Parse raw string to `Uri`; throw `MsalClientException` on malformed input |
| 2 | **Detect** | Call `AuthorityRegistry.Detect(uri)` to identify the `AuthorityRegistration` |
| 3 | **Normalize** | Apply type-specific URI normalization (trailing slash, lowercase host, strip query) |
| 4 | **Merge** | Combine config-level and request-level authority, applying environment override |
| 5 | **Validate** | Run `registration.Validator.ValidateAsync()` (network call if required) |
| 6 | **Construct** | Call `registration.Factory(normalizedInfo)` to produce the `Authority` instance |

```csharp
public sealed class AuthorityCreationPipeline
{
    private readonly IInstanceDiscoveryManager _discoveryManager;
    private readonly RequestContext _requestContext;

    public async Task<Authority> CreateAsync(
        string rawAuthorityUri,
        AuthorityInfo configAuthority,
        AuthorityOverride requestOverride)
    {
        // Step 1: Parse
        var uri = ParseUri(rawAuthorityUri);

        // Step 2: Detect
        var registration = AuthorityRegistry.Detect(uri);

        // Step 3: Normalize
        var normalized = registration.Normalizer.Normalize(uri);

        // Step 4: Merge
        var merged = AuthorityMerger.Merge(configAuthority, normalized, requestOverride);

        // Step 5: Validate
        await registration.Validator.ValidateAsync(merged, _discoveryManager, _requestContext)
            .ConfigureAwait(false);

        // Step 6: Construct
        return registration.Factory(merged);
    }
}
```

---

## Authority Detection and Normalization

### Detection Predicates

Each authority type has a deterministic predicate evaluated in the order shown. First match wins.

| Order | Type | Detection Rule |
|---|---|---|
| 1 | **DSTS** | Host contains `dstsv2` or host ends with `.dsts.core.windows.net` |
| 2 | **B2C** | Path segments contain a segment matching `b2c_1_*` user-flow pattern, OR host contains `.b2clogin.com` |
| 3 | **ADFS** | Path starts with `/adfs` (case-insensitive) |
| 4 | **CIAM** | Host ends with `.ciamlogin.com` |
| 5 | **AAD** | Host is a known AAD endpoint (`login.microsoftonline.com`, `login.windows.net`, etc.) OR matches instance discovery metadata |
| 6 | **Generic** | Catch-all — any remaining URI (OIDC-compliant custom endpoints) |

### Normalization Rules

All authority URIs are normalized to a canonical form before storage:

1. Scheme forced to `https://`
2. Host lowercased
3. Trailing slash appended if absent
4. Query string and fragment stripped
5. Path segments reduced to at most two (tenant-specific authorities: `/{host}/{tenant}/`)

---

## Integration Points

### 1. `ApplicationConfiguration` → `AuthorityInfo`

No change to the public API surface. `ConfidentialClientApplicationBuilder.WithAuthority(string)` and `PublicClientApplicationBuilder.WithAuthority(string)` continue to accept raw strings. Internally, the string is handed to the new `AuthorityCreationPipeline` (Step 1 + Step 2 only at build time; Steps 3–6 deferred to request time).

### 2. `AuthorityManager.GetRequestAuthorityAsync()`

This is the primary integration point. The existing method is refactored to delegate to `AuthorityCreationPipeline.CreateAsync()`, which executes all 6 steps.

```csharp
// Before
public async Task<Authority> GetRequestAuthorityAsync()
{
    var mergedInfo = AuthorityInfoHelper.BuildAuthorityForRequest(...);
    await RunInstanceDiscoveryAsync(mergedInfo);
    return Authority.CreateAuthority(mergedInfo);
}

// After
public async Task<Authority> GetRequestAuthorityAsync()
{
    return await _pipeline.CreateAsync(
        _initialAuthority.CanonicalAuthority.ToString(),
        _appConfig.Authority,
        _requestOverride).ConfigureAwait(false);
}
```

### 3. MSA Passthrough

MSA passthrough (consumer Microsoft accounts) uses a special tenant value (`consumers`) injected into the authority path. This is handled in Step 4 (Merge) of the pipeline via an `MsaPassthroughResolver` that intercepts AAD authorities when `AcquireTokenSilentParameterBuilder.WithAccount()` is called with a personal account.

**Preservation guarantee:** The test `AcquireTokenSilent_MsaPassthrough_UsesConsumersTenant` is a characterization test that must pass before and after refactoring.

### 4. Environment Override (Multi-Cloud)

When the application is configured for a sovereign cloud or when `WithInstanceDiscoveryMetadata()` provides custom endpoint metadata, the resolved authority may differ from the configured authority. This is handled in Step 4 (Merge) via `EnvironmentOverrideApplicator`.

**Preservation guarantee:** The test `Authority_WithEnvironmentOverride_UpdatesHost` is a characterization test that must pass before and after refactoring.

### 5. Error Messages

All `MsalClientException` and `MsalServiceException` error messages thrown during authority processing must remain identical to preserve backward compatibility with callers that parse error messages (not recommended but common in the wild).

---

## Test Strategy

### Characterization Tests

Before any production code changes, a characterization test suite must be written that captures all observable behaviors. These tests serve as the regression net.

#### Category 1: Detection Tests

```csharp
[TestClass]
public class AuthorityDetectionCharacterizationTests
{
    [DataRow("https://login.microsoftonline.com/tenantid", AuthorityType.Aad)]
    [DataRow("https://login.windows.net/tenantid", AuthorityType.Aad)]
    [DataRow("https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/b2c_1_signupsignin", AuthorityType.B2C)]
    [DataRow("https://contoso.ciamlogin.com/contoso.onmicrosoft.com", AuthorityType.Ciam)]
    [DataRow("https://adfs.contoso.com/adfs", AuthorityType.Adfs)]
    [DataRow("https://dsts.core.azure-test.net/dstsv2/tenantid", AuthorityType.Dsts)]
    [DataRow("https://custom.idp.example.com/", AuthorityType.Generic)]
    [DataTestMethod]
    public void AuthorityType_IsDetectedCorrectly(string authorityUri, AuthorityType expectedType)
    {
        var info = AuthorityInfo.FromAuthorityUri(authorityUri, validateAuthority: false);
        Assert.AreEqual(expectedType, info.AuthorityType);
    }
}
```

#### Category 2: Normalization Tests

```csharp
[TestClass]
public class AuthorityNormalizationCharacterizationTests
{
    [DataRow("https://Login.MicrosoftOnline.COM/TenantId/",
             "https://login.microsoftonline.com/tenantid/")]
    [DataRow("https://login.microsoftonline.com/tenantid?foo=bar",
             "https://login.microsoftonline.com/tenantid/")]
    [DataTestMethod]
    public void Authority_IsNormalizedToCanonicalForm(string input, string expectedCanonical)
    {
        var info = AuthorityInfo.FromAuthorityUri(input, validateAuthority: false);
        Assert.AreEqual(expectedCanonical, info.CanonicalAuthority.ToString());
    }
}
```

#### Category 3: Endpoint Construction Tests

```csharp
[TestClass]
public class AuthorityEndpointCharacterizationTests
{
    [TestMethod]
    public async Task AadAuthority_TokenEndpoint_IsCorrect()
    {
        var authority = Authority.CreateAuthority("https://login.microsoftonline.com/tenantid");
        var endpoint = await authority.GetTokenEndpointAsync(null).ConfigureAwait(false);
        Assert.AreEqual("https://login.microsoftonline.com/tenantid/oauth2/v2.0/token", endpoint);
    }

    [TestMethod]
    public async Task B2CAuthority_TokenEndpoint_IncludesUserFlow()
    {
        var authority = Authority.CreateAuthority(
            "https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/b2c_1_susi");
        var endpoint = await authority.GetTokenEndpointAsync(null).ConfigureAwait(false);
        Assert.IsTrue(endpoint.Contains("b2c_1_susi"));
    }
}
```

#### Category 4: MSA Passthrough Tests

```csharp
[TestClass]
public class MsaPassthroughCharacterizationTests
{
    [TestMethod]
    public async Task AcquireTokenSilent_MsaPassthrough_UsesConsumersTenant()
    {
        // Arrange: personal account with home tenant "9188040d-..."
        var account = new Account("uid.9188040d-6c67-4c5b-b112-36a304b66dad",
                                  "user@outlook.com", "login.microsoftonline.com");
        var app = PublicClientApplicationBuilder
            .Create(TestConstants.ClientId)
            .WithAuthority("https://login.microsoftonline.com/common")
            .BuildConcrete();

        // Act: silent token acquisition with MSA home tenant
        var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
            () => app.AcquireTokenSilent(TestConstants.s_scope, account)
                     .ExecuteAsync()).ConfigureAwait(false);

        // Assert: request was sent to /consumers/, not /common/
        Assert.IsTrue(ex.Claims?.Contains("consumers") == true ||
                      ex.Message.Contains("consumers"));
    }
}
```

#### Category 5: Environment Override Tests

```csharp
[TestClass]
public class EnvironmentOverrideCharacterizationTests
{
    [TestMethod]
    public void Authority_WithEnvironmentOverride_UpdatesHost()
    {
        var original = Authority.CreateAuthority(
            "https://login.microsoftonline.com/tenantid");
        var updated = original.CreateAuthorityWithEnvironment("login.microsoftonline.us");
        Assert.AreEqual("login.microsoftonline.us", updated.AuthorityInfo.Host);
        Assert.AreEqual("tenantid", updated.TenantId);
    }
}
```

#### Category 6: Error Message Tests

```csharp
[TestClass]
public class AuthorityErrorMessageCharacterizationTests
{
    [TestMethod]
    public void InvalidAuthority_ThrowsWithExpectedMessage()
    {
        var ex = Assert.ThrowsException<MsalClientException>(
            () => AuthorityInfo.FromAuthorityUri("not-a-valid-uri", validateAuthority: true));
        Assert.AreEqual(MsalError.InvalidAuthority, ex.ErrorCode);
    }

    [TestMethod]
    public void MismatchedAuthority_ThrowsWithExpectedMessage()
    {
        // Config-level AAD authority vs request-level B2C authority
        var ex = Assert.ThrowsException<MsalClientException>(() =>
        {
            var info = AuthorityInfo.FromAuthorityUri(
                "https://login.microsoftonline.com/tenantid", validateAuthority: false);
            AuthorityInfoHelper.ValidateAuthorityInfoForRequest(
                info,
                AuthorityInfo.FromAuthorityUri(
                    "https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/b2c_1_susi",
                    validateAuthority: false));
        });
        StringAssert.Contains(ex.Message, MsalErrorMessage.AuthorityTypeMismatch);
    }
}
```

### Test Execution Policy

1. All characterization tests must pass on `main` before any production code changes are merged.
2. All characterization tests must continue to pass on each commit of the implementation PR.
3. The implementation PR must not decrease overall code coverage for the `Instance/` namespace.

---

## Backwards Compatibility

### Public API Surface

No public API changes are introduced by this refactoring. The following public members are preserved unchanged:

| Member | Preserved |
|---|---|
| `PublicClientApplicationBuilder.WithAuthority(string)` | Yes |
| `ConfidentialClientApplicationBuilder.WithAuthority(string)` | Yes |
| `AcquireTokenSilentParameterBuilder.WithAuthority(string)` | Yes |
| `AuthenticationResult.TenantId` | Yes |
| `AuthenticationResult.Account.Environment` | Yes |
| `MsalError.InvalidAuthority` | Yes |
| `MsalError.AuthorityTypeMismatch` | Yes |

### Internal API Surface

Selected internal types are refactored. Callers within the MSAL.NET codebase are updated atomically in the same PR.

### Behavioral Preservation

All behaviors captured in the characterization test suite (see [Test Strategy](#test-strategy)) are guaranteed to be preserved. This includes:

- Token endpoint construction for all 6 authority types
- Tenant injection and `GetTenantedAuthority()` semantics
- Environment override (multi-cloud authority aliasing)
- MSA passthrough (`consumers` tenant injection)
- Instance discovery caching and `s_validatedEnvironments` semantics
- All error codes and messages from `MsalError` and `MsalErrorMessage`

---

## Extensibility Example

Adding a new authority type — for example, a hypothetical `FabrikamAuthority` — requires changes in only one place:

```csharp
// 1. Define the registration (new file: FabrikamAuthorityRegistration.cs)
internal static class FabrikamAuthorityRegistration
{
    public static AuthorityRegistration Create() => new AuthorityRegistration
    {
        Type = AuthorityType.Fabrikam,                          // add to enum
        DetectionPredicate = uri =>
            uri.Host.EndsWith(".fabrikam.example.com", StringComparison.OrdinalIgnoreCase),
        Factory = info => new FabrikamAuthority(info),          // new Authority subclass
        Validator = new NullAuthorityValidator(),               // or custom validator
        Resolver = new PassthroughAuthorityResolver()           // or custom resolver
    };
}

// 2. Register in AuthorityRegistry (one line in the static constructor)
s_registrations = new[]
{
    DstsAuthorityRegistration.Create(),
    B2CAuthorityRegistration.Create(),
    AdfsAuthorityRegistration.Create(),
    CiamAuthorityRegistration.Create(),
    FabrikamAuthorityRegistration.Create(),   // ← new line
    AadAuthorityRegistration.Create(),
    GenericAuthorityRegistration.Create()
};
```

No changes are needed in `AuthorityInfo`, `Authority`, `AuthorityManager`, or any pipeline step.

---

## Rollout Plan

### Phase 1: Characterization Tests (No Production Code Changes)

**Goal:** Establish a regression safety net before any refactoring.

**Deliverables:**
- All 6 categories of characterization tests committed to `tests/Microsoft.Identity.Test.Unit/CoreTests/AuthorityTests/`
- All tests passing on `main`
- Code coverage baseline recorded

**Acceptance criteria:** PR approved with all tests green on CI.

### Phase 2: Registry and Pipeline Infrastructure (Internal Only)

**Goal:** Introduce `AuthorityRegistration`, `AuthorityRegistry`, and `AuthorityCreationPipeline` as new internal types without changing any callers.

**Deliverables:**
- `src/client/Microsoft.Identity.Client/Instance/AuthorityRegistration.cs`
- `src/client/Microsoft.Identity.Client/Instance/AuthorityRegistry.cs`
- `src/client/Microsoft.Identity.Client/Instance/AuthorityCreationPipeline.cs`
- `src/client/Microsoft.Identity.Client/Instance/IAuthorityResolver.cs`
- Unit tests for the registry detection logic

**Acceptance criteria:**
- All characterization tests still passing
- New unit tests for registry detection passing
- No existing tests broken

### Phase 3: Wire Up Pipeline in AuthorityManager

**Goal:** Replace the existing creation logic in `AuthorityManager.GetRequestAuthorityAsync()` with a call to `AuthorityCreationPipeline.CreateAsync()`.

**Deliverables:**
- `AuthorityManager` updated to use the pipeline
- `AuthorityInfoHelper` deprecated (methods moved to pipeline steps)
- All existing unit tests and integration tests passing

**Acceptance criteria:**
- All characterization tests passing
- No existing tests broken
- Code coverage for `Instance/` namespace maintained or improved

### Phase 4: Cleanup

**Goal:** Remove legacy code paths made obsolete by the pipeline.

**Deliverables:**
- Remove deprecated `AuthorityInfoHelper` methods
- Remove duplicate type-detection switch statements from `AuthorityInfo` and `Authority`
- Update inline documentation
- Update this design document to reflect final implementation

**Acceptance criteria:**
- All tests passing
- No `[Obsolete]` members remaining in the `Instance/` namespace
- SonarQube code smell count for `Instance/` namespace reduced

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Behavioral regression in authority detection | Medium | High | Characterization tests (Phase 1) executed before any code change |
| MSA passthrough broken | Low | High | Dedicated characterization test; manual E2E test with personal account |
| Environment override broken for sovereign clouds | Low | High | Dedicated characterization test; E2E test against US Government cloud |
| Instance discovery cache invalidated unexpectedly | Low | Medium | Cache key equality tests; compare `s_validatedEnvironments` before and after |
| Error message format changed, breaking callers that parse messages | Low | Medium | Error message characterization tests |
| ADFS WebFinger validation bypassed | Very Low | High | ADFS validator is registered explicitly; null validator cannot be substituted |
| Performance regression from pipeline overhead | Very Low | Low | Pipeline steps are synchronous except Step 5 (validator); benchmark before/after |

---

## Success Criteria

The refactoring is considered successful when:

1. All characterization tests (Phase 1) pass before and after implementation.
2. No new test failures are introduced relative to `main`.
3. Adding a new authority type requires changes to at most 2 files: the registration file and `AuthorityRegistry`.
4. The `Instance/` namespace has no duplicate type-detection logic.
5. Code coverage for `Instance/` namespace is maintained or improved.
6. The design review is approved by at least two MSAL.NET maintainers.

---

## Appendices

### Appendix A: Key Source File References

| File | Path |
|---|---|
| `Authority.cs` | `src/client/Microsoft.Identity.Client/Instance/Authority.cs` |
| `AuthorityInfo.cs` | `src/client/Microsoft.Identity.Client/AppConfig/AuthorityInfo.cs` |
| `AuthorityManager.cs` | `src/client/Microsoft.Identity.Client/Instance/AuthorityManager.cs` |
| `AadAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/AadAuthority.cs` |
| `B2CAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/B2CAuthority.cs` |
| `AdfsAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/AdfsAuthority.cs` |
| `DstsAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/DstsAuthority.cs` |
| `CiamAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/CiamAuthority.cs` |
| `GenericAuthority.cs` | `src/client/Microsoft.Identity.Client/Instance/GenericAuthority.cs` |
| `AuthorityType.cs` (enum) | `src/client/Microsoft.Identity.Client/AppConfig/AuthorityType.cs` |
| `IAuthorityValidator.cs` | `src/client/Microsoft.Identity.Client/Instance/Validation/IAuthorityValidator.cs` |
| `AadAuthorityValidator.cs` | `src/client/Microsoft.Identity.Client/Instance/Validation/AadAuthorityValidator.cs` |
| `AdfsAuthorityValidator.cs` | `src/client/Microsoft.Identity.Client/Instance/Validation/AdfsAuthorityValidator.cs` |
| `NullAuthorityValidator.cs` | `src/client/Microsoft.Identity.Client/Instance/Validation/NullAuthorityValidator.cs` |

### Appendix B: AuthorityType Enum Values

```csharp
public enum AuthorityType
{
    Aad     = 0,  // Azure Active Directory
    Adfs    = 1,  // Active Directory Federation Services
    B2C     = 2,  // Azure AD B2C
    Dsts    = 3,  // Distributed Token Service
    Generic = 4,  // Custom / OIDC-compliant
    Ciam    = 5,  // Customer Identity and Access Management
}
```

### Appendix C: Related Design Documents

- [AUTHORITY_REFACTORING_ARCHITECTURE.md](AUTHORITY_REFACTORING_ARCHITECTURE.md) — Architecture diagrams and data flow
- [cache_extensibility.md](../cache_extensibility.md) — Token cache extensibility design
- [sni_mtls_pop_token_design.md](../sni_mtls_pop_token_design.md) — SNI/mTLS PoP token design (uses authority for endpoint construction)

### Appendix D: Glossary

| Term | Definition |
|---|---|
| Authority | A trusted security token service (STS) that issues OAuth 2.0 tokens |
| Canonical URI | The normalized, lowercased, trailing-slash-appended form of an authority URI |
| Instance Discovery | The process of fetching cloud endpoint metadata from the AAD instance discovery endpoint |
| MSA Passthrough | The mechanism by which MSAL routes personal Microsoft Account requests to the `consumers` tenant |
| Environment Override | Replacing the host component of an authority URI with a cloud-specific alias (e.g., for sovereign clouds) |
| DSTS | Distributed Token Service — Microsoft internal token service used by Azure Resource Manager |
| CIAM | Customer Identity and Access Management — Azure AD External Identities for B2C-like scenarios |
