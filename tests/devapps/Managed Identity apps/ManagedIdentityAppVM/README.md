# Managed Identity VM test app

This existing console app references the local MSAL and KeyAttestation projects.
It calls the real managed identity endpoints; it contains no IMDS simulator,
synthetic certificate generator, HTTP transport, or loopback resource server.

| Choice | Acquisition |
|---|---|
| A | Existing bearer flow |
| P | Existing mTLS PoP flow with attestation |
| N | Non-attested mTLS PoP using the RDP certificate selected internally by MSAL |

Identity selection, resource selection, force refresh, and cache-repeat controls
remain available. N supports SAMI or a UAMI client ID. All modes use the existing
token display controls: a preview by default and the full token when F is enabled.
Full tokens are secrets; do not share that output.

## Non-attested API call

The parameterless request without
`WithAttestationSupport()` makes MSAL locate the configured Windows RDP listener
certificate. The app does not load or pass a certificate:

```csharp
var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
    .Build();

var result = await app.AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

No `WithExperimentalFeatures(true)` or `WithAttestationSupport()` call is needed for N.
MSAL owns RDP certificate discovery, proof signing,
the IMDS request, response handling, token caching, and returning
`AuthenticationResult.BindingCertificate`. The flow uses MSAL's existing HTTP
client configuration without introducing feature-specific proxy settings.
The app retains the MSAL application for repeat acquisitions. Each execution
selects the current listener certificate before cache lookup and retains that
certificate for its result and any proactive refresh. Certificate rotation
therefore selects a separate cache entry without invalidating earlier results.

`PoPOptions.BindingCertificate` remains an optional explicit override:
`WithMtlsProofOfPossession(new PoPOptions { BindingCertificate = certificate })`.
It bypasses automatic RDP discovery and accepts a caller-owned certificate for
that request only. The caller must keep an explicitly supplied certificate usable
for the request, proactive refresh, and returned results.

Automatic discovery reads the RDP-Tcp listener's configured thumbprint through WMI
and looks for that exact certificate in the local-machine Remote Desktop and My
stores. It requires an accessible RSA private key. Missing configuration, a
missing/invalid certificate, or insufficient access fails explicitly: MSAL does
not generate a replacement key, change permissions, or fall back to bearer.
The existing attested flow is unchanged when `WithAttestationSupport()` is used.
Non-attested parameterless PoP requests now select the RDP certificate regardless
of the application's experimental-feature setting.
Both explicit and automatically selected certificates use the same experimental
Software-strength protocol; a KeyGuard minimum is rejected before discovery.

No custom HTTP factory is needed for this sample. If one is configured for this
experimental flow, it must implement `IMsalWsTrustHttpClientFactory` and return an
anonymous client with automatic redirects disabled from `GetHttpClient(false)`.
A factory implementing only `IMsalHttpClientFactory` is rejected before sending;
MSAL will not silently replace its proxy, timeout, or handlers with defaults.

## Run

Run without arguments for the interactive menu. For one acquisition, including
noninteractive scheduled-task execution:

```text
ManagedIdentityAppVM.exe --rdp-live https://graph.microsoft.com
ManagedIdentityAppVM.exe --rdp-live https://graph.microsoft.com <UAMI-client-ID>
```

Append `--imds-error-details` to display selected service error fields on failure.
This is opt-in: error descriptions may contain identifiers or echoed request
data, so review them before sharing. This option does not change the token-display
toggle. MSAL retains failed HTTP response data in the standard
`MsalServiceException.ResponseBody` and `Headers` properties.

The framework-dependent app requires .NET 8. An existing private runtime can
launch it with `dotnet.exe ManagedIdentityAppVM.dll` and the same arguments.
The app does not elevate itself or change certificate/key permissions.

## Verified behavior and limits

A live run on November 7, 2026 acquired a Graph `mtls_pop` token through the
explicit-certificate version of this MSAL path under SYSTEM, saved it to the cache, and retained the supplied RDP
certificate. That VM's ordinary user could not open the RDP private key; SYSTEM
was the demonstrated test identity, not a general requirement of MSAL.
Automatic discovery inside MSAL has not yet been rerun on that VM.

The experimental endpoint uses API version `2018-02-01`, JSON `req_cnf_proof`,
and the resource with `/.default` in both the query and signed proof, matching
the successful PoC. MSAL's cache/result resource remains normalized without the
suffix. The normal bearer and attested protocols are unchanged.

Key Vault still returned `invalid_resource`. No live Graph resource call,
resource-side binding validation, or live cache-hit test was established by
that acquisition. The RDP certificate's server-only EKU is also a separate
consideration for resource-side client TLS.

Regression coverage lives in
`tests\Microsoft.Identity.Test.Unit\ManagedIdentityTests\ProvidedCertificateManagedIdentityTests.cs`,
including cryptographic proof checks, wire values, response validation, cache
isolation/reuse, force refresh, certificate ownership, and failure behavior.
