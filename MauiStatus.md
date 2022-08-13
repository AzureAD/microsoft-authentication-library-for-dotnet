# MAUI Status
This file tracks the progress of MAUI. Main branch has MSAL.NET code that is ported to MAUI. **It currently supports only iOS and Android platforms.** The branch has [dev apps](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/tree/main/tests/devapps/MauiApps) used for testing.   
A preview package has been published on NuGet. [Microsoft.Identity.Client 4.46.0-preview2]( https://www.nuget.org/packages/Microsoft.Identity.Client/4.46.0-preview2)  
**Note:** This is a preview package and is subject to change at any time.

## Known issue with MSAL MAUI support
- UWP project does not compile with the NuGet package. [Issue #3460](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3460).

## Prerequisites
To build and run the branch, it will require:
- Visual Studio 2022 **Preview**
- Mac to compile iOS Apps

# Getting started
- Clone this repository  
`git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet`  
- Open Visual Studio 2022 **Preview** version
- Open the solution  
`/tests/devapps/MauiApps/MauiApps.sln`
- The following projects in it
    - MauiAppBasic  
    This shows how to perform authentication with no broker. It has the common pattern of Acquire Token Silent (ATS) + Acquire Token Interactive (ATI). Note: Android does not support the embedded browser.
    - MauiAppWithBroker  
    This shows how to perform authentication with broker. It has the common pattern of ATS + ATI.
    - MauiAppB2C  
    This shows how to perform authentication with OAuth providers such as Google, Facebook, Microsoft Personal account. It has the common pattern of ATS + ATI.
    - Microsoft.Identity.client  
    This compiles the source code of the MSAL.NET library. **Note:** Please uncomment the following in the Microsoft.Identity.Client.csproj.  
    ``` xml
      <!--<PropertyGroup>
            <MAUI_SUPPORT>
            Yes
            </MAUI_SUPPORT>
        </PropertyGroup>-->
    ```
    If you want to use the package, you can remove references to this project and the project from the solution and add the Nuget package.

## MSAL.NET
The branch compiles using Visual Studio 2022 Preview and the release version.

## Dev Apps
Two dev apps have been added for testing.

### MauiAppBasic
This performs basic authentication using ATS + ATI flow. There is no broker involved. 
The repro steps are:
1. ATS + ATI
2. ATS
3. Logout followed by ATS + ATI


The results are as follows:

<div style="margin-left: auto;
            margin-right: auto;
            width: 30%">

| Platform | Status |
| ----------- | ----------- |
| iOS (System) | **Works** |
| iOS (Embedded) | **Works** |
| Android (System) | **Works** |
| Android (Embedded) | **NA** |
| Forms UWP | Does not compile |
| WinUI3 | n/a |
| MacOS | n/a |
</div>

### MauiAppBroker
This performs basic authentication using ATS + ATI flow using broker.  
The repro steps are:
1. ATS + ATI
2. ATS
3. Logout followed by ATS + ATI


The results are as follows:

<div style="margin-left: auto;
            margin-right: auto;
            width: 30%">

| Platform | Status |
| ----------- | ----------- |
| iOS | **Works** |
| Android | **Works** |
| Forms UWP | Does not compile |
| WinUI3 | n/a |
| MacOS | n/a |
</div>

### MauiAppB2C
This performs basic authentication using ATS + ATI flow using external identity providers.  
The repro steps are:
1. ATS + ATI
2. ATS
3. Logout followed by ATS + ATI


The results are as follows:

<div style="margin-left: auto;
            margin-right: auto;
            width: 30%">

| Platform | Status |
| ----------- | ----------- |
| iOS | **Works** |
| Android | **Works** |
| Forms UWP | n/a |
| WinUI3 | n/a |
| MacOS | n/a |
</div>
