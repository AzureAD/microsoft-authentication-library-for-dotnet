# **PoP-Bound Client Assertion**  
*(MEAv2 Phase-2 / GitHub #5398)*

---

## 1  Problem Statement
Managed Identity (dSTS) and future Federated Identity Credential (FIC) flows issue a **first-leg JWT** that

* already contains a `cnf` claim (public-key / thumbprint),
* must be forwarded to Entra ID (`client_assertion`) using  
  `client_assertion_type = urn:ietf:params:oauth:client-assertion-type:jwt-pop`,
* **must** be presented **over an mTLS session** using the *same* X-509 certificate.

MSAL .NET currently exposes **`WithCertificate()`** (mTLS) *and* a bearer-assertion overload; the two cannot coexist.  
We need a single API that passes **both** the signed JWT *and* the certificate.

---

## 2  Design Goals
| # | Goal |
|---|------|
| G-1 | **Single builder call** – provide JWT *and* cert together. |
| G-2 | Async path (mirrors existing `Func<CancellationToken,Task<string>>`). |
| G-3 | Use existing mTLS plumbing (`CertificateClientCredential` ➞ `MtlsPopAuthenticationOperation`). |
| G-4 | Preserve binary compatibility; no changes to current APIs. |
| G-5 | Work on **net462** without refactor (tuple via `System.ValueTuple`). |

---

## 3  Public API (additive)

```csharp
/// <summary>
/// Registers an <b>async</b> delegate that returns a tuple containing
///  • a PoP-bound client-assertion (JWT) and
///  • the X-509 certificate referenced by that JWT’s <c>cnf</c> claim.
/// The delegate receives a <see cref="CancellationToken"/> so callers can
/// cancel external signing or network work.
/// </summary>
public ConfidentialClientApplicationBuilder WithClientAssertion(
    Func<CancellationToken,
         Task<(string Assertion, X509Certificate2 Certificate)>> boundAssertionAsync)
```

---

## 4  Implementation

### 4.1 Credential

`CertificateAndAssertionDelegateCredential` : `CertificateClientCredential`

```
﻿┌─AddConfidentialClientParametersAsync───────────────┐
│  (jwt, cert) = await _delegateAsync(ct);            │
│  req.AuthenticationOperation = MtlsPop(cert);       │ pins cert
│  req.MtlsCertificate         = cert;               │
│  body: client_assertion_type = JwtPop               │ sends JWT
│        client_assertion      = jwt                  │
└──────────────────────────────────────────────────────┘
```

*inherits* mTLS certificate-pinning from base class.

### 4.2 Builder wiring

```csharp
Config.ClientCredential =
    new CertificateAndAssertionDelegateCredential(boundAssertionAsync);
```

No prior `WithCertificate()` call is required.

### 4.3 Tuple support on net462

*Directory.Packages.props*  
`<PackageVersion Include="System.ValueTuple" Version="4.5.0" />`

*csproj* (net462 only)  
`<PackageReference Include="System.ValueTuple" />`

---

## 5  Usage example

```csharp
var cca = ConfidentialClientApplicationBuilder
            .Create(appId)
            .WithClientAssertion(async ct =>
            {
                var cert = CertStore.GetServiceCert();
                string jwt = await dSTS.GetFirstLegJwtAsync(cert, ct);
                return (jwt, cert);             // tuple
            })
            .WithAuthority("https://login.microsoftonline.com/tenant")
            .Build();

var token = await cca.AcquireTokenForClient(
                 new[] { "https://graph.microsoft.com/.default" })
             .WithMtlsPop() //Existing API 
             .ExecuteAsync();
```

---

## 6  Compatibility & Telemetry
* Add `JwtPop` branch to `AssertionType` telemetry field.
* No behavioural change for existing bearer-assertion or cert-only flows.
* Purely additive; consumers recompile to use the new overload.

---
