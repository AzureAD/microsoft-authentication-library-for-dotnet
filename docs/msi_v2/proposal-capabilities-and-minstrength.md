# Proposal: `GetManagedIdentityCapabilitiesAsync` + `WithMtlsProofOfPossession(PoPOptions)`

**Status:** Proposal
**Related:** host capability detection

---

## Why

`ManagedIdentityApplication.GetManagedIdentitySourceAsync` is already a shipped public API. Its return type was meant to be a thin wrapper over `ManagedIdentitySource` — but it's grown:

- **today:** `Source` + `ImdsV1FailureReason` + `ImdsV2FailureReason`
- **after PR #6026:** + `IsMtlsPopSupportedByHost`
- **next:** + `MaxSupportedBindingStrength` (the range of attestation strengths the host supports)

The method name `GetManagedIdentitySource…` no longer describes what callers get back. Callers using it for capability decisions (Azure SDK, AKV SDK) read it as "tell me what this host can do," which is what we want — and the name should match.

Separately, we may later want a way to **assert** the minimum binding strength an app requires, so a CVM/TVM app accidentally deployed on a software-only host fails at request time instead of silently downgrading.

### Plan / phasing

This builds on Bogdan's PR #6026 rather than starting fresh. That PR already does the load-bearing work — the `/metadata/instance/compute` call, parsing the security profile, and deciding whether the host is a TVM/CVM to set the capability flag — and it's already been validated on a real VM across app restart with the cert, app restart without it, and a full VM restart.

- **Phase 1 (this cut — unblocks the AKV SDK):** the renamed discovery API (`GetManagedIdentityCapabilitiesAsync` returning `ManagedIdentityCapabilities`) carrying the range of attestation strengths, plus folding the `ImdsV2` source value back into `Imds`.
- **Phase 2 (follow-up):** the `WithMtlsProofOfPossession(PoPOptions)` minimum-strength enforcement. It isn't needed for AKV, so it doesn't gate the unblock.

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
        // Detected source. `ImdsV2` is folded into `Imds` (see 2a below); the
        // v1/v2 distinction is now an internal routing detail, not a public label.
        public ManagedIdentitySource Source { get; }

        // Single concatenated reason if detection failed; null on success.
        // Not coupled to v1/v2 — internal probe diagnostics are folded in here.
        public string ErrorReason { get; }

        // Highest binding strength this host can produce — the primary capability signal.
        public MtlsBindingStrength MaxSupportedBindingStrength { get; }

        // Convenience derived from MaxSupportedBindingStrength > Bearer.
        // True means "this host can bind a token to a key" — NOT "attested".
        // Callers that need attestation must check for the KeyGuard tier.
        public bool IsMtlsPopSupportedByHost { get; }
    }
}

namespace Microsoft.Identity.Client.AppConfig
{
    // Shared by MI and confidential client, so it lives in AppConfig (not .ManagedIdentity).
    public enum MtlsBindingStrength
    {
        Bearer   = 0,  // no key binding (regular Bearer token)
        Software = 1,  // software-backed key (e.g., persisted CNG on Windows, software key elsewhere)
        // 2 reserved for future (TPM-backed, etc.)
        KeyGuard = 3,  // VBS-isolated key (CVM/TVM with VBS enabled)
    }
}
```

### 2. Old API removed (clean rename)

- Delete `GetManagedIdentitySourceAsync(CancellationToken)` from `ManagedIdentityApplication`.
- Delete `ManagedIdentitySourceResult`.
- Remove both from `PublicAPI.Shipped.txt`; add the new types to `PublicAPI.Unshipped.txt`.
- Feature is internal-only today (Azure SDK private chain is the sole consumer). No external customers to migrate, no obsolete shim needed.

### 2a. Fold `ManagedIdentitySource.ImdsV2` back into `Imds`

The public source enum drops the separate `ImdsV2 = 8` value; detection reports plain `Imds`. Callers branch on the capability signal (`MaxSupportedBindingStrength` / `IsMtlsPopSupportedByHost`), not the v1/v2 label. Internal v1/v2 routing is unchanged.

- **Safe for AKS-on-IMDS:** those nodes are Linux with no TVM/CVM profile, so `IsMtlsPopSupportedByHost` comes back `false`. Azure SDK also checks for AKS via `FEDERATED_TOKEN_FILE` before it ever calls MSAL's MI path — a second safety net.
- **Future bearer-over-IMDSv2:** today bearer is served by v1 and v2 is only used for PoP, so there's no current bearer-over-v2 path. The shape doesn't block it later — `Bearer` is the floor of the strength range and v1/v2 routing is internal, so it can be wired up without an API change.

### 3. `WithMtlsProofOfPossession(PoPOptions)` — floor assertion (Phase 2, follow-up)

> **Deferred to a follow-up.** Phase 1 ships the discovery API only. The enforcement knob below is the agreed shape but is **not** part of the first cut — AKV doesn't need it, so it won't gate the unblock.

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

// 2. Assert the floor when acquiring (Phase 2 — follow-up; not in the first cut)
var result = await mi.AcquireTokenForManagedIdentity("https://vault.azure.net/.default")
    .WithMtlsProofOfPossession(new PoPOptions { MinStrength = MtlsBindingStrength.KeyGuard })  // throws if host can't deliver
    .ExecuteAsync(ct);
```

## API surface impact

These files are split per target framework under `src/client/Microsoft.Identity.Client/PublicApi/<tfm>/`, so every change below applies across all TFMs.

**Phase 1 — `PublicAPI.Unshipped.txt` additions:**
- `ManagedIdentityApplication.GetManagedIdentityCapabilitiesAsync(CancellationToken)`
- `ManagedIdentityCapabilities` (class + members)
- `MtlsBindingStrength` (enum + members)

**Phase 1 — `PublicAPI.Shipped.txt` removals:**
- `ManagedIdentityApplication.GetManagedIdentitySourceAsync(CancellationToken)`
- `ManagedIdentitySourceResult` (class + members)
- `ManagedIdentitySource.ImdsV2` (enum member — folded into `Imds`)

**Phase 2 (follow-up) — `PublicAPI.Unshipped.txt` additions:**
- `PoPOptions` (class + `MinStrength` member)
- `ManagedIdentityPopExtensions.WithMtlsProofOfPossession(PoPOptions)`
- `AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession(PoPOptions)`
- `MsalError.MinStrengthNotMet`

Feature is internal-only (Azure SDK private chain). No external migration story required.

## Alternatives considered

| Shape | Pro | Con |
|---|---|---|
| Chained: `WithMtlsProofOfPossession().WithMinStrength(KeyGuard)` | Discoverable, easy to add later | Two calls to express one intent; grows builder surface for every future PoP knob |
| `WithMtlsProofOfPossession(MtlsBindingStrength minStrength)` overload | One call, single intent | Future PoP knobs each need their own overload — surface area explodes |
| **`WithMtlsProofOfPossession(PoPOptions options)`** (chosen) | Extensible — future PoP knobs slot into `PoPOptions` without touching the builder | New options type to maintain |

**Decision:** `PoPOptions` overload. Future PoP-related settings (custom binding key, attestation level, etc.) land on `PoPOptions` rather than growing the builder.

## Acceptance

**Phase 1 (discovery — this cut):**

- [ ] `MtlsBindingStrength` enum added in `Microsoft.Identity.Client.AppConfig` (`Bearer=0`, `Software=1`, `KeyGuard=3`).
- [ ] `ManagedIdentityCapabilities` class added with `Source`, `ErrorReason`, `MaxSupportedBindingStrength`, and a derived `IsMtlsPopSupportedByHost` (`> Bearer`).
- [ ] `GetManagedIdentityCapabilitiesAsync` added; populates `MaxSupportedBindingStrength` by combining the `/compute` security-profile signal (from #6026) + KeyGuard probe + .NET TFM.
- [ ] `ManagedIdentitySource.ImdsV2` folded into `Imds`; public enum member removed and the `PublicAPI` files updated for all TFMs.
- [ ] Old `GetManagedIdentitySourceAsync` + `ManagedIdentitySourceResult` deleted; removed from `PublicAPI.Shipped.txt`.
- [ ] Tests cover: AKS/Linux host → `IsMtlsPopSupportedByHost = false`; Windows TVM/CVM → KeyGuard tier; existing token flows unchanged.

**Phase 2 (enforcement — follow-up):**

- [ ] `PoPOptions` class added in `Microsoft.Identity.Client.AppConfig` with `MinStrength` property (default `Bearer`).
- [ ] `WithMtlsProofOfPossession(PoPOptions)` overload added on `AcquireTokenForManagedIdentityParameterBuilder` and `AcquireTokenForClientParameterBuilder`.
- [ ] `MsalError.MinStrengthNotMet` thrown when host can't meet floor; error message names host actual strength + required floor.
- [ ] Tests cover: KeyGuard host passes floor, software host fails floor, .NET 4.6.2 fails floor, default `Bearer` floor behaves identically to parameterless overload, ConfClient parity.

## Resolved

- **Phasing.** Discovery API ships first (Phase 1) to unblock the AKV SDK; the `PoPOptions` floor-enforcement helper is a Phase 2 follow-up and does not gate the unblock.
- **Build on #6026.** Reuse Bogdan's `/compute` + capability detection rather than starting fresh; it's already validated on a real VM.
- **`IsMtlsPopSupportedByHost` is derived**, not a standalone capability: it's `MaxSupportedBindingStrength > Bearer`. The strength tier is the source of truth. `true` means "host can bind a token to a key," NOT "attested" — software-key binding on high-density platforms (AKS/SF/ACI, where the host is the node) can report `true` without VBS, so attestation must be read from the KeyGuard tier, never the bool.
- **`ManagedIdentitySource.ImdsV2` folds into `Imds`.** The v1/v2 distinction becomes an internal routing detail; callers branch on the capability signal. Safe for AKS (reports `false`; Azure SDK also gates on `FEDERATED_TOKEN_FILE`).
- `MtlsBindingStrength` and `PoPOptions` live in `Microsoft.Identity.Client.AppConfig` (shared by MI and confidential client).
- Enforcement shape: `WithMtlsProofOfPossession(PoPOptions options)` overload (per Bogdan's suggestion) — keeps the builder surface stable and lets future PoP knobs land on `PoPOptions`.
