# Contributing to MSAL.NET

Microsoft Authentication Library (MSAL) for .NET welcomes new contributors.  This document will guide you through the process.

## Contributor License agreement

Please visit [https://cla.microsoft.com/](https://cla.microsoft.com/) and sign the Contributor License Agreement.  You only need to do that once. We can not look at your code until you've submitted this request.

## Tests

It's all standard stuff, but please note that you won't be able to run integration tests locally because they connect to a KeyVault to fetch some test users and passwords. The CI will run them for you.

## How the MSAL team deals with forks

The CI build will not run on a PR opened from a fork, as a security measure. The MSAL team will manually move your branch from your fork to the main repository, to be able to run the CI. This will preserve the identity of the commit.

```bash
# list existing remotes
git remote -v 

# add a remote to the fork of the contributor
git remote add joe joes_repo_url

# sync
git fetch joe

# checkout the contributor's branch 
git checkout joes_feature_branch

# push it to the original repository (AzureAD/MSAL)
git push origin
```

## Submitting bugs and feature requests

### Before submitting an issue

First, please do a search of [open issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues) to see if the issue or feature request has already been filed. Use the tags to narrow down your search. Here's an example of a [query for Xamarin iOS specific issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues?utf8=%E2%9C%93&q=is:issue+is:open+label:scenario:Mobile-iOS).

If you find your issue already exists, add a relevant comment. You can also use an upvote or downvote reaction in place of a "+1" comment.

If your issue is a question, and is not addressed in the documentation, please ask the question on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-ad-msal) using the tag `azure-ad-msal`.

If you cannot find an existing issue that describes your bug or feature request, submit an issue using the guidelines below.

### Write detailed bug reports and feature requests

File a single issue per bug and feature request:

- Do not enumerate multiple bugs or feature requests in the same issue.
- Do not add your issue as a comment to an existing issue unless it's for the identical input. Many issues look similar, but have different causes.

When [submitting an issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new/choose), select the correct category, **Bug Report**, **Documentation**, or **Feature request**.

#### Bug report

The more information you provide, the more likely someone will be successful in reproducing the issue and finding a fix.
Please use the [Bug Report template](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?template=bug_report.md) and complete as much of the information listed as possible. Please use the [latest version of the library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases) and see if your bug still exists before filing an issue.

Remember to do the following:

- Search the issue repository to see if there exists a duplicate issue.
- Update to the latest version of the library to see if the issue still exists.
- Submit an issue by filling out all as much of the information in the Bug Report as possible.

#### Documentation Requests

If you find our documentation or XML comments lacking in necessary detail, submit a [Documentation request](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?template=documentation.md).

If you have found errors in the documentation, or if an example or code snippet is needed, [open an issue in the documentation repository](https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/issues).

#### Feature requests

Have a feature request for MSAL? Complete a [Feature Request](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?template=feature_request.md) or consider making a [contribution](../contribute/overview.md) to the library. Make sure your feature request is clear and concise and contains a detailed description of the problem. Please include any alternative solutions or features you may have considered.

## Building and testing MSAL.NET

### Prerequisites to build MSAL.NET

The following are instructions to setup Visual Studio to build various MSAL.NET solution files on Windows and Mac platforms.

#### Windows

##### Minimal Visual Studio installation

* Install or update Visual Studio 2022. Any edition, such as Community, Pro, or Enterprise will work.
* Install the following workloads:
  * .NET desktop development
  * Universal Windows Platform development
  * Mobile Development with .NET
  * .NET Core cross-platform development
* From the **Individual Components** tab, make sure these items are selected:
  * .NET Framework 4.5.2 targeting pack
  * .NET Framework 4.6.1 SDK
  * .NET Framework 4.6.1 targeting pack
  * .NET Framework 4.6.2 targeting pack
  * Android SDK setup (API level 29)
  * Windows 10 SDK 10.0.17134.0
  * Windows 10 SDK 10.0.17763.0
* Android 9.0 Pie and Android 8.1 Oreo are required. These are not installed through the Visual Studio Installer. Instead, use the Android SDK Manager (**Visual Studio** > **Tools** > **Android** > **Android SDK Manager…**)

With the setup above, you should be able to open and compile `Libs.sln` and `LibsAndSamples.sln` from the [MSAL.NET repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet).

##### Troubleshooting

* If you get an exception similar to `"System.InvalidOperationException: Could not determine Android SDK location"` while restoring NuGet packages, make sure you have the latest Android SDK installed. If you do, you probably hit a bug with the Visual Studio Installer - uninstall and reinstall the SDK from the Visual Studio Installer.
* If you get an exception similar to `"System.TypeLoadException: Could not set up parent class, due to: Could not load type of field 'Microsoft.Identity.Core.UI.WebviewBase:asWebAuthenticationSession' (5) due to: Could not resolve type with token 0100004c from typeref (expected class AuthenticationServices.ASWebAuthenticationSession in assembly 'Xamarin.iOS)"` when running on an iOS simulator, **make sure that you have installed the latest Visual Studio 2022 release and the latest version of [Xcode](https://developer.apple.com/xcode/) on your Mac** to get the classes needed to run `ASWebAuthenticationSession` (e.g., `AuthenticationServices`).

#### macOS

##### [Install Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/)

* During setup, install
  * .NET Core
  * Android
  * iOS
  * MacOS
* In Visual Studio for Mac, select **Tools** > **SDK Manager** and install Android SDK with API 29.

The steps above should enable you to compile `Libs.sln`. You will need a developer certificate to compile `LibsMacOS.sln`.

### Fast build

MSAL.NET supports several target frameworks, but most of the time contributors are only interested in one or two. To get MSAL to build for all frameworks, contributors will need a [hefty Visual Studio installation as well as several SDKs](#prerequisites-to-build-msalnet).

To work around this requirement, open [`Microsoft.Identity.Client.csproj`](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/src/client/Microsoft.Identity.Client/Microsoft.Identity.Client.csproj) and comment out the targets you are not interested in. Keeping the pure .NET targets and eliminating UWP and Xamarin results in a fast build as well as the ability to run all unit tests.

Visual Studio may need to be restarted to ensure that updated target frameworks take effect.

### Visual Studio for Mac

MSAL is a multi-target library and at the time of writing, Visual Studio for Mac is not able to understand and layout this project correctly. The library can still be built from the command line on macOS.

### Visual Studio

1. Load `LibsAndSamples.sln` for a bigger solution with lots of apps that showcase and exercise MSAL. Load `LibsNoSamples.sln` for a small solution that has the library and the tests.
2. Build in Visual Studio (if configured) or via the command line with `msbuild /t:restore` and `msbuild`. If using the command line, developers might need to use the [Visual Studio Developer Command Prompt](/visualstudio/ide/reference/command-prompt-powershell).

>**Note**
>If you run into strong name validation issues, please [log a bug](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues). Workaround is to disable strong name validation on your dev box by running the following command in the Visual Studio Developer Command Prompt with Administrator permissions:
>
>```bash
>sn -Vr *
>```

>**Note**
>You won't be able to run the integration or automation tests because they require access to a Microsoft Key Vault instance which is only accessible to the MSAL.NET engineering team. These tests will run as part of our automation pipelines in GitHub.

### Package

You can create a package from Visual Studio or from the command line with custom version parameters:

```bash
msbuild <msal>.csproj /t:pack /p:MsalClientSemVer=1.2.3-preview
```

### Command Line

You can use `msbuild` commands to build the solution. Use `msbuild /t:restore` and `msbuild`.

>**Note**
>Do not use the `dotnet` command line because it is only for .NET and .NET Core - this library has many other targets that are not possible to build with the .NET tooling.

>**Note**
>To enable the library to target Xamarin as well as .NET and .NET Core, there is a dependency on [MsBuild SDK Extras](https://github.com/novotnyllc/MSBuildSdkExtras). See the ["Support for command line dotnet build or dotnet msbuild"](https://github.com/novotnyllc/MSBuildSdkExtras/issues/102) issue in regards to using `dotnet` CLI commands.
