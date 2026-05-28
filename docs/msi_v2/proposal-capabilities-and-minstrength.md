# Proposal: `GetManagedIdentityCapabilitiesAsync` + `WithMinStrength`

**Status:** Proposal
**Related:** host capability detection

---

## Why

`ManagedIdentityApplication.GetManagedIdentitySourceAsync` is already a shipped public API. Its return type was meant to be a thin wrapper over `ManagedIdentitySource` — but it's grown:

- **today:** `Source` + `ImdsV1FailureReason` + `ImdsV2FailureReason`
- **after PR #6026:** + `IsMtlsPopSupportedByHost`
- **next:** + `SupportedBindingStrengths` (for the MinStrength feature)

The method name `GetManagedIdentitySource…` no longer describes what callers get back. Callers using it for capability decisions (Azure SDK, AKV SDK) read it as "tell me what this host can do," which is what we want — and the name should match.

Separately, we may need a way to **assert** the minimum binding strength their app requires, so a CVM/TVM app accidentally deployed on a software-only host fails at build/request time instead of silently downgrading.

## What

### 1. New discovery API (capability-shaped name)

```csharp
namespace Microsoft.Identity.Client.ManagedIdentity
{
    public class ManagedIdentityApplication : IManagedIdentityApplication
    {
        public Task<ManagedIdentityCapabilities> GetManagedIdentityCapabilitiesAsync(
            CancellationToken cancellationToken);
    }

    public class ManagedIdentityCapabilities
    {
        public ManagedIdentitySource Source { get; }

        // Existing probe-failure detail (carried over)
        public string ImdsV1FailureReason { get; set; }
        public string ImdsV2FailureReason { get; set; }

        // Host capability surface
        public bool IsMtlsPopSupportedByHost { get; }
        public IReadOnlyList<MtlsBindingStrength> SupportedBindingStrengths { get; }
    }

    public enum MtlsBindingStrength
    {
        Bearer            = 0,  // .NET 4.6.2 (no PoP)
        EphemeralSoftware = 1,  // in-memory RSA, process lifetime
        // 2 reserved for future (TPM-backed, etc.)
        KeyGuard          = 3,  // VBS-isolated (CVM/TVM with VBS enabled)
    }
}
```

### 2. Old API stays, marked Obsolete

```csharp
[Obsolete(
    "Use GetManagedIdentityCapabilitiesAsync instead. " +
    "The returned ManagedIdentityCapabilities exposes the same Source plus host capability.",
    error: false)]
public Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(
    CancellationToken cancellationToken);
```

- `ManagedIdentitySourceResult` continues to exist for back-compat; no new members added to it.
- Implementation of the obsolete method delegates to the new one and projects the result.

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
| KeyGuard available | `EphemeralSoftware` | Uses KeyGuard (host max, never downgrades). |
| Software only | `KeyGuard` | **Throws `MsalClientException` at AcquireToken.** |
| Software only | `EphemeralSoftware` | Uses ephemeral software. ✅ |
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

if (!caps.SupportedBindingStrengths.Contains(MtlsBindingStrength.KeyGuard))
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

`PublicAPI.Shipped.txt` change:
- `GetManagedIdentitySourceAsync` gets `[Obsolete]` (does not remove from shipped surface).

No binary-breaking changes. No semver bump.

## Acceptance

- [ ] `MtlsBindingStrength` enum added.
- [ ] `ManagedIdentityCapabilities` class added.
- [ ] `GetManagedIdentityCapabilitiesAsync` added; populates `SupportedBindingStrengths` by combining IMDS hint + KeyGuard probe + .NET TFM.
- [ ] Old `GetManagedIdentitySourceAsync` marked `[Obsolete]` (non-error), delegates to new method.
- [ ] `WithMinStrength` added on `AcquireTokenForManagedIdentityParameterBuilder` and `AcquireTokenForClientParameterBuilder`.
- [ ] `MsalError.MinStrengthNotMet` thrown when host can't meet floor; error message names host actual strength + required floor.
- [ ] `WithMinStrength` without `WithMtlsProofOfPossession` throws at request time.
- [ ] Tests cover: KeyGuard host passes floor, software host fails floor, .NET 4.6.2 fails floor, ConfClient parity.

## Open questions for the thread

1. Bogdan — keep `IsMtlsPopSupportedByHost` (boolean) on the new `ManagedIdentityCapabilities`, or drop it in favor of `SupportedBindingStrengths.Count > 0`?
2. Dragos — does `WithMinStrength` as a floor-only assertion meet the no-downgrade goal? Or do you want the discovery API alone (no enforcement helper)?
3. Should `MtlsBindingStrength` live in `Microsoft.Identity.Client.ManagedIdentity` or `Microsoft.Identity.Client.AppConfig`? It's shared by both MI and ConfClient.
