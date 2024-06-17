4.60.3
==========
### Bug Fixes
Updated Android webview attribute.

4.60.2
==========
### Bug Fixes
When `OnBeforeTokenRequest` extensibility API is used, MSAL now correctly uses the user-provided `OnBeforeTokenRequestData.RequestUri` to set the token request endpoint. See [4701](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4701).

4.60.1
==========
### Bug Fixes
Resolved an issue where MSAL attempts to acquire a token via certificate authentication using SHA2 and PSS resulting in a `MsalServiceException' (Error code: AADSTS5002730). See [4690](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4690)

4.60.0
==========
### New Features
- AAD client assertions are computed using SHA 256 and PSS padding. See [4428](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4428)
- CorrelationId is available in MsalException. See [4187](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4187)
- Open telemetry records telemetry for proactive token refresh background process. See [4492](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4492)
- MSAL.Net now supports generic authorities with query parameters. See [4631](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4631)

### Bug Fixes
- MSAL.Net now logs an error when OBO is performed over common or organizations. See [4606](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4606)
- MSAL.Net now handles the v2.0 authorization endpoint. See [4416](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4416)
- Improved logging and error message when the web api receives a claims challenge. See [4496](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4496)
- Cloud shell error message from the managed identity endpoint is now parsed correctly. See [4402](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4402)
- Improved error message when CCA certificate is disposed before MSAL can use it. See [4602](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4602)
- Client id is now accepted as a scope. See [4652](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4652)

4.59.0
==========
### New Features
- Removed support for deprecated Xamarin.Android 9 and Xamarin.Android 10 frameworks. MSAL.NET packages will no longer include `monoandroid90` and `monoandroid10.0` binaries and instead include `monoandroid12.0`. Xamarin.Android apps should now target framework version 12 (corresponding to Android API level 31) or above. See [3530](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3530).
- Removed support for deprecated .NET 4.5 framework. MSAL.NET packages will no longer include `net45` binary. Existing applications should target at least .NET 4.6.2. See [4314](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4314).

### Bug Fixes
- When public client apps persist cache data on Linux platforms, exceptions are now thrown, instead of just logged. This behavior is now consistent with Windows and Mac cache accessors. See [4493](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4493).
- Downgraded System.Diagnostics.DiagnosticSource dependency to 6.0.1 from 7.0.2 to enable apps to run in .NET 6 in-process Azure Functions. Added extra checks to prevent crashing if OpenTelemetry dependencies cannot be used in the app's runtime. See [4456](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4456).
- MSAL now throws `MsalServiceException` instead of `MsalManagedIdentityException` in managed identity flows. See [4483](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4483).
- Background proactive token refresh operation can now be cancelled using the cancelation token passed into the parent acquire token call. See [4473](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4473).
- Fixed `SemaphoreFullException` happening in managed identity flows. See [4472](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4472).
- Improved exception messages when using non-RSA certificates. See [4407](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4407).
- Fixed a scenario when the same tokens are cached under different cache keys when an identity provider sends scopes in a different order. See [4474](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4474).

4.58.1
==========
### New Features
- Added `WithForceRefresh` support for silent flows using the Windows broker. See [4457](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4457).

### Bug Fixes
- Fixed a bug when a `x-ms-pkeyauth` HTTP header was incorrectly sent on Mac and Linux platforms. See [4445](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4445).
- Fixed an issue with client capabilities and claims JSON not being merged correctly. See [4447](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4447).
- MSAL can now be used in .NET 8 applications which use native AOT configuration binder source generator. See [4453](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4453).
- Fixed an issue with sending an incorrect operating system descriptor in silent flows on Mac. See [4444](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4444).

4.58.0
==========
### New Features
- Removed support for deprecated .NET 4.6.1 framework and added .NET 4.6.2 support. MSAL.NET packages will no longer include `net461` binary. Existing .NET 4.6.1 apps will now reference .NET Standard 2.0 MSAL binary. See [4315](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4315).
- MSAL.NET repository now supports Central Package Management. See [3434](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3434).
- Added instrumentation to collect metrics with Open Telemetry. Aggregated metrics consist of successful and failed token acquisition calls, total request duration, duration in cache, and duration in a network call. See [4229](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4229)

### Bug Fixes
- Resolved the issue with dual-headed accounts that share the same UPN for both, Microsoft (MSA) and Microsoft Entra ID (Azure AD) accounts. See [4425](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4425).
- MSAL now correctly falls back to use local cache if broker fails to return a result for `AcquireTokenSilent` calls. See [4395](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4395).
- Fixed a bug when the cache level in the telemetry was not correctly set to L1 Cache when in-memory cache was used. See [4414](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4414).
- Deprecated `WithAuthority` on the request builders. Set the authority on the application builders. Use `WithTenantId` or `WithTenantIdFromAuthority` on the request builder to update the tenant ID. See [4406](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4406).
- Fixed an issue with the Windows broker dependencies when the app was targetting NativeAOT on Windows. See [4424](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4424).
- Updated Microsoft.Identity.Client.NativeInterop reference to version 0.13.14, which includes bug fixes and stability improvements. See [4439](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4439).

4.57.0
==========
### New Features
- Removed support for deprecated .NET Core 2.1 framework. MSAL.NET packages will no longer include `netcoreapp2.1` binary. Existing .NET Core 2.1 apps will now reference .NET Standard 2.0 MSAL binary. See [4313](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4313).
- Added additional logging in the cache. See [3957](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3957).
- Removed unused HTTP telemetry data (`x-client-info`). See [4167](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4167).
- Updated `Microsoft.Identity.Client.NativeInterop` reference to version 0.13.12, which includes bug fixes and stability improvements. See [4374](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4374).

### Bug Fixes
- Added simple retry logic for signing client assertions failures. See [4366](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4366).
- Fixed inconsistencies in throwing exceptions for badly formatted authorities. Now MSAL will always throw an `ArgumentException` if an authority is in incorrect format (e.g., doesn't start with HTTPS, has spaces, etc.) See [4280](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4280).
- Included missing Windows broker-related exception data when serializing MSAL exceptions. See [4371](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4371).
- Fixed a crash when using managed identity and provided resource is null. See [4332](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4332).
- Removed duplicate Windows broker logs. See [4353](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4353).

4.56.0
==========
### New Features
- MSAL.NET cache extensions ([Microsoft.Identity.Client.Extensions.Msal](https://www.nuget.org/packages/Microsoft.Identity.Client.Extensions.Msal)) package has been moved to the main MSAL.NET repository (where any [new issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues) should be created). The package version has been increased to match the main MSAL version. Along with this move, support for .NET 4.5 and .NET Core 3.1 was removed and this package now only supports .NET Standard 2.0. Additionally, [Microsoft.Identity.Client.Extensions.Adal](https://www.nuget.org/packages/Microsoft.Identity.Client.Extensions.Adal) has been deprecated. See [3152](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3152), [4330](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4330).
- Added `AuthenticationResult.AuthenticationResultMetadata.Telemetry` that currently contains telemetry from the [Windows broker (WAM)](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam). See [4159](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4159).  

### Bug Fixes
- Added throttling logic for acquiring tokens for managed identity (using `AcquireTokenForManagedIdentity` and `WithAppTokenProvider`) to prevent the throttling exceptions thrown by the managed identity endpoints. See [4196](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4196).  
- Enabled cache synchronization by default. This helps to keep the cache consistent when a singleton confidential client application (CCA) is used with enabled external token cache serialization. The cache synchronization has a negligible performance effect when CCA is created per request. See [4268](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4268).  
- Fixed an authority validation error in interactive flows when an Active Directory Federation Services (ADFS) authority with a tenant ID was used. See [4272](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4272).  
- Added clarity to the Windows broker logs. See [4318](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4318).  

4.55.0
==========
### New Features
- A user-assigned managed identity can now be specified using its object ID. See [4215](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4215).  

### Bug Fixes
- [`WithTenantId`](https://learn.microsoft.com/dotnet/api/microsoft.identity.client.abstractapplicationbuilder-1.withtenantid?view=msal-dotnet-latest) now works with CIAM authorities. See [4191](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4191).
- Improved the error message when cache serialization fails. See [4206](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4206).  
- Improved logging when using the Windows broker (WAM). See [4183](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/4183).  

4.54.1
==========
### New Features
- The client-side telemetry API (`ITelemetryClient`) is now generally available. See [3784](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3784).  
- Added `WithSearchInCacheForLongRunningProcess()` modifier which allows `InitiateLongRunningProcessInWebApi` method to search in cache. This flag is intended only for rare legacy cases; for most cases, rely on the default behavior of `InitiateLongRunningProcessInWebApi` and `AcquireTokenInLongRunningProcess`. See [4124](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4124).  

### Bug Fixes
- `WithTenantId` can now be used with dSTS authorities to overwrite the tenant. See [4144](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4144), [4145](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4145).  
- Fixed a bug in token serialization for rare cases when an ID token has no `oid` claim. See [4140](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4140).  

4.54.0
==========
### New Features
- Acquiring tokens with managed identity is now generally available. See [4125](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4125) and [documentation for managed identity in MSAL.NET](https://aka.ms/msal-net-managed-identity).  
- Updated the managed identity API to specify the identity type when creating an `ManagedIdentityApplication`. See [4114](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4114).  
- When acquiring tokens with managed identity and using the default HTTP client, MSAL will retry the request for certain exception codes. See [4067](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4067).  
- Adds `MsalManagedIdentityException` class that represents any managed identity related exceptions. It includes general exception information including the Azure source from which the exception originates. See [4041](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4041).  
- MSAL will now proactively refresh tokens acquired with managed identity. See [4062](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4062).  
- MSAL will now proactively refresh tokens acquired using `AppTokenProvider` API. See [4074](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4074).  
- `MsalException` and derived exception classes now have a property `AdditionalExceptionData`, which holds any extra error information. Currently it is only populated for exceptions coming from the Windows authentication broker (WAM). See [4106](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4106).  
- For HTTP telemetry. added a new telemetry ID for long-running on-behalf-of requests. See [4099](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4099).  

### Bug Fixes
- Fixed a JSON serialization issue in iOS apps that are built in release Ahead-Of-Time (AOT) compilation mode. See [4082](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4082).  
- MSAL.NET package now references correct Microsoft.iOS version. See [4091](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4091).  
- Microsoft.Identity.Client.Broker package can now be used in projects which rely on the older package.config. See [4108](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4108).  
- Fixed a `user_mismatch` error when `WithAccount` is specified when acquiring tokens interactively and selecting a different account in the account picker. See [3991](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3991).  

4.53.0
==========
### New Features
- Added support for CIAM authorities. See [3990](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3990).  

### Bug Fixes
- Fixed issue where WAM is invoked for B2C authorities. MSAL will now fall back to the browser for this scenario. See [4072](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4072).  

4.52.0
==========
### New Features
- The improved experience using Windows broker (WAM) is now generally available for all desktop platforms, except UWP. See [3375](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3375), [3447](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3447).  
- Acquiring Proof-of-Possession tokens on public desktop clients using WAM broker is now generally available. See [3992](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3992).  
- The requirement for a specific Windows SDK version on .NET 6 platform has been removed, which should improve the package usage on .NET 6 platforms. MSAL.NET now targets a more general `net6.0-windows` instead of `net6.0-windows10.0.17763.0`. See [3986](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3986).  
- Added support for non-Azure AD IdP's in client credential flows. Use `WithGenericAuthority(authority)`. This is still an experimental API and may change in the future. See [4047](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4047), [1538](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1538).  
- Added `AuthenticationResult.AdditionalResponseParameters` property bag with any extra parameters from the AAD response. This collection will also have `spa_accountId` parameter which can be used in brokered hybrid single-page application (SPA) scenarios. See [3994](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3994).  

4.51.0
==========
### New Features
- Simplified managed identity API. Use `ManagedIdentityApplicationBuilder` to create a `IManagedIdentityApplication` and call `AcquireTokenForManagedIdentity`. See [3970](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3970) and [wiki](https://aka.ms/msal-net-managed-identity).  
- Added `StopLongRunningProcessInWebApiAsync` which allows to remove cached tokens based on a long-running OBO key. See [3346](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3346) and [wiki](https://aka.ms/msal-net-long-running-obo).  

### Bug Fixes
- `InitiateLongRunningProcessInWebApi` will now always acquire new tokens from AAD without checking the token cache first. See [3825](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3825).  
- When using preview Windows broker, MSAL will correctly handle the transitive reference to Microsoft.Identity.Client.NativeInterop. Any explicit references to Microsoft.Identity.Client.NativeInterop in projects also referencing MSAL should be removed. See [3964](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3964).  
- Fixed preview Windows broker throwing a signed out exception when calling `AcquireTokenSilent` after acquiring token using the Username/Password flow. See [3916](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3916) and See [3961](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3961).  


4.50.0
==========
### New Features
- Extended managed identity experimental functionality with support for Azure Cloud Shell. See [3832](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3832).  
- Added support for PII logging for WAM preview. See [3845](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3845), [3822](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3822).  


### Bug Fixes
- Fixed JSON serialization issues for apps running on .NET 7. See [3892](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3892).  
- Improved logging performance to only create logs when a specified log level is enabled. See [3901](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3901).  
- Fixed `Unable to load DLL 'msalruntime'` exception for apps that use WAM preview and are packaged as MSIX. See [3740](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3740).  
- WAM preview now honors the login hint. See [3301](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3301) and [WAM docs](https://aka.ms/msal-net-wam).  
- WAM preview now allows to sign in with an account different from the provided login hint. See [3929](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3929).  
- Fixed an `ApiContractViolation` exception in WAM preview when signing out. See [3685](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3685).  
- MSAL now allows passing no scopes when using WAM preview. See [3675](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3675).  
- When broker is enabled, MSAL will now use the refresh token from the broker instead of a locally cached one. See [3613](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3613). 
- Added a more descriptive error message when combined flat user and app cache is used. Use a partitioned token cache (for ex. distributed cache like Redis) or separate files for app and user token caches. See [3218](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3218).   
- Updated logs to clarify that managed identity correlation ID differs from MSAL one. See [#3908](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3908).  
- Fixed an occasional cryptographic exception by removing the RSA public key size check - AAD is better suited to handle this verification. See [3896](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3896).  
- Fixed JSON parsing errors when receiving an error token response. See [3883](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3883).  
- Added better error handling when receiving WS-Trust responses. See [3614](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3614).  
- `WithAuthority` methods on the request builders are hidden. Use either `WithTenantId` on the request builders or `WithAuthority` only on the application builder. See [#2929](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2929).  
- Deprecated `IsBrokerAvailable` method on mobile platforms. Applications should rely on the library automatically falling back to a browser if the broker is not available. See [3320](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3320).  
- Deprecated unused extended expiry API. See [1377](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1377).  

4.49.1
==========
### New Features
- Extended managed identity experimental functionality with support for Azure Arc. See [3862](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3862)  

### Bug Fixes
- Updated the Broker package to use Microsoft.Identity.Client.NativeInterop 0.13.3 to resolve crash related to garbage collection when using new WAM broker preview. See [3868](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3868)  
- Disabled additional logging in new WAM broker introduced in MSAL 4.49.0. See [3875](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3875)  

4.49.0
==========
### New Features
- MSAL will now use `<region>.login.microsoft.com` when using regional ESTS-R for public cloud. See [3252](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3252)
- Added support for acquiring Work and School accounts when calling `GetAccounts` using the new Broker preview. See [3458](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3458)
- Added the ability to disable Instance Discovery/Authority validation using `WithInstanceDiscovery(bool enableInstanceDiscovery)`. See [3775](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3775)
- Added new APIs to acquire authentication data from WWW-Authenticate and Authentication-Info request headers. This will provide additional support for Proof-of-Possession. See [3026](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3026)

### Experimental Features
- [Managed identities for Azure resources](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview) provide Azure services with an automatically managed identity in Azure Active Directory. You can use this identity to authenticate to any service that supports Azure AD authentication, without having credentials in your code. MSAL now supports acquiring token for managed identities for Azure App Services and Azure Virtual Machines. Use `WithManagedIdentity()` method on the `AcquireTokenForClient` API to get an MSI token. This is an experimental feature and may change in the future versions of MSAL. See [3754](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3754) and [3829](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3829)

### Supportability
- Enabled more logging for new WAM broker. See [3575](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3575)

### Bug Fixes
- Optimized MSAL cache key logic to improve performance. See [3393](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3393)

4.48.1
==========

### Supportability
- Fixes an internal (Microsoft 1P only) MSA-PT issue for the new WAM preview broker. See [VS#1809364](https://identitydivision.visualstudio.com/Engineering/_workitems/edit/1809364) and [VS#1643652](https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1643652)

### Bug Fixes
- Added header title to the Account Picker for the new WAM preview broker. See [3803](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3769).  


4.48.0
==========

### New Features
- Removed support for deprecated `net5.0-windows10.0.17763.0` target. See [3770](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3770) and note below.  
- Added support for `net6.0` and `net6.0-windows10.0.17763.0` targets. See [3682](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3682) and note below. Library is trimmable and uses System.Text.Json for serialization via code generation.
- Removed support for old `xamarinmac20` target. See [3722](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3722).  
- `WithProofOfPossession` for public client applications is now generally available. See [3767](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3767).  
- Added telemetry to log Proof-of-Possession usage. See [3718](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3718).  
- Exposed tenant profiles for all authorities which are tenanted (B2C and dSTS). See [3703](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3703).  
- Now logging MSAL version to common telemetry client. See [3745](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3745).  
- Updated guidance on retry policies. See [Retry Policy wiki](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Retry-Policy) and [3561](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3561).  

### Bug Fixes
- Fixed a `NullReferenceException` related to authority URLs when calling `AcquireTokenSilent` with an Operating System account in apps using WAM. See [3769](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3769).  
- Fixed a `NullReferenceException` when using preview broker and calling `AcquireTokenSilent` with MSA account and MSA-PT enabled. See [3743](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3743).  
- Added an `Exported` attribute to Android activities to be compliant with Android OS 12.1 (API 32) and above requirements. See [3680](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3680).  
- Fixed incorrect home account details in `AuthenticationResult` of `AcquireTokenByRefreshToken`. See [3736](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3736).  

### .NET 5 and .NET 6 public client applications
If you have a Windows application which targets `net5.0`, `net5.0-windows`, `net5.0-windowsX`, `net6.0`, or `net6.0-windows` and would like to use either WAM or embedded browser, you must change the app target to at least `net6.0-windows10.0.17763.0`.  System browser works on all of the above targets.  
The recommendation is to use new Windows broker preview, as it offers better experience than current WAM implementation and will be generally available in the near future. If you want to try the new broker preview, install the NuGet package Microsoft.Identity.Client.Broker and call the `.WithBrokerPreview()` method. For details, see https://aka.ms/msal-net-wam.


4.47.2
==========

### New Features
- Hide legacy API's that are available only to internal Microsoft only (1P) applications. See [3670](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3670).
- Soft deprecate `WithAuthority` API on AcquireTokenXXX methods. Instead use `WithTenantId` or `WithTenantIdFromAuthority`, or `WithB2CAuthority` for B2C authorities. See [#3716](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3716)
- Logging error codes to MSAL Telemetry. See [3595](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3595)
- Add more logging around client creds and claims. See [3707](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3707).
- Improve extensibility APIs to support new POP

### Bug Fixes
- Improved error messages when new preview broker exceptions are thrown. [#3696](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3696)
- MSAL will now throw an exception if no scopes are passed for the new preview Broker or for B2C scenarios. See [#3675](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3675)
- Removed .NET 6 MacCatalyst target because MSAL.NET doesn't currently support it. See [#3693](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3693)
- Throw an exception when new WAM DLLs are not loaded when invoking the new WAM preview broker. See [#3699](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3699)

4.47.1
==========

### Supportability
- Fixes an internal (Microsoft 1P only) NuGet feed issue. See [#3689](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3689)

4.47.0
==========

### New Features
- Support for .NET MAUI is now generally available for iOS, Windows and Android targets. The package also works with UWP. Refer to [`MauiStatus.md`](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/MauiStatus.md) for details.
- The new MSAL logging feature is now generally available. `WithExperimentalFeatures()` is no longer required when calling `WithLogging()`. See [3548](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3548), [wiki](https://aka.ms/msal-net-logging).
- Adding IsProofOfPosessionSupportedByClient api to be used to determine if the current broker is able to support Proof-of-Posession. See [3496](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3496) 
- Adding ability to turn off the default retry-once policy on 5xx errors. See [2877](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2877)
- Adds new public builder API accepting instances of `ITelemetryClient`. See [3533](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3533).
- Added logic to log some acquire token data via the new telemetry pipeline. See [3534](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3534).

### Bug Fixes
- MSAL will now throw an exception if no scopes are passed when the new preview broker is invoked. See [#3654](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3654) and [#3677](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3677)
- `MsalServiceException.IsRetryable` is now correctly set. See [#3661](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3661)
- Added extra logging in Preview Broker `RemoveAccountAsync` API. See [#3658](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3658)
- Added a check for null account in Preview Broker `RemoveAccountAsync` API. See [#3657](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3657)
- `AuthenticationResult` now shows correct authority for multi-cloud requests using WAM. See [#3637](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3637)
- Adding null IdentityLogger to prevent null reference exception when using cache logger. See [#3678](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3678)

4.46.2
==========

### New Features
- WAM Authentication Library now explicitly supports .NET 4.6.2. See [#3539](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3539)

### Bug Fixes
- Fixed 'Authenticator Factory has already been started` exception in new MSAL WAM preview. See [#3604](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3604)
- Added back missing .NET Standard 2.0 target to MSAL.NativeInterop package. See [#3612](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3612)
- [Resilience] Changed to an improved implementation of HTTP client factory on .NET Framework to improve resiliency (for ex. by reducing the amount of request timeouts). See [#3546](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3546)
- Logging additional exceptions to telemetry. See [#3547](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3547)

4.46.1
==========

### New Features
- Added Explicit .net 461 support to new WAM Preview broker. See [3550](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3550).  
- Added MSALRuntime TelemetryData to verbose logging when a broker exception is thrown. See [3585](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3585)

### Bug Fixes
- Minor clarifications in caching logs. See [3582](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3582)


4.46.0
==========

### New Features
- Added `AcquireTokenByUsernamePassword` flow in WAM broker preview. See [3308](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3308).  
- Added support for Proof-of-Possession tokens to `AcquireTokenByUsernamePassword` flow in WAM broker preview. See [3308](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3308).  
- Added `WithTenantIdFromAuthority` API to request builder. See [3429](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3429).  
- Exposed new Identity Logger in the `TokenCacheNotificationArgs`. See [3404](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3404).  
- [Security] Increased size of PKCE verifier. See [1777](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1777).  
- Enabled multi-cloud support in WAM. See [3477](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3477).  

### Bug Fixes
- Deprecated and replaced `SecureString` usage with strings. See [2437](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2437).  
- Refactored authority related code to use URI class instead of strings. See [3487](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3487).  
- Fixed authority resolution for B2C authorities. See [3471](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3471).  
- Improved WAM broker preview behavior for remembered accounts. See [3437](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3437).  
- Obsoleted with a warning `AcquireTokenSilent(scopes, login_hint)` for confidential client applications as it's not applicable in those scenarios. See [3403](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3403).  
- Now passing `intune_mam_resource` to the mobile broker. See [3490](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3490).  
- Fixed DSTS endpoints. See [3492](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3492).  
- Cancellation tokens are now correctly passed to Windows broker and embedded web views. See [3225](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3225).  
- Move app token provider feature to extensibility namespace and clarified its use. See [3475](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3475).  

### Fundamentals
- Improved and simplified .NET Standard platform specific code. See [3451](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3451).  
- Fix line endings in unit test files to enable running on Linux. See [3425](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3425).  

4.46.0-Preview2
==========

### New Features
This preview package adds support for.NET MAUI. It adds .NET 6 iOS and Android targets. The package also works with UWP. Refer to [MauiStatus.md](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/MauiStatus.md) for details.  

4.45.0
==========

### Important changes for UWP apps
Upgrade the minimum target platform to 10.0.17763.0
Upgrade Microsoft.NETCore.UniversalWindowsPlatform to 6.1.9 or above
Add a reference to Microsoft.IdentityModel.Abstractions, for projects that use package.json

### New Features
Logs are now consistent when you use several .NET authentication libraries from Microsoft**. See [3028](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3028).  
**Exposed tenant ID and scopes in `TokenCacheNotificationArgs`**. See [3389](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3389).  
**Added new `WithClientAssertion` API** that exposes the token endpoint. See [3352](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3352).  
**Added additional descriptive information to error logs**. See [3278](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3278).  
**Updated support from .NET Standard 1.3 to.NET Standard 2.0**. See [1991](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1991).  

### Bug Fixes
**Tenant profiles are now returned when calling `GetAccounts` with broker enabled**. See [3349](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3349).  
**Fixed parsing of authentication result from broker preview**. See [3354](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3354).  
**Fixed DSTS endpoints**. See [3367](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3367).  
**Privacy and Terms of Use links are now visible** in embedded picker UI on smaller screens. See [3153](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3153).  
**Fixed broker Proof-of-Possession token appearing as `Bearer`** when calling `GetAuthorizationHeader()`. See [3353](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3353).  
**Ensured MSAL doesn't check local cache for tokens** when using Proof-of-Possession with the broker preview. See [3363](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3363).  

4.44.0
==========

### New Features
**Added support in MSAL for dSTS authority** See [3198](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3198).  
**Enabled Azure.Identity (Azure SDK) to benefit from MSAL.NET token cache when used for Managed Identity** See [3137](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3137).  

### Experimental Features
**MSAL.NET now has a new WAM preview which is an abstraction layer based on MSAL C++ with support for Proof-of-Possession access tokens**. This fixes some issues with current WAM implementation. See [3192](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3192) and [wiki](http://aka.ms/msal-net-wam).  

### Bug Fixes
[IMPORTANT][RELIABILITY] **In case of cancellation or timeout via CancellationToken, MSAL now throws the correct TaskCancelledException instead of MsalServiceException with error code request_tiomeout** See [3283](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3283).  
**Fixed `AcquireTokenSilent` to not display a login prompt unnecessarily for operating system accounts in WAM**. See [3294](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3294).  
**Fixed NullReferenceException in IsBrokerAvailable()** See [3261](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3261).  
**Fixed a race condition to improve stability of region autodiscovery**. See [3277](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3277).  
**Fixed a bug in instance discovery by adding pre-production environment (PPE) domains to known endpoints**. See [3265](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3265).  

### Fundamentals
**Improved automated performance microbenchmarks to better reflect common scenarios** See [3297](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3297) and [wiki](https://aka.ms/msal-net-performance-testing).  

4.43.2
==========

### Bug Fix
**MSAL will now allow the use of different scopes when acquiring access tokens using a cached refresh token in long running On-Behalf-Of processes**. See [2817](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2817).  

4.43.1
==========

### Bug Fix
**MSAL now uses WebView1 instead of WebView2 for `AcquireTokenInteractive` with AAD or ADFS authority because WebView2 doesn't support SSO**. See [3270](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3270).  


4.43.0
==========

### New Features
**Added Intune Mobile App Management (MAM) support for Android**. See [3185](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3185).  
**MSAL.NET Cache Extensions now protects plaintext cache files with owner only read/write permissions**.See [3186](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3186), [169](https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/169).  

### Bug Fixes
**Client capabilities flags are correctly passed to Android Broker**. See [3203](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3203).  
**Fixed `WithAccount(result.Account)` to work when using WAM**. See [3121](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3121).   
**Improved token cache filtering logic**. See [3178](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3178), and [3233](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3233).  
**Fixed an error in creating UWP package for Microsoft Store upload**.  See [3184](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3184), [3239](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3239).  
**Fixed a bug to correctly sign-out an account from WAM**. See [3248](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3248).  
**Correctly showing a browser in WSL2**. See [3251](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3251).  

4.42.1
==========

### Bug Fixes
**Fixed a bug affecting WAM authentication with new accounts when the authority ends in `/organizations`**. See  [3217](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3217).  
**Fixed an error in creating UWP package for Microsoft Store upload**.  See [3184](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3184).  

4.42.0
==========

### New Features
**Multi Cloud Support** Allows 1st party public client apps which target the public cloud to log in users from other clouds. Not supported for broker flows. See [Multi-cloud support](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Multi-Cloud-Support-or-Instance-Aware), [2524](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2524).  
**Expose the region or error used by MSAL** in AuthenticationResult.AuthenticationResultMedatadata and in logs. See [2975](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2975).  
**App protection (true MAM) support for iOS**. See [2894](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2894).  

### Bug Fixes

**Fix a bug causing an "Sequence Contains No Elements" exception** This occurs in rare circumstances when saving the token cache. [3130](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3130).  
**Fix a bug causing an "ArgumentOutOfRangeException: the relative expiration value must be positive" exception** This occurs in rare circumstances when saving the token cache. [2859](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2859).  
**Default OS account login with MSA fails** This affects some first party applications (MSA passthrough) when using WAM [3157](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3157).  
**WwwAuthenticateParameters should not expose Resource** [3144](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3144).  


4.41.0
==========
### New Features:
**MSAL now uses the WAM AAD plugin's account selector if authority is AAD only.** This overcomes the issue of console apps not being able to display the account picker and other issues with Account Picker instability. See [2289](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2289).  
**Added `OnBeforeTokenRequest` public API which allows to execute a custom delegate before MSAL makes a token request**. and enables support for legacy Proof-of-Possession implementations. See [3114](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3114).  
**Added `kid` in cache keys for client credential flows using Proof-of-Possession**. See [3115](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3115).  
**Improved the error message when both region and custom metadata are configured.** See [3014](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3014).  
**Exposed the ability to add a custom header text to auth dialogs such as WAM.** See [3125](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3125).  
**MSAL now supports using Linux broker via Microsoft Edge.** Use `WithBroker()` to authenticate with Microsoft Edge system browser, if installed, which integrates with Linux broker to offer a better authentication experience. See [3051](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3051).  

### Bug Fixes:
**Added support for WAM on Windows Server 2022 and Windows 11,** and improved operation system detection for future versions. See [3040](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3040).  
**WAM is not supported on Windows Server 2016.** MSAL will now fall back to browser if this OS is detected. See [2946](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2946).  
**Fix for `GetAccountAsync` API by checking for null on `accountId` parameter.** See [3118](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/3118).  
**WAM is not supported in pure ADFS environments.** MSAL will now fall back to browser if the ADFS authority is used. See [2836](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2836).  

4.40.0
==========
### New Features:
**Authorization Code for Single Page Applications (SPA) feature is now generally available. `WithExperimentalFeatures()` is no longer required when calling `WithSpaAuthorizationCode()`**. See [2920](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2920), [wiki](https://aka.ms/msal-net/spa-auth-code), and [sample](https://aka.ms/msal-net/hybrid-spa-sample).  
**Allow POP token envelope to be created externally**. See [3059](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3059).  
**Remove obsolete telemetry (MATS) code** to improve performance and stability. See [3043](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3043).  
**Log clarification in several places**. See [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9e827ff0fda472a24aef87d790718ecc95c993a8) and
[here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/191d0dcacfe602858bbb77a1ae0ee5b2403fb54e).  

### Bug Fixes:
**Allow res:// error pages to be displayed in embedded WebView**. See [3083](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3083).  
**MSAL Logs are now more clear when regional is enabled and tokens are acquired from the cache**. See [3073](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3073).  

4.39.0
==========
### New Features:
**Added new `LogLevel.Always` and logging of important health metrics** to help with diagnostics of MSAL. See [3004](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3004) and [MSAL logging](https://aka.ms/msal-net-logging).  

### Bug Fixes:
**Fixed a crash in telemetry API when `AcquireToken*` builder is reused`**. See [3024](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3024).  
**Fixed sending an incorrect backup authentication system (CCS) value in B2C apps**. See [2748](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2748).  

4.38.0
==========
### New Features:
**Disabling cache synchronization for confidential client apps by default** to improve performance. See [2848](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2848).  
**MSAL now provides the correlation ID used in a to call Azure AD as part of cache callback (`TokenCacheNotificationArgs`)**. See [3008](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3008).  
**MSAL now provides a new specific API for long running web APIs, in addition to `AcquireTokenOnBehalfOf`**, which no longer requests refresh tokens. The advantage is that the On-Behalf-Of token cache is now smaller and automatically has an eviction, and long running web APIs are easier to write. See https://aka.ms/msal-net-long-running-obo and [2733](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2733).  
**Added hybrid SPA support to MSAL**. See https://aka.ms/msal-net/spa-auth-code and [2920](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2920).  

### Bug Fixes:
**Fixed issue where the authentication browser pop up would fail to show without an exception being thrown**. See [2839](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2839).  
**MSAL WAM now properly signs out guest accounts**. See [3016](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3016).  
**Reworded in-memory cache warning for web apps not using serialization**. See [2990](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2990).  
**Fixed issue where Proof-of-Possession token does not rotate properly for confidential client applications**. See [3003](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3003).  
**MSAL now returns a more descriptive exception when the browser back button is pressed during authentication**. See [2991](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2991).  
**On the request builder, `WithAuthority` has been deprecated and `WithTenantId` was added as an alternative instead**. See [2837](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2837).  
**MSAL will now only perform regional look up for client credential flows** See [3029](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3029).  

4.37.0
==========
### New Features:
**MSAL.NET now logs an error when `common` or `organizations` authority is used in `AcquireTokenForClient`**. See [#2887](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2887).  
**Added the ability to enable sending the certificate (as x5c) once when building the confidential client application**, rather than on every single token acquisition request. See [#2804](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2804).  
**Added additional methods to help create `WwwAuthenticateParameters` and get tenant ID by calling `GetTenantId`**. See [#2907](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2907), [#2922](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2922).  
**Added an additional async overload for `ConfidentialClientApplicationBuilder.CreateClientAssertion`**. See [#2863](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2863).  
**Added an ability to enable a shared token cache between different MSAL client application instances**, which can be set with the new `WithCacheOptions` API call. See [Enabling shared cache](https://aka.ms/msal-net-shared-cache), [#2849](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2849).  
**Added an `AuthenticationResult.AuthenticationResultMetadata.TokenEndpoint` property from which you can derive which authority was effectively used to fetch the token**. This can be used to determine if regional endpoint was used. See [#2830](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2830).  
**Added a cache refresh reason and time remaining before proactive token refresh to `AuthenticationResult.AuthenticationResultMetadata`**. See [#2832](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2832).  
**Added the ability to specify tenant ID instead of the full authority at the token acquisition APIs level with `WithTenantId`**. See [#2280](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2280).  

### Bug Fixes:
**Improved support for calling regional endpoints, especially in Azure Functions**. See [#2803](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2803).  
**Fixed a `NullReferenceException` when calling`AcquireTokenInteractive` with a login hint when using .WithBroker on Windows (WAM) **. See [#2903](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2903).  
**Improved the error message when the application is throttled by the identity provider**. See [#2855](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2855).  
**When proactive token refresh is enabled, MSAL.NET now refreshes the tokens on a background thread to improve performance**. See [#2795](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2795).  
**Improved caching performance by adding partitioning to the default in-memory user cache** used in user flows (like acquire token on-behalf-of, by authorization code). See [#2861](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2861), [#2881](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2881).  
**Improved performance by refactoring date handling when working with access tokens**. See [#2893](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2893).  
**Fixed a `Non-HTTPS URL redirect is not supported in webview` exception on Xamarin iOS for Facebook logins**. See [#2735](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2735).  
**Enabled setting the window title in WebView1 desktop browser**. See [#2936](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2936).  
**Added `WithPrompt` to the `GetAuthorizationRequestUrl` builder** to give the ability to specify the interaction experience for the user. See [#2896](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2896).  
**Added a more descriptive error message when `WithAuthority` is set at the request level and `WithAzureRegion` is used**. See [#2965](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2965).  

4.37.0-preview
==========
### New Features:
**MSAL.NET now logs an error when `common` or `organizations` authority is used in the client credentials request**. See [#2887](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2887).  
**Added the ability to enable sending the certificate (as x5c) once when building the confidential client application**, rather than on every single request. See [#2804](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2804).  
**Added additional methods to help create `WwwAuthenticateParameters`**. See [#2907](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2907).  
**Added an additional async overload for `ConfidentialClientApplicationBuilder.CreateClientAssertion`**. See [#2863](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2863).  
**Added an ability to enable a shared token cache between different MSAL client application instances**, which can be set with the new `WithCacheOptions` API call. See [Enabling shared cache](https://aka.ms/msal-net-shared-cache), [#2849](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2849).  

### Bug Fixes:
**Improved support for calling regional endpoints, especially in Azure Functions**. See [#2803](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2803).  
**Fixed a `NullReferenceException` when calling`AcquireTokenInteractive` with a login hint in WAM**. See [#2903](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2903).  
**Improved the error message when the application is throttled by the identity provider**. See [#2855](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2855).  
**When proactive token refresh is enabled, MSAL.NET now refreshes the tokens on a background thread to improve performance**. See [#2795](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2795).  
**Improved caching performance by adding partitioning to the default in-memory user cache** used in user flows (like acquire token on-behalf-of, by authorization code). See [#2861](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2861), [#2881](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2881).  
**Improved performance by refactoring date handling when working with access tokens**. See [#2893](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2893).  

4.36.2
==========
### Bug Fixes:
**Fixed a regression in authentication with the iOS broker**. See [#2913](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2913).  

4.36.1
==========
### New Features:
**Added support for Application ID URIs to be used in confidential client applications**. Confidential client applications, specifically web APIs, will now be able to use either the Client ID (GUID) or the Application ID URI, in the confidential client application builder. See [#2852](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2852).  

4.36.0
==========
### New Features:
**Added custom nonce support to Proof-of-Possession requests**. See issue [#2809](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2809).  
**Added a random jitter (within ten minutes range) to the Refresh In time for a token to optimize for resiliency**. See issue [#2796](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2796).  

### Bug Fixes:
**Added a more descriptive and actionable error message when AAD throttles the requests from the app**. See issue [#2808](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2808).  
**Improved error messaging related to broker support**. See issue [#2706](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2706).  
**MSA Pass-through enabled applications using MSAL can now use WAM**. See issue [#2822](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2822).  
**Improved error messaging when MSAL fails during the user realm discovery**. See issue [#2835](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2835).  
**Improved performance by removing unnecessary serialization in default app token cache** used in client credentials flow. See issue [#2826](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2826).  

4.35.1
==========
### Bug Fixes:
**Fixed a race condition in confidential client requests** when an authority with a different tenant is specified for each request. See issue [#2798](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2798).  

**Fix to correctly propagate `EnableCacheSynchronization` flag from `ConfidentialClientApplicationOptions`**. See pull request [#2801](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2801).  

4.35.0
==========
### Bug Fixes:
**AcquireTokenByIntegratedWindowsAuth provides better error messages.** Error messages are now more actionable. See issue [#2752](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2752) 

**MSAL no longer performs instance discovery on well known authorities.** This will improve performance for customers in regional scenarios See issue  [#2777](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2777)

**MSAL uses preferred_network name on sovereign clouds.** Skipping discovery will improve the performance. See issue [#2778](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2778)

**Error messages in Integrated Windows Authentication are now clearer**. The following message related issues are fixed
    - [#2731](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2731)
    - [#2752](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2752)

**ConfidentialClientApplicationBuilder with auto region discovery no longer throws UriFormatException.** This has been fixed by validating region string. See issue [#2772](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2772)

**Memory leak in AuthorityEndpoint caching has been fixed.** This will reduce memory leaks in the apps. See issue [#2770](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2770)

**ADFS now has consistent values for UserName between STS and cache.**. Tenant profiles will provide the consistency. [#1559](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1559)

**MSAL.NET no longer throws ArgumentNullException when the parameters for WithCcsRoutingHint() are null.** Authentication will now proceed as if not hint was provided. See issue [#2755](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2755)

**MSAL no longer throws exceptions for mismatched authorities if they are known aliases.** MSAL now ensures authorities configured in the application and request are not aliased before throwing. See issue [#2736](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2736)

**WebView2 was throwing error when KeyDown was handled.** This error has been removed. See issue [#2685](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2685)

**Instance discovery was performed multiple times on non-public non-sovereign clouds.** MSAL provides improved performance as it now only performs discovery when needed. See issue [#2701](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2701)

**RemoveAsync(account) in confidential client apps now returns suggested web cache key.** Empty key is no longer returned. [#2643](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2643)

**Invalid syntax in XML comments for NoPromptFailedError and NoTokensFoundError has now been fixed.** See issue [#2756](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2756)

**MSAL.NET now enables confidential client apps to disable the internal cache semaphore by setting the `EnableCacheSynchronization` property to 'false'**. This allows requests to bypass other requests that timeout, for example in the case of using a distributed cache. See PR [#2702](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2702) for details.

### New Features
**Limits on URL length in embedded browsers was causing errors with auth code.** Applications will not fail on the embedded browers due to the limitation.  See issue [#2743](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2743)

**IAccount now provides Tenant profile for each ID token.** This will enable customers to get ID tokens in the authentication results. See issue [#2583](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2583)

4.34.0
==========
### Bug Fixes:
**MSAL now has `WithCcsRoutingHint()`** to enable developers to more easily provide the CCS routing hint during authentication. See issue [2725](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2725)

### New Features
**MSAL.NET provides helper methods to extract the authentication parameters from the WWW-Authenticate headers.** This allows for dynamic scenarios such as claim challenge, Conditional Access Evaluation and Conditional Access authentication context scenarios. See https://aka.ms/msal-net/wwwAuthenticate and issue [#2679](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2679) for details.
**WAM support is now generally available in MSAL.NET.** `WithExperimentalFeatures()` is no longer required to authenticate with WAM. See https://aka.ms/msal-net-wam for more details
**MSAL enables easier cache eviction by exposing `SuggestedCacheExpiry`** which helps determine the cache eviction time for for client credentials scenarios. See issue [#2486](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2486)
**MSAL now adds runtime information to logs** enabling easier diagnosing of authentication issues on all platforms. See issue [2559](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2559)

4.33.0
==========
### New Features
**On-Behalf-Of flow logic now performs refresh token flow** eliminating the need to call `AcquireTokenSilent` and `GetAccounts` in OBO scenarios. See issue [#2623](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2623).
**Added monitoring flags for global stats**. See issue [#2646](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2646).
**MSAL.NET adds CCS routing information for interactive requests using client info.**. See issue [#2525](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2525) and PR [#2687](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2687).

4.32.1
==========
### Bug Fixes:
**When doing a client credential flow with an authority specified at the request level, the region is used and not the public cloud as the authority, which results in a cache miss**. See issue [#2686](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2686) for details.

### Fundamentals:
**Improved logging for cache performance**. See issues/PRs [#2690](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2690), [#2688](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2688), [#2680](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2680), and [#2678](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2678).

4.32.0
==========
### New Features:
** Add Kerberos ticket support **, see https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2602
**Added cancellation token to TokenCacheNotificationArgs**, to allow apps to send cancellation token to Redis. See issue [#2551](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2551)
**FindAccessToken now logs the number of access tokens**. See issue [#2417](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2417)
**MSAL now exposes AuthenticationResult.TokenType**. See issue [#2637](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2637)
**Introduce WithFederationMetadata option for IWA and Username/Password flows, allowing developers to inject the federation metadata XML document**. See issue [#2152](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2152)
**MSAL.NET now provides routing information to Cached Credential Service (CCS)**. See issue [#2525](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2525)

### Bug Fixes:
**Improved search of metadata in the federation metadata XML during WS-Trust flows**. See issue [#2665](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2665)
**Fixed a bug where WithTenant is ignored**. See issue [#2543](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2543)
**Fixed a bug with UWP token caching not being thread safe**. See issue [#2616](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2616)
**Handle multiple work accounts on same machine with WAM**. See issue [#2615](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2615)
**Fix B2C failure when user flows/policies have a name containing a `.`**. See issue [#2444](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2444)
**Handle the scenario where TokenType is null**. See issue [#2636](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2636)
**Handle Unity Windows Standalone il2cpp: NotSupportedException**. See issue [#2586](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2586)
**MSAL Xamarin Android now opens EDGE browser for authentication with OpenWithChromeEdgeBrowserAsync**. See issue [#2399](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2399)
**Handle WAM failure after account picker shows up on Win Server 2016**. See issue [#2572](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2572)
**Updated the regional telemetry schema**. See issue [#2622](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2622)

4.31.0
==========
### New Features:
**Added time metrics to `AuthenticationResultMetadata`**, which includes total duration, time spent in HTTP, and duration of token cache callbacks. See pull request [#2581](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2581).  
**Added telemetry data to requests sent to WAM**. See issue [#2562](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2562).  
**Added option to hide iOS security prompt for system browser** for iOS 13+. See issues [#2131](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2131), [#114](https://github.com/Azure-Samples/active-directory-xamarin-native-v2/issues/114), [#512](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/512).  

### Bug Fixes:
**Fixed parenting of WAM account picker control**. See issue [#2566](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2566).  
**Fixed an exception in console apps that use WAM when they are run directly as an executable**. See issue [#2608](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2608).  

4.30.1
==========
### Bug Fix:
**MSAL.NET now correctly does Base64 encoding instead of Base64 URL encoding when interacting with the broker**. See issue [#2554](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2554).

4.30.0
==========
### New Features:
**Added support for Proof Key for Code Exchange (PKCE) in confidential client authorization code flows**. See issue [#1473](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1473).  

### Bug Fixes:
**Removed iOS Xamarin workaround for background threads**, as it's no longer needed with fixes done in [Mono](https://github.com/xamarin/xamarin-macios/issues/7080#issuecomment-609945804). See issue [#2556](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2556).  
**PKeyAuth challenge is now correctly performed on .NET Core, .NET 5, and .NET Standard platforms**. See issue [#2363](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2363).  
**WebView2 embedded browser now works in an app that executes in a protected directory**. See issue [#2502](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2502).  
**MSAL.NET now redirects standard and error output streams when starting a system browser on Linux**. See issue [#2427](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2427).  
**Correct OS version is now sent in `x-client-os` header on .NET classic**. See issue [#2517](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2517).  
**Fixed a `NotImplementedException` when setting a `ConnectionLeaseTimeout` on Unity**. See issue [#2537](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2537).  
**Improved the error message when an app is unable to listen to system browser on localhost URL for interactive flow**. See issue [#2219](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2219).  
**MSAL.NET will now fall back to WebView1 if WebView2 is unavailable** on .NET 5, .NET Core, and .NET classic. See issue [#2495](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2495).  
**MSAL.NET now, by default, enables a partitioned token serialization cache for client credential flow** to improve performance. See issue [#2544](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2544).  
**MSAL.NET now validates the domain of a regionalized authority** to enhance usability. See issue [#2514](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2514).  

### Fundamentals:
**Symbols are now published to //symweb**. See issue [#2497](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2497).  

4.29.0
==========
### New Features:
**Added support for calling On-Behalf-Of flow for Service Principals**. See issue [#1845](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1845).  
**MSAL.NET now supports `Prompt.Create`, which is needed for the self-service sign-up experience with External Identities**. See issue [#2463](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2463) and [documentation](https://aka.ms/msal-net-prompt-create) and learn more [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-interactively#withprompt) about the different ways to control the user interaction.  
**MSAL.NET now suggests the correct redirect URI to use, if WAM was used with an incorrect URI**. See issue [#2358](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2358).  

### Enhancements:
**Redesigned support for calling regional token services** to increase resilience and API simplicity. See issue [#2508](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2508).  

### Bug Fixes:
**Custom Tabs now work correctly in Android 29+**. See issue [#2418](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2418).  

4.28.1
==========
### Bug Fixes:
**MSAL.NET now honors the `shouldClearExistingCache` when deserializing a null or empty blob**. See issues [#2490](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2490) and [#2216](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2216).  

4.28.0
==========
### New Features:
**Updated token cache related telemetry**. See issue [#2406](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2406) for details.  
**Added support for WebView2**. See issue [#1398](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1398) and [WebView2 wiki](https://aka.ms/msal-net-webview2.) for details.  
**Added the ability to set a window title of a WebView2 window**. See issue [#2397](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2397) for details.  
**Added support for specifying a custom fixed version of WebView2 runtime**. See issue [#2446](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2446) for details.  
**Added helper methods for desktop apps**. See issue [#2459](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2459) for details.  
**Added `refresh_in` logic to On-Behalf-Of flows** as was the other flows, to improve resilence. See issue [#2389](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2389) for details.  
**Stopped using reflection to deserialize JSON** to improve Unity apps built for UWP. See issue [#2343](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2343) and [Troubleshooting Unity](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Troubleshooting-Unity) for details.  

### Enhancements:
**Added additional logging when the cache is not serialized in confidential client apps** to help choosing the right token cache serialization. See issue [#2461](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2461) and [Token cache serialization](https://aka.ms/msal-net-cca-token-cache-serialization) for details.  
**`GetAccountsAsync()` is now obsolete in confidential client apps** as confidential client applications need to have one cache per account. Use `GetAccountAsync(string)`. See issue [#1967](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1967) for details.  

### Bug Fixes:
**Fixed `System.InvalidOperationException` when calling `GetAccountAsync` in a Xamarin Android app**. See issue [#2434](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2434) for details.  
**Fixed a bug when a WAM account picker window was not correctly parented to windows**. See issue [#2469](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2469) for details.  
**Fixed the behavior of `WithAuthority(string)` to correctly parse an authority string**. See [#2412](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2412) for details.  
**Improved .NET 5 support for older versions of Windows**. See issue [#2445](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2445) for details.  
**Added MSAL.NET assembly to `rd.xml` to enable MSAL.NET to work in optimized UWP apps**. See issue [#1617](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1617) for details.  

### Fundamentals:
**Added additional code analyzers**. See issue [#2419](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2419) for details.  
**Improved documentation to support Android 11**. See [Xamarin Android 11](https://learn.microsoft.com/entra/identity-platform/msal-net-xamarin-android-considerations#android-11-support) docs.

4.27.0
==========
### New Features: 
**Updated communication mechanism used in brokered authentication on Android to improve reliability and avoid power optimization issues**. See issue [#2150](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2150).

4.26.0
==========
### New Features: 
**MSAL.NET now has support for MSA-passthrough with WAM**, See issue [#2126](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2126).
**MSAL.NET now logs telemetry for the cache refresh status**, See issue [#2356](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2356).
**MSAL.NET now uses ClientID+tenantID instead of just ClientId in the computation of the `SuggestedCacheKey` for `AcquireTokenForClient` (client credentials). This helps keeping the cache smaller in multi-tenant confidential client applications.**, See issue[#2381](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2381).

### Bug Fixes:
**Fixed `ArgumentNullException` and improved resiliency when using `RSACryptoServiceProvider` on NetCore and NetStandard** See issues [#2342](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2342).
**Removed "Unresolved P/Invoke" warning from UWP**, See issue [#2367](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2367).
**Fixed issue where PKEY auth would fail if `WithExtraQueryParams` were used**, See issue [#2359](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2359).

4.25.0
==========
### New Features: 
**MSAL.NET now advertises PKAuth support only on supported platforms**. See issues [#1849](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1849), [#2302](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2302) for details.  
**Added support for embedded view  for .NET 5.0 projects**. See issue [#2310](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2310) for details.  
**Improved handling of broker's power optimization exception in Xamarin Android**. See issue [#2144](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2144) for details.  
**Added an ability to disable legacy ADAL cache** with `WithLegacyCacheCompatibility(false)`. See issue [#1770](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1770) for details.  
**`WithClientAssertion` allows specifying a delegate to set the assertions**. See issue [#2184](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2184) for details.  

### Bug Fixes:
**Account is not longer deleted from the MSAL cache when a `bad_token` response is received from the authentication server**. See issue [#2294](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2294) for details.  
**Minimum UWP target supported is now 10.0.0.0**. See issue [#2330](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2330) for details.  
**Fixed `ArgumentNullException` and improved resiliency when using `RSACryptoServiceProvider`**. See issue [#2189](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2189) for details.  
**Honoring a provided localhost redirect URI in `WithRedirectUri`**. See issue [#2167](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2167) for details.  

### Experimental Features:
**`WithPreferredAzureRegion` allows specifying an option to fallback to global endpoint if the region lookup fails**. See issue [#2287](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2287) for details.  
**`WithPreferredAzureRegion` allows specifying a region to use**. See issue [#2259](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2259) for details.  
**Optimized IMDS calling logic during regional lookup**. See issue [#2177](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2177) for details.  
**WAM is enabled on net5.0-windows10.0.17763.0**. See issue [#2274](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2274) for details.  

### Experimental Features (Bug Fixes):
**Regional lookup now correctly uses a global endpoint when `WithPreferredAzureRegion` is set to `false` after the initial lookup was done with a regional endpoint**. See issue [#2260](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2260) for details.  
**WAM can now be used in console apps**. See issue [#2196](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2196) for details.  
**WAM support is moved to a separate package, `Microsoft.Identity.Client.Desktop`**, which fixes dependency issues during build. See issues [#2299](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2299), [#2300](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2300), [#2247](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2247) for details.  
**In WAM, account picker is now correctly not used when `AcquireTokenInteractive` is called with the default OS account**. See issue [#2246](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2246) for details.  

### Fundamentals:
**Added cache compatibility tests for MSAL.Node**. See issue [#2158](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2158) for details.  

4.24.0
============

Fundamentals:
**`AcquireTokenForClient` and `AcquireTokenSilent` have improved performance, especially for large token caches**. See issue [#2204](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2204) for details.
**TokenCache notifications are not fired when the TokenCache is not serialized by developers, improving performance of all APIs utilizing the token cache**. 
**MSAL .NET now logs to telemetry if the token cache is serialized**. See issue [#2185](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2185) for details.
**Cleaner IntelliSense**. See issue [#2263](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2263) for details.

Bug Fixes:
**MSAL .NET will not force the user to enter their credentials when logging-in with WAM**. See issue [#2233](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2233) for details. 
**MSAL .NET now throws an actionable error message when ROPC is attempted with MSA accounts**. See issue [#2169](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2169) for details.
**MSAL .NET now supports `WithForceRefresh` as part of the `AcquireTokenOnBehalfOfParameterBuilder`**. See issue [#2232](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2232) for details.
**Fix `PlatformNotSupportedException` in MacOS**. See issue [#2251](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2251) for details.

4.23.0
============

New Features:
**MSAL .NET no longer includes the ref assemblies, which are unsupported by older tools and custom build systems**. See issue [#2100](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2100) for details.

Experimental Features:
https://aka.ms/msal-net-experimental-features
**Windows Account Manager (WAM) is now available on .NET classic**. See issue [#2181](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2181) for details and [#2182](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2182), which includes a new static `OperatingSystemAccount` property on the `PublicClientApplication` to use the user signed-in on the Windows machine. More information here: https://aka.ms/msal-net-wam.
**Proof of Possession (Signed HTTP Request) for confidential clients now support key management**. See issue [#2013](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2013) and https://aka.ms/msal-net-pop for details.
**Proof of Possession (Signed HTTP Request) has been removed for public clients**.
**MSAL .NET includes a fallback in case calling the local instance metadata service fails due to an unsupported version**. See issue [#2055](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2055) for details.
**MSAL .NET now sends the source of region discovery in the telemetry**. See issue [#2166](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2166) for details.

Bug Fixes:
**MSAL .NET now sends no prompt value by default when doing interactive login with iOS and Android brokers**. See issue [#2133](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2133) for details.
**MSAL .NET now includes more logging around Android broker to assist with troubleshooting**. The new log information is available as PII logs. See issue [#2151](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2151) for details.
**Due to changes with the v3 B2C responses, MSAL was crashing due to a new unexpected error code format**. MSAL .NET now sanitizes the error codes for HTTP header transport. See issue [#1881](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1881) for details. 
**MSAL .NET now throws `MsalUiRequiredException` for more error codes coming from the Android broker**. See issue [#2140](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2140) for details.
**On iOS, the SSO extension makes background requests, and the NSUrlConnection HttpClient cancels requests when the app moves to the background**. MSAL .NET now sets the `BypassBackgroundSessionCheck` to false. See issue [#2164](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2164) for details.
**Fix badly named header on WsTrust**. See issue [#2193](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2193) for details.

4.22.0
============

New Features: 
**Enable WAM integration for UWP apps** experimentally. To provide feedback, please open an issue. For details see https://aka.ms/msal-net-wam.

4.21.1
============

Bug Fixes: 
**Fix the URI for IMDS call to detect the region**. This fixes the typo in the URI for local IMDS call which is made to detect the region for regional auth.

4.21.0
============

Bug Fixes: 
**Add new constructors for AuthenticationResult for backwards compatibility purposes**. This fixes an API breaking change introduced in MSAL 4.17 where a new param was added to the AuthenticationResult constructor without a default value and swapping the last two parameters. This fix ensures compatibility both with MSAL 4.16.x and before, and with MSAL 4.17 until 4.20.1.

4.20.1
============
Bug Fixes: 
**Fixes the incompatibility of MSAL.NET 4.20 with .NET 5.0 by temporarily removing the WAM experimental support (for the moment, please use 4.20 if you are interested in WAM)**. For details see [#2095](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2095).

4.20
============
New Features: 
**MSAL now expose the configured certificate on Confidential Client Application**. This helps manage multiple instances of Confidential Client Application.
**Experimental WAM integration on Windows for .NET classic, .NET core and UWP**. See https://aka.ms/msal-net-wam.

Bug Fixes: 
**Fix AcquireTokenByIntegratedWindowsAuthentication on .NET core**. Reverted the HTTP client used on .NET core as it was not possible to use default authentication which is needed for WS-trust. See issue [#1988](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1988).
**MSAL correctly returns errors when using Android broker**. See issue [#2062](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2062).
**Fix 2 problems with returning the status codes and exceptions when using Android broker**. See issues [#2062](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2062) and [#2078](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2078).
**Throw a better error when some Facebook accounts cause MSAL to throw a state mismatch exception**. See issue [1872](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1872) for details.
**MSAL now  migrates the ADAL cache for multi-tenant scenarios**. See issue [#2090](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2090).

4.19
============
New Features:
**MSAL now adds telemetry data for the detected region**. See issue [#2018](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2018).

Bug Fixes:
**The creation of HTTPClient is now threadsafe**. This will prevent threading issues MSAL.NET's HttpClient. See issue [2034](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2034).
**MSAL will now add missing data from broker communication issues**, this will allow us to more easily diagnose broker authentication issues on Android. See issue [2045](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2045).
**MSAL now properly bypasses device auth challenges on mobile**. This will allow users to bypass challenges when the web client cannot handle client TLS. [See issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2052).

4.18.0
============
New Features:
**MsalServiceException now allows for the setting of the headers, the response body and the correlation id**. This allows developers to more easily mock the MsalServiceException. See issue [#1977](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1977) for details.
**MSAL now supports regionalization to keep traffic inside a geographical area**. See issue [#1956](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1956) for details.
**MSAL now supports Proof of Possession (POP) on confidential client applications**. See issue [#1946](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1946) for details.

Bug Fixes:
**When the parsing of a WsTrust error fails, MSAL will now return the entire body**. This allows for a better understanding of the error. See issue [#1984](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1984) for details.
**When creating an HttpClient we were forcing a ServicePointManager.DefaultConnectionLimit = 30**. The fix removes the setting of the connection limit (though the max limit in the config setting is updated to 50 connections it's not set for net desktop and net core. See issue [#1992](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1992).
**MSAL.NET now hides the Sign In Title bar in embedded webview sign-in on Android**. See issue [#2014](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2014) and [#1927](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1927).

4.17.1
============

Bug Fixes:

**ID token related information is no longer lost in the second call of AcquireTokenOnBehalfOf**. [Issue #1950 for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1950).
**AcquireTokenOnBehalfOf now respects the WithAuthority modifer enabling multi-tenant resources access**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1965).

4.17.0
============

New Features:

**New enum TokenSource indicates the source of a token** (cache, identity provider or broker). [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1728)

Bug Fixes:

**Fix for CryptographicException when using CNG certificate**. Added support for .net classic 4.6.1. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1726).
**Fix for ArgumentNullException thrown by MsalExtensionException constructor**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1947).
**Reduce response time for GetDeviceId**. MSAL now will disable MATS telemetry if it is not configured. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1912).
**Fix for System.Net.HttpListenerException when system browser flow is cancelled**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1773).

4.16.1
============

Bug Fixes:

**Improved error message for embedded webview http redirect failure**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1264)
**SuggestedCacheKey property in TokenCacheNotificationArgs** now works correctly in the case of AcquireTokenByAuthorizationCode. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1902).

4.16
============

New Features:

**MSAL exposes a SuggestedCacheKey property in TokenCacheNotificationArgs**. This property will help determine the token cache location in web site / web api / daemon app scenarios, making it easier to adapt MSAL's token cache to a general purpose distributed cache. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1902).

**MSAL hardnes its HTTP stack to prevent port exhaustion**. Before this improvement, MSAL would use an `HttpClient` object for each request. In high scale scenarios, this can lead to port exhaustion, as disposing of `HttpClient` does not release ports. With this improvement, MSAL uses a static `HttpClient`, which prevents port exhaustion, combined with platform specific techniques to respect DNS changes. This change affects .net classic and .net core implementations. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1756).

Bug Fixes:

**MSAL no longer misses the cache when an empty scope is requested**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1909).

4.15
============
New Features:

**MSAL has been upgraded to use Android X**. MSAL.NET will now use the latest Android SDKs for it's Xamarin.Android platform. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1653).

**`GetAccounts()` can now filter by user flow for B2C accounts**. MSAL's `GetAccounts()` api will now allow you to pass in a user flow to filter B2C accounts when quering the cache. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1889).

Bug Fixes:

**MSAL can now migrate from ADALV3 to MSALV3 when multiple resourceId's are used**. MSAL will now  ignore ADAL resource strings when fetching RT to enable migration from ADALV3 to MSALV3 cachetokens. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1815).

**MSAL will now maintain the correlation ID of the authentication request with broker specific interactions throughout it's entire execution**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1805).

**MSAL will now return the correct value for `ExpiresOn` in the authentication result during brokered authentication**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1806).

**MSAL now has improved logic for `AcquireTokenSilent()`, `GetAccounts()` and `RemoveAccount()` during brokered authentication**. During brokered authentication, MSAL will now check its local cache for tokens first before sending the silent authentication request to broker. `GetAccounts()` will now merge the accounts from the local MSAL and broker caches when returning results. `RemoveAccount()` will now remove the account from both the local cache and the broker cache. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1820).

**MSAL now has better error reporting during Integrated Windows Authentication**. MSAL will now return the error in the body on WsTrust parse errors. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1838).

**MSAL will now handle null intents returned to `SetAuthenticationContinuationEventArgs`**. MSAL will now handle null intents returned to `SetAuthenticationContinuationEventArgs` to avoid throwing null reference exceptions. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/636).

4.14.0
============
New Features: 

**MSAL no longer calls the OIDC metadata endpoint, as it can infer the authorization and token URLs based on the authority URL. This will speed up token acquisition, especially for multi-tenant applications, as fewer network calls will be made. For details see [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1750).

**Client throttling extended support** MSAL will throttle all /token calls during an event in which the server sends a Retry-After header, thus ensuring the Retry-After instruction is observed. MSAL will also throttle server requests that result in `MsalUiRequiredException` being thrown, for example when the user is required to perform MFA but the app keeps trying to acquire a token silently. For details see [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1624).

Bug Fixes:

**MSAL .NET now respects the ValidateAuthority=false flag**. See [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1791).

**When the Android broker (Authenticator / Company Portal) is configured but it is not installed, MSAL should revert to using its own cache to try to perform the AcquireTokenSilent call**. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1809).

4.13.0
============
New Features: 

**Client throttling is supported in Public Client Applications**. MSAL will now implement client side throttling to reduce excessive authentication requests sent to the service: In the case where the Azure AD service replies with an HTTP error implying throttling, MSAL.NET now respects itself the delay imposed by the service by throwing an exception telling the application after which delay/when it will be able to acquire a token again without even attempting to call the service. For details see [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1624).

**MSAL now can perform device authentication on Desktop**. On Operating systems prior to Windows 10 (Windows 7, 8, 8.1 and their server conterparts) MSAL.NET is able to perform device authentication using PKey Authentication. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1543).

Bug Fixes:

**MSAL .NET would throw a null ref when no authentication type was specified when creating a confidential client application**. MSAL .NET now verifies the developer has specified one client credential (client secret, certificate, or client assertion) when using a confidential client application. See [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1795).

**GetAccountsAsync() used to return 0 accounts when the broker was not installed** (on Xamarin.Android). MSAL will now return accounts from the local MSAL cache when the broker is not installed and WithBroker(trus) is used. [Issue for details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1775).

4.12.0
============
New Features: 

**Integrated Windows Auth available on .NET Core on Windows without username**. On .NET Core, for the Windows platforms, AcquireTokenByIntegratedWindowsAuthAsync(scopes) works without passing the username.

**The scope parameter is now less strict in some of the AcquireTokenXXX methods**. MSAL now allows developers to call AcquireToken* methods without scopes. MSAL continues to ask for "offline_access", "profile" and "openid" scopes, which makes token providers (AAD B2B, AAD B2C, ADFS) return Id Tokens, which contain user metadata. Some token providers continue to issue access tokens, which can be used to access the UserInfo metadata endpoint. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/715).

Bug fixes:

**Fix potential cache consistency issues in multi-threaded environment**. Synchronize token cache to avoid cache inconsistency where token cache is shared with many environments. 

**Fix null reference exception thrown by AcquireTokenForClient when using a cert in .cer format / without a private key**. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1719).

**Fix the spelling in API WithInstanceDicoveryMetadata**. Marked the WithInstanceDicovery as deprecated and added WithInstanceDiscoveryMetadata to fix the spelling.

**Fix MsalClientException UserMismatchSaveToken sometimes thrown in web apps**. Fix the scenario where in web app / web api scenarios where a token cache was shared across multiple users, MSAL would sometimes throw an MsalClientException.


4.11.0
============
New Features: 

**MSAL.NET will now remove accounts from the cache that have expired refresh tokens**. MSAL.NET will remove both the refresh token and the associated account if the `suberror` is "bad_token" to avaoid unnecessary calls to AzureAD. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1720).

**MSAL.NET uses telemetry schema V2** MSAL.NET has been updated to use HTTP telemetry schema V2. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1681).

Bug Fixes:

**When migrating a Xamarin application from ADAL.NET to MSAL.NET and preserving the keychain, a CryptographicException can be thrown from the BrokerKeyHelper.** MSAL.NET now does the broker key keychain look up by Service and Account only. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1628).

**WithProofOfPossession produces a token of type POP when it is expected to be PoP**. MSAL.NET will now produce a token of type PoP when WithProofOfPossession() is used. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1711).


4.10.0
============
New Features:

**MSAL.NET now allows configuration of instance metadata end-point**. WithInstanceDicoveryMetadata method now allows developers to pass an Uri with metadata. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1603).

Bug fixes:

**Client Credentials flow not working with ADFS 2019**. MSAL.NET now uses the token endpoint as audience and adds x5t to the signed assertion it creates from a certificate. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1708).
**Certain error messages are not returned from the Android Broker**. MSAL.NET now throws better exceptions that show the root cause of Android broker failures. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1696).
**WithProofOfPossesion not exposed on AcquireTokenSilent builder**. MSAL.NET now exposes the WithProofOfPossesion call on AcquireTokenSilent [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1715).

4.9.0
============
New Features: 

**Added support for Android Broker to MSAL.NET**. MSAL.NET will now be able to take advantage of the brokered authentication scenarios using the Microsoft Authenticator and the Intune Company Portal. Learn how to levereage the broker [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Leveraging-the-broker-on-iOS-and-Android#brokered-authentication-for-android). See [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1402).

**Added client capabilities support to MSAL.NET**. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1545).

Bug Fixes:
**Wrong Authority created in CreateAuthorityForRequest**. MSAL.NET now properly configures the authority when set from acquire Token apis and is not set on the application. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1606).

4.8.2
============

Bug Fixes:
**When using `.WithBroker(true)`, but no broker is installed on the device, MSAL.NET would throw a null ref**. MSAL.NET now checks if the user is required to have their device managed, and if not, the user will be guided through the regular authentication process with no broker. If device mangagement is required, the user will be guided to the App Store to install the Authenticator App. [See more details in the issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1598).
**Starting with version 4.8.1, MSAL.NET would throw a MonoTouchException on iOS 10 and 11 devices**. Starting with iOS 13 , all WKWebViews report their full page user agent as desktop, previously this was reported as mobile to the server. A check was added in 4.8.1 to switch to use macOS user-agent for all browsers by default. Now, for devices lower than iOS 13, this check will not occur. [See issue for more details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1621).

4.8.1
============

**Fix a Null Reference bug in the main AcquireTokenInteractive scenario on Android.** This is the reason why release 4.8.0 was unlisted from NuGet -[Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1585).

**Change the internal serialization library logic to prevent Mono errors with DataContract serializers** [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1586).

4.8.0
============
Bug fix:
**Cannot acquire token in UWP app on HoloLens via a unity plugin as json serialization fails**. Serialization now works properly in MSAL.NET. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1468).

**AAD Security question registration page unresponsive in Android embedded webview**. MSAL.NET now properly handles the andoid activity when using the embedded webview. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1532).

**"offline_access" scope causes token cache misses**. MSAL.NET now properly filters the cache during silent authentication. [Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1548).

**Improved invalid client error message**. MSAL.NET now has a better error message when an invalid client error is sent back from AAD. [Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1576).

New Features:
**MSAL.NET now supports Proof of Possession**. The PublicClientApplication on every target has support for this. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1488).

**Token cache serialization for Mac**. MSAL.NET is now able to serialize and deserialize the token cache during authentication on MAC OS. [Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1487).

**MSAL.NET now uses "mobile" configuration for iOS Xamarin embedded webview**. MSAL.NET now properly uses the WKWebview on iPad when using the embedded webview. [Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1552).

4.7.1.
============
Bug fix:
**Interactive auth with Edge system browser sometimes hanged. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1506)

4.7.0
============
New Features:
**Added Subject Name + Issuer authentication to the acquire token by authorization code and acquire token by refresh token flows with the WithSendX5C() api on the confidential client application.** All confidential client authentication flows will now have access to this feature. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1490).

4.6.0
============
New Features:
**MSAL .NET now stores the application token returned from the iOS broker (Authenticator)**. This may result in the user experiencing less prompts. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1396).
**New TokenCacheNotificationArgs.IsApplicationCache property simplifies the development of token cache serialization**. TokenCacheNotificationArgs now include a flag named `IsApplicationCache`, which disambiguates between the app token cache and the user token cache. 

Bug Fixes:
- **Device Code Flow would fail with a misleading error message if the app was misconfigured in the Azure Application Portal**. MSAL.NET now provides a better error message. - #1407
- **Setting a non tenanted authority when calling AcquireTokenXX is now ignored**. #1456 
- **Setting an authority audience of `AzureADMyOrg` and a tenant ID would fail**. It's now possible to specify `.WithAuthority(audience)` and `.WithTenantId()` #1320 
 
Fundamentals:
- Added tests which check cache format interoperability between MSAL Java and MSAL .NET.

See the [MSAL .NET 4.6.0 blog post](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.6) for more details.

4.5.1
=============
Bug Fix:
- **Starting in v4.5.0 of MSAL.NET, when using Xamarin Android, a System.TypeInitializationException would be thrown**. This is due to the Resource.designer.cs class being included automatically by the MSBuildExtrasSdk. See [MSAL.NET issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1449) and [Xamarin Android issue](https://github.com/xamarin/xamarin-android/issues/3812) for details.

4.5.0
=============
New Features: 
**MSAL now supports the device code grant for ADFS 2019**. [#1403](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1403)
**MSAL now supports the device code grant for Microsoft personal accounts**. [#1367](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1367)
**MSAL.NET now sends telemetry data to the /token endpoint in regards to the error code of the previous request, if applicable**. This will enable MSAL.NET to determine reliablity across public client application calls.

Bug Fixes: 
- **Customers reported a nonce mismatch error when signing in with the Authenticator app on iOS 13**. The issue has been resolved and increased logging included in the iOS broker scenario. See [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1421) for more details.
- **On iOS 13, when using the system browser, authentication was broken**. This was because Apple now requires a presentationContext when signing in with the system browser. More information on this requirement [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/iOS-13-issue-with-system-browser-on-MSAL-.NET). And more details in the [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1399).
- **At times, MSAL.NET would randomly fail on UWP.** MSAL.NET now implements retry logic and has improved logging around the cache in UWP. See this [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1098) and this [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1064) for more details.
- **During a client credential flow, MSAL.NET would throw a client exception stating the users should not add their own reserved scopes.** MSAL.NET now merges the scopes if they are already in the reserved list and does not throw. See [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1422) for more details.
- **At times, during an interactive authentication, MSAL.NET would throw an ArgumentNullException**. MSAL.NET now checks for null values when handling the authorization result parsing. See [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1418) for details. 

Fundamentals:
- **MSAL.NET now uses the new internal Lab API for automated and manual testing**. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1375).

4.4.0
=============
Bug Fixes:
- **Ensures that MSAL.NET works fine with brokers on iOS 13**. On iOS 13, iOS, the broker, may or may not return the source application, which is used by MSAL.NET to verify the response is coming from broker. To maintain secure calls, MSAL.NET will now also create a nonce to send in the broker request and will verify the same nonce is returned in the broker response in the case of a missing source application. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1357)
- **After MSAL.NET acquired a token for a user, and the user signed-out - remove account, MSAL.NET was attempting to acquire the token with the same tenant as the first account, instead of using the tenant specified in the authority when building the application**. MSAL.NET now uses the specified tenant. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1365)
- **Claims are now sent to both the /authorize and /token endpoints**. [Issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1328)
- **MSAL.NET on Xamarin iOS now returns the top-level view controller, which allows calling AcquireAuthorizationAsync() with an app RootViewController as a UINavigationController with an empty navigation stack**. [See PR for more details](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1378)

Experimental:
- **MSAL.NET now provides two extension methods, enabling you to acquire an SSH certificate**.

4.3.1
=============
Bug Fixes:
- **.WithCertificate with /common audience scenario was broken**. Confidential Client authorization flow and OBO were not able to use certificates with the common authority set. More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/891)
- *MSAL.NET no longer strips the port from the authority URI**. When passing your own authority uri which includes a port, MSAL used to strip out the port from the URI, making the authority unreachable. More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1292)
- **Fixed a crash on Android when Chrome isn't installed on the device**. Exception was NameNotFoundException: com.android.chrome. More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1334)
- **ConfidentialClient built from options didn't allow certificates**. When building a confidential client from options, MSAL was forcing developers to use a secret. More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1332)
- **Login screen loses information on device orientation change on Android**. Username used to be lost from embedded webview when rotating the device. More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1244)

4.3.0
=============
New Features: 
- **Broker support for Xamarin iOS**. MSAL.NET now supports brokered authentication with Xamarin iOS. For details see https:aka.ms/msal-net-brokers, along with code snippets, and more details in the [4.3 release blog post](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.3#broker-support-on-xamarinios). For help migrating from ADAL.NET using iOS broker to MSAL.NET using iOS broker, see [this page on migration](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/How-to-migrate-from-using-iOS-Broker-on-ADAL.NET-to-MSAL.NET).

Bug Fixes:
- **MSAL.NET was adding an extra `/` to the authority when using `.WithAuthority(AzureCloudInstance azureCloudInstance, Guid tenantId)`**. This resulted in an MsalServiceException: "AADSTAT9002: Tenant `v2.0` not found..." More details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1270).
- **Starting in MSAL.NET 4.0, a MsalClientException was thrown instead of a MsalServiceException in exceptions coming from the server**. Details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1276).
- **MSAL.NET required custom error handling when dealing with a network down error**. This was especially problematic on Xamarin iOS and Android. Details [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/592).
- **MSAL.NET was not correctly catching a network down exception**. MSAL.NET now catches the exception and sets it on the correct TaskCompletionSource object. More information [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1304).

4.2.1
=============
Bug Fixes:

- **Fixed API availability of WithParentActivityOrWindow on ios/android/windows/mac**.  See [this item](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1095)
- **Fixed System browser not on by default in iOS and Android**.  See [this item](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1285)

4.2.0
=============
New Features:
- **Allow users to specify their own instance metadata**.  For details see https://aka.ms/msal-net-custom-instance-metadata [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.2#improved-application-startup-cost-disconnected-scenarios-and-advanced-scenarios)
- **AcquireTokenSilent should not make calls to the network** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.2#cache-is-accessed-less-frequently)
- **Improve CA Error Handling** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1148)
- **AcquireTokenSilent access the cache too many times** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.2#improved-application-startup-cost-disconnected-scenarios-and-advanced-scenarios)
- **Allow injecting the Parent Activity/Window in the Client Builder** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.2#improved-api-on-xamarin)
- **Add framework and version to MsalException ToString()** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.2#self-troubleshooting-improvements)

Bug Fixes:
- **Resolved the "Key not valid for use in specified state" error when a certificate with a non-exportable key is used on .NET Framework 4.7.2+** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1201)
- **Cryptic exceptions when attempting IWA / UP / Device Flow with an app that isn't registered as a public client** [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1249)

4.1.0
=============
New Features:
- **MSAL.NET now provides options to control the system web browser**. From MSAL.NET 4.0.0, you have been able to use the interactive token acquisition with .NET Core, by delegating the sign-in and consent part to the system web browser on your machine. MSAL.NET 4.1, brings improvements to this experience by helping you run a specific browser if you wish, and by giving you ways to decide what to display to the user in case of a successful authentication, and in case of failure. [More information about this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.1#improved-experience-with-the-system-web-browser-on-net-core)
- **MSAL.NET now supports ClientAssertions**. In order to prove their identity, confidential client applications exchange a secret with Azure AD. MSAL.NET 4.1 adds a new capabilities for this advanced scenario: in addition to `.WithClientSecret()` and `.WithCertificate()`, it now provides three new methods: `.WithSignedAssertion()`, `.WithClientClaims()` and `.WithClientAdditionalClaims()`. [More information on this feature here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4.1#confidential-client-applications-now-support-client-assertions)

Bug Fixes:
- **When using the `ConfidentialClientApplicationOptions` and including, for example `Instance = "https://login.microsoftonline.com/"`, MSAL.NET was concatenating the double-slash**. MSAL.NET will now check for a trailing slash and remove it. There is no action needed on the part of the developer. See [#1196] for details.
- **When using ADFS 2019, if no login-hint was included in the call, a null ref was thrown**. See [#1214] for details.
- **On iOS, for certain older auth libraries, sharing the cache with MSAL.NET, there was an issue with null handling in json**. The json serializer in MSAL.NET no longer writes values to json for which the values are null, this is especially important for foci_id. See [#1189] and [#1176] for details.
- **When using `.WithCertificate()` and `/common/` as the authority in a confidential client flow, the MSAL.NET was creating the `aud` claim of the client assertion as `"https://login.microsoftonline.com/{tenantid}/v2.0"`**. Now, MSAL.NET will honor both a tenant specific authority and common or organizations when creating the `aud` claim. [#891]
- **MSAL.NET will  make network calls less often when developers call `GetAccountsAsync` and `AcquireTokenSilent`**. AAD maintains an instance discovery endpoint which lists environment aliases for each cloud. In order to optimize SSO, MSAL fetches this list and caches it - MSAL has to make a network call even in simple cases like `GetAccontsAsync`. This improvement bypasses the need for this network call if the environments used are the standard ones. This work is tracked by [MSAL issue 1174](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1174)

4.0.0
=============
New Features:
- **MSAL now supports ADFS 2019**. You can now connect directly to ADFS 2019. This is especially important if you intend to write an app working with Azure Stack. For more details see [ADFS support](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/ADFS-support)
- **MSAL now provides asynchronous callbacks as part of the ITokenCache interface**. See [Asynchronous token cache serialization](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4/_edit#asynchronous-token-cache-serialization) for more information, code snippets, and a link to a sample. [MSAL issue 481](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/481)
- **.NET Core now supports interactive authentication**. Given that .NET Core does not provide a Web browser control, until MSAL.NET 4.0, the interactive token acquisition was not supported. Starting from this release, you can now use AcquireTokenInteractive with MSAL.NET. For more information and code snippets, see [.NET Core now supports interactive auth](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4/_edit#net-core-now-support-interactive-authentication). [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/1142)

Breaking Changes in 4.0.0
- **ITokenCache's responsibility splatted between ITokenCache and ITokenCacheSerializer**. In order to enable the async methods you need to use to subscribe to cache events, we have rewritten the non-async ones by calling the async ones. While doing that we splatted the responsibility of the ITokenCache interface between ITokenCache which now contains the methods to subscribe to the cache serialization events, and a new interface ITokenCacheSerializer which exposes the methods that you need to use in the cache serialization events, in order to serialize/deserialize the cache. This API is experimental and may change in future versions of the library without a major version. See more information on the impact [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4/_edit#itokencaches-responsibility-splatted-between-itokencache-and-itokencacheserializer)
- **Replace TelemetryCallback with TelemetryConfig**. Until MSAL.NET 3.0.8, you could subscribe to telemetry by adding a telemetry callback .WithTelemetry(), and then sending to your telemetry pipeline of choice a list of events (which themselves were dictionaries of name, values). From MSAL.NET 4.0, if you want to add telemetry to your application, you need to create a class implementing ITelemetryConfig. MSAL.NET provides such a class (TraceTelemetryConfig) which does not send telemetry anywhere, but uses System.Trace.TraceInformation to trace the telemetry events. You could take it from there and add trace listeners to send telemetry. See [Telemetry](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/msal-net-4/_edit#breaking-change-replacing-telemetrycallback-by-telemetryconfig) for more information and code snippets.
- **In confidential client applications, MSAL.NET was not returning a URL in the `GetAuthorizationRequestUrl` flow**. MSAL.NET now returns a URL in both overloads of `GetAuthorizationRequestUrl`. [MSAL issues 1193](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1193) and [issue 1184](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1184)

Bug Fixes:
- **In confidential client applications, MSAL.NET now sends the X5C via AcquireTokenSilent,** as it does with AcquireTokenInteractive using the IClientAssertionCertificate overload. Msal [issue 1149](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1149)
- **MSAL.NET now correctly handles the X509 cert on .NET Core**. [MSAL issue 1139](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1139)
- **MSAL.NET now resolves the TeamID in the Keychain Access Group for the default configuration**. Keychain sharing groups should be prefixed with the TeamID. Now, if the developer does not explicitly set the keychain access group through the WithIosKeychainSecurityGroup api, MSAL.NET will use the default "com.microsoft.adalcache", appended with the TeamID. Previously the TeamID was not included.[MSAL issue 1137](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1137)

3.0.8
=============
Bug Fixes:
- **AcquireTokenSilent sometimes ignored the tenant constraint**. If the same user acquired tokens from different tenants, MSAL.NET would return an account, regardless of the tenant. MSAL.NET now returns the token based on the tenant. [MSAL issue #1123](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1123)
- **DeserializeMsalV3 on ITokenCache should have the option to clear the in memory cache**. DeserializeMsalV3 is currently a merge operation with existing in-memory data. MSAL.NET now has the option to be able to clear the in memory state and then deserialize the content in. [MSAL issue #1109](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1109)

3.0.6-preview
=============
New Features:
- **MSAL.NET now creates an HttpClient that uses the AndroidClientHandler** for Android 4.1 and higher. See [documentation for more information](https://learn.microsoft.com/xamarin/android/app-fundamentals/http-stack?tabs=windows). [MSAL issue #1076](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1076)

Bug Fixes:
- **When doing the ADAL.NET fallback from MSAL.NET, MSAL.NET was doing the lookup based on the account.HomeAccountId or requestParameters.LoginHint**. In ADAL.NET an account will never have a HomeAccountId (by design), so lookup needs to happen by Account.UserName instead. [MSAL.NET issue #1100](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1100)
- **AcquireTokenInteractive would throw a PlatformNotSupportException on NetCore when using CustomWebUI**. MSAL.NET no longer throws an exception when using CustomWebUI on NetCore. [MSAL issue #1058](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1058)


3.0.5-preview
=============
Bug Fixes: 
- **Exception: Failure to parse missing json on first login** [MSAL issue #1052](https://github.com/AzureAD/microsoft-authentication-
library-for-dotnet/issues/1052)
- **B2C ROPC support** [MSAL issue #926](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/926)
- **FOCI is hiding the true cause of refresh token failures** [MSAL issue #1067](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1067)


3.0.4-preview
=============
Bug Fixes:
- ** AcquireTokenInteractive parent param is not intuitive** [MSAL issue #918](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/918)

Breaking Changes in 3.0.4-preview
- **AcquireTokenInteractive** now takes a single parameter - the scopes. A new builder method WithParentActivityOrWindow was introduced for passing in a reference to the UI object that spawns the UI (Activity, Window etc.). 

3.0.3-preview
=============
New Features:
- **MSAL now supports custom B2C domains**. [MSAL issue #1025](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1025)
- **MSAL now initializes an HttpClient with NSUrlSessionHnadler()** for iOS 7+. [MSAL issue #1019](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1019)

Breaking Changes in 3.0.3-preview
- **The ClientCredential class is obsolete**. There is no longer a need for the ClientCredential class to be public. This class has been marked as obsolete. [MSAL issue #1007](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1007)
- **The ApiConfig and AppConfig namespaces have been changed** to the Microsoft.Identity.Client namespace for discoverability. This provides a better user experience when updating from MSALv2 to MSALv3.0.3x. [MSAL issue #1006](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1006)]
- **Deprecate UIParent** and move static classes to a more appropriate class (eg `IsSystemWebviewAvailable()`). [MSAL issue #1005](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1005)
- **Move all error codes to `MSAL.Error`**. [MSAL issue #1004](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1004)
- **Deprecate the MSALv2 api**. Move v2 api methods/properties to the migration aid and remove functionality. [MSAL issue #1001](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1001)
- **The `Component` property is obsolete**. MSAL now transmits client app name and version to authorization and token requests. [MSAL issue #978](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/978)

Bug Fixes:
- **Interactive login from multiple clouds was failing** due to instance discovery, as was GetAccounts. This is now fixed. [MSAL issue 1048](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1048) and [1030](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1030)
- **MSAL was calling `DefaultRequestHeaders`** which is not thread safe and could result in AcquireTokenSilent being called from multiple places at the same time. [MSAL issue #1014](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1014)
- **SourceLink is available again** [MSAL issue #953](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/953)

3.0.2-preview
=============
bug fixes:
[UI can hang due to not having proper SynchronizationContext for UI interaction](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1009)

3.0.1-preview
=============
New Features:
- Device Code supports both verification_url and verification_uri
- MsalError contains all the error messages
- MsalException and its derived exception can now be serialized to JSON and deserialized
- MSAL.NET for .NET Core moved to .NET Core 2.1.
- At both the app creation and the token acquisition, you can now pass extra query parameters as a string (in addition to a Dictionary<string,string> introduced in MSAL 3.0.0
- MSAL.NET symbols are now published to enable SourceLink support 

Breaking Changes in 3.0.1-preview
- AcquireTokenSilent has two overrides that require you to pass-in the account or the loginHint
- SubError property removed from MsalServiceException
- merge removed from ITokenCache's DeserializeXX methods
- WithClaims removed from app creation. it is now available on the AcquireToken methods
- ICustomWebUi.AcquireAuthorizationCodeAsync now takes a cancellation Token

bug fixes:
[When the client id entered is invalid, the error messages can be better](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/727)
[PublicClientApplicationBuilder.CreateWithApplicationOptions does not respect the audience](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/969)
[ASWebAuthenticationSession is skipped due to AppCenter build flags](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/919)

3.0.0-preview
=============

- For more info on the release, along with code samples, checkout https://aka.ms/msal-net-3x

Breaking changes in MSAL.NET 3:

- `UIBehavior` was renamed to `Prompt` (breaking change)
- `TokenCacheNotificationArgs` now surfaces an `ITokenCache` instead of a `TokenCache`. This will allow MSAL.NET to provide, in the future, various token cache implementations.
- `TokenCacheExtensions` was removed and its methods moved to `ITokenCache` (this is a binary breaking change, but not a source level breaking change)
- The `Serialize` and `Deserialize` methods on `TokenCacheExtention` (which were serializing/deserializing the cache to the MSAL v2 format) were moved to `ITokenCache` and renamed `SerializeMsaV2` and `DeserializeV2

Changes related to improving app Creation and configuration [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/810)

- New class `ApplicationOptions` helps you build an application, for instance, from a configuration file
- New interface `IMsalHttpClientFactory` to pass-in the HttpClient to use by MSAL.NET to communicate with the endpoints of Microsoft identity platform for developers.
- New classes `PublicClientApplicationBuilder` and `ConfidentialClientApplicationBuilder` propose a fluent API to instantiate respectively classes implementing `IPublicClientApplication` and `IConfidentialClientApplication` including from configuration files, setting the targetted cloud and audience, but also setting per application logging and telemetry, and setting the `HttpClient`.
- New delegates `TelemetryCallback` and `TokenCacheCallback` can be set at application construction
- New enumerations `AadAuthorityAudience` and `AzureCloudInstance` help you writing applications for sovereign clouds, and help you choose the audience for your application.

Changes related to improving token acquisition, addressing issues [810](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/810), [635](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/635), [426](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/426), [799](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/799) :

- `ClientApplicationBase` now implements `IClientApplicationBase` and has new members:
  - `AppConfig` of new type `IAppConfig` contains the configuration of the application
  - `UserTokenCache` of new type `ITokenCache` contains the user token cache (for both public and confidential client applications for all flows, but `AcquireTokenForClient`)
    - New fluent API `AcquireTokenSilent`
- `PublicClientApplication` and `IPublicClientApplication` have four new fluent APIs: `AcquireTokenByIntegratedWindowsAuth`, `AcquireTokenByUsernamePassword`, `AcquireTokenInteractive`, `AcquireTokenWithDeviceCode`.
- `ConfidentialClientApplication` has new members:
  - `AppTokenCache` used by `AcquireTokenForClient`
  - Five new fluent APIs: `AcquireTokenByAuthorizationCode`, `AcquireTokenForClient`, `AcquireTokenOnBehalfOf`, `GetAuthorizationRequestUrl`, `IByRefreshToken.AcquireTokenByRefreshToken`
- New extensibility mechanism to enable public client applications to provide, in a secure way, their own browsing experience to let the user interact with the Microsoft identity platform endpoint (advanced). For this, applications need to implement the `ICustomWebUi` interface and throw `MsalCustomWebUiFailedException` exceptions in case of failure. This can be useful in the case of platforms which don't have yet a Web browser. For instance, the Visual Studio Feedback tool is an Electron application which uses this mechanism. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/863)
- `MsalServiceException` now surfaces two new properties:
  - `CorrelationId` which can be useful when you interact with Microsoft support.
  - `SubError` which indicates more details about why the error happened, including hints on how to communicate with the end user. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/559)

Changes related to the token cache:

- New interface `ITokenCache` contains primitives to serialize and deserialize the token cache and set the delegates to react to cache changes
- New methods `SerializeMsalV3` and `DeserializeMsalV3` on `ITokenCache` serialize/deserialize the token cache to a new layout format compatible with other MSAL libraries on Windows/Linux/MacOS.

A few bug fixes:
- [Update Xamarin dependencies](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/810)
- [Send client headers to the user realm endpoint](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/820)

2.7.1
=============
- **MSAL now handles B2C domains from sovereign clouds, including US Government, Blackforest, and Mooncake**. B2C domains with *.b2clogin.us, *.b2clogin.cn, and *.b2clogin.de are now included in the MSAL allowed domain list for B2C authorities. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/897)
- **Improved error message handling to detect issues faster and not hit null reference exceptions**. Sometimes, for example, when the instance discovery endpoint is not found, the Oauth2Client in MSAL would hit a null reference exception. MSAL now detects such issues faster and returns a more meaningful error message (e.g. the http response code).

2.7.0
=============
- **MSAL integrates SourceLink https://github.com/dotnet/sourcelink.** This allows MSAL to embed pdb files and source code in the NuGet package, allowing users to debug into MSAL without replacing their package reference with a project reference. [MSAL PR](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/750)
- **MSAL.NET now supports Xamarin.Mac**. We now ship another MSAL assembly, that can be used when building apps using Xamarin.Mac. MSAL.NET for Xamarin.Mac supports interactive authentication via an embedded browser, as well as silent authentication. It does not serialize its token cache to the keychain, instead users are asked to provide their own serialization mechanism as they see fit. A keychain based implementation will likely be implemented in a future release. [MSAL PR](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/756)
- **Easier migration from ADALv2 to MSALv2 due to a new AcquireTokenFromRefreshToken API**. ADAL.NET v2.x exposes the refresh token in the `AuthenticationResult`, as well as methods to acquire a token from a refresh token in the `AuthenticationContext`. Through the `ConfidentialClientApplication`, MSAL now implements an explicit interface to help customers migrate from ADAL v2 to MSAL v2. With this method, developers can provide the previously used refresh token along with any scopes. The refresh token will be exchanged for a new one and cached. Please see https://aka.ms/msal-net-migration-adal2-msal2 for more details. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/690)
- **Token cache account was not being deleted on Android platform**. [MSAL PR](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/754)
- **When using ADAL v4.4.2 and MSAL v2.6 in the same Xamarin project, an error would result of `Cannot register two managed types` due to the iOS view controllers being registered under the same name**. Now the MSAL iOS view controllers are prefixed with `MSAL` so they are distinct from the ones in ADAL. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/776)
- **When using the `KeychainSecurityGroup` property to enable application sharing of the token cache, developers were required to include the TeamId**. Now, MSAL resolves the TeamId at runtime. A new property `iOSKeychainSecurityGroup` should be used instead. See https://aka.ms/msal-net-ios-keychain-security-group for details. [MSAL issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/777)

2.6.2
=============
Move AuthenticationContinuationHelper class back to the Microsoft.Identity.Client namespace to avoid breaking changes to existing apps.


2.6.1
=============
- **Setting ForceRefresh = true in AcquireTokenSilent used to skip access token cache lookup** MSAL now handles ForceRefresh=true correctly and circumvents looking up an access token in the cache, instead using the refresh token to acquire a new access token. [MSAL issue #695](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695)
- **Ensured cache lookup filters on the specified tenantId, otherwise the cache lookup would always find the token for the home tenant** This enables MSAL to acquire tokens for resources outside the home tenant. [MSAL issues #694](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/694)

2.6.0-preview
=============
- **For more info on the release, see https://aka.ms/msal-net-2-6 for details**
- **Improved error messages for Integrated Windows Auth**: MSAL now returns better error messages for managed users using Integrated Windows Auth. [ADAL issue #1398](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues/1398)
- **New parameter for UIBehavior**: B2C developers can now use NoPrompt as a UIBehavior. For example, when envoking the edit profile policy to avoid the account selection UI and move directly to the edit profile UI. [MSAL issue #588](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/588)
- **UIParent is available on all platforms**: The UIParent constructor now takes in (object parent, bool useEmbeddedWebview) and is available on all platforms. [MSAL issue #676](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/676)
- **Remove dependency on Newtonsoft.Json**: MSAL now uses Microsoft.Identity.Json [MSAL PR](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1430)
- **Deprecate `HasStateChanged`**: MSAL was not using this flag, so it has been deprecated [ADAL issue #1186](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues/1186)
- **Obsolete public WebUI net45 types from Internal.UI namespace**: [MSAL](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1438)
- **NetStandard Unification**:  MSAL.NET helps you build multi-platform applications more easily by rationalizing the .NET Standard 1.3 platform. For details see blog post about this release available from: https://aka.ms/msal-net-2-6
- **Public namespace change**: If you implement dual serialization (AdalV3/Unified cache), and therefore are using Microsoft.Identity.Core.Cache to access some of the public cache classes, please note the namespace has changed to Microsoft.Identity.Client.Cache. You will get this error when updating packages: The type or namespace name 'Core' does not exist in the namespace 'Microsoft.Identity' (are you missing an assembly reference?). Just replace Core with Client in the using statement.
- **Move MSAL code to the MSAL repo**

2.5.0-preview
=============
- **Improved the testability of apps using MSAL.NET**: MSAL.NET was not easily mockable because the AuthenticationResult was an immutable sealed class with no public constructors. AuthenticationResult now has a public constructor for testing. [MSAL issue #682](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/682)
- **Improved support for Azure AD B2C**: apps constructors now understand to b2clogin.com based authorities, Developer no longer needs to set ValidateAuthority=false, as the library handles this now. [MSAL issue #686](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/686)
- **GetAccountsAsync() can now be called when the device / computer is offline**. It was making an network call to the instance discovery endpoint to determine the environments (equivalent clouds base URLs) for caching, which meant GetAccountsAsync() did not work off-line. This has been fixed and GetAccountsAsync() is not dependent on a network call and works off-line. [MSAL issue #630](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/630)

2.4.1-preview
=============
Hot fix release includes:
- Fix performance issue [1406] for degredation in .NET Framework compared to .NET Core

2.4.0-preview
=============
Improvements and fixes to the token cache
- The serialized token cache can now be shared by different applications, therefore providing SSO if the same user signs-in in both applications
  - See [PR](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1365) and [MSAL Issue #653](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/654)
- On .NET Core, the Token cache was shared by all instances of applications in memory. This is now fixed (See MSAL.NET issue #656 and [PR](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1360))
- Fixes consistency issues for advanced token cache migration scenarios from ADAL v3.x to ADAL v4.x to MSAL v2.x 
  - [MSAL Issue #652](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/652)
  - [MSAL Issue #651](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/651)
- Cache lookups were optimized. Work done in conjunction with ADAL.iOS and MSAL.iOS native) [PR](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1350)

More browsers are now supported on Xamarin.Android when you choose to use system web browsers.
- Removed chrome dependency for system browser on Android devices. See https://aka.ms/msal-net-system-browsers for more information. [MSAL issue #664](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/664)

Azure AD B2C improvement
- Add support for b2clogin.com for b2c authorities [MSAL issue #669](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/669) [#632](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/632)

2.3.1-preview
=============
This release includes:
- Fix for device code flow where server is now expecting device_code as the body parameter.

2.3.0-preview
=============
This release includes:
- Fix for cross-thread exception when setting the ownerWindow [ADAL issue #1277](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet)
- Ensure error codes are public [MSAL issue #638](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/638)
- Add device code flow api to iOS and Android platforms [MSAL issue #642](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/642)

2.2.1-preview
=============
This release contains bug fixes on top of MSAL 2.2.0-preview:
- Due to static initialization, there was a race condition which appeared randomly. [MSAL issue #629](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/629)
- For iOS, TeamId is now accessible when the device is locked. [MSAL issue #626](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/626)
- For iOS, MSAL returns a useful error message, and an [aka.ms link](https://aka.ms/msal-net-enable-keychain-groups), when keychain access groups have not been set in the Entitlements.plist. [MSAL issue #633](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/633)
- Cache serialization for [.NetCore](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/637)
- Improve logging for device code flow to handle "authorization_pending" exceptions as info messages [MSAL issue #631](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/631)

2.2.0-preview
=============
- MSAL.NET 2.2.0 now supports Device Code Flow. For details see https://aka.ms/msal-device-code-flow
- Xamarin.iOS applications using the system web view now benefit from the integration with SFAuthenticationSession for iOS11 and ASWebAuthenticationSession for iOS12+ [MSAL issue 489](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/489)
- A clear MsalClientException message is now returned when the application is not able to access keychain, with instructions. See https://aka.ms/msal-net-enable-keychain-access for details.
- Removed double-logging in log files and callbacks.  https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/1289
- Improved 429 server error handling by exposing the Http Response headers in MsalServiceException. See https://aka.ms/msal-net-retry-after
- UWP cache fix. The key of the storage on UWP should be 255 characters or less. When using several scopes the key could exceed 255 characters. Now hashing scopes and environment on UWP.  [612](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/612)

2.1.0-preview
=============
- Integrated Windows Authentication and Username / Password authentication flows. For details see https://aka.ms/msal-net-iwa and https://aka.ms/msal-net-up

2.0.1-preview
=============
This release contains bug fixes on top of MSAL 2.0.0-preview:
- When using MSAL 2.0.0-preview with Azure AD B2C, the cache was never hit. (See MSAL#[604](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/604)), 
   and the accounts were not removed correctly (See MSAL #[613](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/613))
- The TokenCacheExtensions.Deserialize was throwing if a null array of bytes was passed as arguments instead of silently not doing anything.
   (See MSAL #[603](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/603))
- When migrating a token cache from ADAL v3 or ADAL v4 to MSAL 2.0, the override of acquire token silent without authority used to work incorrectly (cache was missed)

2.0.0-preview
=============
This release contains:
- Remove support for Windows 8/8.1 and Windows phone 8/8.1
- Add support for .NETCore (Netcoreapp1.0 for portable desktop and web apps) and Uap10.0 for Windows 10 Applications
- Define the notion of Account (through the IAccount interface), instead of User. This breaking change provides the right semantics: 
   the fact that the same user can have several accounts, in different Azure AD directories.
- The methods and properties returning IAccount are now all asynchronous, as in some cases getting the information might require querying the identity provider.
- The types that had fields or properties of type IUser in MSAL.NET 1.x now reference IAccount. 
- In the Xamarin.iOS platform, PublicClientApplication has a new property named KeychainSecurityGroup. 
   This Xamarin iOS specific property enables you to direct the application to share the token cache with other applications sharing the same keychain security group. 
   If you provide this key, you must add the capability to your Application Entitlement. For more info, see https://aka.ms/msal-net-sharing-cache-on-ios.  This API may change in a future release.
- In the previous versions of MSAL.NET, Xamarin.Android and Xamarin.iOS used the System web browser interacting with Chrome tabs. 
   This was great if you wanted to benefit from SSO, but that was not working on some Android phones which device manufacturers did not provide Chrome, or if the end user had disabled Chrome. 
   As an app developer, you can now leverage an embedded browser. To support this, the UIParent class now has a constructor taking a Boolean to specify if you want to choose the embedded browser. 
   It also has a static method, IsSystemWebviewAvailable(), to help you decide if you want to use it. 
   For more details about this possibility see the article in MSAL’s conceptual documentation: https://aka.ms/msal-net-uses-web-browser. 
   Also the web view implementation might change in the future
- If migrating from MSAL 1.x to MSAL 2.x, you’ll get a number of compilation errors, but they are pretty straightforward to fix. In most cases you will only need to: 
   - Replace IUser by IAccount 
   - Replace the calls to application.Users to asynchronous calls to application.GetAccountsAsync 
   - In advanced multi-account applications, where you were using the IUser.Identifier, you will now need to use the IAccount.HomeAccount.Identifier. 
   We have provided meaningful and actionable compiler errors that will tell you exactly what to do and will link to documentation to help you migrate. 
- To preserve the single-sign-on (SSO) state, the new versions of ADAL(v4) and MSAL(v2) share the same token cache, are capable of reading the ADAL 3.x token cache and are 
   capable of writing the ADAL 3.x token cache in addition to the new cache format (named unified cache), see https://aka.ms/adal-net-to-msal-net. 
- For more info on the release, checkout https://aka.ms/msal-net-2-released

1.1.4-preview
=============
Hot fix of null pointer in iOS TokenCacheAccessor(#570)

1.1.3-preview
=============
This release contains updates to Xamarin.Android.Support v27.0.2 and MonoAndroid8.1 (#553 #520).

1.1.2-preview
=============
This release fixes references issues for Xamarin Android (for instance #520 & #524).
When you create a new Xamarin Forms project and reference MSAL this now works out of the box. 
If you want to migrate an existing project to MSAL 1.1.2, please read this [wiki] 
(https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Troubleshooting-Xamarin.Android-issues-with-MSAL) page.

1.1.1-preview
=============
This release contains -
- Added support to use RSACng in .net 4.7 (#448)
- Expose claims as an attribute for MSALUiRequiredException (#459)
- Updated Xamarin Forms Android support libraries to 25.3.1 (#450)
- Added Arlington URL to list of trusted authorities (#495)
- Changes for GDPR complicance with PiiLogs (#492)
- Several bug fixes

1.1.0-preview
=============
This release marks the seconds preview of the library which brings in several features and changes - 
- Support for NetStandard
- Client Certificate Assertion in NetCore
- Support for system webviews in iOS/Android
- Updated Object Model
- Updated to JSON cache storage
- Several bug fixes
