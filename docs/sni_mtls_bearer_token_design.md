# Mini Spec: mTLS Bearer for Confidential Client Certificates

## Decision

Add an app-level certificate transport option:

```csharp
public record CertificateOptions
{
    public bool SendX5C { get; init; } = false;
    public bool AssociateTokensWithCertificate { get; init; } = false;

    // New
    public bool SendCertificateOverMtls { get; init; } = false;
}
```

## Intent

Separate these two concerns:

1. **Certificate transport**
   - JWT client assertion in request body
   - TLS client certificate over mTLS

2. **Token type**
   - Bearer
   - mTLS PoP

## Rule

`WithMtlsProofOfPossession()` is a request-level requirement and **implies mTLS transport**.

`SendCertificateOverMtls` is only the **default transport setting** for requests that do not explicitly request PoP.

## Effective Behavior

### App-level default
- `SendCertificateOverMtls = false`
  - default to assertion-based certificate auth
- `SendCertificateOverMtls = true`
  - default to mTLS certificate transport

### Request-level override
- `.WithMtlsProofOfPossession()`
  - always use mTLS transport
  - request mTLS PoP token type

## Behavior Matrix

| App `SendCertificateOverMtls` | Request `.WithMtlsProofOfPossession()` | Effective Certificate Transport | Token Type | Result |
|---|---:|---|---|---|
| false | No | Request body assertion | Bearer | Existing |
| true  | No | mTLS handshake | Bearer | New |
| false | Yes | mTLS handshake | mTLS PoP | Existing / preserved |
| true  | Yes | mTLS handshake | mTLS PoP | Existing / preserved |

## Why no throw when `SendCertificateOverMtls` is set to `false` for mTLS PoP?

Because `false` does **not** mean “mTLS forbidden.”
It only means “do not use mTLS by default.”

If the request explicitly asks for PoP, request-level semantics win.

## Validation

### Valid
- Certificate + default options + Bearer
- Certificate + `SendCertificateOverMtls=true` + Bearer
- Certificate + PoP
- Certificate + `SendCertificateOverMtls=true` + PoP

### Invalid
- Secret credential + `SendCertificateOverMtls=true`
- Signed assertion string/delegate + `SendCertificateOverMtls=true`
- `.WithMtlsProofOfPossession()` without a certificate credential

## Implementation Notes

### 1. Public API
Add `SendCertificateOverMtls` to `CertificateOptions`.

### 2. Builder/config
Flow `SendCertificateOverMtls` through:
- `ConfidentialClientApplicationBuilder.WithCertificate(X509Certificate2, CertificateOptions)`
- callback-based `WithCertificate(..., CertificateOptions)` overloads

### 3. Resolver
Compute:

```csharp
bool useMtlsTransport =
    request.IsMtlsPopRequested ||
    appConfig.SendCertificateOverMtls;
```

### 4. Credential material
For certificate credentials:

- if `useMtlsTransport == true`
  - return empty body auth params
  - return resolved certificate for HTTP/TLS layer

- else
  - return client assertion in request body

### 5. Token client
No semantic change beyond honoring the resolved certificate when `useMtlsTransport == true`.

## Non-Goals

- No new `.WithBoundBearer()` API
- No breaking change to existing PoP behavior
- No cache behavior changes

## Tests

### Unit
- `SendCertificateOverMtls` defaults to false
- builder stores the new option
- PoP forces mTLS even when app option is false
- bearer + `SendCertificateOverMtls=true` uses mTLS
- unsupported credential types fail in mTLS transport mode

### Integration
- SNI Bearer still works
- mTLS Bearer works
- PoP still works without setting `SendCertificateOverMtls`
- PoP also works when `SendCertificateOverMtls=true`
