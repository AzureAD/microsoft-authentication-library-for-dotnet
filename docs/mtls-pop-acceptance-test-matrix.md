# MSAL.NET mTLS PoP Acceptance-Test Matrix

## Scope and acceptance criteria

- This matrix defines the acceptance contract for MSAL.NET mTLS Proof-of-Possession support.
- Every listed test is required unless the row explicitly says it applies only when a platform or cloud is supported.
- End-to-end tests must capture the request endpoint, relevant request parameters, token response, and TLS client-certificate evidence.
- Unit-test success alone is insufficient for scenarios marked as end-to-end.
- Tests should assert externally observable behavior. Certificate-cache, token-cache, synchronization, and HTTP transport implementations remain internal to MSAL.NET.

## Credential modes

| Mode | Credential | Token-endpoint authentication |
|---|---|---|
| Certificate | `X509Certificate2` credential | Present the certificate over mTLS. Do not send client-assertion parameters. |
| Federated assertion | `ClientSignedAssertion` containing an assertion and `TokenBindingCertificate` | Present the binding certificate over mTLS and send the assertion with `client_assertion_type=jwt-pop`. |
| Managed Identity | Managed Identity with SDK-provided binding certificate | Use the Managed Identity flow and return the SDK-provided binding certificate with the token. |

## Configuration and credential validation

| ID | Test | Setup and action | Required assertions |
|---|---|---|---|
| CFG-01 | Certificate credential | Configure a valid `X509Certificate2` credential and request mTLS PoP. | Acquisition succeeds. The certificate presented over TLS matches the configured leaf certificate. |
| CFG-02 | Federated assertion with binding certificate | Return a valid `ClientSignedAssertion` containing an assertion and binding certificate. | Acquisition succeeds. The assertion uses `jwt-pop`, and the same binding certificate is presented over TLS. |
| CFG-03 | Managed Identity credential | Request mTLS PoP through a supported Managed Identity environment. | Acquisition succeeds and returns the SDK-provided binding certificate. |
| CFG-04 | Client secret rejected | Configure a client secret and request mTLS PoP. | Fail before any token request with `MtlsCertificateNotProvided` or the established unsupported-credential error. |
| CFG-05 | Assertion without binding certificate rejected | Configure a static assertion, string callback, or `ClientSignedAssertion` without a binding certificate and request mTLS PoP. | Fail before network with a clear missing-binding-certificate error. |
| CFG-06 | Missing private key | Use a certificate credential or FIC binding certificate without an accessible private key. | Fail before network with a private-key or unusable-certificate error. |
| CFG-07 | Empty or malformed certificate | Use an empty certificate, malformed certificate data, or unusable certificate chain with certificate and FIC credentials. | Fail before network with a deterministic certificate-validation error. Do not defer the failure to the service or TLS handshake. |
| CFG-08 | Non-exportable private key | Use an `X509Certificate2` backed by a non-exportable CNG, KSP, HSM, TPM, or KeyGuard key. | Token and resource TLS handshakes succeed without exporting the private key or requiring a concrete exportable key type. |

## Token-request semantics

| ID | Test | Setup and action | Network assertions | Expected result |
|---|---|---|---|---|
| REQ-01 | Global certificate request | Use a certificate credential with a tenanted AAD authority and no region. | POST to `https://mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. Include `client_id`, `scope`, `grant_type=client_credentials`, and `token_type=mtls_pop`. Do not include `client_assertion`, `client_assertion_type`, or `req_cnf`. | An `mtls_pop` token is returned. |
| REQ-02 | Regional certificate request | Configure `westus3` with a certificate credential. | POST to `https://westus3.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token` with the certificate-request parameters from REQ-01. | An `mtls_pop` token is returned. |
| REQ-03 | Federated assertion request | Use `ClientSignedAssertion` with a binding certificate. | Include `client_assertion`, `client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-pop`, and `token_type=mtls_pop`. Do not include `req_cnf`. Present the callback-provided certificate over TLS. | An `mtls_pop` token is returned. |
| REQ-04 | TLS certificate presented | Configure the token server to require a client certificate for certificate and federated-assertion modes. | Capture the TLS peer certificate. Its leaf DER equals the selected binding certificate's `RawData`. | The handshake and request succeed. |
| REQ-05 | Claims and supported extra parameters | Add legitimate claims and nonreserved extra body parameters. | Preserve the supplied parameters without changing MSAL's mTLS parameters for the selected credential mode. | PoP acquisition succeeds. |
| REQ-06 | Server returns `Bearer` | Mock the token endpoint to return a valid token with `token_type=Bearer` for an mTLS PoP request. | Do not expose or cache the response as a successful PoP result. | Return the established token-type mismatch error. |

## Baseline SNI regression

| ID | Test | Setup and action | Required assertions |
|---|---|---|---|
| SNI-01 | Existing SNI assertion end-to-end | Use the same certificate without the PoP option and enable x5c. | Use the regular token endpoint. Send a certificate-signed `client_assertion` with x5c. Do not present a TLS client certificate. Return a `Bearer` token with no `BindingCertificate`. |
| SNI-02 | Normal-to-PoP isolation | Request a normal token and then a PoP token for the same app and scope. | The tokens and cache entries differ. The PoP request cannot return the cached normal token. |
| SNI-03 | PoP-to-normal isolation | Request a PoP token and then a normal token for the same app and scope. | The normal request cannot return the cached PoP token. |

## Endpoint and authority routing

| ID | Test | Setup and action | Expected endpoint or result |
|---|---|---|---|
| ROUTE-01 | Specific tenant | Use a tenant GUID or verified tenant domain. | Token acquisition succeeds against the corresponding mTLS endpoint. |
| ROUTE-02 | Explicit region | Configure a valid region. | Use `https://{region}.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. |
| ROUTE-03 | Automatic detection succeeds | Mock IMDS to return `eastus`. | Use `https://eastus.mtlsauth.microsoft.com/{tenant}/oauth2/v2.0/token`. |
| ROUTE-04 | Automatic detection fails | Make IMDS time out or return an error. | Use the global mTLS endpoint without downgrading to a normal token. |
| ROUTE-05 | Invalid region | Configure a malformed region or return one from mocked discovery. | Ignore or reject the invalid value according to the public regional contract. Never incorporate it into the authority hostname. |
| ROUTE-06 | `/common` rejected | Request PoP using `/common`. | Fail before the token request or credential transmission. |
| ROUTE-07 | `/organizations` rejected | Request PoP using `/organizations`. | Fail before the token request or credential transmission. |
| ROUTE-08 | `/consumers` supported | Request PoP using `/consumers`. | Treat `/consumers` as a tenant and continue through normal endpoint and service validation. |
| ROUTE-09 | Tenant override | Override the authority with another specific tenant. | The selected tenant appears in the endpoint path and cache key. |
| ROUTE-10 | Public AAD | Use `login.microsoftonline.com`. | Route to the appropriate global or regional `mtlsauth.microsoft.com` endpoint. |
| ROUTE-11 | US Government | Use `login.microsoftonline.us`. | Route to `mtlsauth.microsoftonline.us` when the cloud is supported. |
| ROUTE-12 | China | Use `login.partner.microsoftonline.cn`. | Route to `mtlsauth.partner.microsoftonline.cn` when the cloud is supported. |
| ROUTE-13 | France sovereign cloud | Use `login.sovcloud-identity.fr`. | Route to `mtlsauth.sovcloud-identity.fr` when the cloud is supported. |
| ROUTE-14 | Germany sovereign cloud | Use `login.sovcloud-identity.de`. | Route to `mtlsauth.sovcloud-identity.de` when the cloud is supported. |
| ROUTE-15 | Singapore sovereign cloud | Use `login.sovcloud-identity.sg`. | Route to `mtlsauth.sovcloud-identity.sg` when the cloud is supported. |
| ROUTE-16 | Unsupported legacy aliases | Use `login.usgovcloudapi.net` or `login.chinacloudapi.cn`. | Fail before sending the certificate with `MtlsPopNotSupportedForEnvironment`. |
| ROUTE-17 | Unsupported non-`login.*` host | Configure an unsupported AAD-shaped hostname. | Fail before sending the certificate to a transformed host. |
| ROUTE-18 | Malformed authority | Use HTTP, omit the tenant, or provide an invalid URL. | Fail before network. |

## dSTS and generic identity providers

| ID | Test | Setup and action | Expected behavior |
|---|---|---|---|
| IDP-01 | Tenanted dSTS | Configure `https://{host}/dstsv2/{tenant}/`. | Send the request to `https://{host}/dstsv2/{tenant}/oauth2/v2.0/token`. Do not apply an AAD `mtlsauth` rewrite or regional routing. Verify `token_type=mtls_pop`. |
| IDP-02 | dSTS cache reuse | Repeat the identical dSTS mTLS PoP request. | Return the token from cache with token type and binding-certificate information preserved. |
| IDP-03 | Non-tenanted dSTS | Use dSTS `/common` and `/organizations`. | Reject the request without acquiring a token. |

## Token binding and resource validation

Run BIND-01, BIND-02, BIND-03, and BIND-07 for each supported credential mode: certificate, federated assertion with `X509Certificate2`, and Managed Identity.

| ID | Test | Setup and action | Required assertions |
|---|---|---|---|
| BIND-01 | Result certificate | Acquire a PoP token. | `AuthenticationResult.BindingCertificate` is a non-null, usable `X509Certificate2` with an accessible private key. |
| BIND-02 | Token confirmation claim | Compute SHA-256 over `BindingCertificate.RawData`, base64url-encode it, and decode the access token. | The computed value equals `cnf["x5t#S256"]`. This is a test assertion, not a new public MSAL crypto API. |
| BIND-03 | Correct authorization scheme | Call the resource using `Authorization: mtls_pop <token>` and `BindingCertificate` on the TLS connection. | The resource returns HTTP 200. |
| BIND-04 | Wrong authorization scheme | Call the resource using a non-PoP authorization scheme. | The resource rejects the request. |
| BIND-05 | Missing resource certificate | Call the resource without a TLS client certificate. | The resource rejects the request. |
| BIND-06 | Wrong resource certificate | Call the resource using another certificate. | The resource rejects the request. |
| BIND-07 | Exact returned certificate | Use `AuthenticationResult.BindingCertificate` directly without reconstructing it. | The TLS handshake and resource call succeed. |

## Cache and certificate lifecycle

| ID | Test | Setup and action | Expected behavior |
|---|---|---|---|
| CACHE-01 | Same certificate | Repeat an identical PoP request. | Return a cache hit with the same token and usable binding-certificate metadata. |
| CACHE-02 | Same key, renewed certificate | Create two certificates with the same key but different leaf DER. | The second certificate causes a token-cache miss. |
| CACHE-03 | Different certificates | Use separate certificates for the same app and scope. | Tokens, binding metadata, and HTTP transport state remain isolated. |
| CACHE-04 | Certificate-chain change | Keep the leaf unchanged but alter the issuer chain. | Cache behavior follows the documented leaf-DER binding definition. |
| CACHE-05 | Failed downgrade | Return a normal token for a PoP request. | Do not write the response to the cache. |
| CACHE-06 | Persistent shared token cache | Share serialized token-cache storage between clients using different certificates. | Neither client receives a token bound to the other certificate. No shared certificate-cache implementation is required. |
| CACHE-07 | Cached result fields | Return a PoP token from cache. | Preserve token type and a usable binding certificate. |

## HTTP transport and concurrency

| ID | Test | Setup and action | Expected behavior |
|---|---|---|---|
| HTTP-01 | Default transport | Use MSAL.NET's built-in mTLS transport. | Enforce the supported TLS minimum while preserving normal proxy, handler, and connection behavior. |
| HTTP-02 | Same-certificate reuse | Perform repeated network requests with one certificate. | Reuse pooled HTTP resources where supported and avoid unbounded transport creation. |
| HTTP-03 | Certificate rotation | Change the certificate leaf DER. | Use the new certificate for subsequent token requests and never reuse the old certificate for that request. |
| HTTP-04 | Concurrent same certificate | Run many parallel acquisitions using the same certificate. | Complete without deadlocks, data races, certificate corruption, or inconsistent results. |
| HTTP-05 | Concurrent different certificates | Run parallel clients with different certificates. | No cross-certificate transport, token, or binding-certificate leakage. |
| HTTP-06 | Cancellation | Cancel before and during the TLS request. | Return promptly and do not cache a partial result. |
| HTTP-07 | Timeout | Make the token endpoint stall. | Apply the configured timeout and return a clear network error. |

## Platform acceptance

| ID | Test | Platform | Expected behavior |
|---|---|---|---|
| PLAT-01 | Windows software certificate | Windows | Token acquisition and the resource call succeed. |
| PLAT-02 | Linux certificate | Linux | Token acquisition and the resource call succeed. |
| PLAT-03 | Supported TLS versions | Windows and Linux | Token and resource TLS handshakes meet MSAL.NET's supported TLS requirements. |
| PLAT-04 | Non-exportable Windows key | Windows CNG, KSP, HSM, TPM, or KeyGuard | Token and resource handshakes succeed without exporting the private key. |
| PLAT-05 | Managed Identity binding certificate | Supported Trusted Launch or Confidential VM environment | Managed Identity acquisition returns a usable binding certificate and the resource call succeeds. |
