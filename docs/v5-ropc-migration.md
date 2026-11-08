# MSAL.NET v5: migrate away from ROPC

ROPC is removed in v5, not disabled behind a runtime exception. Recompiling a
password-grant caller fails. This change targets `rel/v5` only; it does not change
4.x servicing, target frameworks, IWA, or other deprecated APIs.

## Removed APIs

All entries below belong to `Microsoft.Identity.Client`. The removal applies to
every framework shipping the corresponding API, including the .NET Framework,
.NET Standard, .NET 8, Android, and iOS API inventories.

| Removed surface | Replacement |
|---|---|
| `IPublicClientApplication.AcquireTokenByUsernamePassword(IEnumerable<string>, string, string)` and the corresponding `PublicClientApplication` method | Interactive or broker sign-in; device code for an appropriate public-client environment |
| `IPublicClientApplication.AcquireTokenByUsernamePassword(IEnumerable<string>, string, SecureString)` and the corresponding `PublicClientApplication` method | Same as above; no password-taking replacement |
| `AcquireTokenByUsernamePasswordParameterBuilder`, including `WithFederationMetadata(string)` and `WithProofOfPossession(string, HttpMethod, Uri)` | Use the supported flow's builder; there is no replacement for overriding ROPC federation metadata |
| `IByUsernameAndPassword` and `AcquireTokenByUsernamePassword(IEnumerable<string>, string, string)` | A server obtains a delegated user assertion through a supported sign-in flow, then uses OBO; app-only jobs use workload identity |
| `AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder`, including `WithSendX5C(bool)` | Configure the surviving confidential-client flow; no password-taking confidential-client replacement |

The confidential client no longer implements `IByUsernameAndPassword`. The
corresponding parameters, request, executor overloads, and broker methods are
deleted, including the Linux route that forwarded native runtime username and
password properties through an interactive call. Telemetry API ID `1003` is
reserved, not renumbered or reused.

## Choose the intended identity

Windows desktop apps can use WAM through the
`Microsoft.Identity.Client.Broker` package:

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

var app = PublicClientApplicationBuilder.Create(clientId)
    .WithDefaultRedirectUri()
    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
    .Build();

var result = await app.AcquireTokenInteractive(scopes)
    .WithAccount(PublicClientApplication.OperatingSystemAccount)
    .WithParentActivityOrWindow(windowHandle)
    .ExecuteAsync();
```

Configure the application's broker redirect URI and supply a valid parent window.
Subsequent calls can use `AcquireTokenSilent`, including broker-backed silent
SSO. On other platforms use their supported broker or interactive configuration.
For headless public clients where device authorization is supported, use
`AcquireTokenWithDeviceCode` with a user completing sign-in. These flows can honor
MFA and Conditional Access; they are not unattended password substitutes.

For genuinely app-only automation, use managed identity, workload identity
federation, or certificate-based client credentials as appropriate. These change
the identity and authorization model. An app-only access token is **not** a
replacement for a delegated token used as an OBO assertion.

Existing supported delegated cache serialization and silent acquisition remain
unchanged. This PR does not delete historical CacheCompat clients or change their
persisted cache formats.

## Independent IWA removal

This PR is based directly on `rel/v5`, independently of the IWA-removal PR. IWA
still calls `CommonNonInteractiveHandler`, user-realm discovery, MEX, WS-Trust,
and `IMsalWsTrustHttpClientFactory` on this base, so those shared components and
their transport tests remain. Their redirect, certificate-validation, and
default-credential protections are unchanged. Shared username/password-policy
and SOAP helpers are not a surviving public or broker ROPC route.

After both removals are integrated, reconcile their overlapping changes and
remove any newly orphaned federation implementation in a follow-up. Do not remove
the HTTP factory or its protections while IWA still needs them. IWA's existing
shared error-code guard is also retained.

## Preserve delegated integration coverage

OBO, long-running OBO, mTLS user transport, silent, CIAM, and refresh-token
integration tests now bootstrap with real delegated refresh tokens obtained
beforehand through supported interactive or device-code sign-in, using each
test's actual client registration, authority, user, consent, and resource scopes.
They still execute their original assertions; no app-only assertion is substituted.
The two-user OBO test additionally requires different home-account IDs.

Set `MSAL_V5_DELEGATED_TOKEN_FIXTURE` to a protected local JSON file containing
an array with `ClientId`, `Username`, and `RefreshToken` properties for each
required client/user pair. Do not commit this file or print its tokens. The helper
matches client and username, redeems using `grant_type=refresh_token`, requires a
delegated account, and verifies the returned username. A missing fixture or
matching entry makes setup explicitly inconclusive, not passed. Invalid files,
authentication failures, or wrong identities fail visibly.

Native broker tests require their lab accounts provisioned by supported
interactive sign-in and `MSAL_V5_BROKER_LAB_READY=1`. They acquire a forced-refresh
silent token from the provisioned account and retain their subsequent silent,
cache, telemetry, SSH, PoP, and account-removal assertions. Re-provision accounts
between tests that remove them. Missing opt-in is explicitly inconclusive.

The current-source CacheCompat harness still reads/writes the same v2/v3 storage
and checks the requested user's silent reuse across versions. The matrix deletes
its test caches before the first client runs, so externally seeding those same
cache files is not sufficient. For a .NET-first scenario, provision
`MSAL_V5_DELEGATED_TOKEN_FIXTURE` for every participating client/user pair with
consent for the requested Microsoft Graph scopes. On a cold silent miss, the
harness redeems the matching refresh token and persists the delegated result in
the registered file-backed cache. The first client must acquire from the network;
the second must reuse the cache, and both must return the requested username.
These assertions and the Java-first direction remain unchanged.

Missing .NET-first provisioning is explicitly inconclusive before deleting
caches or launching clients; malformed fixtures and authentication failures still
fail. Manual device-code setup remains available only through the explicit
`MSAL_CACHECOMPAT_DEVICE_CODE=1` opt-in and requires a user to complete sign-in.
Do not enable that interactive path in unattended CI. Both setup paths reject a
different signed-in user or a missing delegated account/token. Cross-version
live replay and identity-provider/native-broker labs still need provisioning and
are not proved by local mocked tests or an inconclusive CI result.
