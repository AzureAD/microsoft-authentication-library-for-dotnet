---
name: Bug report
about: Please do NOT file bugs without filling in this form.
title: '[Bug] '
labels: ''
assignees: ''

---

**Logs and network traces**
Without logs or traces, it is unlikely that the team can investigate your issue. Capturing logs and network traces is described in [Logging wiki](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging).

**Which version of MSAL.NET are you using?**
<!-- Ex: MSAL.NET 4.30.0 -->

**Platform**
<!-- Ex: .NET 4.5, .NET Core, UWP, Xamarin Android, Xamarin iOS -->

**What authentication flow has the issue?**
* Desktop / Mobile
    * [ ] Interactive
    * [ ] Integrated Windows Authentication
    * [ ] Username Password
    * [ ] Device code flow (browserless)
* Web app 
    * [ ] Authorization code
    * [ ] On-Behalf-Of
* Daemon app 
    * [ ] Service to Service calls

Other?
<!-- Please describe here -->

**Is this a new or existing app?**
<!-- Ex:
a. The app is in production, and I have upgraded to a new version of MSAL.
b. The app is in production, I haven't upgraded MSAL, but started seeing this issue.
c. This is a new app or experiment.
-->

**Repro**

```csharp
var your = (code) => here;
```

**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. exception is thrown, UI freezes.  

**Possible solution**
<!--- Only if you have suggestions on a fix for the bug -->

**Additional context / logs / screenshots**
Add any other context about the problem here, such as logs and screenshots. 
