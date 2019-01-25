---
name: Bug report
about: Please do NOT file bugs without filling in this form.
---

**Which Version of MSAL are you using ?**
Note that to get help, you need to run the latest version. Preview version are also ok. 
For ADAL, please log issues to https://github.com/AzureAD/azure-activedirectory-library-for-dotnet
<!-- E.g. MSAL 2.6.2, MSAL 3.0.0-preview -->

**Platform**
<!-- Ex: net45, netcore, UWP, xamarin android, xamarin iOS -->

**What authentication flow has the issue?**
* Desktop / Mobile
    * [ ] Interactive
    * [ ] Integrated Windows Auth
    * [ ] Username Password
    * [ ] Device code flow (browserless)
* Web App
    * [ ] Authorization code
    * [ ] OBO
* Web API
    * [ ] OBO

Other? - please describe;

**Is this a new or existing app?**
<!-- Ex:
a. The app is in production, and I have upgraded to a new version of MSAL
b. The app is in production, I haven't upgraded MSAL, but started seeing this issue
c. This is a new app or experiment
-->

**Repro**

```csharp
var your = (code) => here;
```

**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. exception is thrown, UI freezes  

**Possible Solution**
<!--- Only if you have suggestions on a fix for the bug -->

**Additional context/ Logs / Screenshots**
Add any other context about the problem here, such as logs and screebshots. Logging is described at https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging
