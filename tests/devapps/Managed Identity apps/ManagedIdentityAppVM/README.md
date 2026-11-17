# Managed Identity VM test app

This app references the local MSAL and KeyAttestation projects and calls real
managed identity endpoints. Run without arguments for the interactive menu:

| Choice | Acquisition |
|---|---|
| A | Bearer |
| P | Windows mTLS PoP with attestation |
| N | Workload binding with optional RDP/xrdp endorsement |

## Workload binding

```csharp
var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://vault.azure.net")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

Reuse the application instance. No new public option, application-supplied
certificate, or experimental-feature opt-in is required.

- **K1** is MSAL's workload key. Its certificate is returned as
  `AuthenticationResult.BindingCertificate` and binds the token.
- **K2** is the existing configured RDP/xrdp key. When found, it endorses K1's
  signed proof; it is not the resource TLS key.
- No K2 means unendorsed acquisition. Unreadable configuration, an unusable key,
  or a rejected endorsement fails; MSAL never retries without endorsement.

MSAL signs `req_cnf_proof` with K1. With K2 available, it adds
`endorsement_certificate` and an RSA/SHA-256 PKCS#1 signature over the exact
UTF-8 proof string. The IMDS request sends `withMinStrength=1` with endorsement,
or `0` without it. This wire parameter is separate from `PoPOptions.MinStrength`,
which remains a key-protection floor. Both paths use software-protected K1.

### Certificates and lifetime

K1's certificate expires 30 days after creation. Before acquisition, MSAL rotates
K1 when 24 hours plus its five-minute expiration buffer remain. This relies on
the prototype service's 24-hour token-lifetime contract. MSAL honors IMDS's
reported token lifetime without rejecting or clamping it to K1's certificate.
Previously returned certificates are not disposed during rotation.

MSAL checks that K2 has an accessible RSA private key of at least 2048 bits.
IMDS owns K2's acceptance policy, including validity dates and registration.
MSAL does not reject K2 solely because its certificate is expired or not yet valid.
This does not relax K1's validity checks for resource TLS.

Windows discovery selects the RDP-Tcp listener certificate by its configured
thumbprint from the local-machine Remote Desktop or My store. Linux discovery
requires .NET 8 or later and reads `certificate` and `key_file` in the first
`[Globals]` section of `/etc/xrdp/xrdp.ini`. Defaults are `/etc/xrdp/cert.pem`
and `/etc/xrdp/key.pem`; configured paths must be absolute and the private key
must be unencrypted. MSAL does not install RDP/xrdp, register certificates, or
change private-key permissions.

Workload keys are reused per application instance. Windows uses the existing
persisted CNG-key provider for Schannel compatibility; Linux uses in-memory RSA.
Tokens are cached separately by K1, endorsement context, identity, and requested
strength. Replacing K2 triggers a new acquisition without necessarily rotating K1.

### Resource calls and transport

Call the resource separately using the token's returned `BindingCertificate`
and its token type in the Authorization header. Keep each token paired with its
own certificate across rotation; K2 is not used in the resource call.
Successful acquisition alone does not demonstrate resource access.

Acquisition uses HTTP to IMDS, not client-certificate TLS. MSAL sends each signed
proof once. A configured `IMsalHttpClientFactory` remains supported, but its
handler pipeline must not follow redirects, send server credentials, or retry
proof requests. Without a custom factory, MSAL uses its pooled anonymous,
redirect-free client.

## Run a single acquisition

```text
ManagedIdentityAppVM.exe --rdp-live https://vault.azure.net
ManagedIdentityAppVM.exe --rdp-live https://vault.azure.net <UAMI-client-ID>
```

On Linux, use `dotnet ManagedIdentityAppVM.dll` or a self-contained executable.
The framework-dependent app requires .NET 8. Append `--imds-error-details`
only when service-error diagnostics are needed; review that output before sharing.
The interactive full-token display option prints secrets and should remain off.

## Prototype limits

This flow requires a compatible IMDS implementation and, for endorsement, the
matching platform certificate registration. Local discovery does not establish
registration or hardware protection. API version `2018-02-01` and the resource's
`/.default` wire suffix are provisional protocol details, not a production
compatibility guarantee.

Supported identities are SAMI and UAMI by client ID. Claims, endpoint overrides,
and KeyGuard minimum strength are not supported by this unattested protocol.
The existing Windows attested flow remains available through
`WithAttestationSupport()`.
