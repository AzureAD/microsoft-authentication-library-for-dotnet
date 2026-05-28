# Proposal: `GetManagedIdentityCapabilitiesAsync` + `WithMtlsProofOfPossession(PoPOptions)`

**Status:** Proposal
**Related:** host capability detection

---

## Why

`ManagedIdentityApplication.GetManagedIdentitySourceAsync` is already a shipped public API. Its return type was meant to be a thin wrapper over `ManagedIdentitySource` — but it's grown:

- **today:** `Source` + `ImdsV1FailureReason` + `ImdsV2FailureReason`
- **after PR #6026:** + `IsMtlsPopSupportedByHost`
- **next:** + `MaxSupportedBindingStrength` (for the floor-assertion feature)

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
        // for callers that want to assert a floor via PoPOptions.MinStrength.
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

### 3. `WithMtlsProofOfPossession(PoPOptions)` — floor assertion via options

```csharp
namespace Microsoft.Identity.Client.AppConfig
{
    // Options bag for PoP knobs. Today it carries the floor; future PoP-related
    // settings (custom binding key, attestation level, etc.) slot in here without
    // adding new builder methods.
    public class PoPOptions
    {
        // Floor — request fails if host can't meet this strength.
        // Default: Bearer (i.e., no floor; behaves like the parameterless overload).
        public MtlsBindingStrength MinStrength { get; set; } = MtlsBindingStrength.Bearer;
    }
}

public static class ManagedIdentityPopExtensions
{
    // Existing — parameterless: use whatever the host supports (no floor).
    public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
        this AcquireTokenForManagedIdentityParameterBuilder builder);

    // New overload — same behavior, plus enforces options.MinStrength as a floor.
    public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
        this AcquireTokenForManagedIdentityParameterBuilder builder,
        PoPOptions options);
}

// Symmetry for confidential client
public class AcquireTokenForClientParameterBuilder
{
    public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession();              // existing
    public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession(PoPOptions);    // new
}
```

#### Semantics — floor, not selector

| Host actual | `PoPOptions.MinStrength` | Behavior |
|---|---|---|
| KeyGuard available | `KeyGuard` | Uses KeyGuard. ✅ |
| KeyGuard available | `Software` | Uses KeyGuard (host max, never downgrades). |
| Software only | `KeyGuard` | **Throws `MsalClientException` at AcquireToken.** |
| Software only | `Software` | Uses software. ✅ |
| .NET 4.6.2 | any non-`Bearer` | **Throws.** |
| any | `Bearer` (default) | No floor; same as parameterless `WithMtlsProofOfPossession()`. |

- `PoPOptions.MinStrength` is a **floor assertion**, never a downgrade dial.
- MSAL always picks the host's max binding strength; `MinStrength` just adds a tripwire that fails the request if the host can't meet the floor.
- Failure is `MsalClientException` with error code `MsalError.MinStrengthNotMet`. Message names host actual strength + required floor.

## Why this shape

- **Name reflects content.** `GetManagedIdentityCapabilities` is honest about returning a capability bag, not just a source enum.
- **No silent downgrade.** A CVM-bound service that asserts `KeyGuard` will fail fast on the wrong host. No production drift.
- **No developer downgrade lever.** There's no way to use `MinStrength` to opt for *less* binding than the host provides. The dial only enforces a floor.
- **Discovery + enforcement composed cleanly:** call `GetManagedIdentityCapabilitiesAsync` to inspect, then pass `PoPOptions` to enforce.
- **One builder method, extensible.** New PoP knobs land on `PoPOptions` without growing the builder surface.

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
    .WithMtlsProofOfPossession(new PoPOptions { MinStrength = MtlsBindingStrength.KeyGuard })  // throws if host can't deliver
    .ExecuteAsync(ct);
```

## API surface impact

`PublicAPI.Unshipped.txt` additions:
- `ManagedIdentityApplication.GetManagedIdentityCapabilitiesAsync(CancellationToken)`
- `ManagedIdentityCapabilities` (class + members)
- `MtlsBindingStrength` (enum + members)
- `PoPOptions` (class + `MinStrength` member)
- `ManagedIdentityPopExtensions.WithMtlsProofOfPossession(PoPOptions)`
- `AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession(PoPOptions)`
- `MsalError.MinStrengthNotMet`

`PublicAPI.Shipped.txt` removals:
- `ManagedIdentityApplication.GetManagedIdentitySourceAsync(CancellationToken)`
- `ManagedIdentitySourceResult` (class + members)

Feature is internal-only (Azure SDK private chain). No external migration story required.

## Alternatives considered

| Shape | Pro | Con |
|---|---|---|
| Chained: `WithMtlsProofOfPossession().WithMinStrength(KeyGuard)` | Discoverable, easy to add later | Two calls to express one intent; grows builder surface for every future PoP knob |
| `WithMtlsProofOfPossession(MtlsBindingStrength minStrength)` overload | One call, single intent | Future PoP knobs each need their own overload — surface area explodes |
| **`WithMtlsProofOfPossession(PoPOptions options)`** (chosen) | Extensible — future PoP knobs slot into `PoPOptions` without touching the builder | New options type to maintain |

**Decision:** `PoPOptions` overload. Future PoP-related settings (custom binding key, attestation level, etc.) land on `PoPOptions` rather than growing the builder.

## Acceptance

- [ ] `MtlsBindingStrength` enum added in `Microsoft.Identity.Client.AppConfig` (`Bearer=0`, `Software=1`, `KeyGuard=3`).
- [ ] `PoPOptions` class added in `Microsoft.Identity.Client.AppConfig` with `MinStrength` property (default `Bearer`).
- [ ] `ManagedIdentityCapabilities` class added with `Source`, `ErrorReason`, `IsMtlsPopSupportedByHost`, `MaxSupportedBindingStrength`.
- [ ] `GetManagedIdentityCapabilitiesAsync` added; populates `MaxSupportedBindingStrength` by combining IMDS hint + KeyGuard probe + .NET TFM.
- [ ] Old `GetManagedIdentitySourceAsync` + `ManagedIdentitySourceResult` deleted; removed from `PublicAPI.Shipped.txt`.
- [ ] `WithMtlsProofOfPossession(PoPOptions)` overload added on `AcquireTokenForManagedIdentityParameterBuilder` and `AcquireTokenForClientParameterBuilder`.
- [ ] `MsalError.MinStrengthNotMet` thrown when host can't meet floor; error message names host actual strength + required floor.
- [ ] Tests cover: KeyGuard host passes floor, software host fails floor, .NET 4.6.2 fails floor, default `Bearer` floor behaves identically to parameterless overload, ConfClient parity.

## Open questions for the thread

1. Dragos — does the floor-via-`PoPOptions` shape meet the no-downgrade goal? Or do you want the discovery API alone (no enforcement helper)?

Resolved:
- `IsMtlsPopSupportedByHost` stays on `ManagedIdentityCapabilities` alongside `MaxSupportedBindingStrength`. The boolean is the single, callable check the Azure SDK chain already wants ("can this host do PoP at all?"); `MaxSupportedBindingStrength` is the finer-grained signal for callers that care about the strength tier.
- `MtlsBindingStrength` and `PoPOptions` live in `Microsoft.Identity.Client.AppConfig` (shared by MI and confidential client).
- Enforcement shape: `WithMtlsProofOfPossession(PoPOptions options)` overload (per Bogdan's suggestion) — keeps the builder surface stable and lets future PoP knobs land on `PoPOptions`.
