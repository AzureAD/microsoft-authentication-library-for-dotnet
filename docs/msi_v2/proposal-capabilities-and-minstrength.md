# Proposal: `GetManagedIdentityCapabilitiesAsync` + `WithMinStrength`

**Status:** Proposal
**Related:** host capability detection

---

## Why

`ManagedIdentityApplication.GetManagedIdentitySourceAsync` is already a shipped public API. Its return type was meant to be a thin wrapper over `ManagedIdentitySource` — but it's grown:

- **today:** `Source` + `ImdsV1FailureReason` + `ImdsV2FailureReason`
- **after PR #6026:** + `IsMtlsPopSupportedByHost`
- **next:** + `MaxSupportedBindingStrength` (for the MinStrength feature)

The method name `GetManagedIdentitySource…` no longer describes what callers get back. Callers using it for capability decisions (Azure SDK, AKV SDK) read it as "tell me what this host can do," which is what we want — and the name should match.

Separately, we may need a way to **assert** the minimum binding strength their app requires, so a CVM/TVM app accidentally deployed on a software-only host fails at request time instead of silently downgrading.

## What

### 1. New discovery API (capability-shaped name)

```csharp
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client
{
    public class ManagedIdentityApplication : IManagedIdentityApplication
    {
        public Task<ManagedIdentityCapabilities> GetManagedIdentityCapabilitiesAsync(
            CancellationToken cancellationToken);
    }
}

namespace Microsoft.Identity.Client.ManagedIdentity
{
    public class ManagedIdentityCapabilities
    {
        public ManagedIdentitySource Source { get; }

        // Single concatenated reason if detection failed; null on success.
        // Not coupled to v1/v2 — internal probe diagnostics are folded in here.
        public string ErrorReason { get; }

        // Host capability surface
        public bool IsMtlsPopSupportedByHost { get; }

        // Highest binding strength this host can produce.
        // MSAL always uses this max; the value is exposed for diagnostics and
        // for callers that want to assert a floor via WithMinStrength(...).
        public MtlsBindingStrength MaxSupportedBindingStrength { get; }
    }
}

namespace Microsoft.Identity.Client.AppConfig
{
    // Shared by MI and confidential client, so it lives in AppConfig (not .ManagedIdentity).
    public enum MtlsBindingStrength
    {
        Bearer   = 0,  // .NET 4.6.2 (no PoP)
        Software = 1,  // software-backed RSA — persisted CNG on Windows, RSA.Create() elsewhere
        // 2 reserved for future (TPM-backed, etc.)
        KeyGuard = 3,  // VBS-isolated (CVM/TVM with VBS enabled)
    }
}
```

### 2. Old API removed (clean rename)

- Delete `GetManagedIdentitySourceAsync(CancellationToken)` from `ManagedIdentityApplication`.
- Delete `ManagedIdentitySourceResult`.
- Remove both from `PublicAPI.Shipped.txt`; add the new types to `PublicAPI.Unshipped.txt`.
- Feature is internal-only today (Azure SDK private chain is the sole consumer). No external customers to migrate, no obsolete shim needed.

### 3. `WithMinStrength` — floor assertion on the request

```csharp
public static class ManagedIdentityPopExtensions
{
    // Existing
    public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
        this AcquireTokenForManagedIdentityParameterBuilder builder);

    // New
    public static AcquireTokenForManagedIdentityParameterBuilder WithMinStrength(
        this AcquireTokenForManagedIdentityParameterBuilder builder,
        MtlsBindingStrength minStrength);
}

// Symmetry for confidential client
public class AcquireTokenForClientParameterBuilder
{
    public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession();         // existing
    public AcquireTokenForClientParameterBuilder WithMinStrength(MtlsBindingStrength); // new
}
```

#### Semantics — floor, not selector

| Host actual | `WithMinStrength(...)` | Behavior |
|---|---|---|
| KeyGuard available | `KeyGuard` | Uses KeyGuard. ✅ |
| KeyGuard available | `Software` | Uses KeyGuard (host max, never downgrades). |
| Software only | `KeyGuard` | **Throws `MsalClientException` at AcquireToken.** |
| Software only | `Software` | Uses software. ✅ |
| .NET 4.6.2 | any non-Bearer | **Throws.** |
| Not chained with `WithMtlsProofOfPossession()` | any | **Throws** — `WithMinStrength` only meaningful with PoP. |

- `WithMinStrength` is a **floor assertion**, never a downgrade dial.
- MSAL always picks the host's max binding strength; `WithMinStrength` just adds a tripwire that fails the request if the host can't meet the floor.
- Failure is `MsalClientException` with a new error code: `MsalError.MinStrengthNotMet`.

## Why this shape

- **Name reflects content.** `GetManagedIdentityCapabilities` is honest about returning a capability bag, not just a source enum.
- **No silent downgrade.** A CVM-bound service that asserts `KeyGuard` will fail fast on the wrong host. No production drift.
- **No developer downgrade lever.** There's no way to use `WithMinStrength` to opt for *less* binding than the host provides. The dial only enforces a floor.
- **Discovery + enforcement composed cleanly:** call `GetManagedIdentityCapabilitiesAsync` to inspect, then `WithMinStrength` to enforce.

## Sample usage

```csharp
var mi = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

// 1. Inspect what this host can do (optional — for diagnostics / DefaultAzureCredential chain)
var caps = await ((ManagedIdentityApplication)mi)
    .GetManagedIdentityCapabilitiesAsync(ct);

if (caps.MaxSupportedBindingStrength < MtlsBindingStrength.KeyGuard)
{
    logger.LogWarning("This host cannot meet KeyGuard requirement; service will refuse to start.");
}

// 2. Assert the floor when acquiring
var result = await mi.AcquireTokenForManagedIdentity("https://vault.azure.net/.default")
    .WithMtlsProofOfPossession()
    .WithMinStrength(MtlsBindingStrength.KeyGuard)  // throws if host can't deliver
    .ExecuteAsync(ct);
```

## API surface impact

`PublicAPI.Unshipped.txt` additions:
- `ManagedIdentityApplication.GetManagedIdentityCapabilitiesAsync(CancellationToken)`
- `ManagedIdentityCapabilities` (class + members)
- `MtlsBindingStrength` (enum + members)
- `ManagedIdentityPopExtensions.WithMinStrength(...)`
- `AcquireTokenForClientParameterBuilder.WithMinStrength(...)`
- `MsalError.MinStrengthNotMet`

`PublicAPI.Shipped.txt` removals:
- `ManagedIdentityApplication.GetManagedIdentitySourceAsync(CancellationToken)`
- `ManagedIdentitySourceResult` (class + members)

Feature is internal-only (Azure SDK private chain). No external migration story required.

## Alternatives considered for `WithMinStrength`

| Shape | Pro | Con |
|---|---|---|
| **Chained: `WithMtlsProofOfPossession().WithMinStrength(KeyGuard)`** (current proposal) | Discoverable, composes with existing API, easy to add later without touching `WithMtlsProofOfPossession` signature | Two calls to express one intent |
| `WithMtlsProofOfPossession(MtlsBindingStrength minStrength)` overload | One call, single intent | Adds an overload to an already-shipped method; semantics overloaded (PoP-on + floor) |
| `WithMtlsProofOfPossession(new PoPOptions { MinStrength = KeyGuard })` | Extensible — future PoP options slot in cleanly | New options type to maintain; verbose for the common case |

Open to swapping to the overload shape if the team prefers it — they're all valid; the chained form is just the least-invasive.

## Acceptance

- [ ] `MtlsBindingStrength` enum added (`Bearer=0`, `Software=1`, `KeyGuard=3`).
- [ ] `ManagedIdentityCapabilities` class added with `Source`, `ErrorReason`, `IsMtlsPopSupportedByHost`, `MaxSupportedBindingStrength`.
- [ ] `GetManagedIdentityCapabilitiesAsync` added; populates `MaxSupportedBindingStrength` by combining IMDS hint + KeyGuard probe + .NET TFM.
- [ ] Old `GetManagedIdentitySourceAsync` + `ManagedIdentitySourceResult` deleted; removed from `PublicAPI.Shipped.txt`.
- [ ] `WithMinStrength` added on `AcquireTokenForManagedIdentityParameterBuilder` and `AcquireTokenForClientParameterBuilder`.
- [ ] `MsalError.MinStrengthNotMet` thrown when host can't meet floor; error message names host actual strength + required floor.
- [ ] `WithMinStrength` without `WithMtlsProofOfPossession` throws at request time.
- [ ] Tests cover: KeyGuard host passes floor, software host fails floor, .NET 4.6.2 fails floor, ConfClient parity.

## Open questions for the thread

1. Dragos — does `WithMinStrength` as a floor-only assertion meet the no-downgrade goal? Or do you want the discovery API alone (no enforcement helper)?
2. `WithMinStrength` shape — chained (current), overload, or options object? See *Alternatives considered* above.

Notes:
- `IsMtlsPopSupportedByHost` stays on `ManagedIdentityCapabilities` alongside `MaxSupportedBindingStrength`. The boolean is the single, callable check the Azure SDK chain already wants ("can this host do PoP at all?"); `MaxSupportedBindingStrength` is the finer-grained signal for callers that care about the strength tier.
- `MtlsBindingStrength` lives in `Microsoft.Identity.Client.AppConfig` (it's shared by MI and confidential client).
