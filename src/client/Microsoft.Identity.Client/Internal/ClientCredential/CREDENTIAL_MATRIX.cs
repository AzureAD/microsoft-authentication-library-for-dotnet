// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// ─────────────────────────────────────────────────────────────────────────────
//  CREDENTIAL MATRIX  –  canonical mapping of (credential, mode) → output
//
//  This file is purely documentary. It contains no executable code.
//  It exists so that reviewers and future maintainers can see, in one place,
//  every supported input/mode combination and what each produces.
//
//  Legend
//  ------
//  Mode:   Regular  = standard JWT-bearer / client-secret flow
//           MtlsMode = certificate-bound mTLS Proof-of-Possession flow
//
//  Output columns
//  ──────────────
//  TokenRequestParameters  – key/value pairs added to the token-request body
//  ResolvedCertificate     – X509Certificate2 stored on the request for
//                            transport binding / logging; null = not applicable
//
//  Row │ Credential type          │ Mode      │ TokenRequestParameters                           │ ResolvedCertificate
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   1  │ X509Certificate         │ Regular   │ client_assertion_type=jwt-bearer                 │ certificate
//      │                         │           │ client_assertion=<signed JWT>                    │
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   2  │ X509Certificate         │ MtlsMode  │ (empty – TLS layer authenticates the client)     │ certificate
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   3  │ ClientSecret            │ Regular   │ client_secret=<secret>                           │ null
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   4  │ ClientSecret            │ MtlsMode  │ ── UNSUPPORTED → MsalClientException ──          │ n/a
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   5  │ SignedAssertion (static) │ Regular   │ client_assertion_type=jwt-bearer                 │ null
//      │                         │           │ client_assertion=<static JWT>                    │
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   6  │ SignedAssertion (static) │ MtlsMode  │ ── UNSUPPORTED → MsalClientException ──          │ n/a
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   7  │ JWT callback (string)   │ Regular   │ client_assertion_type=jwt-bearer                 │ null
//      │                         │           │ client_assertion=<callback JWT>                  │
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   8  │ JWT callback (string)   │ MtlsMode  │ ── UNSUPPORTED → MsalClientException ──          │ n/a
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//   9  │ JWT+cert callback       │ Regular   │ client_assertion_type=jwt-pop (cert bound)        │ certificate
//      │                         │           │ client_assertion=<callback JWT>                  │
//  ────┼─────────────────────────┼───────────┼─────────────────────────────────────────────────┼─────────────────────
//  10  │ JWT+cert callback       │ MtlsMode  │ client_assertion_type=urn:ietf:params:           │ certificate
//      │                         │           │   oauth:client-assertion-type:jwt-pop            │
//      │                         │           │ client_assertion=<callback JWT>                  │
//  ────┴─────────────────────────┴───────────┴─────────────────────────────────────────────────┴─────────────────────
//
//  Notes
//  ─────
//  • Rows 4, 6, 8 throw MsalClientException(MsalError.InvalidCredentialMaterial) because
//    the credential type cannot supply a certificate for mTLS transport.
//  • Row 9 (bearer-over-mTLS): when the callback returns a certificate in Regular mode, the
//    credential uses JWT-PoP (not jwt-bearer) so the token is bound to the presented certificate.
//    The TLS certificate has already been set on the OAuth2Client by MtlsPopParametersInitializer.
//  • Row 10 (JWT-PoP): the assertion uses the JWT-PoP client_assertion_type so the
//    authorization server can verify the token is bound to the presented certificate.
//  • "Static" sources (rows 1–3, 5) set CredentialSource.Static.
//    Callback sources (rows 7–10) set CredentialSource.Callback.
// ─────────────────────────────────────────────────────────────────────────────
