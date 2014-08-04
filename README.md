# Active Directory Authentication Library (ADAL) for .NET, Windows Store and Windows Phone 8.1

Active Directory Authentication Library (ADAL) provides easy to use authentication functionality for your .NET client and Windows Store apps by taking advantage of Windows Server Active Directory and Windows Azure Active Directory.
Here you can find the source code for the library. You can find the corresponding releases (both stable and prerelease) on the NuGet gallery at [http://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/](http://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/).
- The latest stable release is [1.0.4](https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/1.0.4). Note that this is for .NET only.
- The latest prerelease is [2.8.10804.1442-rc](https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/2.8.10804.1442-rc).

## Projects in this repo

### ADAL.NET

* This project contains the source of ADAL .NET.

### ADAL.NET.WindowsForms

* This project contains the source of the internal component used by ADAL .NET to drive user interaction on the Windows desktop.

### ADAL.WinRT

* This project contains the source of ADAL for Windows Store. ADAL for Windows Store is packaged as a Windows Runtime Component (.winmd).

### ADAL.WinPhone

* This project contains the source of ADAL for Windows Phone. ADAL for Windows Phone is packaged as a Windows Runtime Component (.winmd).
=======
* This project (under /WinPhone) contains the source of ADAL for Windows Phone 8.1. ADAL for Windows Phone 8.1  is packaged as a Windows Runtime Component. The same /WinPhone folder contains its own tests.

### Test.ADAL.NET

* End to end tests for ADAL .NET.

### Test.ADAL.NET.Friend

* The friend project to access internal classes in ADAL.NET project to be used by tests.

### Test.ADAL.NET.Unit

* Unit tests for various components in ADAL .NET.

### Test.ADAL.NET.WindowsForms

* End to end tests for ADAL .NET inside a Windows Forms application. The tests in this project are identical to those in Test.ADAL.NET.

### Test.ADAL.WinRT

* End to end tests for ADAL for Windows Store. These tests require Test.ADAL.WinRT.Dashboard application running to be able to test interactive scenarios with UI automation.

### Test.ADAL.WinRT.Dashboard

* The Windows Store application used for running ADAL for Windows Store tests.

### Test.ADAL.WinPhone.Dashboard

* The Windows Phone application used for running ADAL for Windows Phone tests.

### Test.ADAL.WinRT.Unit

* Unit tests for various components in ADAL for Windows Store as well as mock based tests for ADAL for Windows Store.

### Test.ADAL.WinPhone.Unit

* Unit tests for various components in ADAL for Windows Phone as well as mock based tests for ADAL for Windows Phone.

## How to Run Tests

The majority of tests in this repo are mstests which run either as unit tests (with TestCategory 'AdalDotNetUnit' or 'AdalWinRTUnit') or as end to end test running against a mock service (with TestCategory 'AdalDotNetMock' or 'AdalWinRTMock'). 
These tests are self contained and can run either using Test Explorer in Visual Studio or command line tool mstest.exe.

To run the rest of the tests, you need to create an account on Azure Active Directory (AAD) and/or setup your own ADFS server and then configure them with configurations in file STS.cs.

## License

Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved. Licensed under the Apache License, Version 2.0 (the "License"); 
