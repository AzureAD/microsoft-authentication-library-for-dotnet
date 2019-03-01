# MSAL.NET 3.0.0 released

## Overview

We are excited to announce the release of MSAL.NET 3.0-preview, which has a number of changes that we hope, you'll love. In this article, you'll learn about:

- [Changes in MSAL 3.x](#changes-in-msalnet-3x)
  - [Reference - list of all changes in MSAL.NET 3](#reference---list-of-changes-in-msalnet-3)
  - [Plans for deprecation in MSAL.NET 3.x, MSAL 4.x](#plans-for-deprecation-in-msalnet-3x-and-msalnet-4x)
  - [Configuring an app got simpler](#configuring-an-app-got-simpler)
  - [Acquiring a token also got simpler](#acquiring-a-token-also-got-simpler)
  - [Advanced: You can provide your own web view](#you-can-provide-your-own-web-view)
  - [Breaking changes in MSAL.NET 3.x](#breaking-changes-in-msalnet-3x)
  - [How to maintain SSO with apps written with ADAL v3, ADAL v4, MSAL.NET v2](#how-to-maintain-sso-with-apps-written-with-adal-v3-adal-v4-msalnet-v2)
- [Why MSAL.NET moved from MSAL 2.x to MSAL 3.x](#why-msalnet-moved-from-msal-2x-to-msal-3x)
  - [Reacting to your feedback](#reacting-to-your-feedback)
  - [Unified cache layout format change](#unified-cache-layout-format-change)

Contrary to the previous versions for which the NuGet package had lost its -preview, the NuGet package for this version is 3.0.0-preview as we'd want to have your feedback about our changes, the new API, and the [Plans for deprecation in MSAL.NET 3.x and MSAL.NET 4.x](#plans-for-deprecation-in-msalnet-3x-and-msalnet-4x)

## Changes in MSAL.NET 3.x

The following paragraph explains the exhaustive reference list of changes in MSAL.NET in more detail.

### Reference - list of changes in MSAL.NET 3

Breaking changes in MSAL.NET 3:

- `UIBehavior` was renamed to `Prompt` (breaking change)
- `TokenCacheNotificationArgs` now surfaces an `ITokenCache` instead of a `TokenCache`. This will allow MSAL.NET to provide, in the future, various token cache implementations.
- `TokenCacheExtensions` was removed and its methods moved to `ITokenCache` (this is a binary breaking change, but not a source level breaking change)
- The `Serialize` and `Deserialize` methods on `TokenCacheExtention` (which were serializing/deserializing the cache to the MSAL v2 format) were moved to `ITokenCache` and renamed `SerializeMsaV2` and `DeserializeV2

Changes related to improving app creation and configuration

- New class `ApplicationOptions` helps you build an application, for instance, from a configuration file.
- New interface `IMsalHttpClientFactory` to pass-in the HttpClient to be used by MSAL.NET to communicate with the endpoints of Microsoft identity platform for developers.
- New classes `PublicClientApplicationBuilder` and `ConfidentialClientApplicationBuilder` propose a fluent API to instantiate respectively classes implementing `IPublicClientApplication` and `IConfidentialClientApplication` including from configuration files, setting the targetted cloud and audience, but also setting per application logging and telemetry, and setting the `HttpClient`.
- New delegates `TelemetryCallback` and `TokenCacheCallback` can be set at application construction
- New enumerations `AadAuthorityAudience` and `AzureCloudInstance` help you write applications for sovereign and national clouds, and choose the audience for your application.

Changes related to improving token acquisition:

- `ClientApplicationBase` now implements `IClientApplicationBase` and has new members:
  - `AppConfig` of new type `IAppConfig` contains the configuration of the application
  - `UserTokenCache` of new type `ITokenCache` contains the user token cache (for both public and confidential client applications for all flows, but `AcquireTokenForClient`)
    - New fluent API `AcquireTokenSilent`
- `PublicClientApplication` and `IPublicClientApplication` have four new fluent APIs: `AcquireTokenByIntegratedWindowsAuth`, `AcquireTokenByUsernamePassword`, `AcquireTokenInteractive`, `AcquireTokenWithDeviceCode`.
- `ConfidentialClientApplication` has new members:
  - `AppTokenCache` used by `AcquireTokenForClient`
  - Five new fluent APIs: `AcquireTokenByAuthorizationCode`, `AcquireTokenForClient`, `AcquireTokenOnBehalfOf`, `GetAuthorizationRequestUrl`, `IByRefreshToken.AcquireTokenByRefreshToken`
- New extensibility mechanism to enable public client applications to securly provide their own browsing experience to let the user interact with the Microsoft identity platform endpoint (advanced). For this, applications need to implement the `ICustomWebUi` interface and throw `MsalCustomWebUiFailedException` exceptions in case of failure. This can be useful in the case of platforms which don't yet have a Web browser. For instance, the Visual Studio Feedback tool is an Electron application which uses this mechanism.
- `MsalServiceException` now surfaces two new properties:
  - `CorrelationId` which can be useful when you interact with Microsoft support.
  - `SubError` which indicates more details about why the error happened, including hints on how to communicate with the end user.

Changes related to the token cache:

- New interface `ITokenCache` contains primitives to serialize and deserialize the token cache and set the delegates to react to cache changes
- New methods `SerializeMsalV3` and `DeserializeMsalV3` on `ITokenCache` serialize/deserialize the token cache to a new layout format compatible with other MSAL libraries on Windows/Linux/MacOS.

### Configuring an app got simpler

With MSAL.NET 3.x, we made it much simpler to configure your application. You don't need to choose an override for a constructor. The recommended way is to use the static `PublicClientApplicationBuilder` and `ConfidentiaClientApplicationBuilder` classes and call the `Create()` or `CreateFromOption()` method. They both return a builder, to which you chain optional properties. When you have all your properties, you call `Build()` and that's it!

#### Simple scenarios are simple

If you want to create a desktop application, you can do it with minimal information.

```CSharp
IPublicClientApplication app;
app = PublicClientApplicationBuilder.Create(clientId)
        .Build();
```

By default, this application will target Work and School Accounts and Microsoft personal accounts from the Microsoft Azure public cloud.

Now, let's assume that your application is a line of business application which is only for your organization, then you can write:

```CSharp
IPublicClientApplication app;
app = PublicClientApplicationBuilder.Create(clientId)
        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
        .Build();
```

#### More complex scenarios remain simple

Where it becomes interesting is that programming for national clouds has now become simple: if you want your application to be a multi-tenant application in a national or sovereign cloud, you write could for instance:

```CSharp
IPublicClientApplication app;
app = PublicClientApplicationBuilder.Create(clientId)
        .WithAuthority(AzureCloudInstance.AzureUsGovernment, AadAuthorityAudience.AzureAdMultipleOrgs)
        .Build();
```

Finally, if you are an Azure AD B2C developer, you can specify your tenant like this. Note that you don't need to say that you want to bypass authority validation, which, we heard, was scary.

```CSharp
IPublicClientApplication app;
app = PublicClientApplicationBuilder.Create(clientId)
        .WithB2CAuthority($"https://fabrikamb2c.b2clogin.com/tfp/{tenant}/{PolicySignInSignUp}")
        .Build();
```

Of course there are several overloads of `.WithAuthority` with many different cases. For more information see [Client applications in MSAL 3.x](Client-Applications-in-MSAL-3.x)

#### You can now configure your application more easily through config files

Now, chances are that you probably have the application configuration in a file, rather than in the code. This is also possible as the application builder can create an application from Options. Here is how:

```CSharp
PublicClientApplicationOptions = GetOptions(); // your own method.
var app = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
.Build();
```

You'll find a more complete code sample in [Client applications in MSAL 3.x](Client-Applications-in-MSAL-3.x)

### Acquiring a token also got simpler

Once you have your application, you want to acquire tokens. The same kind of pattern is used where you call:

```CSharp
app.AcquireTokenXXX(scopes, mandatory-parameters)
   .WithOptionalProperty(optional-property).
   .ExecuteAsync();
```

This means you no longer have numerous overrides as you can now choose the parameters you want to set. Finally `ExecuteAsync()` had an override taking a `CancellationToken` argument, making all the `AcquireToken`*XXX* methods cancellable, as requested!

```CSharp
app.AcquireTokenXXX(scopes, mandatory-parameters)
   .WithOptionalProperty(optional-property).
   .ExecuteAsync(cancellationToken);
```

For details, see [Acquiring tokens](Acquiring-Tokens) and [Scenrios](Scenarios) and the links from these articles on the detailed token acquisition methods

### Breaking changes in MSAL.NET 3.x

As mentionned in [Why did MSAL move from 2.x to 3.x?](#why-did-msal-move-from-2x-to-3x), as we were cleaning up the public API, and simplifying it, we introduced a small number of breaking changes which we expect should not affect you too much for most of the scenarios if you use the MSAL 2.x type API.

- We renamed a type (UIBehavior)
- Setting some properties of `IClientApplicationBase` and `ClientApplicationBase` after building the application did not make sense any longer, and was getting in the way of testability
- the application builder now creates the token cache for you, we recommand you to use this mechanism, not instantiate `TokenCache` yourself.

#### UIBehavior renamed to Prompt

The `UIBehavior` class was used until MSAL.NET 2.x to specify, in `AcquireTokenAsync` (interactive) which prompt the application developer wanted to expose to the user. The class was renamed to `Prompt`, as explained in [Naming-could-be-improved](#naming-could-be-improved)

<!--
  commenting as not in yet

#### MSAL.NET 3.x no longer uses SecureString

The recommendation of the .NET team is to avoid using secure strings. They are not that secure (not implemented at the OS level) and are difficult to use in managed code. If you are interested in details, look at this article: [
DE0001: SecureString shouldn't be used](https://github.com/dotnet/platform-compat/blob/master/docs/DE0001.md)

Therefore we changed the signature of `AcquireTokenByUsernamePasswordAsync` so that the password is now a plain string.

```CSharp
AcquireTokenByUsernamePasswordAsync(IEnumerable<string> scopes,
                                    string username,
                                    string password)
```
-->

#### Logger and Telemetry are now set per application, and during its construction

Until MSAL.NET 3.0, you used to set the logger by setting properties on a static class named `Logger`. These properties were, therefore, the same for all the applications in your executable

If you have a method to log information

```CSharp
void MyLogginMethod(LogLevel level, string message, bool containsPii)
{
 Console.WriteLine($"MSAL {level} {containsPii} {message}");
 Console.ResetColor();
}
```

you used to write:

```CSharp
Logger.LogCallback = MyLogginMethod;
Logger.Level = LogLevel.Verbose;
Logger.PiiLoggingEnabled = true;
```

This is no longer possible. You'll need to use the new fluent API if you want to provide logging:

```CSharp
IPublicClientApplication app;
app = PublicClientApplicationBuilder.Create(config.ClientId)
        .WithLogging(MyLogginMethod, LogLevel.Verbose,
                     enablePiiLogging: true,
                     enableDefaultPlatformLogging: true)
        .Buid();
```

For more details see [Logging in MSAL.NET](https://aka.ms/msal-net-logging)

#### Some properties on Applications now need to be set by builders, and read through the configuration

Some properties in the V2.0 API style were settable after the application construction. The V3 builder style makes the application properties immutable. From MSAL.NET 3.x, if you need to use these properties, you now need to:

- Set them when you are building the application (that is with `.With`*Parameter* clauses on the application builder)
- Read them from the application's configuration

Here is the detail. They all are held by:

| Property | Description | Which method of the application builder to user | How to read it?
 --------- | ---------   | ---------   | ---------  
 Component | Identifier of the component (libraries/SDK) consuming MSAL.NET (telemetry) | `.WithComponent(component)` | `app.AppConfig.Component`
SliceParameters | Used for dogfood testing or debugging as a string of segments of the form `key=value` separated by an ampersand character | `.WithExtraQueryParameters` |
ValidateAuthority | No longer needed | `.WithAADAuthority` does not require it, `.WithB2CAuthority` either, `.WithAuthority` still has a Boolean for specific cases |
`RedirectUri`  | URI at which the identity provider will contact back the application with the tokens | Set to defaults, or, for client applications, in the constructor with `.WithRedirectUri` |  app.`AppConfig.RedirectUri`

### How to maintain SSO with apps written with ADAL v3, ADAL v4, MSAL.NET v2

#### Breaking change in the MSAL.NET cache serialization format and API

MSAL 2.x was providing two serialization mechanisms:

- One to serialize/deserialize the cache to the unified cache format (V2). This was done through the `Serialize` and `Deserialize` extension methods on `TokenCacheExtension`.
- The other to serialize/deserialize the cache with both the MSAL V2 formats and the ADAL V3 format at once, therefore, in the case of deserializing, **merging** the information from several serialization files if both were available (`DeserializeUnifiedAndAdalCache` and `SerializeUnifiedAndAdalCache`)

We've taken a breaking change in order to let you:

- benefit out of the box from a “True” Unified Cache serialized json format across libraries on Windows, Linux and MacOS
- upgrade your apps easily from ADAL v3 and ADAL v4 to MSAL v3 with-out loosing SSO (if you need it)
-  easily upgrade your apps from MSAL v2 to MSAL v3 with-out loosing SSO (if you need it)

In MSAL.NET 3.x:, the `ITokenCache` interface is the following:

```CSharp
public interface ITokenCache
{
 void DeserializeMsalV2(byte[] msalV2State, bool merge);   // MSAL V2.0 format (was Deserialize() in MSAL 2.x)
 void DeserializeUnifiedAndAdalCache(CacheData cacheData); // Deprecated: ADAL V3.0 and MSAL V3.0 format
 void DeserializeMsalV3(byte[] bytes, bool merge);         // New: MSAL V3.0 unified cache format
 void DeserializeAdalV3(byte[] bytes, bool merge);         // New ADAL V3.0 format only with merge

 byte[] SerializeMsalV2();                                 // MSAL V2.0 format (was Serialize() in MSAL 2.x)
 CacheData SerializeUnifiedAndAdalCache();                 // Deprecated: ADAL V3.0 and MSAL V3.0 format
 byte[] SerializeMsalV3();                                 // New: MSAL V3.0 unified cache format
 byte[] SerializeAdalV3();                                 // New: for ADAL v3.0 cache format

 void SetAfterAccess(TokenCacheCallback afterAccess);
 void SetBeforeAccess(TokenCacheCallback beforeAccess);
 void SetBeforeWrite(TokenCacheCallback beforeWrite);
}
```

- `CacheData` and its methods are obsolete with an aka.ms link to the explanations (this document)
- Instead of `TokenCacheExtensions.SerializeUnifiedAndAdalCache` we recommend using, `SerializeAdalV3` and `SerializeMsalV3` with an aka.ms link.
- Instead of `TokenCacheExtensions.DeserializeUnifiedAndAdalCache` we recommend using, `DeserializeAdalV3` and `DeserializeMsalV3` with override=false, and an aka.ms link.
- Instead of `TokenCacheExtensions.Serialize` we recommend using `SerializeMsalV3` with an aka.ms link.
- Instead of `TokenCacheExtensions.Deserialize` we recommend using and `DeserializeMsalV3` with an aka.ms link.

#### Support plans for serialization formats

In future versions of MSAL (and ADAL), we'd want to start obsoleting some of the cache serialization format. Here is, for the moment, our proposed support matrix:

|Library/version     | Adal 3.x Format | MSAL 2.x Format | Msal 3.x format |
| :--- | :---: | :---: | :---: |
|ADAL.NET 3.0 | Supported | - | - |
|ADAL.NET 4.0 | Supported | Supported | - |
|ADAL.NET 5.0 | Supported | Deprecate (Error) | Supported |
|ADAL.NET 6.0 | Supported** | Removed | Supported |
| | | | |
|MSAL.NET 2.0*| Supported | Supported | - |
|MSAL.NET 3.0 | Supported | Deprecate (Error) | Supported
|MSAL.NET 4.0 | *| Removed | Supported |
| MSAL.NET 3.0 + 12m* | Deprecate (Error) * |  | Supported |
| MSAL.NET 3.0 + 24m* | Removed * |  | Supported |

**Supported**: means available and Supported

*: We plan on removing v3 support after sometime to make sure our codebase stays as simple as possible. As we ship MSAL 3.0 we mark MSAL v2 and as not supported (same with MSAL v1). We would monitor adoption to understand when the right moment is, to remove support for the v3 cache. Again your feedback would be valuable.

** : Leaving for now but we could consider removing that to ensure that even if you are sticking to ADAL, you'd be using the new format.

##### Example of code showing how to migrate from MSAL.NET v2 to MSAL.NET v3

There are two scenarios depending on the type of app: one time migration and side by side migration:

- **One time migration**. This is the recommended approach for MSAL public client applications which don’t share the cache with other apps or generations of apps, and for confidential client applications that don’t use a distributed cache shared between several web instances: it’s simple (try read V3, if it fails read V2, in any case write V3). This approach is possible as of today with MSAL 3.0.0-preview

  ```CSharp
  /// <summary>
  /// Enables persistence of the token cache to some storage
  /// </summary>
  /// <param name="unifiedCacheStorageKey">Key (for instance file name in the cache of a
  //  file storage) where the cache is serialized with the Unified cache format
  /// </param>
  public static void EnableFilePersistence(ITokenCache userTokenCache,
                                                 string unifiedCacheStorageKey)
  {
   UnifiedCacheStorageKey = unifiedCacheFileName;
   userTokenCache.SetBeforeAccess(BeforeAccessNotification);
   userTokenCache.SetAfterAccess(AfterAccessNotification);
  }

  /// <summary>
  /// File path where the token cache is serialiazed with the unified cache format
  /// </summary>
  public static string UnifiedCacheStorageKey { get; private set; }

  public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
  {
   byte[] serializedData = ReadFromStorageIfExists(UnifiedCacheStorageKey);
   try
   {
    args.TokenCache.DeserializeMsalV3(serializedData, override:true);
   }
   catch (MsalUnexpectedCacheFormatException)
   {
    args.TokenCache.DeserializeMsalV2(serializedData, override:true);
   }
  }

  public static void AfterAccessNotification(TokenCacheNotificationArgs args)
  {
   // if the access operation resulted in a cache update
   if (args.HasStateChanged)
   {
    byte[] serializedCache = args.TokenCache.SerializeMsalV3();
    WriteToStorageIfNotNull(UnifiedCacheStorageKey, serializedCache);
   }
  }
  ```

- For MSAL public client apps that share the cache with other apps or web apps/apis that share the cache between several instances, you'll need to implement a dual MSAL V2 / MSAL V3 for a while until all apps or web instances have migrated, then remove the support for the MSAL V2 cache format. For this we'll provide (in a later version of MSAL 3.x) an API enabling you to deserialize with merge. **This is not implemented yet in MSAL 3.0.0-preview (hence the preview), but our plans are to enable it soon**. For the moment if you try to use the Deserialize APIs with merge=true, MSAL will throw a `NotImplementedException` with a meaningful message

  ```CSharp
  /// <summary>
  /// Enables persistence of the token cache to some storage
  /// </summary>
  /// <param name="msalV2CacheStorageKey">Key (for instance file name in the cache of a
  //  file storage) where the cache is serialized with the Unified cache format of
  /// MSAL 2.x
  /// <param name="msalV3CacheStorageKey">Key (for instance file name in the cache of a
  //  file storage) where the cache is serialized with the Unified cache format of
  /// MSAL 3.x
  /// </param>
  public static void EnableFilePersistence(ITokenCache userTokenCache,
                                           string msalV2CacheStorageKey,
                                           string msalV3CacheStorageKey)
  {
   MsalV2CacheStorageKey = msalV2CacheStorageKey;
   MsalV3CacheStorageKey = msalV3CacheStorageKey;
   userTokenCache.SetBeforeAccess(BeforeAccessNotification);
   userTokenCache.SetAfterAccess(AfterAccessNotification);
  }

  /// <summary>
  /// File path where the token cache is serialiazed with the unified cache format
  /// </summary>
  public static string UnifiedCacheStorageKey { get; private set; }

  public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
  {
   args.TokenCache.DeserializeMsalV2(null);

   byte[] serializedData = ReadFromStorageIfExists(MsalV2CacheStorageKey);
   args.TokenCache.DeserializeMsalV2(serializedData, merge:true);
   serializedData = ReadFromStorageIfExists(MsalV3CacheStorageKey);
   args.TokenCache.DeserializeMsalV3(serializedData, merge:true);
  }

  public static void AfterAccessNotification(TokenCacheNotificationArgs args)
  {
   // if the access operation resulted in a cache update
   if (args.HasStateChanged)
   {
    byte[] serializedCache = args.TokenCache.SerializeMsalV3();
    WriteToStorageIfNotNull(MsalV3CacheStorageKey, serializedCache);
    serializedCache = args.TokenCache.SerializeMsalV2();
    WriteToStorageIfNotNull(MsalV2CacheStorageKey, serializedCache);
   }
  }
  ```

### Example code demonstrating how it will be possible (in MSAL 3.1+) to share SSO across all versions

For the moment, the versions of the API exposing a `merge` parameter are not implemented in MSAL 3.0.0-preview. Here are our plans to help you with updating the SSO state in all of the ways it will be possible to leverage this parameter in the future (soon after 3.0.0-preview)

```CSharp
// Assigning a token cache to the public client (it's similar for Confidential client, though there a cache for the Application is also avaialble):
IPublicClientApplication app = PublicClientApplicationBuilder.Create(ClientId)
 .WithAuthority(Authority)
 .Build();

// Setting up the cache allowing loading and persisting the data
TokenCache usertokenCache = app.UserTokenCache
usertokenCache.SetBeforeAccess(BeforeAccessNotification);
usertokenCache.SetAfterAccess(AfterAccessNotification);

// Event handlers
private void BeforeAccessNotification(TokenCacheNotificationArgs args)
{
 // Load cache state from file
 byte[] adalV3State = FileStorage.ReadFromFileIfExists(AdalV3FileName);
 byte[] msalv2State = FileStorage.ReadFromFileIfExists(MsalV2FileNAme);
 byte[] msalv3State = FileStorage.ReadFromFileIfExists(MsalV3FileName);

 // To be deprecated
 args.TokenCache.DeserializeUnifiedAndAdalCache(cacheData);

 // Each deserialize call will merge from what has already been loaded
 bool merge = true;

 // Add the following cache deserializers
 // Writes to cache used in ClientApplication
 args.TokenCache.DeserializeAdalV3(adalV3State, merge);
 args.TokenCache.DeserializeMsalV2(msalv2State, merge);
 args.TokenCache.DeserializeMsalV3(msalv3State, merge);
}

private void AfterAccessNotification(TokenCacheNotificationArgs args)
{
    if (!args.HasStateChanged)
    {
        return;
    }

    // Deprecate
    CacheData cacheData = args.TokenCache.SerializeUnifiedAndAdalCache();
    var prevAdalV3State = cacheData.AdalV3State;
    var prevMsalV2State = cacheData.UnifiedState;

    // Add the following cache serializers
    // Reads from the cache in the ClientApplication
    byte[] adalV3State = args.TokenCache.SerializeAdalV3();
    byte[] msalv2State = args.TokenCache.SerializeMsalV2();
    byte[] msalv3State = args.TokenCache.SerializeMsalV3();

    // Persist cache state to file (depending on the migration scenario)
    FileStorage.WriteToFileIfNotNull(AdalV3FileName, adalV3State);
    FileStorage.WriteToFileIfNotNull(MsalV2FileNAme, msalv2State);
    FileStorage.WriteToFileIfNotNull(MsalV3FileName, msalv3State);
}

```

### You can provide your own web view

#### Implement ICustomWebUI

As explained in [You told us you needed extensibility](#you-told-us-you-needed-extensibility), we have added extensibility that allows you provide your own UI in public client applications, and to let the user go through the /Authorize endpoint of the identity provider and let them sign-in and consent. MSAL.NET will then be able to redeem the authentication code and get a token.

If you need to provide your own Web UI:

1. Implement the `ICustomWebUi`  interface (See [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/053a98d16596be7e9ca1ab916924e5736e341fe8/src/Microsoft.Identity.Client/Extensibility/ICustomWebUI.cs#L32-L70). You'll basically need to implement one method `AcquireAuthorizationCodeAsync` accepting the authorization code URL (computed by MSAL.NET), letting the user go through the interaction with the identity provider, and then returning back the URL by which the identity provider would have called your implemetnation back (including the authorization code). In case of issues, your implementation should throw a `MsalExtensionException` exception in order to nicely cooperate with MSAL.
2. In your `AcquireTokenInteractiveCall`, you can use the `.WithCustomUI()` modifier passing the instance of your custom web UI

#### MSAL.NET UI tests also use this mechanism

We have rewritten our UI tests to leverage this extensibility mechanism. In case you are interested you can have a look at the [SeleniumWebUI](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/053a98d16596be7e9ca1ab916924e5736e341fe8/tests/Microsoft.Identity.Test.Integration/Infrastructure/SeleniumWebUI.cs#L15-L160) class in the MSAL.NET source code

#### Security is respected

Note that, in public client applications, MSAL.NET leverages the PKCE standard ([RFC 7636 - Proof Key for Code Exchange by OAuth Public Clients](https://tools.ietf.org/html/rfc7636)) to ensure that security is respected: Only MSAL.NET can redeem the code.

### Plans for deprecation in MSAL.NET 3.x and MSAL.NET 4.x

As seen above, we are proposing a new MSAL.NET V3.0 API based on builders. For the moment (MSAL 3.0), uses this new API side by side with the previous V2.0 APIs (still having `AcquireToken`*XXX*`Async`, and many overrides of these), in order to leave you time to migrate to the new API.

Here is the current proposal, on which we'd like to get your feedback:

- In 3 months, supposing that, like us, you love the new APIs, we'd want to deprecate the old V2.0 API. At that point, if you still use the old-style API, you'll see warnings encouraging you to move to the new API.
- Then in the next major version of MSAL.NET, the old V2.0 style APIs would disappear, leaving a very simple shape for the API. In the class diagram below, we've hidden the V2.0 style APIs; that gives you an idea of what the public API could be in the next major release.

![image](https://user-images.githubusercontent.com/13203188/51684356-28471200-1fec-11e9-937a-009f02268aae.png)

## Why MSAL.NET moved from MSAL 2.x to MSAL 3.x

This paragraph explains why MSAL.NET's major version number was bumped-up from 2 to 3. 

### Reacting to your feedback

In August, we released MSAL.NET 2.0 and you've been using it and providing feedback eversince. Since then we've [released](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki#roadmap) fourteen incremental updates of MSAL.NET, improving both the API and the behavior of the library. You've been awesome helping us make MSAL.NET a great authentication library. But you also told us that things could be improved upon, and that you needed more flexibility. If you are interested in the analysis of your feedback with links to issues you raised, see the following GitHub issue [[New API] Improved 3.x API leveraging builders #810](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/810)). We wanted to bring you these improvements, but we reached a limit in terms of API complexity. Here are the details of this journey.

#### You told us you needed more configuration for applications

##### Providing your own HttpClient, supporting Http proxy and customization of user agent headers

We understand that there are cases where you want fine grained control on the Http proxy for instance, which we had not been able to provide you at all (on .NET core), or in a limited way (.NET framework). Also, ASP.NET Core has some very efficient ways of pooling the `HttpClient` instance, and MSAL.NET clearly did not benefit from it (for details see [Use HttpClientFactory to implement resilient HTTP requests](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests))

```csharp
IMsalHttpClientFactory httpClientFactory = new MyHttpClientFactory();

var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId) 
                                        .WithHttpClientFactory(httpClientFactory)
                                        .Build();

```

##### Configuring your app to target national and sovereign clouds was not straightforward

As some of you start working on writing apps that can not only target users in the Azure public cloud, but also in national and sovereign clouds (for instance the US government cloud), you gave us the feedback that you'd need help making this transition easier. There are several ongoing initiatives for this, but MSAL.NET could already make it easier for you to configure the cloud instance you want to target, and the audience of your application. Along the same lines, we also came to the realization that we could help you with the configuration of the application from configuration files, and provide guidance on how to do that better.

```csharp
var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                        .WithAuthority(
                                             AzureCloudInstance.AzureGermany, 
                                             AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                                         .Build();

```

##### Partial summary: Better configuration for apps

It was clear that you needed more configuration options for instantiating applications … therefore more parameters to the constructors, and help to support configuration files.

#### More flexibility in methods acquiring tokens

MSAL.NET enables you to get access tokens to call protected APIs in different ways, depending on your scenario, on the kind of app you build, and on the platform. See [Scenarios](Scenarios).

All MSAL methods are async, so they should accept a CancellationToken. 

Similarly, the only way to react to conditional access exceptions in MSAL 2.x was, as explained in [Handling Claim challenge exceptions in MSAL.NET](exceptions#handling-claim-challenge-exceptions-in-msalnet), to use the `extraQueryParameter`. Unfortunately, this parameter was available only in two overrides of the `AcquireTokenAsync` (interactive) flow, and it was not working correctly. We needed to fix it, and provide a proper `claims` parameter to `AcquireToken`*XXX*`Async` methods, including `AcquireTokenSilentAsync`.

Finally, some advanced end-user scenarios, like letting the user pre-consent ahead of time were really hard to achieve, as you'll see in the next paragraph.

#### A limit was reached in terms of method overloads

Let's take one of the scenarios where the developer experience was not good: acquiring a token interactively in a desktop or mobile application (The same reasoning can be extended to other scenarios). In that case you had to call `AcquireTokenAsync`, but the issue is that this method had 14 overloads (for details of all the parameters see [Acquiring tokens interactively](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-interactively)).

![image](https://user-images.githubusercontent.com/13203188/37063988-40e52368-2193-11e8-887e-e86b97c54d19.png)

But what's worse is that to achieve one goal (for instance, letting the end user pre-consent to specific scopes ahead of time), you had to use one specific overload with 7 parameters, and, just to pass `extraScopesToConsent`, you had to fill-in all the other parameters, and as a result, you had to study the documentation in detail to understand what could be good values for these parameters. This pain is illustrated in this article [How to get consent for several resources](Acquiring-tokens-interactively#how-to-get-consent-for-several-resources), where you see that you had to write code like this just to be able to pass-in `scopesForVendorApi` (and some of you had no idea what to use for `uiBehavior`, or `authority`)

```CSharp
var result = await app.AcquireTokenAsync(scopesForCustomerApi,
                                         accounts.FirstOrDefault(),
                                         uiBehavior,
                                         string.Empty,
                                         scopesForVendorApi,
                                         app.Authority,
                                         uiParent);
```

With each new addition, there would be an explosion in the number of `AcquireTokenAsync` methods and method params, making the API unusable. 

The standard practice to deal with this is to use builder objects, which is introduced in MSAL 3. 

```CSharp
var result = await app.AcquireTokenInteractive(scopesForCustomerApi)
                     .WithAccount(accounts.FirstOrDefault())
                     .WithExtraScopesToConsent(scopesForVendorApi)
                     .ExecuteAsync();
```

#### More extensibility

Another request you made was to allow developers to bring their own UI - [allow public applications to acquire token via authorization code #863](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/863). In .NET Core we don't provide interactive authentication because no Web control is available there (yet). But there are cases, like what the Azure CLI team has done, where you want to let the user sign-in and consent into the machine browser on the desktop. This can be done, but at the same time you don't want to sacrifice the security. Therefore you asked us to provide an extensible way to let you do that. Another example of this is our own Visual Studio team who has Electron applications (VS Feedback and Azure Storage explorer) and wanted sign-in to be delegated in their UI, while still benefiting from the rest of MSAL.NET.

```csharp

  // Here we inject a custom web UI that is controlled by Selenium to test authentication. 
  // See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/dev3x/tests/Microsoft.Identity.Test.Integration/SeleniumTests/InteractiveFlowTests.cs#L171 
  AuthenticationResult result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, false))
                .ExecuteAsync()
                .ConfigureAwait(false);
```

#### Naming could be improved

Finally we heard that we could improve the naming of things a bit. Here is an example `UIBehavior` was the mechanism, in MSAL 2.x by which you could customize the user experience and direct the Microsoft identity platform to present a particular prompt experience. We got the feedback that the name `UIBehavior` did not speak much, and everybody in the industry is naming it `Prompt` therefore we've decided to rename `UIBehavior` to `Prompt` in all MSAL libraries, starting with MSAL 3.x

#### Testability of your app could be improved

In the past to help you test your app (for instance in [MSAL 2.5.0-preview](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/releases/tag/MSAL-v2.5.0)), it was still not easy to test the cache, and the applications due to a lack of interfaces (for instance `ITokenCache`), and the mutability of application configuration (for example it was possible to set the `RedirectUri` of an app after constructing it)

### Unified cache layout format change

MSAL 2.0 already enabled [common token cache scenarios](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/adalnet4-0-preview#common-token-cache-scenarios) between ADAL and MSAL on the platforms supported by ADAL, and also between MSAL libraries on different platforms. However as we were recently working on initiatives to enable SSO between tools written in different languages, we discovered that, on Windows/Linux/MacOS, MSAL.NET and MSAL.Python (for instance), although using the same cache schema, did not share the same layout for the blob that is serialized when you implement your own [custom cache serialization](https://aka.ms/msal-net-token-cache-serialization). Therefore, we slightly changed the layout format of the token cache blob to harmonize the format between MSAL.NET, MSAL Python and MSAL.Java. This is a breaking change, but we also provided a migration path. See [how to maintain SSO with apps written with ADAL v3, ADAL v4, MSAL.NET v2](#how-to-maintain-sso-with-apps-written-with-adal-v3-adal-v4-msalnet-v2) if you are interested in one of these.

### Summarizing the feedback

To summarize what we learned, you needed:

- More flexibility in configuring your apps,
- More options to acquire tokens
- An API which enables your apps to be more testable
- Customization of the Web view should be possible (on .NET Core)

We had to change the API to enable this flexibility without making it overly complex. Therefore we've decided that MSAL.NET 3 would bring a lot of changes, including a few breaking changes.

#### Future plans

- `AcquireTokenInteractive` for .NET Core 3+ on Windows
- Leveraging the desktop default browser on MacOS and Linux
