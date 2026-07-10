# Filling the mTLS-PoP gap for FIC — what MSAL exposes today

> **Doc 3 — the gap and how to close it.** Doc 2 established that all three of Azure.Identity's FIC
> credentials converge on a **plain-string** client assertion and produce **Bearer** tokens. This
> document shows **what MSAL (.NET) already exposes** for **mTLS Proof-of-Possession (PoP)** in the
> two FIC shapes the team cares about — **SNI FIC** and **MSI FIC** — and sketches how `Azure.Identity`
> could adopt it.

**Audience:** engineers deciding how to bring mTLS-PoP FIC to `Azure.Identity`.
**Sources of truth:**
- MSAL .NET — `AzureAD/microsoft-authentication-library-for-dotnet` @ `main`
  (`src/client/Microsoft.Identity.Client/...`).
- Azure SDK — `Azure/azure-sdk-for-net` @ `main` (`sdk/core/Azure.Core/src/Identity/...`).
- Verified against source 2026-07-09.

---

## 1. The gap in one paragraph

MSAL exposes mTLS PoP for confidential clients in two ways: (a) a **certificate application**
(SNI cert) + `WithMtlsProofOfPossession()`, and (b) a **cert-bound client assertion** — the assertion
callback returns a `ClientSignedAssertion` carrying a `TokenBindingCertificate`, which flips
`client_assertion_type` to **`jwt-pop`**. For managed identity, MSAL exposes
`AcquireTokenForManagedIdentity(...).WithMtlsProofOfPossession()` (IMDSv2). **Azure.Identity's FIC
credentials use none of this**: they only call MSAL's *string* `WithClientAssertion` overloads and
never request mTLS PoP (Doc 2 §5). So there is **no way, through `ClientAssertionCredential`,
`WorkloadIdentityCredential`, or `ManagedIdentityAsFederatedIdentityCredential`, to obtain a cert-bound
(`mtls_pop` / `jwt-pop`) token** — even though the *direct* `ManagedIdentityCredential` already can
(Doc 2 §7), and even though `AccessToken` can already carry a `BindingCertificate` (Doc 2 §6).

---

## 2. What MSAL exposes today

### 2.1 The cert-bound assertion type — `ClientSignedAssertion`

`src/client/Microsoft.Identity.Client/AppConfig/ClientSignedAssertion.cs`:

```csharp
public class ClientSignedAssertion
{
    // Forwarded to the token endpoint as client_assertion.
    public string Assertion { get; set; }

    // Optional. Binds the assertion for mutual-TLS PoP.
    // When PoP is enabled AND this is non-null, MSAL sets
    //   client_assertion_type = urn:ietf:params:oauth:client-assertion-type:jwt-pop
    // otherwise it uses ...:jwt-bearer.
    public X509Certificate2 TokenBindingCertificate { get; set; }
}
```

Delivered via the newer `WithClientAssertion` overload:

```csharp
ConfidentialClientApplicationBuilder WithClientAssertion(
    Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> clientAssertionProvider)
```

The callback receives an **`AssertionRequestOptions`**
(`src/client/Microsoft.Identity.Client/AppConfig/AssertionRequestOptions.cs`) exposing `ClientID`,
`TokenEndpoint`, `Authority`, `TenantId`, `CorrelationId`, `Claims`, `ClientCapabilities`, and
`ClientAssertionFmiPath` — notably the **`TokenEndpoint`**, which identifies the token endpoint MSAL intends to use (in explicit mTLS PoP, it can be best-effort during preflight; the actual token request uses the resolved endpoint).

> **Note on the overload set.** MSAL also exposes a context-aware *string* overload
> (`WithClientAssertion(Func<AssertionRequestOptions, Task<string>>)`) and marks the raw
> `WithClientAssertion(string)` overload `[Obsolete]`, steering callers to the callback overloads
> "…which can be a Federated Credential." Today Azure.Identity wires the **context-less** overloads
> (`Func<string>` / `Func<CancellationToken, Task<string>>`), so its assertion callbacks receive
> **neither** the `AssertionRequestOptions` context **nor** a way to return a binding certificate.
> Closing the gap means moving to `Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>`.
> MSAL's tests confirm this context genuinely reaches the callback — `WithCorrelationId()` surfaces on
> `AssertionRequestOptions.CorrelationId` for **FIC two-leg tracing** (MSAL issue #5924).

### 2.2 The request-side switch — `WithMtlsProofOfPossession`

**Confidential client** (`AcquireTokenForClientParameterBuilder.WithMtlsProofOfPossession()`):

```csharp
public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession()
{
    if (ServiceBundle.Config.ClientCredential is CertificateClientCredential certificateCredential)
    {
        if (certificateCredential.Certificate == null)
            throw new MsalClientException(MsalError.MtlsCertificateNotProvided, ...);
        CommonParameters.AuthenticationOperation = new MtlsPopAuthenticationOperation(certificateCredential.Certificate);
        CommonParameters.MtlsCertificate = certificateCredential.Certificate;
    }
    CommonParameters.IsMtlsPopRequested = true;
    return this;
}
```

- With a **`WithCertificate` app** (SNI), the cert *is* the mTLS binding cert.
- With a **`WithClientAssertion` app** that returns a `TokenBindingCertificate`, MSAL uses that cert to
  produce a **`jwt-pop`** assertion (no app-level certificate required).
- On success: `AuthenticationResult.TokenType == "mtls_pop"` and `AuthenticationResult.BindingCertificate`
  is populated.
- **Authority / region behavior** is enforced **deeper in MSAL, not by the builder method shown above**
  (and is evidenced by `ClientCredentialsMtlsPopTests.cs`, not `AcquireTokenForClientParameterBuilder.cs`):
  mTLS PoP requires a **tenanted AAD authority** — `/common` and `/organizations` throw — while both
  **regional and global** mTLS endpoints are supported.

> **Three axes — don't conflate mTLS *transport* with mTLS *PoP*.** MSAL's tests separate them:
> - **mTLS transport:** `WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })`
>   presents the client cert at the TLS layer and routes through the `mtlsauth` endpoint, but **returns a
>   plain `Bearer` token** when `WithMtlsProofOfPossession()` is *not* called
>   (`Sni_Over_Mtls_Gets_Bearer_Token_Successfully_TestAsync`, asserts `TokenType == "Bearer"`).
> - **mTLS PoP (token binding):** `WithMtlsProofOfPossession()` returns an `mtls_pop` (cert-bound) token
>   **regardless** of `SendCertificateOverMtls` (`Sni_Gets_Pop_Token_WithSendCertificateOverMtls_*` —
>   *"should always produce PoP, regardless of SendCertificateOverMtls"*).
>
> So "the cert was sent over mTLS" does **not** imply "the token is PoP-bound." Only
> `WithMtlsProofOfPossession()` — this doc's subject — produces a bound token; `SendCertificateOverMtls`
> is an orthogonal transport knob.

**Managed identity** (`ManagedIdentity/ManagedIdentityPopExtensions.WithMtlsProofOfPossession()`):

```csharp
public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
    this AcquireTokenForManagedIdentityParameterBuilder builder, PoPOptions options)
{
    if (!DesktopOsHelper.IsWindows())
        throw new MsalClientException(MsalError.MtlsNotSupportedForManagedIdentity,
                                      MsalErrorMessage.MtlsNotSupportedForNonWindowsMessage);
#if NET462
    throw new MsalClientException(MsalError.MtlsNotSupportedForManagedIdentity,
                                  MsalErrorMessage.MtlsNotSupportedForManagedIdentityMessage);
#else
    builder.CommonParameters.IsMtlsPopRequested = true;
    builder.CommonParameters.MtlsPopMinStrength = options.MinStrength;
    return builder;
#endif
}
```

- Served **exclusively by IMDSv2**, which mints the binding certificate via CSR; App Service / Arc /
  Service Fabric / Cloud Shell / ML sources are rejected, IMDSv1 throws `MtlsPopTokenNotSupportedinImdsV1`.
- **Windows-only** today — and, notably, **blocked on `net462` even on Windows** via a distinct
  `#if NET462` throw (`MtlsNotSupportedForManagedIdentityMessage`); the non-Windows path throws
  `MtlsNotSupportedForNonWindowsMessage`. Optional `.WithAttestationSupport()` (the `Msal.KeyAttestation`
  package) binds to a Credential Guard (VBS) key.
- Token binding follows RFC 8705: a `cnf` claim carrying `x5t#S256` = base64url(SHA-256(cert DER)).

### 2.3 SNI FIC — the two patterns (from MSAL's own integration tests)

`tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs`.

**Pattern A — direct SNI cert app gets an `mtls_pop` token:**

```csharp
var app = ConfidentialClientApplicationBuilder.Create(appId)
    .WithAuthority(authority)
    .WithAzureRegion("westus3")
    .WithCertificate(cert, sendX5C: true)   // SNI
    .Build();

var result = await app.AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// result.TokenType == "mtls_pop"; result.BindingCertificate.Thumbprint == cert.Thumbprint
```

**Pattern B — cert-bound *assertion* app (FIC), no `WithCertificate`:**

```csharp
// Step 1: obtain a JWT to reuse as the assertion (token-exchange audience)
//   api://AzureADTokenExchange/.default   with WithMtlsProofOfPossession()

// Step 2: present it as a cert-bound assertion
var assertionApp = ConfidentialClientApplicationBuilder.Create(appId)
    .WithAuthority(authority)
    .WithAzureRegion("westus3")
    .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
        Task.FromResult(new ClientSignedAssertion
        {
            Assertion = assertionJwt,        // forwarded as client_assertion
            TokenBindingCertificate = cert   // binds assertion for mTLS PoP (jwt-pop)
        }))
    .Build();

var second = await assertionApp.AcquireTokenForClient(new[] { "https://vault.azure.net/.default" })
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Token request carries client_assertion_type =
//   urn:ietf:params:oauth:client-assertion-type:jwt-pop
```

**Pattern B is exactly the shape Azure.Identity's `ClientAssertionCredential` cannot express** — its
callback returns a bare `string`, so there is nowhere to put `TokenBindingCertificate`, and the wrapper
never calls `WithMtlsProofOfPossession()`.

### 2.4 MSI FIC — IMDSv2 mTLS PoP (from MSAL's E2E tests)

`tests/Microsoft.Identity.Test.E2e/ManagedIdentityImdsV2Tests.cs`:

```csharp
var mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned).Build();

var result = await mi.AcquireTokenForManagedIdentity("https://graph.microsoft.com")
    .WithMtlsProofOfPossession()
    .WithAttestationSupport()          // optional; Credential Guard / KeyGuard
    .ExecuteAsync();

// result.TokenType == "mtls_pop"; result.BindingCertificate is set;
// access token carries cnf { "x5t#S256": <thumbprint> } (RFC 8705)
```

Two related-but-distinct notions matter here: **(i)** an MI obtaining its **own** IMDSv2-bound
`mtls_pop` access token (shown above), versus **(ii) MI-as-FIC**, where an MI token for
`api://AzureADTokenExchange` is used as a *client assertion* to obtain an **app** token. Making (ii)
mTLS-bound means **combining** the two: use the IMDSv2-minted binding certificate from (i) as the
`TokenBindingCertificate` of the (ii) assertion (§2.3 Pattern B). Azure.Identity's direct
`ManagedIdentityCredential` already wires (i) (Doc 2 §7); the **FIC wrapper wires neither**.

---

## 3. Gap map — feature by feature

| Capability | MSAL (.NET) today | Azure.Identity FIC creds today |
|---|---|---|
| String (bearer) client assertion | ✅ `WithClientAssertion(string / Task<string>)` | ✅ (all three) |
| Cert-bound assertion (`jwt-pop`) | ✅ `ClientSignedAssertion.TokenBindingCertificate` | ❌ callback returns `string` only |
| Request mTLS PoP on confidential client | ✅ `AcquireTokenForClient().WithMtlsProofOfPossession()` | ❌ never called by `MsalConfidentialClient` |
| SNI cert as mTLS binding cert | ✅ `WithCertificate(cert, sendX5C:true)` + PoP | ❌ `ClientCertificateCredential` never requests PoP |
| MI mTLS PoP (IMDSv2) | ✅ `AcquireTokenForManagedIdentity().WithMtlsProofOfPossession()` | ✅ **direct** MI only; ❌ MI-as-FIC |
| Return binding cert to caller | ✅ `AuthenticationResult.BindingCertificate` | ⚠️ `AccessToken.BindingCertificate` exists but FIC creds never set it |
| Signal PoP intent on a request | — | ⚠️ `TokenRequestContext.IsProofOfPossessionEnabled` (used by direct MI; ignored by FIC creds) |

Legend: ✅ present · ❌ missing · ⚠️ partially present / latent.

---

## 4. How to fill the gap

The **direct `ManagedIdentityCredential`** path is the working template (Doc 2 §7). Closing the FIC gap
means giving the confidential-client path the same three things it already gives MI: a way to **signal
PoP intent**, a way to **supply/obtain a binding certificate**, and a way to **return** it.

### 4.1 Let the assertion carry a binding certificate

Add an assertion callback shape that mirrors MSAL's `ClientSignedAssertion`, e.g. a new
`ClientAssertionCredential` overload whose callback returns both the JWT and an optional binding cert
(and ideally receives request context — the **token endpoint** (for regional mTLS binding) and the
**correlation id** (for FIC two-leg tracing; MSAL already flows `WithCorrelationId()` into the callback,
issue #5924) — mirroring `AssertionRequestOptions`):

```csharp
// Illustrative — not current API
public ClientAssertionCredential(
    string tenantId, string clientId,
    Func<ClientAssertionRequestContext, CancellationToken, Task<ClientAssertion>> assertionCallback,
    ClientAssertionCredentialOptions options = default);

public class ClientAssertion            // maps to MSAL ClientSignedAssertion
{
    public string Assertion { get; set; }
    public X509Certificate2 TokenBindingCertificate { get; set; }  // optional
}
```

### 4.2 Wire `MsalConfidentialClient` to MSAL's PoP APIs

When a binding cert is available and PoP is requested, use MSAL's cert-bound overload and flip on
`WithMtlsProofOfPossession()`:

```csharp
// Illustrative — inside MsalConfidentialClient
confClientBuilder.WithClientAssertion(async (assertionOptions, ct) =>
{
    var a = await _clientAssertionWithCertCallback(assertionOptions, ct).ConfigureAwait(false);
    return new ClientSignedAssertion { Assertion = a.Assertion, TokenBindingCertificate = a.TokenBindingCertificate };
});
// ...on the request:
var builder = app.AcquireTokenForClient(scopes);
if (mtlsPopRequested)
    builder = builder.WithMtlsProofOfPossession();   // → client_assertion_type=jwt-pop when cert present
```

### 4.3 Signal mTLS-PoP intent

Reuse `TokenRequestContext.IsProofOfPossessionEnabled` (as the direct-MI path already does) **or** add
an explicit credential-level option (e.g. `EnableMtlsProofOfPossession` on
`ClientAssertionCredentialOptions`). **Caution:** for confidential clients `IsProofOfPossessionEnabled`
would otherwise imply *SHR* PoP; the two PoP meanings must be disambiguated so a request can't be
routed to the wrong scheme.

### 4.4 Return the binding cert and manage transport

- Populate `AccessToken.BindingCertificate` and `TokenType = "mtls_pop"` on the way out (the property
  already exists — Doc 2 §6) so the **resource** client's transport can perform mTLS to the resource.
- As MSAL requires (and as the MI path already handles), the **token acquisition** leg must let MSAL own
  its HTTP client for the mTLS handshake — the Azure.Core pipeline transport does not carry the client
  certificate.

### 4.5 SNI and MSI variants fall out naturally

- **SNI FIC:** either (a) let `ClientCertificateCredential` request PoP so its SNI cert becomes the
  binding cert (Pattern A), or (b) supply the cert via the new cert-bound assertion callback (Pattern B).
- **MSI FIC:** reuse the direct-MI mTLS path to mint an IMDSv2-bound MI token, then present it as a
  cert-bound assertion — bringing `ManagedIdentityAsFederatedIdentityCredential` up to parity with the
  direct MI credential.

---

## 5. Open questions / caveats

- **Platform limits:** MSAL MI mTLS PoP is **Windows + IMDSv2 only** today; any MSI-FIC binding inherits
  those constraints. SNI/assertion mTLS PoP is broader but **not supported on Linux** in MSAL's tests
  ("POP is not supported on Linux").
- **PoP-flag overloading:** decide whether `IsProofOfPossessionEnabled` should mean SHR PoP, mTLS PoP,
  or be split, before reusing it for confidential clients.
- **Authority constraints:** mTLS PoP requires a tenanted AAD authority (not `/common` or
  `/organizations`).
- **Caching:** MSAL keys PoP tokens by binding; the caller owns caching of the assertion + certificate.
- **`AccessToken` equality ignores the binding cert:** `AccessToken.Equals` compares only `Token`,
  `ExpiresOn`, and `TokenType`, and `GetHashCode` combines those same three — **`BindingCertificate` is
  excluded from both**. Any dedup/caching keyed on `AccessToken` equality would treat two tokens that
  differ only by binding certificate as identical, so binding-cert identity must be tracked out-of-band,
  not inferred from token equality.
- **API review:** any new public callback/options shape is a `PublicAPI.Unshipped.txt` change and needs
  Azure SDK API-review sign-off.

---

## 6. Source references

**MSAL .NET** — `AzureAD/microsoft-authentication-library-for-dotnet` @ `main` (verified 2026-07-09):
- `src/client/Microsoft.Identity.Client/AppConfig/ClientSignedAssertion.cs` — `Assertion` + `TokenBindingCertificate`; jwt-pop vs jwt-bearer rule.
- `src/client/Microsoft.Identity.Client/AppConfig/AssertionRequestOptions.cs` — `TokenEndpoint`, `CorrelationId`, `ClientCapabilities`, etc.
- `src/client/Microsoft.Identity.Client/AppConfig/ConfidentialClientApplicationBuilder.cs` — `WithClientAssertion` overloads: `[Obsolete]` `WithClientAssertion(string)` (steers to the FIC-capable callback), `Func<string>`, `Func<CancellationToken,Task<string>>`, `Func<AssertionRequestOptions,Task<string>>`, and the cert-bound `Func<AssertionRequestOptions,CancellationToken,Task<ClientSignedAssertion>>`; plus `WithCertificate(cert, sendX5C)`.
- `src/client/Microsoft.Identity.Client/ApiConfig/AcquireTokenForClientParameterBuilder.cs` — confidential-client `WithMtlsProofOfPossession()` (method body only; **does not** enforce the authority/region constraints — those live deeper in MSAL).
- `src/client/Microsoft.Identity.Client/ManagedIdentity/ManagedIdentityPopExtensions.cs` — MI `WithMtlsProofOfPossession()` (+ `PoPOptions.MinStrength`).
- `tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsMtlsPopTests.cs` — SNI FIC patterns A/B; `CorrelationId` flow into the assertion callback (issue #5924, L243–245); and the mTLS-transport-vs-PoP distinction (`Sni_Over_Mtls_Gets_Bearer_Token_Successfully_TestAsync` → `Bearer` vs `Sni_Gets_Pop_Token_WithSendCertificateOverMtls_*` → `mtls_pop`).
- `tests/Microsoft.Identity.Test.E2e/ManagedIdentityImdsV2Tests.cs` — MSI FIC (IMDSv2 mTLS PoP + attestation, RFC 8705 cnf).

**Azure SDK** — `Azure/azure-sdk-for-net` @ `main` (verified 2026-07-09):
- `sdk/core/Azure.Core/src/Identity/MsalConfidentialClient.cs` — string-only `WithClientAssertion` wiring (the gap).
- `sdk/core/Azure.Core/src/Identity/MsalManagedIdentityClient.cs` — existing MI mTLS-PoP plumbing (the template).
- `sdk/core/Azure.Core/src/AccessToken.cs` — `TokenType`, `BindingCertificate` (return-path already present).
- `sdk/core/Azure.Core/src/TokenRequestContext.cs` — SHR-PoP fields; PoP-intent signal candidate.
