# Confidential-client ID-JAG grant extension

## Status and scope

Local experimental implementation of feature [3782588](https://dev.azure.com/IdentityDivision/Engineering/_workitems/edit/3782588).
The public API remains subject to review. This is the extension of existing user grants,
not an implementation of RFC 8693 token exchange. No live ESTS readiness or interoperability
has been verified. In particular, authorization-code availability must be confirmed with the service owner.

Supported entry points:

- `AcquireTokenByAuthorizationCode(...).WithIdJag(audience, resource)`
- `AcquireTokenOnBehalfOf(...).WithIdJag(audience, resource)`
- Confidential-client `AcquireTokenSilent(...).WithIdJag(audience, resource)` using the SDK-held refresh token.

Enable the API with `WithExperimentalFeatures()` on the application builder. A nonempty
audience is required; resource is optional but must be nonempty when supplied. Audience is
the external authorization server; resource is the external API. Scopes are the external
permissions supplied to the acquisition method. They are not inferred from audience or resource.

Public clients, brokers, PoP token options, long-running OBO, and app-only acquisition are
not supported by this API. Existing client authentication, PKCE, correlation, claims handling,
and normal service-error processing are reused. Only the three builders above expose the option.

## Usage

Configure the existing confidential client with a certificate, authority, redirect URI,
and `WithExperimentalFeatures()`. The examples assume that client is named `app`.

```csharp
string[] externalScopes = { "agent.read", "agent.write" };
string audience = "https://authorization-server.example/";
string resource = "https://api.example/";

AuthenticationResult grant = await app
    .AcquireTokenByAuthorizationCode(externalScopes, authorizationCode)
    .WithPkceCodeVerifier(pkceVerifier)
    .WithIdJag(audience, resource)
    .ExecuteAsync(cancellationToken);
```

For OBO, the hosting API must have already validated the incoming access token:

```csharp
AuthenticationResult grant = await app
    .AcquireTokenOnBehalfOf(externalScopes, new UserAssertion(validatedIncomingAccessToken))
    .WithIdJag(audience, resource)
    .ExecuteAsync(cancellationToken);
```

For silent acquisition, select the account for the current authenticated session, not an
arbitrary account from the cache. Refresh tokens remain inside MSAL:

```csharp
AuthenticationResult grant = await app
    .AcquireTokenSilent(externalScopes, account)
    .WithIdJag(audience, resource)
    .ExecuteAsync(cancellationToken);
```

`grant.AccessToken` contains the ID-JAG; `grant.ExpiresOn` reflects the server lifetime.
Redeem the grant at the external authorization server. Do not send it to an API as a bearer
access token or use `CreateAuthorizationHeader()` for that purpose. SaaS redemption is not implemented here.

## Protocol and cache decisions

The existing grant type and inputs are retained. The token request adds:

```text
requested_token_type=urn:ietf:params:oauth:token-type:id-jag
audience=<external authorization server>
resource=<optional external API>
```

The expected compatibility response has the grant in `access_token` and `token_type=Bearer`.
There is no requirement for RFC-style `token_type=N_A` or `issued_token_type`; if supplied,
scalar extension metadata is retained through the existing result handling. The SDK treats the
grant as opaque and does not add JWT parsing or signature validation.

This implementation preserves the existing scope pipeline, including its reserved scopes
(`openid`, `profile`, `offline_access`). Whether the extension needs different scope behavior
is a service-contract question to settle before release, not established by these mock fixtures.

ID-JAG requests never use an ordinary cached access token, even with `WithForceRefresh(false)`.
The returned grant is used to construct the result but is not persisted as an access-token entry,
does not evict overlapping ordinary access tokens, and is not exported to the legacy ADAL cache.
MSAL account, ID-token, and refresh-token updates are retained. Silent acquisition retains the
old refresh token when no replacement is returned. Ordinary OBO retains its existing policy of
discarding refresh tokens returned by the service.

No-grant-cache is an initial implementation decision, not a protocol claim that grants are
single-use or inherently uncacheable. A future grant cache would need explicit replay/redemption
rules and independent keys for audience, resource, scopes, account, and tenant.

Extra query parameters may not duplicate ID-JAG fields. Token-request callbacks cannot change
or remove the ID-JAG fields or change the underlying grant type. Extra-body APIs use their existing
duplicate-key rejection; callers should supply ID-JAG fields only through `WithIdJag`.

## Mock-Test Plan

Reuse the existing HTTP-only mock harness in [IdJagTests.cs](../tests/Microsoft.Identity.Test.Unit/RequestsTests/IdJagTests.cs).
No new test framework, credentials, tenant configuration, or network calls are required.
The `IdJagFunctional` category exercises the public acquisition and cache APIs with only
HTTP mocked. It uses real MSAL cache serialization/deserialization and notifications, storing
the serialized bytes in memory through the public callbacks rather than substituting a cache
implementation or seeding internal entries. Fresh client instances model session restoration.

| Area | Implemented assertions |
| --- | --- |
| Authorization code | Existing code, redirect URI, PKCE and credentials; added ID-JAG fields; optional resource omitted; server lifetimes 300 and 900 seconds; correlation and result metadata. |
| Session bootstrap | Account and ID-token entries created; refresh token usable by a subsequent silent ID-JAG request; no AT entry created. |
| Silent refresh | Ordinary AT bypassed with force refresh both false and true; rotated RT used on the next request; absent replacement retains the RT; changed audience causes a new request. |
| OBO | Existing JWT-bearer grant, assertion and requested-token-use retained; ordinary AT bypassed; existing ordinary AT survives; ordinary OBO still discards RTs. |
| Reverse isolation | Ordinary silent and OBO calls return their original cached AT, never the ID-JAG response. Cache entry state is asserted after the grant request. |
| Validation | Invalid audience on all builders; whitespace resource; experimental opt-in; public client; long-running OBO; SHR PoP rejected in either builder call order. |
| Extensibility | Callback mutations of audience, resource, requested token type, and grant type rejected before token HTTP or cache writes. |
| Failure paths | Missing RT cannot fall back to ordinary AT; invalid-grant error and pre-cancelled request do not create token entries. |
| Serialized session | Auth-code ID-JAG bootstraps account/ID-token/RT state; fresh clients recover the account and rotate the RT; grants never enter serialized AT entries; ordinary silent acquisition then obtains and caches an ordinary AT. Cache-write notifications are asserted. |
| Repeated grants | Both silent and OBO paths survive cache round-trips; identical requests and independently changed audience, resource, and scopes always reach HTTP. Ordinary AT entries remain intact and ordinary acquisitions still hit their cache. OBO still discards returned RTs. |
| Account isolation | Two users' real auth-code sessions coexist in one serialized cache; ID-JAG uses and rotates only the selected account's RT. Removing that account prevents further silent ID-JAG for it without borrowing the other user's RT or evicting their AT. |
| Failed refresh recovery | Silent invalid-grant propagates UI-required rather than returning an ordinary AT, leaves persisted bytes unchanged and emits no cache write, and a fresh client can retry successfully with the retained session. |

Run from the repository root with a .NET 8 runtime installed:

```powershell
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter FullyQualifiedName~IdJagTests
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter TestCategory=IdJagFunctional
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter 'FullyQualifiedName~RequestsTests|FullyQualifiedName~TokenResponseTests'
```

Local evidence: all 21 ID-JAG cases pass, including the five new functional cases, which also
pass when selected independently by category. The combined request/response/token-cache
selection passed 366 cases with one skipped and no failures (367 total, including the ID-JAG
cases). Product builds previously passed for .NET Framework 4.6.2 and 4.7.2, .NET Standard 2.0,
and .NET 8. Tests compiled for .NET 8 but executed on the installed .NET 10 ARM64 runtime with a temporary
`DOTNET_ROLL_FORWARD=Major` process setting because .NET 8 was absent. This is not a native
.NET 8 runtime pass. The environment setting was restored afterward. An existing analyzer-version
warning remains; no SDK, dependency, or build configuration was changed to suppress it.

### Follow-Up Coverage Before Release

1. Extend mocks for certificate/client-assertion credentials; claims and SPA-code combinations;
   mTLS PoP rejection; app-level and request-level extra-query conflicts; benign callbacks and
   extra-body duplicate rejection; wrong/missing response token type and malformed responses.
2. Extend account-isolation tests to cross-tenant and multiple-client RT fixtures, claims-challenge
   recovery, legacy ADAL serialization, and disabled-cache cases. Assert state after each call,
   and ensure the grant never enters serialized AT data or diagnostics.
3. Run native .NET 8 and supported .NET Framework tests and the normal release/CI gates. Mobile
   public-API baselines are updated for the existing builder types, but mobile builds were not run.
4. Confirm the final request/response contract and scope rules with the service owner. Then use an
   approved ESTS test application to verify OBO and RT grants, auth-code grants when available,
   credential handling, actual expiry, claims challenges, and external redemption separately.

Mocks prove the SDK behavior against these fixtures. They do not prove Entra issues a valid
ID-JAG, validates its audience/policies correctly, or that an external authorization server accepts it.