# Maui Status
This file tracks the progress of MAUI. This branch has MSAL.NET code that is ported to MAUI. It has two dev apps used to for testing. **It currently supports only iOS and Android platforms.**

## Prerequisites
To build and run the branch, it will require:
- Visual Studio 2022 **Preview**
- Mac to compile iOS Apps

# Getting started
- Clone this repository  
`git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet`  
- Checkout the brnach for MAUI  
`git checkout -b sameerk/MauiCI origin/sameerk/Maui_CI`  
- Open Visual Studio 2022 **Preview** version
- Open the solution  
`/tests/devapps/MauiApps/MauiApps.sln`
- There are following projects in it
    - MauiAppBasic  
    This shows how to perform authentication with no broker. It has the common pattern of ATS+ATI. Note: Android does not support embedded browser.
    - MauiAppWithBroker  
    This shows how to perform authentication with broker. It has the common pattern of ATS+ATI.
    - Microsoft.Identity.client  
    This is the MAUI branch of the main branch. As of this writing, it is not up to date with the main branch. This is work in progress.

## MSAL.NET
The branch compiles using Visual Studio 2022 Preview and the release version.

## Dev Apps
Two dev apps have been added for testing.

### MauiAppBasic
This performs basic authentication using ATS + ATI flow. There is no broker involved. 
The test steps used are:
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
| WinUI3 | - |
| MacOS | - |
</div>

### MauiAppBroker
This performs basic authentication using ATS + ATI flow using broker.  
The test steps used are:
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
| WinUI3 | - |
| MacOS | - |
</div>
