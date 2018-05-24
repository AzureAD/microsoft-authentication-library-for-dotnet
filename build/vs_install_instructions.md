# Components required to build the library

The following are instructions to setup Visual Studio to build the Combined.NoWinRT.sln

# Minimal Visual Studio 2017 installation 

* Install or update Visual Studio 2017 with the following workloads: Universal Windows Platform, Mobile Development with .NET and .NET Core cross-platform development 
* Then from the "Individual Components" tab, you'll need these additional items: 

    * .NET Framework 4.5 targeting pack
    * .NET Framework 4.5.2 targeting pack
    * .NET Framework 4.6.1 SDK 
    * .NET Framework 4.6.1 targeting pack
    * .NET Framework 4.6.2 targeting pack
    * Android SDK setup (API level 27)


* Android SDK level 24 and 26 are also required. These are not installed through the VS Installer, so instead use the Android SDK Manager (Visual Studio > Tools > Android > Android SDK Manager…)

This should be enough to allow you to build the libraries from the **adalv3/dev branch**. For the **dev branch**, you also need to install Visual Studio 2015 (see below) 

 ### Troubleshooting
If you get an exception similar to "System.InvalidOperationException: Could not determine Android SDK location" while restoring the NuGet packages, make sure you have the latest Android SDK installed (27 at the time of writing). If you do, you probably hit a bug with the VS installer - uninstall and reinstall the SDK from the Visual Studio Installer. 

## Minimal Visual Studio 2015 installation

This is an additional requirement to build the **dev branch**.

Perform a custom install of Visual Studio 2015 Update 3 and include "Windows 8.1 and Windows Phone 8.0 / 8.1 Tools" and "Universal Windows Apps" components.
