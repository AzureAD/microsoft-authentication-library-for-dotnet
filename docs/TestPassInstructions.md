# Manual Test Pass Instructions

## Spreadsheet: [MSAL.NET Release Signoff Spreadsheet](https://microsoft.sharepoint.com/:x:/r/teams/aad/devex/_layouts/15/doc2.aspx?sourcedoc=%7B88CE7D1B-889A-4A32-93B9-942DD0FB7D8C%7D&file=dotNet%20MSAL%20Releases.xlsx)

---
## Web App calls graph on behalf of user
* Insert first step here
* Insert second step here

---
## Web API call graph on behalf of user
---
## Web App calls V1 Web API on behalf of user
---
## Web App calls V2 Web API on behalf of AAD user
---
## Web App calls V2 Web API on behalf of MSA user
---
## An ASP.NET Core web app with Azure AD B2C
---
## Enumerating through tenants - sample coming - 
---
## Domain/AAD Joined - Interactive Auth to handle Device Auth
---
## Hidden webview Auth with Prompt=never
---
## Application signing in users with B2C
---
## Foci happy path (2 Foci apps sharing the cache, A interactive, B silent). See OneNote for clientIds.
---
## Foci global sign out  

Application: FociTestApp

**TODO: add link to internal site with client ids**

Foci happy path test
* Start with empty cache
* Login to Office interactively
* Close app.
* Login to Teams silently.

Foci global sign-out test
* Sign in into Office
* Get Accounts using Teams – you should see the Office account
* Sign out of one Teams
* Get Accounts with Office – no accounts present

**iOS / Android have FOCI disabled.**

---
## Multi-cloud for 1st parties 

Application: MultiCloudTestApp

**TODO: add link to internal site with client ids**

The list of users you can login with is

|Cloud|UPN|KeyVault|
|-----|---|-------|
|DE|DEIDLAB@blfmsidlab1.onmicrosoft.de|https://aka.ms/GetLabUserSecret?Secret=blfmsidlab1|
|CN|idlab@mncmsidlab1.partner.onmschina.cn|https://aka.ms/GetLabUserSecret?Secret=mncmsidlab1|
|Public|IDLAB@msidlab4.onmicrosoft.com|https://aka.ms/GetLabUserSecret?Secret=msidlab4|

Happy path test case:
* Sign in with DE user
* Sign in with CN user
* Sign in wih public cloud user
* Silent sign in with CN, DE and public cloud user (in a different order)
* Remove Accounts and verify each removal
---
## Silent Auth by using Refresh Token (AT is expired)
---
## Silent Auth for guest tenant authority
---
## Silent Auth with Refresh Token Rejected
---
## Silent Auth with Valid AT in Cache
---
## Silent Auth with ForceRefresh=true
---
## Silent Auth with ForceRefresh=true with B2C account
---
## Interactive Auth w/Prompt (force, consent, login, no_prompt) w/login hint
---
## Interactive Auth w/Prompt (select_account, consent, login, no_prompt) no login hint
---
## .NET Xamarin AcquireToken interactive auth with system browser
---
## Refresh token flow, no Broker
---
## Acquire Token w/Chrome disabled, launch alternate browser w/out custom tabs
---
## Acquire Token w/Chrome disabled and no browser installed on device
---
## Acquire Token w/Chrome disabled and launch alternate browser w/custom tabs
---
## Acquire Token w/Chrome disabled on Android device use IsSystemWebviewAvailable() helper method
---
## .NET AcquireToken with embedded webview with Xamarin
---
## Azure AD B2C with b2clogin.com 
---
## Azure AD B2C with b2clogin.com with prompt_none 
---
## Azure AD B2C with login.microsoftonline.com
---
## Azure AD B2C with custom domain
---
## Azure AD B2C ROPC
---
## .NET Embedded auth with Claims challenge - MFA
---
## Token Cache works cross platform - xamarin iOS to native iOS
---
## Token Cache works cross platform - C++ to .net
---
## Interactive auth. Restart App. Silent Auth. Do NOT use EQUIVALENT.
---
## Set custom serialization. Interactive Auth. Restart App. Silent Auth
---
## Set custom serialization. U/P Auth. Restart App. Silent Auth.
---
## ADAL V3 Token Cache is compatibile with MSAL token cache
---
## ADAL V4 Token Cache is compatibile with MSAL token cache (See comment).
---
## Token Cache supports authority migration
---
## Interactive Auth from an Xforms app - UWP, Android, iOS; Some code must be in common
---
## Token cache is backwards compatible (MSAL v_current to MSAL v_next). This includes ### platforms with custom token cache serialization.
---
## Integrated Auth, federated / managed user,  empty username.
---
## Username/Passsword, with username. All users (AAD, ADFS v4 Fed & NonFed, ADFSv3 Fed & NonFed, ADFV2 Fed)
---
## "Interactive auth, clear cache, interactive, silent. 
---
## AAD, ADFSv2, ADFSv3 (Fed, NonFed), ADSFv4 (Fed, NonFed)"
---
## DeviceCode registration followed by AcquireToken
---
## Verify that the 2 packages and their contents are signed
---
## Check the associated release build for warnings and inconclusive steps
---
## Smoke test a sample
---
