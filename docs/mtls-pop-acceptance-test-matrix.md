# MSAL mTLS PoP Acceptance-Test Matrix

## Approval criteria

- **P0**: Required in the PR.
- **P1**: Required before PR merge.
- Every P0 test must include captured request, response, endpoint, and TLS evidence where applicable.
- Unit-test success alone is insufficient for tests marked as end-to-end.

## Configuration and credential validation

| ID | Test | Setup and action | Required assertions | Priority |
|---|---|---|---|---|
| CFG-01 | Certificate credential | Configure a valid certificate and request mTLS PoP. | The request succeeds. The certificate used for TLS matches the configured leaf certificate. | P0 |
| CFG-02 | Client secret rejected | Configure a client secret and request mTLS PoP. | Fail before any token request with a clear unsupported-credential error. | P0 |
| CFG-03 | Assertion credential rejected | Configure a static or dynamic assertion and request pure-certificate mTLS PoP. | Fail before network. Do not fall back to assertion authentication. | P0 |
| CFG-04 | Missing private key | Provide a public certificate without its private key. | Fail before network with a private-key error. | P0 |
| CFG-05 | Empty certificate chain | Provide an empty TLS certificate. | See if we handle services errors correctly. | P0 |
| CFG-06 | Opaque signer | Use a non-exportable `crypto.Signer` implementation. | The TLS handshake succeeds without exporting the key or casting it to a concrete key type. | P1 |

## Token-request semantics

| ID | Test | Setup and action | Network assertions | Expected result | Priority |
|---|---|---|---|---|---|
| REQ-01 | Global PoP request | Use a tenanted AAD authority without a region. | POST to `https://mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. | An `mtls_pop` token is returned. | P0 |
| REQ-02 | Regional PoP request | Configure `westus3`. | POST to `https://westus3.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. | An `mtls_pop` token is returned. | P0 |
| REQ-03 | TLS certificate presented | Configure the token server to require a client certificate. | Capture the TLS peer certificate. Its leaf DER must equal the configured certificate. | The handshake and request succeed. | P0 |
| REQ-04 | Required body parameters | Capture the PoP request body. | It contains `client_id`, `scope`, `grant_type=client_credentials`, and `token_type=mtls_pop`. | The request is accepted. | P0 |
| REQ-05 | No assertion parameters | Capture the PoP request body. | It contains neither `client_assertion` nor `client_assertion_type`. | Authentication occurs through the TLS certificate. | P0 |
| REQ-06 | No `req_cnf` | Capture the PoP request body. | `req_cnf` is absent. | Binding is performed by the TLS certificate. | P0 |
| REQ-07 | Reserved extra parameters | Supply `client_assertion`, `client_assertion_type`, `req_cnf`, or a conflicting `token_type` through extra body parameters. | Reject the request before network or safely remove the reserved values. | The pure-certificate contract cannot be overridden. | P0 |
| REQ-08 | Claims and supported extra parameters | Add legitimate claims and nonreserved extra body parameters. | The supplied parameters are preserved without changing the mTLS parameters. | PoP acquisition succeeds. | P1 |
| REQ-09 | Server returns `Bearer` | Mock the token endpoint to return a valid token with `token_type=Bearer`. | The token is neither exposed nor cached. | Return a typed token-type mismatch error. | P0 |

## Baseline SNI regression

| ID | Test | Setup and action | Required assertions | Priority |
|---|---|---|---|---|
| SNI-01 | Existing SNI assertion | Use the same certificate without the PoP option and enable x5c. | Use the regular token endpoint. Send `client_assertion` and x5c. Do not present a TLS client certificate. | P0 |
| SNI-02 | Normal token result | Complete the SNI assertion flow. | The token type is `Bearer` and the binding certificate is absent. | P0 |
| SNI-03 | Normal-to-PoP isolation | Request a normal token and then a PoP token for the same app and scope. | The tokens and cache entries differ. The PoP request cannot return the cached normal token. | P0 |
| SNI-04 | PoP-to-normal isolation | Request a PoP token and then a normal token for the same app and scope. | The normal request cannot return the cached PoP token. | P0 |

## Region behavior

| ID | Test | Setup and action | Expected endpoint or result | Priority |
|---|---|---|---|---|
| REG-01 | Explicit region | Configure a known region. | Use `https://{region}.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. | P0 |
| REG-02 | No region | Do not configure a region. | Use `https://mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. | P0 |
| REG-03 | Automatic detection succeeds | Mock IMDS to return `eastus`. | Use `https://eastus.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. | P0 |
| REG-04 | Automatic detection fails | Make IMDS time out or return an error. | Use the global endpoint without downgrading to a normal token. | P0 |
| REG-05 | Invalid region | Configure a malformed or unsupported region. | Return an explicit validation or service error without constructing an unsafe hostname. | P1 |
| REG-06 | Regional resource use | Acquire a regional PoP token and call the mTLS resource. | The resource returns HTTP 200. | P0 |
| REG-07 | Global and regional cache behavior | Acquire globally and then regionally with the same certificate. | Follow the defined cache behavior, retain the correct binding certificate, and do not crash. | P1 |

## Authority and cloud routing

| ID | Test | Setup and action | Expected behavior | Priority |
|---|---|---|---|---|
| AUTH-01 | Specific tenant | Use a tenant GUID or verified tenant domain. | Token acquisition succeeds. | P0 |
| AUTH-02 | `/common` | Request PoP using `/common`. | Fail before the token request or credential transmission. | P0 |
| AUTH-03 | `/organizations` | Request PoP using `/organizations`. | Fail before the token request or credential transmission. | P0 |
| AUTH-04 | `/consumers` | Request PoP using `/consumers`. | Fail before the token request or credential transmission. | P0 |
| AUTH-05 | Tenant override | Override the authority with another specific tenant. | The correct tenant appears in the endpoint path and cache key. | P1 |
| AUTH-06 | Public AAD | Use `login.microsoftonline.com`. | Route to the appropriate global or regional `mtlsauth.microsoft.com` endpoint. | P0 |
| AUTH-07 | US Government | Use `login.microsoftonline.us`. | Route to `mtlsauth.microsoftonline.us`. | P0 when sovereign support is claimed |
| AUTH-08 | China | Use `login.partner.microsoftonline.cn`. | Route to `mtlsauth.partner.microsoftonline.cn`. | P0 when sovereign support is claimed |
| AUTH-09 | Legacy US Government alias | Use `login.usgovcloudapi.net`. | Route to `mtlsauth.microsoftonline.us`. | P0 when sovereign support is claimed |
| AUTH-10 | Legacy China alias | Use `login.chinacloudapi.cn`. | Route to `mtlsauth.partner.microsoftonline.cn`. | P0 when sovereign support is claimed |
| AUTH-11 | Non-`login.*` AAD host | Configure an unsupported AAD-shaped hostname. | Fail before sending the certificate to a transformed host. | P0 |
| AUTH-12 | Malformed authority | Use HTTP, omit the tenant, or provide an invalid URL. | Fail before network. | P0 |

## dSTS and generic identity providers

| ID | Test | Setup and action | Expected behavior | Priority |
|---|---|---|---|---|
| IDP-01 | Tenanted dSTS | Configure `https://{host}/dstsv2/{tenant}/`. | Send the request to `https://{host}/dstsv2/{tenant}/oauth2/v2.0/token`; do not apply an AAD `mtlsauth` rewrite or regional routing; verify `token_type=mtls_pop`. | P0 |
| IDP-02 | dSTS cache reuse | Repeat the identical dSTS mTLS PoP request. | Return the token from cache with token type and binding information preserved. | P0 |
| IDP-03 | Non-tenanted dSTS | Use dSTS `/common` and `/organizations`. | Reject the request without acquiring a token. | P0 |

## Token binding and result validation

| ID | Test | Setup and action | Required assertions | Priority |
|---|---|---|---|---|
| BIND-01 | Result certificate | Acquire a PoP token. | The result contains a TLS certificate, parsed leaf, and private key. | P0 |
| BIND-02 | Thumbprint algorithm | Compute SHA-256 over the complete leaf DER. | Its base64url value equals `BindingCertificateThumbprint()`. | P0 |
| BIND-03 | Token confirmation claim | Decode the real access token. | `cnf["x5t#S256"]` equals the result certificate thumbprint. | P0 |
| BIND-04 | Correct authorization scheme | Call the resource using `Authorization: mtls_pop <token>`. | The resource returns HTTP 200. | P0 |
| BIND-05 | Wrong authorization scheme | Call the resource using `Authorization: Bearer <token>`. | The resource rejects the request. | P0 |
| BIND-06 | Missing resource certificate | Call the resource without a TLS client certificate. | The resource rejects the request. | P0 |
| BIND-07 | Wrong resource certificate | Call the resource using another certificate. | The resource rejects the request. | P0 |
| BIND-08 | Exact returned certificate | Use the result certificate directly without reconstructing it. | The TLS handshake and resource call succeed. | P0 |

## Cache and certificate lifecycle

| ID | Test | Setup and action | Expected behavior | Priority |
|---|---|---|---|---|
| CACHE-01 | Same certificate | Repeat an identical PoP request. | Return a cache hit with the same token and complete binding-certificate metadata. | P0 |
| CACHE-02 | Same key, renewed certificate | Create two certificates with the same key but different DER. | The second certificate causes a token-cache miss. | P0 |
| CACHE-03 | Different certificates | Use separate certificates for the same app and scope. | Tokens and transports remain isolated. | P0 |
| CACHE-04 | Certificate-chain change | Keep the leaf unchanged but alter the issuer chain. | Cache behavior follows the documented leaf-DER binding definition. | P1 |
| CACHE-05 | Failed downgrade | Return a normal token for a PoP request. | Do not write the response to the cache. | P0 |
| CACHE-06 | Persistent shared cache | Share serialized cache storage between clients using different certificates. | Neither client receives a token bound to the other certificate. | P0 |
| CACHE-07 | Cached result fields | Return a PoP token from cache. | Preserve token type, binding certificate, leaf, private key, and thumbprint. | P0 |

## HTTP transport and concurrency

| ID | Test | Setup and action | Expected behavior | Priority |
|---|---|---|---|---|
| HTTP-01 | Default transport | Use the built-in mTLS transport. | Enforce TLS 1.2 minimum while preserving normal proxy and dialing behavior. | P1 |
| HTTP-02 | Custom mTLS factory | Supply a recording factory. | The factory receives the correct certificate and returns the token-request client. | P0 |
| HTTP-03 | Nil factory result | Make the factory return `nil`. | Return an explicit error without panicking. | P0 |
| HTTP-04 | Same-certificate reuse | Perform repeated requests with one certificate. | Reuse one mTLS client and connection pool for the certificate thumbprint. | P1 |
| HTTP-05 | Certificate rotation | Change the certificate DER. | Create a new mTLS client and do not reuse the old certificate's transport. | P0 |
| HTTP-06 | Concurrent same certificate | Run many parallel acquisitions under the race detector. | No races, one transport, and consistent results. | P0 |
| HTTP-07 | Concurrent different certificates | Run parallel clients with different certificates. | No cross-certificate transport or cache leakage. | P0 |
| HTTP-08 | Cancellation | Cancel before and during the TLS request. | Return promptly and do not cache a partial result. | P0 |
| HTTP-09 | Timeout | Make the token endpoint stall. | Apply a bounded timeout and return a clear network error. | P1 |

## Platform acceptance

| ID | Test | Platform | Expected behavior | Priority |
|---|---|---|---|---|
| PLAT-01 | Windows software certificate | Windows | Token acquisition and the resource call succeed. | P0 |
| PLAT-02 | Linux PEM certificate | Linux | Token acquisition and the resource call succeed. | P0 |
| PLAT-03 | TLS 1.2 | Windows and Linux | The token and resource TLS handshakes succeed. | P0 |
| PLAT-04 | Non-exportable signer | A supported hardware or platform key provider | Token and resource handshakes use `crypto.Signer` without exporting the key. | P1 |
