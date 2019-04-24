# Dev Box Setup

These instructions are for setting up your machine if you're working on the MSAL.NET library itself.

## Windows Setup

Windows is the primary development environment for the library at this time.
VS for Mac is not able to understand and layout the project correctly.  You should be able
to build from the command line.

### Visual Studio 2019

* Install or update VS 2019 (Community is sufficient but any SKU should work) with the following workloads:
  * .Net Desktop Development
  * Universal Windows Platform Development
  * Mobile Development with .Net
  * .Net Core cross-platform development

* Then from the "Individual Components" tab, make sure these additional items are selected:
  * .NET Framework 4.5 targeting pack
  * .NET Framework 4.5.2 targeting pack
  * .NET Framework 4.6.1 SDK
  * .NET Framework 4.6.1 targeting pack
  * .NET Framework 4.6.2 targeting pack
  * Android SDK setup (API level 27)
  * Windows 10 SDK (10.0.17134.0)

* Android SDK level 27 (oreo) and 28 (pie), and Android SDK build tools 27.0.3 are also required. These are not installed through the VS Installer, so instead use the Android SDK Manager (Visual Studio > Tools > Android > Android SDK Manager…)

## Debugging or running samples on Windows 10

* The dev-built binaries are not signed so you will regularly need to run `sn -Vr *` (from an admin console) to bypass strong name validation.  If you're at MSFT on CorpNet then group policy will likely revert this setting and you may need to run this often or setup a Scheduled Task.
* [Microsoft Only] You will need the Lab Certificate from [here](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/57f88e5d-dc7d-422b-b87a-e215ad6a352c/resourceGroups/DevEx_Automation/providers/Microsoft.KeyVault/vaults/BuildAutomation/certificates) installed on your machines in order to access the KeyVault containing passwords for the lab accounts.

## Debugging or running samples on iOS

* You will need a Macintosh running the latest macOS and XCode installed
* From Visual Studio 2019 on the PC, go to Tools / iOS and select "Pair to Mac"
* TODO: how to setup entitlements to get the simulator/samples to work

## Debugging or running samples on macOS

## Debugging or running samples on Android

* As part of the Visual Studio 2019 installation on your PC, add the optional component "Intel Hardware Accelerated Execution Manager (HAXM) (local install)" from the Visual Studio Installer

## Debugging or running samples on UWP

## Build

* Run <repo_root>/build/install_dependencies.ps1 from an admin powershell console to setup additional dependencies and environment variables
* You can run <repo_root>/restore.ps1 to do a full NuGet package restore from the command line.
* LibsNoSamples.sln is the base solution to build the library and run unit tests.  It does not contain sample or test applications so it builds faster and with less dependencies.
* Load LibsAndSamples.sln for a bigger solution with lots of apps that exercise MSAL.

## Run tests

You won't be able to run the Integration test or Automation tests because they require access to a Microsoft KeyVault which is locked down. These tests will run as part of our DevOps pipelines though.

Run the unit tests from the assemblies:

- Microsoft.Identity.Test.Unit.net45
- Microsoft.Identity.Test.Unit.netcore

## Package

From VS or from the command line if you wish to control the versioning:

`msbuild <msal>.csproj /t:pack /p:MsalClientSemVer=3.1.0-preview`

### Command Line

Use `msbuild` commands - `msbuild /t:restore` and `msbuild`. Do not rely on `dotnet` command line because it is only for .Net Core, but this library has many other targets.

Note: To enable us to target Xamarin as well as .Net core, we took a dependency on the MsBuild SDK extras - https://github.com/onovotny/MSBuildSdkExtras See this [issue](https://github.com/onovotny/MSBuildSdkExtras/issues/102) about using `dotnet`


