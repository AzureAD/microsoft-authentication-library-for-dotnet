---
name: Bug report
about: Create a report to help us improve

---

---
name: Bug report
about: Create a report to help us improve

---

**Which Version of MSAL are you using ?**
Note that to get help, you need to run the latest preview or non-preview version
For ADAL, please log issues to https://github.com/AzureAD/azure-activedirectory-library-for-dotnet
<!-- E.g. MSAL 2.0.0-preview -->

**Which platform has the issue?**
<!-- Ex: net45, netcore 2.1, UWP, xamarin android, xamarin iOS -->

**What authentication flow has the issue?**
* Desktop 
    * [ ] Interactive
    * [ ] Integrated Windows Auth
    * [ ] Username / Password
    * [ ] Device code flow (browserless)
* Mobile
    * [ ] Xamarin.iOS
    * [ ] Xamarin.Android
    * [ ] UWP
* Web App
    * [ ] Authorization code
    * [ ] OBO
* Web API
    * [ ] OBO
* Daemon App
    * [ ] Client credentials
    
Other? - please describe;

**What is the identity provider ?**
* [ ] Azure AD
* [ ] Azure AD B2C

If B2C, what social identity did you use?

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
