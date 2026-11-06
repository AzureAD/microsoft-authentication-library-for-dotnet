# Migrating from IWA to WAM in MSAL.NET 5

This guidance applies to the unreleased `rel/v5` development line. MSAL.NET 4.x is unchanged.

## Removed APIs

`AcquireTokenByIntegratedWindowsAuth` is removed from `IPublicClientApplication` and
`PublicClientApplication`, along with `AcquireTokenByIntegratedWindowsAuthParameterBuilder`
(including `WithUsername` and `WithFederationMetadata`) and its execution path.
The IWA-specific `MsalError.IntegratedWindowsAuthenticationFailed` and
`MsalError.IntegratedWindowsAuthNotSupportedForManagedUser` constants are also removed.
There is no throwing compatibility stub: rebuilding an old IWA caller against v5 fails at compile time.
Telemetry API ID `1002` remains reserved and is not assigned to another flow.

## Windows desktop OS-account SSO

Use the Windows broker (WAM), not a username/password request, to sign in with the
Windows OS account. Reference `Microsoft.Identity.Client.Broker`, configure the app
registration and broker redirect URI as described in the [WAM guide](https://aka.ms/msal-net-wam),
and supply the application's valid parent window handle.

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
    .WithDefaultRedirectUri()
    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
    .WithParentActivityOrWindow(() => windowHandle)
    .Build();

AuthenticationResult result;
try
{
    result = await app.AcquireTokenSilent(
        scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync();
}
catch (MsalUiRequiredException)
{
    result = await app.AcquireTokenInteractive(scopes)
        .WithAccount(PublicClientApplication.OperatingSystemAccount)
        .ExecuteAsync();
}
```

`clientId`, `windowHandle` and `scopes` come from the application's configuration and UI.
Run the interactive fallback only where user interaction is permitted. WAM can require
consent, account setup or other interaction; silent success is not guaranteed.
For subsequent requests for a specific account, retain `result.Account` and pass it to
`AcquireTokenSilent`. Broker-backed silent SSO and the OS-account sentinel remain supported.
Headless applications should select an appropriate supported flow, such as device code;
WAM is not a replacement for an unattended service.

## Federated username/password is separate

This change does not remove the existing username/password API or its broker path.
Federated ROPC retains user-realm discovery, MEX metadata (including supplied metadata),
HTTPS username/password endpoint selection, WS-Trust 2005/1.3 SOAP requests and SAML exchange.
Windows-transport endpoint selection and the credential-free IWA SOAP request are removed.

ROPC remains deprecated and is not the recommended migration for OS-account SSO.
Its federation HTTP factory contract, MSAL-controlled redirect handling, HTTPS validation,
cross-origin credential suppression and rejection of credential-bearing POST redirects remain intact.
Historical shipped API inventories record the removed APIs; each affected target's unshipped
inventory marks them removed. Target framework changes and package/release versioning are outside this PR.
