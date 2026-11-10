# Managed Identity VM test app

This existing console app references the local MSAL and KeyAttestation projects.
It calls the real managed identity endpoints; it contains no IMDS simulator,
synthetic certificate generator, HTTP transport, or loopback resource server.

| Choice | Acquisition |
|---|---|
| A | Existing bearer flow |
| P | Existing Windows-only mTLS PoP flow with attestation |
| N | Endorsed unattested flow for token binding using the RDP certificate selected internally by MSAL |

Identity selection, resource selection, force refresh, and cache-repeat controls
remain available. N supports SAMI or a UAMI client ID. All modes use the existing
token display controls: a preview by default and the full token when F is enabled.
Full tokens are secrets; do not share that output.

## Endorsed unattested flow for token binding

**Implementation status:** "endorsed unattested" names the intended flow.
The IMDS-side check against the RDP certificate in the WS container document
is not implemented yet; current acquisition does not establish WS-backed
endorsement. See [Pending IMDS endorsement check](#pending-imds-endorsement-check).

The parameterless request without
`WithAttestationSupport()` makes MSAL locate the configured Windows RDP listener
certificate or Linux xrdp TLS certificate. Linux support requires .NET 8 or later.
The app does not load or pass a certificate:

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

There is no bring-your-own-certificate option for this managed identity flow.
MSAL selects the configured RDP/xrdp certificate and returns it through
`AuthenticationResult.BindingCertificate`. The selected certificate remains
usable through the returned result and any queued proactive refresh.

On Windows, automatic discovery reads the RDP-Tcp listener's configured thumbprint
through WMI and looks for that exact certificate in the local-machine Remote
Desktop and My stores.

On Linux, automatic discovery reads the first `[Globals]` section in
`/etc/xrdp/xrdp.ini`. It loads the PEM `certificate` and `key_file` paths, using
`/etc/xrdp/cert.pem` and `/etc/xrdp/key.pem` when those settings are absent or empty.
The security layer must be `tls` or `negotiate` (the xrdp default).
Configured paths must be absolute; duplicate settings and environment-variable
paths fail explicitly rather than depending on a different daemon environment.
The certificate must match an accessible, unencrypted RSA private key.
MSAL does not install xrdp or assume that every Linux VM has an RDP certificate.
An SSH login key is not an X.509 binding certificate.

Both platforms require an accessible RSA private key. Missing configuration, a
missing/invalid certificate, or insufficient access throws `MsalClientException`: MSAL does
not generate a replacement key, change permissions, or fall back to bearer.
The existing Windows attested flow is unchanged when `WithAttestationSupport()` is used.
Attested PoP and bearer-over-mTLS remain Windows-only and fail explicitly on Linux.
The endorsed unattested flow selects the RDP certificate regardless
of the application's experimental-feature setting.
The automatically selected certificate uses the experimental Software-strength
protocol; a KeyGuard minimum is rejected before discovery.

No custom HTTP factory is needed for this sample. If configured through
`WithHttpClientFactory`, the existing `IMsalHttpClientFactory.GetHttpClient()`
is used without requiring another interface or replacing the client's handlers,
proxy configuration, or timeout. The custom client must not send server
authentication credentials, follow redirects, or retry/duplicate proof requests
in its handler pipeline. Use a separate factory instance for the managed identity
application if other applications need different handler policies.

Without a custom factory, MSAL uses its pooled anonymous, redirect-free client.
MSAL sends each signed proof once, with no automatic retry. MEX and WS-Trust keep
their existing factory selection and fallback behavior.

## What the IMDS token request proves

The **endorsed unattested flow for token binding** is part of the existing
`AcquireTokenForManagedIdentity(...).WithMtlsProofOfPossession().ExecuteAsync()`
operation. There is no separate public MSAL API called a "proof request."
The precise term is **the IMDS token request containing the signed certificate
proof**.

MSAL selects the configured Windows RDP or Linux xrdp certificate, validates it
and its RSA private-key access, and checks the certificate-specific token cache.
If discovery or validation fails, it throws `MsalClientException` without
generating or selecting a replacement key. There is no BYOC option or bearer
fallback. A usable cache hit can be returned without a foreground IMDS request;
MSAL may still queue a proactive refresh.

When a token must be acquired, MSAL creates a short-lived JWT and signs it with
the selected certificate's private key using RS256. The JWT header includes
`alg: "RS256"`, `typ: "JWT"`, and `x5c` containing the public certificate.
Its payload contains:

| Claim | Value or purpose |
|---|---|
| `iss` | `urn:azure:token-binding:self-signed` |
| `sub` | Base64url-encoded SHA-256 hash of the certificate's DER encoding |
| `aud` | `http://169.254.169.254/metadata/identity/oauth2` |
| `resource` | Requested resource with `/.default`, matching the query parameter |
| `managed_identity` | `system_assigned` or the selected UAMI client ID |
| `cnf["x5t#S256"]` | The same SHA-256 certificate hash, identifying the requested binding |
| `iat`, `nbf`, `exp` | Issue time, validity start, and expiration; the proof lasts 60 seconds |
| `jti` | A new unique identifier for this acquisition attempt |

For example, a system-assigned Graph acquisition sends this request to the VM's
local IMDS endpoint. A UAMI acquisition also includes its `client_id` in the
query string:

```http
POST /metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fgraph.microsoft.com%2F.default HTTP/1.1
Host: 169.254.169.254
Metadata: true
Content-Type: application/json

{
  "req_cnf_proof": "<JWT signed with the configured RDP/xrdp private key>"
}
```

The signature demonstrates: **"I can use this certificate's private key, and I
am requesting a token bound to this certificate."** The public certificate lets
IMDS verify the signature. The private key is used locally for signing and is
never placed in the request. This proves possession of the signing key, not
hardware protection or attestation.

### Pending IMDS endorsement check

MSAL sends IMDS a signed `req_cnf_proof` using the selected certificate's private
key. This proof lets IMDS verify that the caller can use the corresponding
private key.

**For this non-attested flow, IMDS currently takes the submitted binding key at
face value.** Proof of possession alone does not establish that the submitted
certificate is the RDP certificate recorded for that VM.

The planned additional validation for the RDP path would happen inside IMDS.
The WS container document already includes the VM's RDP certificate, so IMDS
would read that certificate from the document and check the submitted binding
certificate against that record. This would add verification against the VM's
recorded certificate to the existing proof-of-possession mechanism.

**That additional WS-based certificate validation in IMDS is not implemented
yet.** Selecting the RDP certificate in MSAL does not, by itself, perform that
server-side check.

Until that IMDS change is implemented, successful token acquisition demonstrates
the current possession-proof flow, not completed WS-backed endorsement. The
Linux xrdp test likewise does not establish endorsement through a WS document.
Verification against the recorded certificate would not itself establish
hardware-backed key protection.
This pending server-side work does not introduce a requirement for MSAL to use
`IMsalWsTrustHttpClientFactory`.

### Why an ordinary HTTP factory is sufficient

This acquisition request uses **HTTP to IMDS**, not a client-certificate TLS
connection. The certificate signs the request body; it is not attached as a
TLS client certificate. MSAL passes `mtlsCertificate: null` to its HTTP manager.
Consequently, a configured `IMsalHttpClientFactory.GetHttpClient()` is sufficient;
neither a WS-Trust-specific interface nor an mTLS-specific interface is required.

Without a custom factory, MSAL selects its reusable client with automatic
redirects and server credentials disabled. With a custom factory, MSAL uses
the supplied client without replacing it or changing its handler settings.
That client and its handlers must not send server authentication credentials,
follow redirects, or retry/duplicate this request.

MSAL does not automatically replay the signed proof. A failed acquisition is
reported to the caller; a subsequent acquisition that reaches IMDS creates a
fresh proof with a new `jti`. Each execution retains its selected certificate
and request options, including while its proactive refresh is pending.

### Token acquisition is not a downstream mTLS call

On success, IMDS returns an `mtls_pop` token and MSAL returns the selected
certificate through `AuthenticationResult.BindingCertificate`. Using that token
at a downstream API is a separate step, requiring the resource's supported
certificate-bound authentication protocol and access to the matching private key.
Successful acquisition alone does not demonstrate resource-side acceptance or
rejection without the matching certificate.

The wire example describes the IMDS test-endpoint contract exercised by this
prototype. It does not establish production protocol compatibility, WS-backed
endorsement, or hardware-backed binding.

## Run

Run without arguments for the interactive menu. For one acquisition, including
noninteractive scheduled-task execution:

```text
ManagedIdentityAppVM.exe --rdp-live https://graph.microsoft.com
ManagedIdentityAppVM.exe --rdp-live https://graph.microsoft.com <UAMI-client-ID>
```

For a Linux self-contained publish, run `./ManagedIdentityAppVM` with the same
arguments instead. No separately installed .NET runtime is required for a
self-contained package. The default framework-dependent build requires .NET 8.

Append `--imds-error-details` to display selected service error fields on failure.
This is opt-in: error descriptions may contain identifiers or echoed request
data, so review them before sharing. This option does not change the token-display
toggle. MSAL retains failed HTTP response data in the standard
`MsalServiceException.ResponseBody` and `Headers` properties.

The framework-dependent app requires .NET 8. An existing private runtime can
launch it with `dotnet.exe ManagedIdentityAppVM.dll` and the same arguments.
The app does not elevate itself or change certificate/key permissions.

## Verified behavior and limits

A live run on November 7, 2026 acquired a Graph `mtls_pop` token through an earlier
explicit-certificate prototype under SYSTEM, saved it to the cache, and retained the supplied RDP
certificate. That VM's ordinary user could not open the RDP private key; SYSTEM
was the demonstrated test identity, not a general requirement of MSAL.
Automatic discovery inside MSAL has not yet been rerun on that VM.
The earlier explicit-certificate option has since been removed.

A live Linux run on November 9, 2026 exercised the parameterless API on Ubuntu
22.04 with the self-contained .NET 8 app. The VM initially had no xrdp
configuration. Test setup installed xrdp, created a dedicated self-signed
3072-bit RSA certificate, and configured its TLS listener on localhost only.
Private-key read access was granted to the service and test account through a
dedicated group; no public RDP access was opened. These were explicit test setup
steps, not actions performed by MSAL or the sample.

An RDP TLS handshake confirmed that xrdp presented the configured certificate.
MSAL automatically discovered that certificate, signed the proof, and acquired
a Graph `mtls_pop` token from IMDS with HTTP 200. The returned binding certificate
matched the configured certificate, and the token's `cnf["x5t#S256"]` matched its
SHA-256 certificate hash. No application-supplied certificate, experimental opt-in,
or attestation provider was used.

The Linux VM's IMDS compute `securityProfile` reported
`secureBootEnabled: "false"`, `virtualTpmEnabled: "false"`,
`encryptionAtHost: "false"`, and `securityType: ""`. This was a non-attested
software-key acquisition, not evidence of hardware-backed key protection.
The token endpoint itself did not return a `security_profile` field.
These live runs preceded the later custom-factory, option-snapshot, and diagnostic
repairs. Those repairs are validated locally; the VM deployment has not been
updated to the latest local source.

The experimental endpoint uses API version `2018-02-01`, JSON `req_cnf_proof`,
and the resource with `/.default` in both the query and signed proof, matching
the successful PoC. MSAL's cache/result resource remains normalized without the
suffix. The normal bearer and attested protocols are unchanged.

The earlier Windows Key Vault request returned `invalid_resource`; it was not
retested on Linux. Neither live acquisition established a Graph resource call,
resource-side binding enforcement, or a live cache-hit result. The RDP
certificates' server-only EKU is also a separate consideration for resource-side
client TLS. Successful discovery and token acquisition do not establish that a
downstream API accepts the certificate or rejects a request without it.

Regression coverage lives in
`tests\Microsoft.Identity.Test.Unit\ManagedIdentityTests\EndorsedUnattestedManagedIdentityTests.cs`,
including cryptographic proof checks, wire values, response validation, cache
isolation/reuse, force refresh, certificate ownership, and failure behavior.
`LinuxRdpCertificateReaderTests.cs` covers xrdp configuration parsing, documented
defaults, matching PKCS#1/PKCS#8 PEM pairs, retained private-key usability,
cancellation, inaccessible files, and failure without selecting a replacement key.
