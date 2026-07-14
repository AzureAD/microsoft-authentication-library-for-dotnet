# Azure.Identity mTLS-PoP / FIC gaps

MSAL.NET supports certificate-bound client assertions and mTLS Proof-of-Possession (PoP). Azure.Identity's
Federated Identity Credential (FIC) path does not use them: it reduces every assertion to a `string`, never
requests mTLS-PoP, and so returns Bearer tokens only. This note records that gap.

> Public sources only (MSAL.NET, `azure-sdk-for-net`, and the design PR in §6). On current `main`, Azure.Identity's
> credentials live in **Azure.Core** (`sdk/core/Azure.Core/src/Identity/…`; `Azure.Identity` forwards the
> types). Checked against source on 2026-07-12.

---

## 1. Summary

- **MSAL.NET supports it.** Certificate-bound client tokens via `WithMtlsProofOfPossession()` (confidential
  clients and managed identity), cert-bound assertions (`ClientSignedAssertion` carrying a
  `TokenBindingCertificate`), and `AuthenticationResult.BindingCertificate`. This ships in the netstandard2.0 public API (and higher TFMs), and
  the two-leg FIC pattern is documented and tested in the MSAL repo (4.82.1+).
- **Azure.Identity's FIC path doesn't.** `ClientAssertionCredential`, `WorkloadIdentityCredential`, and the
  internal `ManagedIdentityAsFederatedIdentityCredential` all converge on a `string` assertion, never request
  PoP, and return Bearer tokens.
- **The gap.** There is no way, through these credentials, to pass a binding certificate or ask for an
  `mtls_pop` token. `AccessToken` can represent a binding certificate, but the current FIC path never produces one.

---

## 2. Bearer tokens today (SNI / MSI / FIC)

| Flow | MSAL.NET API | Azure.Identity credential | Result |
|---|---|---|---|
| **SNI** (subject-name-issuer cert) | `WithCertificate(cert, sendX5C:true)` | `ClientCertificateCredential` | Bearer |
| **MSI** | `AcquireTokenForManagedIdentity(resource)` | `ManagedIdentityCredential` | Bearer (unless PoP opted-in) |
| **FIC** (assertion) | `WithClientAssertion(string)` / `Func<…,Task<string>>` | `ClientAssertionCredential`; `WorkloadIdentityCredential`; internal `ManagedIdentityAsFederatedIdentityCredential` | Bearer |

The three FIC credentials are one path. `WorkloadIdentityCredential` wraps `ClientAssertionCredential`, and
`ManagedIdentityAsFederatedIdentityCredential` builds one whose callback returns a managed-identity token's
`.Token` string. They all end up at: assertion → `string` → `MsalConfidentialClient` →
`WithClientAssertion(string)` → `AcquireTokenForClient` → Bearer. `ClientAssertionCredential.GetToken` never
looks at `IsProofOfPossessionEnabled`.

---

## 3. Cert-bound tokens today: MSAL vs Azure.Identity

| Flow | MSAL.NET | Azure.Identity |
|---|---|---|
| **SNI cert** | `WithCertificate(cert)` + `WithMtlsProofOfPossession()` → `mtls_pop` + `BindingCertificate` | ❌ `ClientCertificateCredential` never requests PoP |
| **FIC cert-bound assertion** | `WithClientAssertion(Func<AssertionRequestOptions,CT,Task<ClientSignedAssertion>>)` with `TokenBindingCertificate` → `jwt-pop` (add `WithMtlsProofOfPossession()` for an `mtls_pop` result) | ❌ callback returns `string` only |
| **MSI (IMDSv2)** | `AcquireTokenForManagedIdentity(res).WithMtlsProofOfPossession()` | ✅ direct MI (opt-in; host + KeyAttestation-gated); ❌ MI-as-FIC |

An unattested managed-identity mTLS-PoP flow is described separately in PR #6108. It is a design and is not
part of current Azure.Identity behavior.

Two facts to keep straight:

- **`jwt-pop` and `mtls_pop` are different.** `jwt-pop` is the client assertion the app sends (the
  `client_assertion_type`). `mtls_pop` is the token ESTS returns (the `token_type`). A `jwt-pop` assertion —
  one carrying a `TokenBindingCertificate` — can result in either a Bearer or an `mtls_pop` token; you get
  `mtls_pop` only if that leg also calls `WithMtlsProofOfPossession()`.
- **Attestation and binding are separate in MSAL** (`WithMtlsProofOfPossession()` vs `WithAttestationSupport()`
  in the KeyAttestation package). Today Azure.Identity only tries mTLS-PoP for managed identity when the
  KeyAttestation package is present, so it ties the two together.

Direct `ManagedIdentityCredential` can already get an `mtls_pop` token, but that is a separate flow and does
not close the FIC gap.

---

## 4. The gaps in Azure.Identity

| # | Gap | Why |
|---|---|---|
| G1 | **No way to pass a certificate** | `MsalConfidentialClient` callbacks are `Func<string>` / `Func<CT,Task<string>>`, so there is nowhere to return a `BindingCertificate`. |
| G2 | **The FIC path never asks for PoP** | `AcquireTokenForClientCoreAsync` never calls `WithMtlsProofOfPossession()`, and `ClientAssertionCredential` ignores `IsProofOfPossessionEnabled`. |
| G3 | **No way to identify the PoP scheme** | `IsProofOfPossessionEnabled` is per request, not global, but it does not distinguish SHR (Signed HTTP Request) from mTLS. The FIC path ignores it today. |
| G4 | **The cert is never produced** | `AccessToken.BindingCertificate` exists and `ToAccessToken()` already copies `TokenType` and `BindingCertificate` — but the FIC path never produces a bound result to copy. |

On G4: this covers the normal `TokenCredential` / `AccessToken` path. The newer `System.ClientModel`
`AuthenticationToken` has no field for a binding certificate (the conversion drops it).

---

## 5. What the FIC path is missing

Returning a cert-bound FIC token needs two things the request side does not have: a way for the assertion
callback to return a certificate (today it returns a `string`), and a way to request mTLS-PoP on the exchange.
MSAL already provides both halves — the cert-bound `WithClientAssertion` overload and
`WithMtlsProofOfPossession()` — and the result would flow back through `AccessToken.BindingCertificate`. How to
surface this in the public API (for example, extending a credential, adding one, or configuring it per request)
is a separate design decision, outside this note.

`ManagedIdentityAsFederatedIdentityCredential` shows the gap concretely. Its callback is
`async _ => (await mi.GetTokenAsync(ctx)).Token`: the inner managed-identity request does not ask for PoP, so
it returns a Bearer token; the callback keeps only `.Token`; the binding certificate is not preserved; and the
second leg is therefore an ordinary, unbound FIC exchange. Binding it would need a bound MI token first, the
certificate carried through the assertion, and mTLS-PoP requested on the second leg — which must be a
confidential client, since managed identity has no `WithClientAssertion`.

The token exchange and the resource call must also use the binding certificate. Transport and certificate
lifetime are separate implementation concerns.

---

## 6. References

| Source | Files |
|---|---|
| MSAL.NET (`src/client/Microsoft.Identity.Client`) | `AppConfig/ClientSignedAssertion.cs`, `AppConfig/AssertionRequestOptions.cs`, `AppConfig/ConfidentialClientApplicationBuilder.cs`, `ApiConfig/AcquireTokenForClientParameterBuilder.cs` |
| MSAL.NET tests | `ClientCredentialsMtlsPopTests.cs`, `ManagedIdentityImdsV2Tests.cs` |
| Azure SDK (`sdk/core/Azure.Core/src`) | `Identity/MsalConfidentialClient.cs`, `Identity/Credentials/ClientAssertionCredential.cs`, `Identity/Credentials/WorkloadIdentityCredential.cs`, `Identity/ChainedTokenCredentialFactory.cs`, `Identity/AuthenticationResultExtensions.cs`, `AccessToken.cs`, `TokenRequestContext.cs` |
| Design reference | MSAL PR #6108 — unattested MSI V2 mTLS-PoP (`docs/msi_v2/msi_unattested_mtls_pop_design.md`); a design, not shipping behavior. |
