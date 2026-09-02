# IMDSv2 Kill Switch (`MSAL_MI_DISABLE_IMDS_V2`)

## Purpose

`MSAL_MI_DISABLE_IMDS_V2` is a process-wide environment variable that disables IMDSv2. IMDSv2 is
**enabled by default** and should stay that way. When the variable is set, MSAL skips all IMDSv2
capability probes and execution paths, falls back to IMDSv1 for bearer-token requests, and fails
fast with a clear error when the request requires IMDSv2.

### Design rationale

The following is the reasoning behind the design, not a requirement stated in the originating
task ([#6174](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/6174)).

It is treated as an emergency mitigation rather than a supported configuration knob. That framing
drives three choices:

- **Environment variable, not a `ManagedIdentityApplicationBuilder` API.** An environment variable
  is settable by whoever controls the process environment; a builder API is settable only by
  whoever ships and redeploys the application. A mitigation is worth little if using it requires
  the same release that a code fix would.
- **Read live on every check, not cached at startup.** Setting and clearing the variable both take
  effect on the next token request, so the mitigation is reversible in place.
- **Unrecognized values leave IMDSv2 enabled.** The switch never fails closed on a typo. A
  mistyped value is a no-op, logged once per process so the misconfiguration is visible.

## Accepted values

| Value | Effect |
|-------|--------|
| `true` / `TRUE` / `True` | IMDSv2 disabled |
| `1` | IMDSv2 disabled |
| unset | IMDSv2 enabled (default) |
| empty string | IMDSv2 enabled |
| anything else (e.g. `yes`, `on`, `false`, `0`) | IMDSv2 enabled |

Comparison is ordinal and case-insensitive. Any unrecognized value is a **no-op that leaves
IMDSv2 enabled** — the switch never fails closed on a typo, so a mistyped value cannot silently
weaken token binding across the machines it was deployed to.

The variable is read **live on every check**, not cached at startup. Flipping it takes effect
on the next token request without restarting the process. This mirrors the existing
`MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE` switch.

## Behavior when the switch is on

### Plain bearer tokens — fall back silently

`AcquireTokenForManagedIdentity(...)` with no mTLS option continues to work. MSAL routes the
request over IMDSv1 and returns a normal bearer token. Nothing is thrown, because nothing the
caller asked for was taken away.

### mTLS requests — fail fast

Both mTLS request shapes are served *exclusively* by IMDSv2 and have **no IMDSv1 equivalent**:

| API | Result |
|-----|--------|
| `.WithMtlsProofOfPossession()` | throws `MsalClientException` / `MsalError.ImdsV2Disabled` |
| `.WithRequestOverMtls()` | throws `MsalClientException` / `MsalError.ImdsV2Disabled` |

These throw rather than downgrade. Downgrading would hand back a weaker, unbound token to a
caller who explicitly opted into a bound one — a security property the caller would have no way
to notice they had lost. Failing loudly is the safer default, and it is consistent with what
MSAL already does when these APIs are used on a host that genuinely has no IMDSv2 support
(`MsalError.MtlsPopTokenNotSupportedinImdsV1`).

The two error codes are deliberately distinct so operators can tell the cases apart:

- `MsalError.ImdsV2Disabled` — IMDSv2 was administratively turned off.
- `MsalError.MtlsPopTokenNotSupportedinImdsV1` — the host is genuinely incapable.

Administrative disablement **takes precedence and does not imply host capability**. On a host that
only ever supported IMDSv1, an mTLS request made while the switch is on reports `ImdsV2Disabled`,
not `MtlsPopTokenNotSupportedinImdsV1` — the switch is checked first, and it is the condition the
operator can act on. `MtlsPopTokenNotSupportedinImdsV1` is therefore only reachable while the
switch is off.

On `net462`/`net472`, `WithMtlsProofOfPossession()` and `WithRequestOverMtls()` throw
`MsalError.MtlsNotSupportedForManagedIdentity` at build time regardless of this switch, so the
behavior above applies to `net8.0`/`netstandard2.0` hosts.

### Already-cached tokens are not revoked

The switch governs **token acquisition**, not tokens already in MSAL's cache. A process holding an
unexpired mTLS PoP or mTLS bearer token continues to serve it from cache until it expires; the
throw above happens on the next acquisition that actually reaches the network. This is standard
MSAL cache behavior and is not a downgrade — the cached token is still genuinely key-bound. If a
mitigation must take effect immediately, restart the process or use `WithForceRefresh(true)`.

### Capability discovery — reports no binding support

`ManagedIdentityApplication.GetManagedIdentityCapabilitiesAsync()` reports the host as having
no binding capability while the switch is on:

| Property | Value |
|----------|-------|
| `Source` | the detected source (unchanged, e.g. `Imds`) |
| `MaxSupportedBindingStrength` | `MtlsBindingStrength.None` |
| `IsMtlsPopSupportedByHost` | `false` |
| `ErrorReason` | `"IMDSv2 is disabled by the MSAL_MI_DISABLE_IMDS_V2 environment variable."` |

This is a **behavior change on a public API** (the signature is unchanged), and it is
intentional. This API exists so credential chains such as `DefaultAzureCredential` can decide
up front whether to request a PoP token. If it kept advertising PoP support while the switch
was on, those callers would confidently select the PoP path and then take an exception on
every token request. Reporting `None` lets them pick the bearer path and keep working.

This holds even if the process already discovered IMDSv2 before the switch was set — the
cached result is masked on read rather than trusted. See
[The discovery cache is masked, not overwritten](#the-discovery-cache-is-masked-not-overwritten).

`ErrorReason` is populated even though `Source` is a real detected source, so a caller that
sees an unexpected `None` strength can find out why without guessing.

While the switch is on, MSAL **does not issue the IMDSv2 probe request at all** during
discovery — the switch removes network traffic rather than merely ignoring its result.

## Where the switch is enforced

The switch is checked at **three** points rather than once at startup. This is the subtle part
of the design and the reason a naive implementation is incorrect.

MSAL caches "this machine supports IMDSv2" in a **process-wide static** after the first
successful discovery, and separately caches the **binding certificate**. A check performed only
during discovery would therefore be bypassed by any process that had already probed
successfully before the variable was set — exactly the situation during an incident, when the
switch gets flipped on a process that is already running and already warm.

| Gate | Location | Prevents |
|------|----------|----------|
| 1 | `GetManagedIdentityCapabilitiesAsync` discovery | Probing IMDSv2 endpoints; advertising PoP support |
| 2 | `SelectManagedIdentitySourceType` routing | A **cached** "IMDSv2 supported" result routing past the switch |
| 3 | `AcquireImdsV2MtlsBindingAsync` entry | A **warm certificate cache** serving an mTLS request with no probe at all; also ensures a `PoPOptions.MinStrength` request reports `ImdsV2Disabled` rather than a misleading `MinStrengthNotMet` |

### The discovery cache tracks the switch state

The switch is applied to the discovery result **on every read**, and the cache records **which
switch state produced it**. Together these keep the mitigation reversible in both directions
without giving up caching:

- **Masking on read** stops a process that cached "IMDSv2 / KeyGuard" *before* the switch was set
  from continuing to advertise PoP support afterwards. No re-probe is needed — the cached value
  holds the host's true capability, and the switch is layered over it at the point of use.
- **Recording the switch state** stops the reverse: a "v1-only, no binding" result observed *while*
  the switch was on is an artifact of the switch, not a fact about the host, because the IMDSv2
  probe never ran. That result is discarded as soon as the switch clears, so the mitigation cannot
  outlive itself and become a one-way door needing a process restart.

Gate 2 follows the same principle: it downgrades routing for the current request without mutating
`s_cachedSourceResult`, and it ignores a cached result captured under a switch state that no longer
applies.

The net effect is that the switch is **fully reversible in place, in both directions**, with no
process restart. Discovery stays O(1) in both modes: only the switch-off transition costs one
re-probe. This matters because consumers such as Azure Identity call
`GetManagedIdentityCapabilitiesAsync` on **every authentication** and depend on this cache — an
implementation that skipped caching while the switch was on would add an IMDS round trip to every
token request during an incident, exactly when IMDS is least able to absorb it.

Gates 2 and 3 only ever fire on the IMDS path. Gate 3 checks the detected source explicitly;
gate 2 is scoped structurally (its branches sit inside the "no environment source found" and
`source == Imds` paths). Either way, sources such as App Service and Cloud Shell keep their
existing `MtlsPopNotSupportedForEnvironment` error, which is a more accurate diagnosis for
those hosts — IMDSv2 was never involved there, so "IMDSv2 is disabled" would be misleading.

## Logging

MSAL logs that IMDSv2 was disabled and names the environment variable, but **never logs the
variable's value**. Naming the variable is what makes the condition diagnosable from a log;
echoing the value adds nothing and is avoided on principle.

## Testing

See the `IMDSv2 Kill Switch Tests` region in
`tests/Microsoft.Identity.Test.Unit/ManagedIdentityTests/ImdsV2Tests.cs`.

The suite deliberately covers the cache-bypass scenarios described above, since those are the
cases a discovery-only implementation would get wrong:

- `KillSwitch_SetAfterImdsV2Discovered_StillBlocksMtlsPop`
- `KillSwitch_SetAfterImdsV2Discovered_CapabilitiesStopAdvertisingPop`
- `KillSwitch_WithWarmCertificateCache_StillBlocksMtlsPop`
- `KillSwitch_Cleared_RestoresPopWithoutProcessRestart`

Gate 1 has direct test coverage. Gate 3 is proven by
`KillSwitch_MtlsPopWithMinStrengthFloor_ReportsDisabledNotMinStrengthNotMet`, which fails if that
gate is removed. Gate 2's switch-specific branches are **defense in depth**: gate 3 pre-empts the
mTLS conditions that would otherwise reach them, so removing gate 2 alone does not fail a test
today. They are kept so the switch still fails closed if gate 3 is moved or a new caller of
`SelectManagedIdentitySourceType` is introduced.
