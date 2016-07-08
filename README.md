# Active Directory Authentication Library (ADAL) for .NET, Windows Store, Xamarin iOS and Xamarin Android. 

Active Directory Authentication Library (ADAL) provides easy to use authentication functionality for your .NET client and Windows Store apps by taking advantage of Windows Server Active Directory and Windows Azure Active Directory.
Here you can find the source code for the library. You can find the corresponding releases (both stable and prerelease) on the NuGet gallery at [http://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/](http://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/).

- The latest stable release is [3.10.305110106](https://www.nuget.org/packages/Microsoft.IdentityModel.Clients.ActiveDirectory/). 
- 
- The next version of the library in prerelease form is also avialable on the NuGet gallery.
- 
## Samples and Documentation

[We provide a full suite of sample applications and documentation on GitHub](https://github.com/Azure-Samples) to help you get started with learning the Azure Identity system. This includes tutorials for native clients such as Windows, Windows Phone, iOS, OSX, Android, and Linux. We also provide full walkthroughs for authentication flows such as OAuth2, OpenID Connect, Graph API, and other awesome features. 

## Community Help and Support

We leverage [Stack Overflow](http://stackoverflow.com/) to work with the community on supporting Azure Active Directory and its SDKs, including this one! We highly recommend you ask your questions on Stack Overflow (we're all on there!) Also browser existing issues to see if someone has had your question before. 

We recommend you use the "adal" tag so we can see it! Here is the latest Q&A on Stack Overflow for ADAL: [http://stackoverflow.com/questions/tagged/adal](http://stackoverflow.com/questions/tagged/adal)

## Security Reporting

If you find a security issue with our libraries or services please report it to [secure@microsoft.com](mailto:secure@microsoft.com) with as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bounty](http://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly upon receiving the information. We encourage you to get notifications of when security incidents occur by visiting [this page](https://technet.microsoft.com/en-us/security/dd252948) and subscribing to Security Advisory Alerts.

## Contributing

All code is licensed under the Apache 2.0 license and we triage actively on GitHub. We enthusiastically welcome contributions and feedback. You can clone the repo and start contributing now, but check [this document](./contributing.md) first.

## Community Help and Support

We leverage [Stack Overflow](http://stackoverflow.com/) to work with the community on supporting Azure Active Directory and its SDKs, including this one! We highly recommend you ask your questions on Stack Overflow (we're all on there!) Also browser existing issues to see if someone has had your question before. 

We recommend you use the "adal" tag so we can see it! Here is the latest Q&A on Stack Overflow for ADAL: [http://stackoverflow.com/questions/tagged/adal](http://stackoverflow.com/questions/tagged/adal)

## Security Reporting

If you find a security issue with our libraries or services please report it to [secure@microsoft.com](mailto:secure@microsoft.com) with as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bounty](http://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly upon receiving the information. We encourage you to get notifications of when security incidents occur by visiting [this page](https://technet.microsoft.com/en-us/security/dd252948) and subscribing to Security Advisory Alerts.

## Contributing

All code is licensed under the Apache 2.0 license and we triage actively on GitHub. We enthusiastically welcome contributions and feedback. You can clone the repo and start contributing now, but check [this document](./contributing.md) first.

## Diagnostics

The following are the primary sources of information for diagnosing issues:

+ Exceptions
+ Logs
+ Network traces

Also, note that correlation IDs are central to the diagnostics in the library.  You can set your correlation IDs on a per request basis (by setting `CorrelationId` property on `AuthenticationContext` before calling an acquire token method) if you want to correlate an ADAL request with other operations in your code. If you don't set a correlations id, then ADAL will generate a random one which changes on each request. All log messages and network calls will be stamped with the correlation id.  

### Exceptions

This is obviously the first diagnostic.  We try to provide helpful error messages.  If you find one that is not helpful please file an [issue](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues) and let us know. Please also provide the target platform of your application (e.g. Desktop, Windows Store, Windows Phone).

### Logs

You can configure the library to generate log messages that you can use to help diagnose issues.  You configure logging by setting properties of the static class `AdalTrace`; however, depending on the platform, logging methods and the properties of this class differ. Here is how logging works on each platform:

#### Desktop Applications

ADAL.NET for desktop applications by default logs via `System.Diagnostics.Trace` class. You can add a trace listener to receive those logs. You can also control tracing using this method (e.g. change trace level or turn it off) using `AdalTrace.LegacyTraceSwitch`. 

The following example shows how to add a Console based listener and set trace level to `Information` (the default trace level is `Verbose`):

```
Trace.Listeners.Add(new ConsoleTraceListener());
AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Info;
```

You can achieve the same result by adding the following lines to your application's config file:

```
  <system.diagnostics>
    <sharedListeners>
      <add name="console" 
        type="System.Diagnostics.ConsoleTraceListener" 
        initializeData="false"/>
    </sharedListeners>
    <trace autoflush="true">
      <listeners>
        <add name="console" />
      </listeners>
    </trace>    
    <switches>
      <add name="ADALLegacySwitch" value="Info"/>
    </switches>
  </system.diagnostics>
```

If you would like to have more control over how tracing is done in ADAL, you can add a `TraceListener` to ADAL's dedicated `TraceSource` with name **"Microsoft.IdentityModel.Clients.ActiveDirectory"**. 

The following example shows how to write ADAL's traces to a text file using this method:

```
Stream logFile = File.Create("logFile.txt");
AdalTrace.TraceSource.Listeners.Add(new TextWriterTraceListener(logFile));
AdalTrace.TraceSource.Switch.Level = SourceLevels.Information;
```

You can achieve the same result by adding the following lines to your application's config file:

```
  <system.diagnostics>
    <trace autoflush="true"/>
    <sources>
      <source name="Microsoft.IdentityModel.Clients.ActiveDirectory" 
        switchName="sourceSwitch" 
        switchType="System.Diagnostics.SourceSwitch">
        <listeners>
          <add name="textListener" 
            type="System.Diagnostics.TextWriterTraceListener" 
            initializeData="logFile.txt"/>
          <remove name="Default" />
        </listeners>
      </source>
    </sources>    
    <switches>
      <add name="sourceSwitch" value="Information"/>
    </switches>
  </system.diagnostics>
``` 

#### Windows Store Applications

Tracing in ADAL for Windows Store is done via an instance of class `System.Diagnostics.Tracing.EventSource` with name **"Microsoft.IdentityModel.Clients.ActiveDirectory"**. You can define your own ```EventListener```, connect it to the event source and set your desired trace level. Here is an example:
```
var eventListener = new SampleEventListener();

class SampleEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft.IdentityModel.Clients.ActiveDirectory")
        {
            this.EnableEvents(eventSource, EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
	    ...
    }
}

```

There is also a default event listener which writes logs to a local file named **"AdalTraces.log"**. You can control the level of tracing to that event listener using the property ```AdalTrace.Level```. By default, trace level for this event listener is set to "None" and to enable tracing to this particular listener, you need to set the above property. This is an example:

```
AdalTrace.Level = AdalTraceLevel.Informational;
```

### Network Traces

You can use various tools to capture the HTTP traffic that ADAL generates.  This is most useful if you are familiar with the OAuth protocol or if you need to provide diagnostic information to Microsoft or other support channels.

Fiddler is the easiest HTTP tracing tool.  In order to be useful it is necessary to configure fiddler to record unencrypted SSL traffic.  

NOTE: Traces generated in this way may contain highly privileged information such as access tokens, usernames and passwords.  If you are using production accounts, do not share these traces with 3rd parties.  If you need to supply a trace to someone in order to get support, reproduce the issue with a temporary account with usernames and passwords that you don't mind sharing.

## Projects in this repo

### ADAL.PCL

* This project contains the source of ADAL Portable Library.

### ADAL.PCL.Desktop

* This project contains the source of the platform specific implementation for Windows desktop.

### ADAL.PCL.WinRT

* This project contains the source of the platform specific implementation for Windows Store.

### ADAL.PCL.CoreCLR

* This project contains the source of the platform specific implementation for Core CLR (still in preview).

### ADAL.PCL.iOS

* This project contains the source of the platform specific implementation for Xamarin iOS.

### ADAL.PCL.Android

* This project contains the source of the platform specific implementation for Xamarin Android.


## License

Copyright (c) Microsoft Corporation.  All rights reserved. Licensed under the MIT License (the "License"); 

## We Value and Adhere to the Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
