# Active Directory Authentication Library (ADAL) for .NET, Windows Store, .NET Core, Xamarin iOS and Xamarin Android.

| [Code Samples](https://github.com/azure-samples?utf8=✓&q=active-directory-dotnet) | [Reference Docs](https://docs.microsoft.com/active-directory/adal/microsoft.identitymodel.clients.activedirectory) | [Developer Guide](https://aka.ms/aaddev)
| --- | --- | --- |

Active Directory Authentication Library (ADAL) provides easy to use authentication functionality for your .NET/.NET Core client, Windows Store/Xamarin.iOS/Xamarin.Android apps by taking advantage of Windows Server Active Directory and Windows Azure Active Directory.


 Stable (`master` branch)    | Nightly (`dev` branch)
-----------------------------|-------------------------
 [![NuGet](https://img.shields.io/nuget/v/Microsoft.IdentityModel.Clients.ActiveDirectory.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/) | [![MyGet](https://img.shields.io/myget/aad-clients-nightly/vpre/Microsoft.IdentityModel.Clients.ActiveDirectory.svg?style=flat-square&label=myget&colorB=ff0000)](https://www.myget.org/feed/aad-clients-nightly/package/nuget/Microsoft.IdentityModel.Clients.ActiveDirectory)

## Build status
| Branch  | Status |
| ------------- | ------------- |
| dev (AppVeyor)  | [![Build status](https://ci.appveyor.com/api/projects/status/e9rsfjshqr3vj6b7/branch/dev?svg=true)](https://ci.appveyor.com/project/AADDevExLibraries/azure-activedirectory-library-for-dotnet/branch/dev) |

## Versions
Current version - latest one at [nuget.org](https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/).  
Minimum recommended version - 2.29.0
You can find the changes for each version in the [change log](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/blob/master/changelog.txt).

## Security Issue in Multiple Versions of ADAL .Net ###

A defect in ADAL .Net can result in an elevation of privilege in specific problem scenarios. The problem scenarios involve the On Behalf Of protocol flow and specific use cases of a ClientAssertion/ClientAssertionCertificate/ClientCredential and UserAssertion being passed to the AcquireToken* API. Multiple versions of the library are affected. Affected versions are listed below.

We have emailed owners of active applications that are using an impacted version of the library in the specific problem scenarios.

The latest stable version of the library does not have the defect. To avoid being impacted we strongly recommend you update to at least 2.28.1 for 2.x, 3.13.4 for 3.x, or the latest stable version. If you have questions about this issue, please email aadintegrate@microsoft.com.

Affected 2.x versions: 2.27.306291202, 2.26.305102204, 2.26.305100852, 2.25.305061457, 2.21.301221612, 2.20.301151232, 2.19.208020213, 2.18.206251556, 2.17.206230854, 2.16.204221202, 2.15.204151539, 2.14.201151115, 2.13.112191810, 2.12.111071459, 2.11.10918.1222, 2.10.10910.1511, 2.9.10826.1824, 2.8.10804.1442-rc, 2.7.10707.1513-rc, 2.6.2-alpha, 2.6.1-alpha, 2.5.1-alpha

Affected 3.x versions: 3.11.305310302-alpha, 3.10.305231913, 3.10.305161347, 3.10.305110106, 3.5.208051316-alpha, 3.5.208012240-alpha, 3.5.207081303-alpha, 3.4.206191646-alpha, 3.3.205061641-alpha, 3.2.204281119-alpha, 3.1.203031538-alpha, 3.0.110281957-alpha

## Samples and Documentation

We provide a full suite of [sample applications](https://github.com/Azure-Samples?utf8=%E2%9C%93&q=active-directory) and [ADAL documentation](https://docs.microsoft.com/active-directory/adal/microsoft.identitymodel.clients.activedirectory) to help you get started with learning the Azure Identity system. Our [Azure AD Developer Guide](https://aka.ms/aaddev) includes tutorials for native clients such as Windows, Windows Phone, iOS, OSX, Android, and Linux. We also provide full walkthroughs for authentication flows such as OAuth2, OpenID Connect, Graph API, and other awesome features.

## Community Help and Support

We leverage [Stack Overflow](http://stackoverflow.com/) to work with the community on supporting Azure Active Directory and its SDKs, including this one! We highly recommend you ask your questions on Stack Overflow (we're all on there!) Also browser existing issues to see if someone has had your question before.

We recommend you use the "adal" tag so we can see it! Here is the latest Q&A on Stack Overflow for ADAL: [http://stackoverflow.com/questions/tagged/adal](http://stackoverflow.com/questions/tagged/adal)

## Security Reporting

If you find a security issue with our libraries or services please report it to [secure@microsoft.com](mailto:secure@microsoft.com) with as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bounty](http://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly upon receiving the information. We encourage you to get notifications of when security incidents occur by visiting [this page](https://technet.microsoft.com/en-us/security/dd252948) and subscribing to Security Advisory Alerts.

## Contributing

All code is licensed under the MIT license and we triage actively on GitHub. We enthusiastically welcome contributions and feedback. You can clone the repo and start contributing now, but check [this document](./contributing.md) first.

## Diagnostics

The following are the primary sources of information for diagnosing issues:

+ Exceptions
+ Logs
+ Network traces

Also, note that correlation IDs are central to the diagnostics in the library.  You can set your correlation IDs on a per request basis (by setting `CorrelationId` property on `AuthenticationContext` before calling an acquire token method) if you want to correlate an ADAL request with other operations in your code. If you don't set a correlations id, then ADAL will generate a random one which changes on each request. All log messages and network calls will be stamped with the correlation id.  

### Exceptions

This is obviously the first diagnostic.  We try to provide helpful error messages.  If you find one that is not helpful please file an [issue](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues) and let us know. Please also provide the target platform of your application (e.g. Desktop, Windows Store, Windows Phone).

### Logs

In order to configure logging, implementation of IAdalLogCallback interface should be provided

```C#
class LoggerCallbackImpl : IAdalLogCallback
{
    public void Log(LogLevel level, string message)
    {
        // process log message, for example write it to your favorite logging framework
    }
}
```
static property Callback of the LoggerCallbackHandler class should be set to the instance of a class implementing IAdalLogCallback interface

```C#
LoggerCallbackHandler.Callback = new LoggerCallbackImpl();
```

### Brokered Authentication for iOS

If your app requires conditional access or certificate authentication (currently in preview) support, you must set up your AuthenticationContext and redirectURI to be able to talk to the Azure Authenticator app. Make sure that your Redirect URI and application's bundle id is all in lower case.

#### Enable Broker Mode on Your Context
Broker is enabled on a per-authentication-context basis. It is disabled by default. You must set useBroker flag to true in PlatformParameters constructor if you wish ADAL to call to broker:

```C#
public PlatformParameters(UIViewController callerViewController, bool useBroker)
```

The userBroker flag setting will allow ADAL to try to call out to the broker.

#### AppDelegate changes
Update the AppDelegate.cs file to  include the override method below. This method is invoked everytime the application is launched and is used as an opportunity to process response from the Broker and complete the authentication process.
```C#
public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
{            
	if (AuthenticationContinuationHelper.IsBrokerResponse(sourceApplication))
    {
		AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url);    
    }
	
    return true;
}
```

#### Registering a URL Scheme
ADAL uses URLs to invoke the broker and then return back to your app. To finish that round trip you need a URL scheme registered for your app. We recommend making the URL scheme fairly unique to minimize the chances of another app using the same URL scheme.
```
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>CFBundleURLName</key>
        <string>com.mycompany.myapp</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>mytestiosapp</string>
        </array>
    </dict>
</array>
```

#### LSApplicationQueriesSchemes
ADAL uses –canOpenURL: to check if the broker is installed on the device. in iOS 9 Apple locked down what schemes an application can query for. You will need to add “msauth” to the LSApplicationQueriesSchemes section of your info.plist file.

```
<key>LSApplicationQueriesSchemes</key>
<array>
     <string>msauth</string>
</array>
````

#### Redirect URI
This adds extra requirements on your redirect URI. Your redirect URI must be in the proper form.

```
<app-scheme>://<your.bundle.id>
ex: mytestiosapp://com.mycompany.myapp
```

This Redirect URI needs to be registered on the app portal as a valid redirect URI. Additionally a second "msauth" form needs to be registered to handle certificate authentication in Azure Authenticator.

```
msauth://code/<broker-redirect-uri-in-url-encoded-form>
AND
msauth://code/<broker-redirect-uri-in-url-encoded-form>/
ex: msauth://code/mytestiosapp%3A%2F%2Fcom.mycompany.myapp and msauth://code/mytestiosapp%3A%2F%2Fcom.mycompany.myapp/  
```
### Brokered Authentication for Android

If your app or your app users require conditional access or certificate authentication support, you must set up your AuthenticationContext and redirectURI to be able to talk to the Azure Authenticator app OR Company Portal. Make sure that your Redirect URI and application's bundle id is all in lower case.

#### Enable Broker Mode on Your Context
Broker is enabled on a per-authentication-context basis. It is disabled by default. You must set useBroker flag to true in PlatformParameters constructor if you wish ADAL to call to broker:

```C#
public PlatformParameters(Activity callerActivity, bool useBroker)
public PlatformParameters(Activity callerActivity, bool useBroker, PromptBehavior promptBehavior)
```

The useBroker flag setting will allow ADAL to try to call out to the broker.

If target version is lower than 23, calling app has to have the following permissions declared in manifest(http://developer.android.com/reference/android/accounts/AccountManager.html):
 - GET_ACCOUNTS
 - USE_CREDENTIALS
 - MANAGE_ACCOUNTS
If target version is 23, USE_CREDENTIALS and MANAGE_ACCOUNTS have been deprecated and GET_ACCOUNTS is under protection level "dangerous". The calling app is responsible for requesting the runtime permission for GET_ACCOUNTS. You can reference Runtime permission request for API 23.

#### Registering Redirect URI
ADAL uses URLs to invoke the broker and then return back to your app. To finish that round trip you need a URL scheme registered for your app. We recommend making the URL scheme fairly unique to minimize the chances of another app using the same URL scheme.
You can call generateRedirectUriForBroker.ps1 (requires updates from the developer to fill in values and details about the app) to compute the redirect uri.

#### App Activity changes
Update the MainActivity.cs file to  include the override method below. This method is invoked when the activity receives a callback from webview or the broker application. This code snippet is required complete the authentication process.
```C#
protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
{
    base.OnActivityResult(requestCode, resultCode, data);
	AuthenticationAgentContinuationHelper.SetAuthenticationAgentContinuationEventArgs(requestCode, resultCode, data);
}
```

### Network Traces

You can use various tools to capture the HTTP traffic that ADAL generates.  This is most useful if you are familiar with the OAuth protocol or if you need to provide diagnostic information to Microsoft or other support channels.

Fiddler is the easiest HTTP tracing tool.  In order to be useful it is necessary to configure fiddler to record unencrypted SSL traffic.  

NOTE: Traces generated in this way may contain highly privileged information such as access tokens, usernames and passwords.  If you are using production accounts, do not share these traces with 3rd parties.  If you need to supply a trace to someone in order to get support, reproduce the issue with a temporary account with usernames and passwords that you don't mind sharing.

## License

Copyright (c) Microsoft Corporation.  All rights reserved. Licensed under the MIT License (the "License");

## We Value and Adhere to the Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.