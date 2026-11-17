# IMDSv2 Kill Switch (`MSAL_MI_DISABLE_IMDS_V2`)

## Purpose

`MSAL_MI_DISABLE_IMDS_V2` turns IMDSv2 off for a process. IMDSv2 is enabled by default and should
stay that way; the switch exists so that a host hitting an IMDSv2 problem can fall back to IMDSv1
without waiting for an MSAL release.

That goal is why this is an environment variable rather than a `ManagedIdentityApplicationBuilder`
option. A builder API is settable only by whoever ships and redeploys the application, so reaching
for it would cost the same release cycle as a code fix and leave the switch worth very little.

## Accepted values

| Value | Effect |
|-------|--------|
| `true` (any casing) | IMDSv2 disabled |
| `1` | IMDSv2 disabled |
| unset, empty, or any other value | IMDSv2 enabled (default) |

Only those two values disable IMDSv2. Anything else is ignored rather than treated as "on", so a
typo cannot quietly downgrade token binding across every host it was deployed to.

## A restart is required

The variable is read from the **process** environment. Nothing outside the process can modify that
block: Windows exposes no supported API for it, and `/proc/<pid>/environ` is read-only on Linux. An
edit made at the machine, service, or container level is therefore invisible to an already-running
process, so setting or clearing the variable takes effect only after a restart or recycle.

This is mechanics, not policy, and it is not a limitation in practice: the deployment mechanisms
that would set the variable (app restart, container replacement, VM reimage) all restart the
process anyway.

A process can still change its own block through `Environment.SetEnvironmentVariable`, which is how
the tests exercise both states, but MSAL exposes no way to do that and no deployment mechanism does
it. In any real host the value is settled before the first call, so MSAL has no warm-cache state to
reconcile — whatever discovery cached was computed under the switch state still in force.

## Behavior when the switch is on

### Bearer tokens keep working

`AcquireTokenForManagedIdentity(...)` with no mTLS option returns a normal bearer token over
IMDSv1. Nothing is thrown, because nothing the caller asked for was taken away.

### mTLS requests fail fast

`WithMtlsProofOfPossession()` and `WithRequestOverMtls()` are served exclusively by IMDSv2 and have
no IMDSv1 equivalent, so both throw rather than return a token. Which error you get depends on how
the request was built:

| Request | Error |
|---|---|
| `WithMtlsProofOfPossession()` | `MtlsPopTokenNotSupportedinImdsV1` |
| `WithRequestOverMtls()` | `MtlsPopTokenNotSupportedinImdsV1` |
| `WithMtlsProofOfPossession(...)` with a `MinStrength` floor | `MinStrengthNotMet`, from the existing floor check measured against the reported `None` |

They throw rather than fall back because the caller opted into something IMDSv1 cannot provide and
has no way to notice that a weaker result came back instead. The two APIs ask for different things —
`WithMtlsProofOfPossession()` for a certificate-bound token, `WithRequestOverMtls()` for a bearer
token issued over an mTLS connection — and neither is reachable without IMDSv2.

Reusing the error MSAL already raises on a host with no IMDSv2 support is deliberate: the caller's
situation is identical either way — mTLS is unavailable here — and the distinction that actually
matters for debugging is in the log. Note that the existing message attributes the failure to the VM
image, which is accurate for a genuinely v1-only host but not when the switch is the cause; the log
line named above is what separates the two.

### Capability discovery reports no binding support

`GetManagedIdentityCapabilitiesAsync()` reports:

| Property | Value |
|----------|-------|
| `Source` | unchanged (for example, `Imds`) |
| `MaxSupportedBindingStrength` | `MtlsBindingStrength.None` |
| `IsMtlsPopSupportedByHost` | `false` |

This API exists so credential chains such as `DefaultAzureCredential` can decide up front whether
to ask for a bound token. Reporting what the hardware could do while the switch is on would let
them confidently pick the PoP path and then fail on every token request. Reporting `None` keeps the
advertised capability equal to what the caller can actually obtain, so they select the bearer path
and keep working.

This makes the reported value effective availability rather than raw hardware capability, which is a
deliberate narrowing of the `MaxSupportedBindingStrength` doc comment. The two already diverge
without the switch: on a v1-only host MSAL grades the platform key provider even though no IMDSv1
request can produce a bound token. The switch widens that gap on purpose, because a consumer
branching on `IsMtlsPopSupportedByHost` needs the answer it can act on.

For the same reason MSAL skips the IMDSv1 binding-strength probe entirely: with no route to a bound
token, there is nothing to grade, and the check would only cost an HTTP call.

## Where the switch is enforced

Two places, because an mTLS request does not always run discovery.

| Enforcement point | Why it is needed |
|---|---|
| `GetManagedIdentityCapabilitiesAsync` | Skips the IMDSv2 probe, so no v2 HTTP, CSR, certificate, or key-provisioning work happens, and reports no binding support. |
| `SelectManagedIdentitySourceType` | An mTLS request with no binding-strength floor never calls discovery; it routes straight to IMDSv2. Without a check here the switch is bypassed on that path. |

The second point adds no new rejection logic. It stops the direct-to-IMDSv2 route so the request
falls through to the IMDSv1 mTLS guard that already exists, and that guard throws.

Sources such as App Service and Cloud Shell are unaffected. IMDSv2 is never involved there, so they
keep their existing `MtlsPopNotSupportedForEnvironment` error, which diagnoses those hosts more
accurately than "IMDSv2 is disabled" would.

## Logging

MSAL names the environment variable in the log when it changes a decision, on both the discovery
and the routing path. It never logs the variable's value: naming the variable is what makes the
condition diagnosable, and echoing the value adds nothing.

## Testing

See the `IMDSv2 Kill Switch Tests` region in
`tests/Microsoft.Identity.Test.Unit/ManagedIdentityTests/ImdsV2Tests.cs`.
