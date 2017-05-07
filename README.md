# Microsoft Authentication Library (MSAL) for .NET, Windows Store, UWP, NetCore, Xamarin Android and iOS

| [Getting Started](https://identity.microsoft.com/portal/register-app?appType=mobileAndDesktopApp&appTech=windowsDesktop) | [Sample Code](https://github.com/Azure-Samples/active-directory-dotnet-desktop-msgraph-v2) | [API Reference](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-libraries) | [Support](README.md#community-help-and-support)
| --- | --- | --- | --- |

## General
The MSAL library for Android gives your app the ability to begin using the [Microsoft Cloud](https://cloud.microsoft.com) by supporting [Microsoft Azure Active Directory](https://azure.microsoft.com/en-us/services/active-directory/) and [Microsoft Accounts](https://account.microsoft.com) in a converged experience using industry standard OAuth2 and OpenID Connect. The library also supports [Azure AD B2C](https://azure.microsoft.com/services/active-directory-b2c/).

 Stable (`master` branch)    | Nightly (`dev` branch)
-----------------------------|-------------------------
 [![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity.Client.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Client/) | [![MyGet](https://img.shields.io/myget/aad-clients-nightly/vpre/Microsoft.Identity.Client.svg?style=flat-square&label=myget&colorB=ff0000)](https://www.myget.org/feed/aad-clients-nightly/package/nuget/Microsoft.Identity.Client)

| Branch  | Status | Notes |
| ------------- | ------------- |  ------------- | 
| dev (VSTS) | ![](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/10/badge)  | Builds the entire MSAL solution |
| dev (AppVeyor)  | [![Build status](https://ci.appveyor.com/api/projects/status/pqtq4xvppjm0o4ul/branch/dev?svg=true)](https://ci.appveyor.com/project/AADDevExLibraries/microsoft-authentication-library-for-dotnet/branch/dev)  | Partial build - product assembly and tests only |

## Important Note about the MSAL Preview

These libraries are suitable to use in a production environment. We provide the same production level support for these libraries as we do our current production libraries. During the preview we reserve the right to make changes to the API, cache format, and other mechanisms of this library without notice which you will be required to take along with bug fixes or feature improvements. This may impact your application. For instance, a change to the cache format may impact your users, such as requiring them to sign in again and an API change may require you to update your code. When we provide our General Availability release later, we will require you to update your application to our General Availability version within six months as applications written using the preview library could no longer work.

### Requirements
* Windows 7 or greater
* .NET 4.5 or greater

### Using MSAL
- Before you can get a token from Azure AD v2.0 or Azure AD B2C, you'll need to register an application. For Azure AD v2.0, use [the app registration portal](https://apps.dev.microsoft.com). For Azure AD B2C, checkout [how to register your app with B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-app-registration).  

- For a full sample,  

    ***Azure AD v2.0***

        [.NET WPF Desktop App](https://github.com/Azure-Samples/active-directory-dotnet-desktop-msgraph-v2)
        [Xamarin Cross-Platform App](https://github.com/Azure-Samples/active-directory-xamarin-native-v2)

    ***Azure AD B2C***

        [Xamarin Cross-Platform App](https://github.com/Azure-Samples/active-directory-b2c-xamarin-native)
        [.NET Web App](https://github.com/Azure-Samples/active-directory-b2c-dotnet-webapp-and-webapi) 

#### Step 1: Add MSAL to your Solution/Project

1.  ***Right click on your project*** > ***Manage packages... ***.
2.	Select ***include prerelease*** checkbox > search ***msal***.
3.	Select the ***Microsoft.Identity.Client*** package > ***install***.

#### Step 2: Instantiate MSAL and Acquire a Token

1.  Create a new PublicClientApplication instance. Make sure to fill in your app/client id

```C#
    PublicClientApplication myApp = new PublicClientApplication(CLIENT_ID);
```

2. Acquire a token

```C#
    AuthenticationResult authenticationResult = await myApp.AcquireToken(SCOPES).ConfigureAwait(false);
```

#### Step 3: Use the token!

The access token can now be used in an [HTTP Bearer request](https://github.com/Azure-Samples/active-directory-dotnet-desktop-msgraph-v2/blob/master/active-directory-wpf-nodejs-webapi-v2/MainWindow.xaml.cs#L84).


## Community Help and Support

We use [Stack Overflow](http://stackoverflow.com/questions/tagged/msal) with the community to provide support. We highly recommend you ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before. 

If you find and bug or have a feature request, please raise the issue on [GitHub Issues](../../issues). 

To provide a recommendation, visit our [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contribute

We enthusiastically welcome contributions and feedback. You can clone the repo and start contributing now. Read our [Contribution Guide](Contributing.md) for more information.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Security Library

This library controls how users sign-in and access services. We recommend you always take the latest version of our library in your app when possible. We use [semantic versioning](http://semver.org) so you can control the risk associated with updating your app. As an example, always downloading the latest minor version number (e.g. x.*y*.x) ensures you get the latest security and feature enhanements but our API surface remains the same. You can always see the latest version and release notes under the Releases tab of GitHub.

## Security Reporting

If you find a security issue with our libraries or services please report it to [secure@microsoft.com](mailto:secure@microsoft.com) with as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bounty](http://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly upon receiving the information. We encourage you to get notifications of when security incidents occur by visiting [this page](https://technet.microsoft.com/en-us/security/dd252948) and subscribing to Security Advisory Alerts.


Copyright (c) Microsoft Corporation.  All rights reserved. Licensed under the MIT License (the "License");


