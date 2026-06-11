# Spike: IMDSv2 — delegate the post-mint token leg to MSAL's internal exchange path

> Tracking issue: **#6042** — *[Engineering task] Spike - IMDSv2: delegate token leg to internal ConfidentialClientApplication*
> Related: **#5982** (`docs/nsp_claims_design.md`, NSP / `WithClaimsFromClient` design)

## 1. Background

MSI v2 (IMDS v2) acquires an mTLS **binding certificate** from IMDS via
`/getplatformmetadata` (CSR metadata) and `/issuecredential` (cert mint), then makes a
`client_credentials` POST **directly** to the ESTS-R mTLS token endpoint
(`{mtlsAuthenticationEndpoint}/{tenant}/oauth2/v2.0/token`).

That post-mint token request is **hand-rolled** in
`ImdsV2ManagedIdentitySource.CreateRequestAsync` and sent through the managed-identity
pipeline. It re-implements behavior that already exists in MSAL's confidential-client
exchange (`ClientCredentialRequest` / `TokenClient`):

- claims → request body
- client-capability (CP1) merge into claims
- claims-as-cache-key
- token-type / auth-scheme validation
- telemetry around the token endpoint

The real driver (per #6042 comments and #5982) is **claims / NSP support**. Adding
NSP / CP1 / client claims to the bespoke MSI v2 path means re-porting all of the above
plus a remint loop. The agreed direction (Bogdan + Robbie on #6042) is to **delegate
the token leg to MSAL's internal exchange path** — *not* to instantiate a public
`ConfidentialClientApplication`.

## 2. Goal & non-goals

**Goal:** Replace only the post-mint HTTP token request with a call into MSAL's
internal `ClientCredentialRequest` / `TokenClient`, so the IMDS-minted cert rides the
same code path that already handles claims, capabilities, caching, and telemetry.

**Non-goals:**
- Do **not** change the cert-mint code (`/getplatformmetadata` + `/issuecredential`).
- Do **not** create or call a public `ConfidentialClientApplication`. Reuse the
  internal `ClientCredentialRequest` / `TokenClient` plumbing only.
- Do **not** finalize the MSIv2 NSP wire contract — #5982 marks MSIv2 as
  "design pending (IMDS team)". This spike delivers the delegation **seam** that the
  NSP claims will later ride on; it does not ship NSP for MSIv2.

## 3. Current state (verified, with citations)

### 3.1 Cert-mint (keep as-is)
`src/client/Microsoft.Identity.Client/ManagedIdentity/V2/ImdsV2ManagedIdentitySource.cs`
- Endpoint constants: lines 36–38 (`CsrMetadataPath`, `CertificateRequestPath`,
  `AcquireEntraTokenPath = "/oauth2/v2.0/token"`).
- `GetCsrMetadataAsync` (42–93) → GET `/getplatformmetadata`.
- `ExecuteCertificateRequestAsync` (~245–333) → POST `/issuecredential`.
- KeyGuard validation + CSR generation + cert assembly inside `CreateRequestAsync`
  (343–411). The mint result yields `MtlsBindingInfo { Certificate, Endpoint, ClientId }`.

### 3.2 Post-mint token request (to be replaced)
`ImdsV2ManagedIdentitySource.CreateRequestAsync` (335–442):
- Builds a `ManagedIdentityRequest` POST to `endpointBaseForToken + "/oauth2/v2.0/token"`
  (417–419).
- Body: `client_id` (434), `grant_type=client_credentials` (435),
  `scope={resource}/.default` (436), `token_type` bearer/mtls_pop (432, 437).
- Sets `request.MtlsCertificate = bindingCertificate` (440), `RequestType = STS` (439).

`src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs`:
- `SendTokenRequestForManagedIdentityAsync` (214–253): resolves authority, forwards
  `ClientClaims` (225–228), calls
  `_managedIdentityClient.SendTokenRequestForManagedIdentityAsync(...)` (230–233),
  applies the `MtlsPopAuthenticationOperation` scheme **before** caching (235–247),
  then `CacheTokenResponseAndCreateAuthenticationResultAsync` (252).

### 3.3 Delegation target (internal exchange)
`src/client/Microsoft.Identity.Client/Internal/Requests/ClientCredentialRequest.cs`:
- `ExecuteAsync` (36–129): scope validation (38–43); cache-skip for ForceRefresh / claims
  (60–76); cache hit + proactive refresh (78–116); `GetAccessTokenAsync` on miss (125).
- `GetBodyParameters` (487–497): `grant_type=client_credentials`, `client_info=2`, scope.

`src/client/Microsoft.Identity.Client/OAuth2/TokenClient.cs`:
- ctor (34–43): builds `OAuth2Client` with `requestParams.MtlsCertificate` → mTLS cert
  flows from the request params, **no extra wiring needed**.
- `SendTokenRequestAsync` (45–116): **accepts `tokenEndpointOverride` (48)**; when set it
  is used verbatim (55–59) and **instance/endpoint discovery is skipped**. This is the
  key enabler — we pass the IMDS-provided ESTS-R endpoint directly.
- `AddBodyParamsAndHeadersAsync` (133–197): `client_id` (139), credential material via
  `CredentialMaterialResolver` (142–163), `scope` (166), `claims` from
  `_requestParams.ClaimsAndClientCapabilities` (168), auth-scheme params (175–178).

### 3.4 Claims / capability merge (free once delegated)
- `AuthenticationRequestParameters.ClaimsAndClientCapabilities` (76–82, 114) merges
  `Claims` + `ClientClaims` with capabilities via
  `ClaimsHelper.GetMergedClaimsAndClientCapabilities` (57–70).

### 3.5 Instance discovery already disabled for MI
- `ManagedIdentityApplicationBuilder.DefaultConfiguration()` sets
  `IsInstanceDiscoveryEnabled = false` (160–167). Combined with `tokenEndpointOverride`,
  the delegated request makes **zero** discovery calls.

### 3.6 No remint loop today
- No `invalid_client` handling exists under `ManagedIdentity/`. The only retry hook is
  the generic `OnMsalServiceFailure` callback in `ClientCredentialRequest` (164–189,
  253–287). The remint-on-`invalid_client` behavior must be **added** around the
  delegated call.

## 4. Proposed delegation

### 4.1 Sequence
```
ManagedIdentityAuthRequest (MSI v2, mTLS PoP)
  └─ mint cert  (UNCHANGED: /getplatformmetadata + /issuecredential)
       → MtlsBindingInfo { cert, estsrEndpoint, clientIdGuid }
  └─ DELEGATE token leg:
       build internal AuthenticationRequestParameters for the exchange:
         • Authority         = ESTS-R mTLS authority from MtlsBindingInfo.Endpoint
         • ClientId          = MtlsBindingInfo.ClientId
         • Scope             = {resource}/.default
         • MtlsCertificate   = MtlsBindingInfo.Certificate
         • AuthenticationScheme = MtlsPopAuthenticationOperation(cert)  (PoP) / Bearer
         • Claims / ClientClaims / capabilities  ← from the MI request params
       new TokenClient(requestParams)
         .SendTokenRequestAsync(
             GetBodyParameters(),
             tokenEndpointOverride: MtlsBindingInfo.Endpoint + "/oauth2/v2.0/token")
       wrap in invalid_client → remint cert → retry-once
  └─ cache + build AuthenticationResult (existing MI cache path)
```

### 4.2 Two viable integration depths
- **(A) Delegate to `TokenClient` only (recommended for the spike).** Keep the MI cache /
  result-shaping path in `ManagedIdentityAuthRequest`; replace just the bespoke POST with
  `new TokenClient(reqParams).SendTokenRequestAsync(body, tokenEndpointOverride: estsr)`.
  Smallest, lowest-risk change; cert flows via `reqParams.MtlsCertificate`; claims via
  `ClaimsAndClientCapabilities`. Endpoint override avoids touching `Authority`/discovery.
- **(B) Delegate to the full `ClientCredentialRequest`.** Gets cache + claims-cache-key +
  proactive refresh "for free", but requires constructing a parallel
  `AuthenticationRequestParameters` + `AcquireTokenForClientParameters` + token cache and
  reconciling it with the MI app cache. Higher fidelity to "share CCA's path", higher
  blast radius. Recommend prototyping (A) first, then assessing (B).

### 4.3 The `invalid_client` remint wrapper (new)
Catch `MsalServiceException` with `invalid_client` (cert rejected/expired) around the
delegated call; on first occurrence invalidate the cert cache entry
(`s_mtlsCertificateCache` / `GetMtlsCertCacheKey`), re-run mint, and retry **once**.
Surface the second failure unchanged. Bound to a single retry to avoid loops.

## 5. Open feasibility questions → answers from this read

| # | Question | Finding |
|---|----------|---------|
| 1 | Endpoint without discovery? | **Yes** — `TokenClient.SendTokenRequestAsync(tokenEndpointOverride)` (48, 55–59) bypasses discovery; MI already disables instance discovery. |
| 2 | mTLS cert injection? | **Yes** — `TokenClient` ctor reads `requestParams.MtlsCertificate` (39–42); set it on the delegated request params. |
| 3 | ClientId/tenant source? | From the mint result (`MtlsBindingInfo.ClientId`, ESTS-R endpoint embeds tenant). |
| 4 | Claims/CP1 wiring? | **Free** — `ClaimsAndClientCapabilities` (ARP 76–82) + `ClaimsHelper` (57–70); `TokenClient` reads it at 168. |
| 5 | invalid_client remint? | **Must add** — no existing handler; wrap the delegated call (see 4.3). |
| 6 | Telemetry parity? | `TokenClient` sets `ApiEvent.TokenEndpoint` (61–63) and clears telemetry on send (219–258); confirm MI-source tags still emitted in the PoC. |
| 7 | ServiceBundle/params? | `TokenClient(AuthenticationRequestParameters)` needs only ARP, which carries `RequestContext`→`ServiceBundle`; MI request already has these. |

## 6. Risks & considerations
- **Token-cache coherence (option B):** the MI app cache vs. a CCA-style cache must not
  diverge; option A sidesteps this by keeping MI caching.
- **Auth-scheme/token-type validation:** `TokenClient` validates `token_type` against
  `AuthenticationScheme.AccessTokenType` (102–112) — the PoP scheme must be set on the
  delegated params before send, mirroring `ManagedIdentityAuthRequest` (235–247).
- **Throttling key:** `TokenClient` participates in `ThrottlingManager` keyed on the
  request params (74–76, 86); verify MSI v2 requests throttle sensibly under the new path.
- **Warnings-as-errors:** repo builds with `TreatWarningsAsErrors=true`
  (`Directory.Build.props`); keep the PoC clean even though throwaway.
- **No reflection** in product code/tests (repo guideline).

## 7. PoC plan & measurement methodology

PoC (throwaway branch `rginsburg/imdsv2-cca-delegation-spike`):
1. Behind a flag, replace the bespoke POST construction in `CreateRequestAsync` /
   `SendTokenRequestForManagedIdentityAsync` with delegation per **option A**.
2. Add the `invalid_client` remint-and-retry-once wrapper.
3. Run existing MSI v2 + mTLS PoP unit/integration tests; adjust as needed.

Measurement (the issue's explicit ask — *lines removed vs. lines added*):
- **Removed:** bespoke body/endpoint/header construction in `ImdsV2ManagedIdentitySource`
  (the `client_credentials` POST assembly, ~ lines 417–442) and any MI-only send glue
  made redundant by delegation.
- **Added:** the delegation glue (params construction, `TokenClient` call,
  endpoint-override plumbing) + the remint wrapper.
- Report as `git diff --stat` net for the token-leg files, plus a short table of
  removed-vs-added with a qualitative duplication-eliminated note (claims/CP1/cache-key
  no longer needing a bespoke MSI v2 port).

## 8. Recommendation (to validate in PoC)
Proceed with **option A** (delegate the send to `TokenClient` with `tokenEndpointOverride`),
keep MI caching, and add the `invalid_client` remint wrapper. This is the minimal seam
that lets the future MSIv2 NSP claims (#5982) ride MSAL's existing
claims→body / CP1-merge / claims-cache-key machinery instead of being re-ported into the
managed-identity path. Reassess option B once the IMDS NSP wire contract lands.
