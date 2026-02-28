# Authority Refactoring — Architecture and Data Flow Diagrams

**Status:** Proposed  
**Authors:** MSAL.NET Team  
**Last Updated:** 2026-02-26  
**Companion document:** [AUTHORITY_REFACTORING_DESIGN.md](AUTHORITY_REFACTORING_DESIGN.md)

---

## Table of Contents

1. [Current Monolithic Architecture](#current-monolithic-architecture)
2. [Proposed Registry + Pipeline Architecture](#proposed-registry--pipeline-architecture)
3. [AuthorityCreationPipeline 6-Step Data Flow](#authoritycreationpipeline-6-step-data-flow)
4. [Registry Lookup Paths](#registry-lookup-paths)
5. [Key Decision Points in Authority Selection](#key-decision-points-in-authority-selection)
6. [Code Locations Affected](#code-locations-affected)

---

## Current Monolithic Architecture

The current design distributes authority creation, detection, and validation logic across multiple loosely-related classes. There is no single entry point or registry.

```mermaid
classDiagram
    direction TB

    class AuthorityInfo {
        +CanonicalAuthority : Uri
        +AuthorityType : AuthorityType
        +ValidateAuthority : bool
        +Host : string
        +FromAuthorityUri(string) AuthorityInfo$
        +FromAadAuthority(...) AuthorityInfo$
        +FromB2CAuthority(...) AuthorityInfo$
        -DetectAuthorityType(Uri) AuthorityType
    }

    class AuthorityInfoHelper {
        <<nested in AuthorityInfo>>
        +BuildAuthorityForRequest(...) AuthorityInfo$
        +ValidateAuthorityInfoForRequest(...)$
        +UpdateEnvironment(AuthorityInfo, string) AuthorityInfo$
    }

    class Authority {
        <<abstract>>
        +AuthorityInfo : AuthorityInfo
        +TenantId : string
        +GetTokenEndpointAsync(...) Task~string~
        +GetAuthorizationEndpointAsync(...) Task~string~
        +GetTenantedAuthority(string) Authority
        +CreateAuthority(string) Authority$
        +CreateAuthorityForRequestAsync(...) Task~Authority~$
    }

    class AadAuthority {
        +TenantId : string
        +GetTokenEndpointAsync(...) Task~string~
    }

    class B2CAuthority {
        +UserFlow : string
        +GetTokenEndpointAsync(...) Task~string~
    }

    class AdfsAuthority {
        +GetTokenEndpointAsync(...) Task~string~
    }

    class DstsAuthority {
        +GetTokenEndpointAsync(...) Task~string~
    }

    class CiamAuthority {
        +GetTokenEndpointAsync(...) Task~string~
    }

    class GenericAuthority {
        +GetTokenEndpointAsync(...) Task~string~
    }

    class AuthorityManager {
        -_initialAuthority : Authority
        -_currentAuthority : Authority
        +GetRequestAuthorityAsync() Task~Authority~
        -RunInstanceDiscoveryAsync(...) Task
        -ValidateAuthorityAsync(...) Task
    }

    class IAuthorityValidator {
        <<interface>>
        +ValidateAuthorityAsync(AuthorityInfo, ...) Task
    }

    class AadAuthorityValidator {
        +ValidateAuthorityAsync(...) Task
    }

    class AdfsAuthorityValidator {
        +ValidateAuthorityAsync(...) Task
    }

    class NullAuthorityValidator {
        +ValidateAuthorityAsync(...) Task
    }

    AuthorityInfo --> AuthorityInfoHelper : contains
    Authority <|-- AadAuthority
    Authority <|-- B2CAuthority
    Authority <|-- AdfsAuthority
    Authority <|-- DstsAuthority
    Authority <|-- CiamAuthority
    Authority <|-- GenericAuthority
    AadAuthority <|-- B2CAuthority : extends
    AadAuthority <|-- CiamAuthority : extends
    Authority --> AuthorityInfo : wraps
    AuthorityManager --> Authority : manages
    AuthorityManager --> IAuthorityValidator : uses
    IAuthorityValidator <|.. AadAuthorityValidator
    IAuthorityValidator <|.. AdfsAuthorityValidator
    IAuthorityValidator <|.. NullAuthorityValidator
```

### Fragility Points

The diagram above highlights two fragility points (marked with `*` in the code):

1. **`AuthorityInfo.DetectAuthorityType(Uri)`** — A private method with a chain of `if/else` checks based on host and path heuristics. Adding a new authority type requires modifying this method.

2. **`Authority.CreateAuthority(AuthorityInfo)`** — A factory method with a `switch(authorityInfo.AuthorityType)` that instantiates the correct subclass. Adding a new authority type requires modifying this switch.

These two points must stay synchronized. There is no compile-time enforcement of that invariant.

---

## Proposed Registry + Pipeline Architecture

The proposed design introduces two new types that centralize authority metadata and creation:

```mermaid
classDiagram
    direction TB

    class AuthorityRegistration {
        +Type : AuthorityType
        +DetectionPredicate : Func~Uri,bool~
        +Factory : Func~AuthorityInfo,Authority~
        +Validator : IAuthorityValidator
        +Resolver : IAuthorityResolver
        +Normalizer : IAuthorityNormalizer
    }

    class AuthorityRegistry {
        <<static>>
        -s_registrations : IReadOnlyList~AuthorityRegistration~
        +Detect(Uri) AuthorityRegistration$
        +Get(AuthorityType) AuthorityRegistration$
    }

    class AuthorityCreationPipeline {
        -_discoveryManager : IInstanceDiscoveryManager
        -_requestContext : RequestContext
        +CreateAsync(string, AuthorityInfo, AuthorityOverride) Task~Authority~
    }

    class IAuthorityNormalizer {
        <<interface>>
        +Normalize(Uri) AuthorityInfo
    }

    class IAuthorityResolver {
        <<interface>>
        +ResolveAsync(AuthorityInfo, ...) Task~AuthorityInfo~
    }

    class AuthorityMerger {
        <<static>>
        +Merge(AuthorityInfo, AuthorityInfo, AuthorityOverride) AuthorityInfo$
    }

    class Authority {
        <<abstract>>
        +AuthorityInfo : AuthorityInfo
        +TenantId : string
        +GetTokenEndpointAsync(...) Task~string~
    }

    class AadAuthority
    class B2CAuthority
    class AdfsAuthority
    class DstsAuthority
    class CiamAuthority
    class GenericAuthority

    AuthorityRegistry "1" --> "*" AuthorityRegistration : holds
    AuthorityCreationPipeline --> AuthorityRegistry : detects via
    AuthorityCreationPipeline --> IAuthorityNormalizer : step 3
    AuthorityCreationPipeline --> AuthorityMerger : step 4
    AuthorityCreationPipeline --> IAuthorityValidator : step 5
    AuthorityCreationPipeline --> Authority : produces
    AuthorityRegistration --> IAuthorityNormalizer : owns
    AuthorityRegistration --> IAuthorityValidator : owns
    AuthorityRegistration --> IAuthorityResolver : owns
    Authority <|-- AadAuthority
    Authority <|-- B2CAuthority
    Authority <|-- AdfsAuthority
    Authority <|-- DstsAuthority
    Authority <|-- CiamAuthority
    Authority <|-- GenericAuthority
```

### Key Improvements

| Concern | Before | After |
|---|---|---|
| Type detection | Inline `if/else` in `AuthorityInfo` | Registered predicate per type in `AuthorityRegistry` |
| Factory | `switch` statement in `Authority` | Registered `Func<AuthorityInfo, Authority>` per type |
| Validation | Manually selected in `AuthorityManager` | Registered `IAuthorityValidator` per type |
| Environment resolution | Mixed into `AuthorityInfoHelper` | Registered `IAuthorityResolver` per type |
| Adding a new type | Change 5+ files | Change 1 file (new registration) + 1 line in registry |

---

## AuthorityCreationPipeline 6-Step Data Flow

```mermaid
flowchart TD
    A([Input: raw authority string\nconfig AuthorityInfo\nrequest AuthorityOverride]) --> B

    B["Step 1: Parse\nUri.TryCreate()"]
    B --> |Valid URI| C
    B --> |Invalid URI| ERR1([MsalClientException\nInvalidAuthority])

    C["Step 2: Detect\nAuthorityRegistry.Detect(uri)"]
    C --> |Match found| D
    C --> |No match| ERR2([MsalClientException\nInvalidAuthority])

    D["Step 3: Normalize\nregistration.Normalizer.Normalize(uri)\n• Force https://\n• Lowercase host\n• Append trailing slash\n• Strip query/fragment\n• Canonicalize path"]
    D --> E

    E["Step 4: Merge\nAuthorityMerger.Merge(configAuth, normalized, requestOverride)\n• Config-level vs request-level authority\n• Apply environment override\n• Inject MSA tenant if needed\n• Validate host consistency"]
    E --> |Hosts match or override permitted| F
    E --> |Host mismatch, no override| ERR3([MsalClientException\nAuthorityTypeMismatch])

    F["Step 5: Validate\nregistration.Validator.ValidateAsync(merged)\n• AAD: instance discovery metadata check\n• ADFS: WebFinger endpoint check\n• B2C/CIAM/DSTS/Generic: NullValidator"]
    F --> |Valid| G
    F --> |Not in trusted list| ERR4([MsalServiceException\nAuthorityValidationFailed])

    G["Step 6: Construct\nregistration.Factory(merged)\n• new AadAuthority(merged)\n• new B2CAuthority(merged)\n• new AdfsAuthority(merged)\n• etc."]
    G --> H([Output: Authority instance])
```

### Step Details

#### Step 1 — Parse

- Input: `string rawAuthorityUri`
- Uses `Uri.TryCreate()` with `UriKind.Absolute`
- Throws `MsalClientException(MsalError.InvalidAuthority)` if parsing fails
- Output: `Uri`

#### Step 2 — Detect

- Input: `Uri`
- Calls `AuthorityRegistry.Detect(uri)` which iterates registrations in order
- First registration whose `DetectionPredicate(uri)` returns `true` is selected
- Throws `MsalClientException(MsalError.InvalidAuthority)` if no predicate matches (impossible in practice since `GenericAuthority` is a catch-all)
- Output: `AuthorityRegistration`

#### Step 3 — Normalize

- Input: `Uri`, `AuthorityRegistration`
- Calls `registration.Normalizer.Normalize(uri)`
- Produces a canonical `AuthorityInfo` with normalized URI
- Output: `AuthorityInfo` (normalized)

#### Step 4 — Merge

- Input: config `AuthorityInfo`, normalized request `AuthorityInfo`, `AuthorityOverride`
- Logic (in priority order):
  1. If `requestOverride` contains an explicit authority URI, use it (request-level wins)
  2. If `requestOverride` contains a tenant-only override, inject tenant into config authority
  3. If an environment override is active (from instance discovery metadata), update host
  4. Inject `consumers` tenant for MSA passthrough scenarios
  5. Validate that config and request authority hosts are compatible
- Output: merged `AuthorityInfo`

#### Step 5 — Validate

- Input: merged `AuthorityInfo`, `AuthorityRegistration`, `IInstanceDiscoveryManager`, `RequestContext`
- AAD validator: checks that the authority host appears in instance discovery metadata for the configured cloud
- ADFS validator: issues a WebFinger request to `{authority}/.well-known/webfinger`
- All other validators: no-op (`NullAuthorityValidator`)
- Skip validation if `AuthorityInfo.ValidateAuthority == false`
- Output: void (throws on failure)

#### Step 6 — Construct

- Input: merged `AuthorityInfo`, `AuthorityRegistration`
- Calls `registration.Factory(merged)` to instantiate the concrete `Authority` subclass
- Output: `Authority` instance

---

## Registry Lookup Paths

The `AuthorityRegistry` provides two lookup paths:

```mermaid
flowchart LR
    subgraph Detection ["Detection Path (Step 2)"]
        direction TB
        URI([Uri]) --> P1{DSTS predicate}
        P1 --> |false| P2{B2C predicate}
        P2 --> |false| P3{ADFS predicate}
        P3 --> |false| P4{CIAM predicate}
        P4 --> |false| P5{AAD predicate}
        P5 --> |false| P6{Generic predicate}
        P1 --> |true| R1([DstsRegistration])
        P2 --> |true| R2([B2CRegistration])
        P3 --> |true| R3([AdfsRegistration])
        P4 --> |true| R4([CiamRegistration])
        P5 --> |true| R5([AadRegistration])
        P6 --> |true| R6([GenericRegistration])
    end

    subgraph TypeLookup ["Type Lookup Path (Steps 5, 6)"]
        direction TB
        AT([AuthorityType]) --> LU{Registry.Get}
        LU --> V([IAuthorityValidator])
        LU --> F([Factory Func])
        LU --> RES([IAuthorityResolver])
    end
```

### Detection Predicate Reference

| Registration | Predicate Logic | Example Match |
|---|---|---|
| `DstsAuthorityRegistration` | `host.Contains("dstsv2")` | `dsts.core.azure-test.net/dstsv2/...` |
| `B2CAuthorityRegistration` | `host.Contains(".b2clogin.com")` OR path contains `b2c_1_` | `contoso.b2clogin.com/...` |
| `AdfsAuthorityRegistration` | `path.StartsWith("/adfs", OrdinalIgnoreCase)` | `adfs.contoso.com/adfs` |
| `CiamAuthorityRegistration` | `host.EndsWith(".ciamlogin.com", OrdinalIgnoreCase)` | `contoso.ciamlogin.com/...` |
| `AadAuthorityRegistration` | Host in known AAD hosts list OR matches instance discovery aliases | `login.microsoftonline.com/...` |
| `GenericAuthorityRegistration` | Always returns `true` (catch-all) | `custom.idp.example.com/...` |

---

## Key Decision Points in Authority Selection

The following decision tree describes the logic in **Step 4 (Merge)** of the pipeline:

```mermaid
flowchart TD
    START([Request received]) --> Q1{Request-level\nauthority set?}

    Q1 --> |Yes| Q2{Same authority\ntype as config?}
    Q1 --> |No| Q5{Tenant-only\noverride set?}

    Q2 --> |Yes| Q3{Environment\noverride active?}
    Q2 --> |No| ERR([Throw AuthorityTypeMismatch])

    Q3 --> |Yes| ENV[Apply environment\noverride to host]
    Q3 --> |No| REQ[Use request-level\nauthority as-is]

    ENV --> Q4{MSA\npassthrough?}
    REQ --> Q4

    Q5 --> |Yes| TEN[Inject tenant into\nconfig authority path]
    Q5 --> |No| Q6{Environment\noverride active?}

    TEN --> Q4
    Q6 --> |Yes| ENV2[Apply environment\noverride to config host]
    Q6 --> |No| CFG[Use config-level\nauthority as-is]

    ENV2 --> Q4
    CFG --> Q4

    Q4 --> |Yes - personal account| MSA[Replace tenant with\n'consumers']
    Q4 --> |No| DONE([Merged AuthorityInfo])
    MSA --> DONE
```

---

## Code Locations Affected

### Files to Be Replaced or Substantially Modified

| File | Change Type | Reason |
|---|---|---|
| `Instance/Authority.cs` | Modify | Remove factory `switch`; delegate to `AuthorityCreationPipeline` |
| `AppConfig/AuthorityInfo.cs` | Modify | Remove `DetectAuthorityType()` private method; remove `AuthorityInfoHelper` nested class |
| `Instance/AuthorityManager.cs` | Modify | Replace direct authority creation with `AuthorityCreationPipeline.CreateAsync()` |

### Files to Be Created

| File | Purpose |
|---|---|
| `Instance/AuthorityRegistration.cs` | The `AuthorityRegistration` record |
| `Instance/AuthorityRegistry.cs` | The static registry holding all registrations |
| `Instance/AuthorityCreationPipeline.cs` | The 6-step pipeline |
| `Instance/AuthorityMerger.cs` | Merge logic extracted from `AuthorityInfoHelper` |
| `Instance/IAuthorityNormalizer.cs` | Normalization interface |
| `Instance/IAuthorityResolver.cs` | Resolution interface |
| `Instance/Registrations/AadAuthorityRegistration.cs` | AAD-specific registration |
| `Instance/Registrations/B2CAuthorityRegistration.cs` | B2C-specific registration |
| `Instance/Registrations/AdfsAuthorityRegistration.cs` | ADFS-specific registration |
| `Instance/Registrations/DstsAuthorityRegistration.cs` | DSTS-specific registration |
| `Instance/Registrations/CiamAuthorityRegistration.cs` | CIAM-specific registration |
| `Instance/Registrations/GenericAuthorityRegistration.cs` | Generic/catch-all registration |

### Files Preserved Unchanged

| File | Reason |
|---|---|
| `Instance/AadAuthority.cs` | Concrete type; no change to endpoint construction logic |
| `Instance/B2CAuthority.cs` | Concrete type; no change |
| `Instance/AdfsAuthority.cs` | Concrete type; no change |
| `Instance/DstsAuthority.cs` | Concrete type; no change |
| `Instance/CiamAuthority.cs` | Concrete type; no change |
| `Instance/GenericAuthority.cs` | Concrete type; no change |
| `Instance/Validation/IAuthorityValidator.cs` | Interface; no change |
| `Instance/Validation/AadAuthorityValidator.cs` | Implementation; no change |
| `Instance/Validation/AdfsAuthorityValidator.cs` | Implementation; no change |
| `Instance/Validation/NullAuthorityValidator.cs` | Implementation; no change |
| `Instance/Discovery/` (all files) | Instance discovery subsystem; no change |
| `AppConfig/AuthorityType.cs` (enum) | Enum values unchanged; one new value added if extending |

### Dependency Graph (Current vs. Proposed)

**Before:**

```
AuthorityManager
  ├── AuthorityInfo (contains AuthorityInfoHelper)
  │     └── Authority (factory methods)
  │           ├── AadAuthority
  │           ├── B2CAuthority
  │           ├── AdfsAuthority
  │           ├── DstsAuthority
  │           ├── CiamAuthority
  │           └── GenericAuthority
  └── IAuthorityValidator (selected by switch in AuthorityManager)
        ├── AadAuthorityValidator
        ├── AdfsAuthorityValidator
        └── NullAuthorityValidator
```

**After:**

```
AuthorityManager
  └── AuthorityCreationPipeline
        ├── AuthorityRegistry
        │     └── AuthorityRegistration[]
        │           ├── DetectionPredicate (per type)
        │           ├── IAuthorityNormalizer (per type)
        │           ├── IAuthorityValidator (per type)
        │           ├── IAuthorityResolver (per type)
        │           └── Factory (per type)
        └── AuthorityMerger
              └── Authority instances
                    ├── AadAuthority
                    ├── B2CAuthority
                    ├── AdfsAuthority
                    ├── DstsAuthority
                    ├── CiamAuthority
                    └── GenericAuthority
```
