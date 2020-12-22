# Bindings

## Errors

### 2020-12-22

MacOSX

```
/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/client/Microsoft.Identity.Client.Ref/CSC: Error CS0006: Metadata file '/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/bindings/Microsoft.Identity.Client.XamarinAndroid.AndroidSupport/bin/Debug/monoandroid9.0/ref/Microsoft.Identity.Client.dll' could not be found (CS0006) (Microsoft.Identity.Client)
/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/client/Microsoft.Identity.Client.Ref/CSC: Error CS0006: Metadata file '/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/bindings/Microsoft.Identity.Client.XamarinAndroid.AndroidX/bin/Debug/monoandroid10.0/ref/Microsoft.Identity.Client.dll' could not be found (CS0006) (Microsoft.Identity.Client)
/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Android/Xamarin.Android.Aapt2.targets(3,3): Error APT2260: resource style/Theme.AppCompat.Light.DarkActionBar (aka Microsoft.Identity.Client:style/Theme.AppCompat.Light.DarkActionBar) not found. (APT2260) (Microsoft.Identity.Client)
/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Android/Xamarin.Android.Aapt2.targets(3,3): Error APT2062: failed linking references. (APT2062) (Microsoft.Identity.Client)
/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/client/Microsoft.Identity.Client/CSC: Error CS0006: Metadata file '/Users/Shared/Projects/d/tmp/microsoft-authentication-library-for-dotnet/src/bindings/Microsoft.Identity.Client.XamarinAndroid.AndroidX/bin/Debug/monoandroid10.0/Microsoft.Identity.Client.Xamarin.Android.dll' could not be found (CS0006) (Microsoft.Identity.Client)
```

Windows

```
Severity	Code	Description	Project	File	Line	Suppression State
Error	APT2062	failed linking references.	Microsoft.Identity.Client.Ref	C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Xamarin\Android\Xamarin.Android.Aapt2.targets	157	
Error	APT2062	failed linking references.	Microsoft.Identity.Client	C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Xamarin\Android\Xamarin.Android.Aapt2.targets	157	
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\devapps\XamarinDev\XamarinDev\bin\Debug\netstandard2.0\XamarinDev.dll' could not be found	XamarinDev.Android	D:\X.tmp\moljac-fork\msal-ax-as\tests\devapps\XamarinDev\XamarinDev.Android\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Common\bin\Debug\netstandard2.0\Microsoft.Identity.Test.Common.dll' could not be found	Microsoft.Identity.Test.Android.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Android.UIAutomation\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Common\bin\Debug\netstandard2.0\Microsoft.Identity.Test.Common.dll' could not be found	Microsoft.Identity.Test.iOS.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.iOS.UIAutomation\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Core.UIAutomation\bin\Debug\Microsoft.Identity.Test.Core.UIAutomation.dll' could not be found	Microsoft.Identity.Test.Android.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Android.UIAutomation\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Core.UIAutomation\bin\Debug\Microsoft.Identity.Test.Core.UIAutomation.dll' could not be found	Microsoft.Identity.Test.iOS.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.iOS.UIAutomation\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.LabInfrastructure\bin\Debug\netstandard2.0\Microsoft.Identity.Test.LabInfrastructure.dll' could not be found	Microsoft.Identity.Test.Android.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.Android.UIAutomation\CSC	1	Active
Error	CS0006	Metadata file 'D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.LabInfrastructure\bin\Debug\netstandard2.0\Microsoft.Identity.Test.LabInfrastructure.dll' could not be found	Microsoft.Identity.Test.iOS.UIAutomation	D:\X.tmp\moljac-fork\msal-ax-as\tests\Microsoft.Identity.Test.iOS.UIAutomation\CSC	1	Active
Error	APT2260	resource style/Theme.AppCompat.Light.DarkActionBar (aka Microsoft.Identity.Client.Ref:style/Theme.AppCompat.Light.DarkActionBar) not found.	Microsoft.Identity.Client.Ref	C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Xamarin\Android\Xamarin.Android.Aapt2.targets	157	
Error	APT2260	resource style/Theme.AppCompat.Light.DarkActionBar (aka Microsoft.Identity.Client:style/Theme.AppCompat.Light.DarkActionBar) not found.	Microsoft.Identity.Client	C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Xamarin\Android\Xamarin.Android.Aapt2.targets	157	

```



## MultiTargeting

```
  <PropertyGroup>
    <TargetFrameworks>monoandroid9.0;monoandroid10.0;monoandroid11.0</TargetFrameworks>
    <WarningLevel>0</WarningLevel>
    <LangVersion>latest</LangVersion>
    <IsBindingProject>True</IsBindingProject>
    <RootNamespace>Microsoft.Identity.Client.Xamarin.Android</RootNamespace>
    <AssemblyName>Microsoft.Identity.Client.Xamarin.Android</AssemblyName>
    <AndroidClassParser>class-parse</AndroidClassParser>
    <AndroidCodegenTarget>XAJavaInterop1</AndroidCodegenTarget>
  </PropertyGroup>
```

 dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidX\bin\Debug\monoandroid9.0\

 ```
 ```




```
dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidX\bin\Debug\monoandroid10.0\
```

```
    Directory: D:\X.tmp\moljac-fork\msal-ax-as\src\bindings\Microsoft.Identity.Client.XamarinAndroid.AndroidX\bin\Debug\monoandroid10.0


Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a----        2020-12-16     16:35        2152448 Microsoft.Identity.Client.Xamarin.Android.AndroidX.dll
-a----        2020-12-16     16:35         181168 Microsoft.Identity.Client.Xamarin.Android.AndroidX.pdb
```

dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidX\bin\Debug\monoandroid11.0\

```
```







```
dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidSupport\bin\Debug\monoandroid9.0\
```

```
dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidSupport\bin\Debug\monoandroid10.0\


    Directory: D:\X.tmp\moljac-fork\msal-ax-as\src\bindings\Microsoft.Identity.Client.XamarinAndroid.AndroidSupport\bin\Debug\monoandroid10.0


Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a----        2020-12-16     16:35        1914880 Microsoft.Identity.Client.Xamarin.Android.AndroidSupport.dll
-a----        2020-12-16     16:35          81380 Microsoft.Identity.Client.Xamarin.Android.AndroidSupport.pdb
```

 dir .\Microsoft.Identity.Client.XamarinAndroid.AndroidSupport\bin\Debug\monoandroid11.0\

 ```
 ```