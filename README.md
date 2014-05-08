# Active Directory Authentication Library (ADAL) for .NET and WinRT
===========

Active Directory Authentication Library (ADAL) provides easy to use authentication functionality for your .NET client and Windows Store apps by taking advantage of Windows Server Active Directory and Windows Azure Active Directory.

## Projects in this repo

### ADAL.NET

* The project to build ADAL for .NET client applications.

### ADAL.NET.WindowsForms

* The project to add browser based interaction to ADAL .NET. The assembly is used internally by the main assembly of ADAL .NET.

### ADAL.WinRT

* The project to build ADAL for Windows Store applications. The output is a WinRT component with .winmd extension.

### Test.ADAL.NET

* End to end tests for ADAL .NET.

### Test.ADAL.NET.Friend

* The friend project to access internal classes in ADAL.NET project to be used by tests.

### Test.ADAL.NET.Unit

* Unit tests for various components in ADAL .NET.

### Test.ADAL.NET.WindowsForms

* End to end tests for ADAL .NET inside a Windows Forms application. The tests in this project are identical to those in Test.ADAL.NET.

### Test.ADAL.WinRT

* End to end tests for ADAL WinRT. These tests require Test.ADAL.WinRT.Dashboard application running to be able to test interactive scenarios with UI automation.

### Test.ADAL.WinRT.Dashboard

* The Windows Store application used for running ADAL WinRT tests.

### Test.ADAL.WinRT.Unit

* Unit tests for various components in ADAL WinRT as well as mock based tests for ADAL WinRT.

## How to Run Tests
------------------------

The majority of tests in this repo are mstests which run either as unit tests (with TestCategory 'AdalDotNetUnit' or 'AdalWinRTUnit') or as end to end test running against a mock service (with TestCategory 'AdalDotNetMock' or 'AdalWinRTMock'). 
These tests are self contained and can run either using Test Explorer in Visual Studio or command line tool mstest.exe.

To run the rest of the tests, you need to create an account on Azure Active Directory (AAD) and/or setup your own ADFS server and then configure them with configurations in file STS.cs.

## License
----------

Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved. Licensed under the Apache License, Version 2.0 (the "License"); 
