# Managed Identity VM test app

This existing console app references the local MSAL and KeyAttestation projects.
It calls the real managed identity endpoints; it contains no IMDS simulator,
synthetic certificate generator, HTTP transport, or loopback resource server.

| Choice | Acquisition |
|---|---|
| A | Existing bearer flow |
| P | Existing Windows-only mTLS PoP flow with attestation |
| N | Unattested workload token binding, with automatic RDP/xrdp endorsement when available |

Identity selection, resource selection, force refresh, and cache-repeat controls
remain available. N supports SAMI or a UAMI client ID. All modes use the existing
token display controls: a preview by default and the full token when F is enabled.
Full tokens are secrets; do not share that output.

## Endorsed unattested flow for token binding

**Implementation status:** this prototype implements the two-key request contract.
Interoperability requires the matching IMDS implementation and, for endorsement,
a matching host-published RDP certificate registration. Local certificate discovery
alone does not establish endorsement or hardware-backed key protection.

The parameterless request without
`WithAttestationSupport()` makes MSAL create or reuse a workload binding certificate
and locate an optional Windows RDP listener or Linux xrdp endorsement certificate.
Linux support requires .NET 8 or later.
The app does not load or pass a certificate:

```csharp
var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
    .Build();

var result = await app.AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

No `WithExperimentalFeatures(true)` or `WithAttestationSupport()` call is needed for N.
MSAL owns workload-key creation, RDP certificate discovery, proof and endorsement signing,
the IMDS request, response handling, token caching, and returning
`AuthenticationResult.BindingCertificate`. The flow uses MSAL's existing HTTP
client configuration without introducing feature-specific proxy settings.
The app retains the MSAL application for repeat acquisitions. Each execution
selects the current endorsement certificate before cache lookup. Cache entries
are separated by workload binding certificate, endorsement certificate (or its
absence), identity, and requested binding-strength floor. Endorsement rotation
therefore requires a fresh acquisition without changing the workload binding key.
Each execution retains both certificates for any queued proactive refresh.
MSAL reuses its workload certificate per application, rotating it when less than
one day remains in its 30-day lifetime, without disposing caller-held certificates.
Common request options, including claims, are also captured for the execution.
Changing claims on a completed builder does not change its queued refresh;
a new execution still rejects claims unsupported by this protocol.

There is no bring-your-own-certificate option for this managed identity flow.
MSAL returns the workload certificate, never the RDP/xrdp endorsement certificate,
through `AuthenticationResult.BindingCertificate`. The workload certificate remains
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
An SSH login key is not an X.509 endorsement certificate.

When an RDP/xrdp certificate is found, it requires an accessible RSA private key.
If the listener/configuration or certificate is absent, MSAL logs that absence and
uses its own workload binding certificate without endorsement. Invalid configuration,
an invalid certificate, an inaccessible or mismatched private key, and other access
failures throw `MsalClientException`; these are not treated as absence.
MSAL does not provision RDP, replace its keys, change permissions, or fall back to bearer.
The existing Windows attested flow is unchanged when `WithAttestationSupport()` is used.
Attested PoP and bearer-over-mTLS remain Windows-only and fail explicitly on Linux.
The endorsed unattested flow selects the RDP certificate regardless
of the application's experimental-feature setting.
Both endorsed and unendorsed requests use the experimental Software-strength
protocol; a KeyGuard minimum is rejected before discovery. Endorsement is an IMDS
admission check, not a stronger binding-strength claim.

On an IMDSv1 host without the existing Windows TVM/CVM capability signal,
capability discovery also checks RDP/xrdp availability on the supported operating
system. A usable certificate or its confirmed absence reports Software strength;
access or validation failures do not enable that capability.
Discovery does not create a workload key, prove WS-backed endorsement, or
guarantee that a production IMDS endpoint implements this prototype protocol.

No custom HTTP factory is needed for this sample. If configured through
`WithHttpClientFactory`, the existing `IMsalHttpClientFactory.GetHttpClient()`
is used without requiring another interface or replacing the client's handlers,
proxy configuration, or timeout. The custom client must not send server
authentication credentials, follow redirects, or retry/duplicate proof requests
in its handler pipeline. Use a separate factory instance for the managed identity
application if other applications need different handler policies.
The custom/default factory decision is captured when the application is built;
reusing its builder cannot change an existing application's transport selection.

Without a custom factory, MSAL uses its pooled anonymous, redirect-free client.
MSAL sends each signed proof once, with no automatic retry. MEX and WS-Trust keep
their existing factory selection and fallback behavior.

## What the IMDS token request proves

The **endorsed unattested flow for token binding** is part of the existing
`AcquireTokenForManagedIdentity(...).WithMtlsProofOfPossession().ExecuteAsync()`
operation. There is no separate public MSAL API called a "proof request."
The precise term is **the IMDS token request containing the signed certificate
proof**.

MSAL creates or reuses its own workload binding certificate and discovers the
optional RDP/xrdp endorsement certificate. Confirmed absence selects unendorsed
binding; access and validation failures remain explicit errors. There is no
BYOC option or bearer fallback. A cache hit for the same binding and endorsement
context can return without a foreground IMDS request; MSAL may queue a proactive refresh.

When a token must be acquired, MSAL creates a short-lived JWT and signs it with
the workload binding private key using RS256. The JWT header includes
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
POST /metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fgraph.microsoft.com%2F.default&withMinStrength=1 HTTP/1.1
Host: 169.254.169.254
Metadata: true
Content-Type: application/json

{
  "req_cnf_proof": "<compact JWT signed with the workload binding key>",
  "endorsement_certificate": "<standard-base64 DER RDP/xrdp public certificate>",
  "endorsement_signature": "<standard-base64 detached signature>"
}
```

The inner signature proves possession of the workload binding key. The detached
endorsement is RSA-PKCS1-v1_5-SHA256 over the UTF-8 bytes of the exact completed
compact JWT, including its inner signature. It is not a signature over the JSON
body or decoded JWT payload. Both private keys remain local.

If no RDP/xrdp certificate is found, the body contains only `req_cnf_proof` and
MSAL sends `withMinStrength=0`. With an endorsement it sends `1`. This query
parameter is independent of `PoPOptions.MinStrength`, which remains a
key-protection floor. No new public option is needed for automatic selection.

### IMDS endorsement admission

The matching IMDS contract compares the supplied endorsement certificate's SHA-1
thumbprint with the requesting container's host-published certificate registration,
then verifies the detached signature. ESTS continues validating the inner proof.
The SHA-1 registration lookup is distinct from the workload binding certificate's
SHA-256 `cnf["x5t#S256"]`.

Only query values `0` and `1` are defined. A supplied endorsement must validate
regardless of that value. A required but missing endorsement, invalid/incomplete
endorsement, or missing/mismatched registration returns HTTP 400; registration
lookup failure returns HTTP 503. MSAL surfaces failures without replaying the
proof, dropping endorsement, or switching to bearer.

This admission gate adds no token-strength claim and does not establish
hardware-backed protection. It requires no WS-Trust-specific HTTP factory.
The server demo bypasses bound-token caching and renewal; that demo behavior does
not disable MSAL's existing token cache or proactive refresh.

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

On success, IMDS returns an `mtls_pop` token with numeric `expires_in`, and MSAL returns the workload
certificate through `AuthenticationResult.BindingCertificate`. Using that token
at a downstream API is a separate step, requiring the resource's supported
certificate-bound authentication protocol and access to the matching private key.
Successful acquisition alone does not demonstrate resource-side acceptance or
rejection without the matching certificate.

The wire example describes the prototype contract. Its implementation does not
establish production compatibility, successful host-registration validation, or
hardware-backed binding; those require independent end-to-end evidence.

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

## Earlier protocol evidence and current limits

The runs below predate the two-key endorsement contract. They used the RDP/xrdp
certificate as the binding certificate and do not validate the updated request,
separate workload key, or host-registration admission.

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
A follow-up Linux VM run used the repaired SDK, including common-option
snapshots, captured HTTP-factory selection, equivalent client-ID handling, and
RDP capability discovery. All 161 focused regression tests passed on that VM.
The updated app also acquired a Graph `mtls_pop` token from IMDS and returned
the configured xrdp certificate. The Windows VM has not been rerun with these
repairs.

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
