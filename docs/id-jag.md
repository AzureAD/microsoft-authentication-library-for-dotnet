# Confidential-client ID-JAG grant extension

## Status and scope

Local experimental implementation of feature [3782588](https://dev.azure.com/IdentityDivision/Engineering/_workitems/edit/3782588).
The public API remains subject to review. This is the extension of existing user grants,
not an implementation of RFC 8693 token exchange. No live ESTS readiness or interoperability
has been verified. In particular, authorization-code availability must be confirmed with the service owner.

Supported entry points:

- `AcquireTokenByAuthorizationCode(...).WithIdJag(audience, resource)`
- `AcquireTokenOnBehalfOf(...).WithIdJag(audience, resource)`
- Confidential-client `AcquireTokenSilent(...).WithIdJag(audience, resource)` using the isolated grant cache or SDK-held refresh token.

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

ID-JAGs are persisted in the MSAL cache and reused by silent and ordinary OBO acquisition.
Their access-token entries include reserved additional key components for the ID-JAG token kind,
exact audience, and optional resource. An omitted resource is distinct from a supplied resource.
Existing scopes, account, tenant, client, and OBO assertion matching still apply. Caller-supplied
additional cache keys are preserved; the `msal.id_jag.` key namespace is reserved and cannot be
supplied by callers, including ordinary token requests.

ID-JAG requests never return an ordinary cached access token, even with `WithForceRefresh(false)`.
Ordinary requests never return a cached ID-JAG. Writes and overlapping-scope eviction stay within
the matching key partition, and ID-JAGs are not exported to the legacy ADAL cache. The existing
cache serialization format preserves the grant-specific additional keys across application restarts.

Normal expiration, claims, and force-refresh rules apply to grants. Use `WithForceRefresh(true)`
on silent or OBO requests when a fresh grant is required. MSAL does not track redemption at the
external authorization server; replay/redemption requirements must be confirmed before release.

The ID-JAG-only components are not added to refresh-token keys. Existing caller-controlled RT
partitioning, account/ID-token processing, and RT rotation are retained. Silent acquisition retains
the old refresh token when no replacement is returned. Ordinary OBO retains its existing policy
of discarding refresh tokens returned by the service.

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
| Session bootstrap | Account, ID-token, isolated grant and RT entries created; refresh token usable by a subsequent forced silent ID-JAG refresh. |
| Silent refresh | Ordinary AT bypassed with force refresh both false and true; rotated RT used on the next request; absent replacement retains the RT; changed audience causes a new request. |
| OBO | Existing JWT-bearer grant, assertion and requested-token-use retained; ordinary AT bypassed; existing ordinary AT survives; ordinary OBO still discards RTs. |
| Reverse isolation | Ordinary silent and OBO calls return their ordinary AT, never a cached ID-JAG; forcing ordinary-token refresh does not evict the cached grant. |
| Validation | Invalid audience on all builders; whitespace resource; experimental opt-in; public client; long-running OBO; SHR PoP rejected in either builder call order. |
| Extensibility | Callback mutations of audience, resource, requested token type, and grant type rejected before token HTTP or cache writes. |
| Failure paths | Missing RT cannot fall back to ordinary AT; invalid-grant error and pre-cancelled request do not create token entries. |
| Serialized session | Auth-code ID-JAG persists alongside account/ID-token/RT state; fresh clients return the cached grant without HTTP or cache writes; forced refresh rotates the RT and replaces only the matching grant. Ordinary acquisition coexists with the serialized grant. |
| Repeated grants | Both silent and OBO paths survive cache round-trips; identical requests hit cache; changed audience, resource (including omission), or unrelated scopes reach HTTP. Ordinary AT entries remain intact and ordinary acquisitions still hit their cache. OBO still discards returned RTs. |
| Expiry and claims | Silent and OBO grant requests refresh entries inside the expiry buffer or bypass valid entries for a claims challenge. |
| Cache-key hooks | Unit tests preserve caller keys and both RT partitioning modes without adding ID-JAG dimensions to RTs; reserved keys reject ordinary and ID-JAG requests before HTTP/cache writes. |
| Account isolation | Two users' real auth-code sessions coexist in one serialized cache; ID-JAG uses and rotates only the selected account's RT. Removing that account removes its ordinary tokens and grants without borrowing the other user's RT or evicting their AT. |
| Failed refresh recovery | Silent invalid-grant propagates UI-required rather than returning an ordinary AT, leaves persisted bytes unchanged and emits no cache write, and a fresh client can retry successfully with the retained session. |

Run from the repository root with a .NET 8 runtime installed:

```powershell
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter FullyQualifiedName~IdJagTests
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter TestCategory=IdJagFunctional
dotnet test tests/Microsoft.Identity.Test.Unit/Microsoft.Identity.Test.Unit.csproj -f net8.0 --filter 'FullyQualifiedName~RequestsTests|FullyQualifiedName~TokenResponseTests'
```

Local evidence: all 31 ID-JAG cases pass with the isolated grant cache, including HTTP-only
functional checks and internal cache-key-hook tests. The broader request/response/cache selection
passed 409 cases with one skipped and no failures (410 total, including the ID-JAG cases).
Product builds passed for .NET Framework 4.6.2 and 4.7.2, .NET Standard 2.0, and .NET 8.
Tests compiled for .NET 8 but executed on the installed .NET 10 ARM64 runtime with a temporary
`DOTNET_ROLL_FORWARD=Major` process setting because .NET 8 was absent. This is not a native
.NET 8 runtime pass. The environment setting was restored afterward. An existing analyzer-version
warning remains; no SDK, dependency, or build configuration was changed to suppress it.

### Follow-Up Coverage Before Release

1. Extend mocks for certificate/client-assertion credentials; claims and SPA-code combinations;
   mTLS PoP rejection; app-level and request-level extra-query conflicts; benign callbacks and
   extra-body duplicate rejection; wrong/missing response token type and malformed responses.
2. Extend account-isolation tests to cross-tenant and multiple-client RT fixtures, claims-challenge
   recovery, legacy ADAL serialization, and disabled-cache cases. Assert state after each call,
   and ensure ordinary requests cannot return persisted grants and no token values enter diagnostics.
3. Run native .NET 8 and supported .NET Framework tests and the normal release/CI gates. Mobile
   public-API baselines are updated for the existing builder types, but mobile builds were not run.
4. Confirm the final request/response contract and scope rules with the service owner. Then use an
   approved ESTS test application to verify OBO and RT grants, auth-code grants when available,
   credential handling, actual expiry, claims challenges, and external redemption separately.

Mocks prove the SDK behavior against these fixtures. They do not prove Entra issues a valid
ID-JAG, validates its audience/policies correctly, or that an external authorization server accepts it.