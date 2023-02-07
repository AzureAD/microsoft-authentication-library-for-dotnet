# Sample WPF App

This app is a minimum WPF app to reproduce the error 'Unable to load DLL 'msalruntime' when trying to follow [this doc](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/wam) to use the new version (4.49.1-preview) of `Microsoft.Identity.Client.Broker`.

## Steps to reproduce

1. Clone this repo.
2. Build the solution to restore all nuget packages.
3. Start the project "WPF" project.
4. You will be displayed with a window with a button and a text box
5. Click on the `Acquire Token Interactively` button

Result : You should now see the Windows Web Account Manager UI showing up and prompting you to sign in to the app. You can use the MS Credentials or use the following user details to sign-in. But sign in is actually not required. The WAM (Web Account Manager) UI is from the Native dll (msalruntime.dll) packaged along with `Microsoft.Identity.Client.Broker`

Identity Lab Account User Name : idlab@msidlab4.onmicrosoft.com 

Identity Lab Account Password : https://msidlab.com/api/LabSecret?&Secret=msidlab4 (Please do not share this secret or add it to your response)

7. Now set the `WPF.Package` as the start up project 
8. Start the project "WPF.Package" project.

```
Microsoft.Identity.Client.MsalClientException: 'Unable to load DLL 'msalruntime': The specified module could not be found. (Exception from HRESULT: 0x8007007E) See https://aka.ms/msal-net-wam#troubleshooting'

Inner Exception
DllNotFoundException: Unable to load DLL 'msalruntime_x86': The specified module could not be found. (Exception from HRESULT: 0x8007007E)
```

![image](https://user-images.githubusercontent.com/90415114/213346100-e43ca2ad-8822-4628-a272-d248c14a3836.png)

***Alternative Repro :***

You can also deploy the package and launch it, this will crash the app. And App logs will show the same error 

![image](https://user-images.githubusercontent.com/90415114/213347329-1bca4b7d-a574-4990-aa19-be8a8f84a8f8.png)


***Investigation so far:***

WPF project gets the runtimes folder and it's contents (msalruntime native dlls)

![image](https://user-images.githubusercontent.com/90415114/213346524-2599a396-32af-4262-83bf-99165ed0e309.png)

![image](https://user-images.githubusercontent.com/90415114/213346635-469deb26-1db2-4ebc-aaf2-6de0462799e5.png)

AppX also contains the runtimes folder and it's contents 

![image](https://user-images.githubusercontent.com/90415114/213350082-52ead864-a436-4f42-abce-f4633262d381.png)

***Fix we have done :***

- Manually copy the dlls from the runtimes folder and place them in the executing assembly directory 
- deploy the package 
- Launch the app. It will work as expected 

![image](https://user-images.githubusercontent.com/90415114/213348405-42b5bce6-648e-47a0-98a8-69555375c888.png)

