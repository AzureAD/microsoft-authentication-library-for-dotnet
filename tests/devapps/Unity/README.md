### Unity UWP dev app

Using MSAL.NET in a Unity app build for UWP.

#### Prerequisites

1. Create a Unity ID. ([https://unity.com/](https://unity.com/))
2. Via Visual Studio Installer, install **Game development with Unity** workload.
3. Open Unity Hub, which was installed in the previous step, and sign into it.
4. Install a Unity version.
   1. In the left side menu, select **Installs**. 
   2. Click **Add** button.
   3. In the **Add Unity Version** window, select a Unity version (Recommended or latest LTS).
   4. Click **Next**.
   5. Check the following modules and click **Done**.
      - Universal Windows Platform Build Support
      - Windows Build Support (IL2CPP)
   6. After the installation completes, this version will show up in the **Installs** section.<br/>
    <img src="https://user-images.githubusercontent.com/34331512/113494681-72b35b80-949f-11eb-8d48-4f247f8c8624.png" width="500" />

#### Opening the project
1.  In Unity Hub, click on **Projects** in the left side menu.
2.  Click **Add** button.
3.  Navigate to the `..\microsoft-authentication-library-for-dotnet\tests\devapps\Unity` directory.
4. Click **Select Folder**.
5. **Projects** section will now show a row entry for the Unity dev app folder.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494723-f8cfa200-949f-11eb-8312-af0f1a3d59a8.png" width="500" />
6. Click on the project.
7. Unity will open the project and start resolving and importing dependencies. (This may take a few minutes.)

#### About the project
This app creates a `PublicClientApplication`, then calls `AcquireTokenWithDeviceCode` and `AcquireTokenSilent`. The device code and progress are printed on the screen.

In the **Project** tab, select **Assets** > **Scenes**. Double-click on **MSALScene** to load it on the center screen. Scenes are like modules or assets that contain content that is part of the app.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494754-421ff180-94a0-11eb-9337-15288832174b.png" width="300" />

Pressing the play button at the top of the screen will run the app. However, this will run the app on the native framework (ex. Windows x64).<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494772-6d0a4580-94a0-11eb-8cc1-f6ced5a5dfea.png" width="300" />

In the top left side, **Hierarchy** tab lists the components that are used in the app and are included in this scene. Main Camera is the default component. Canvas component has a DeviceCodeText text field and a scroll view where progress is printed.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494792-95923f80-94a0-11eb-9eaf-50ee5824061e.png" width="300" />

Select **Canvas** component. The **Inspector** tab on the right will populate with the component properties. At the bottom, it shows that there's **MSAL Script** that's linked to this component. Scripts are behaviors that run along with this component.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494817-e7d36080-94a0-11eb-9157-d6a4774e38c9.png" width="300" />

`MSALScript.cs` is located in `..Unity\Assets\Scripts` folder. Open `Assembly-CSharp.csproj` with Visual Studio to edit the script. The script file must inherit from `MonoBehavior`, that's how Unity knows to execute it. Script files can have specific methods that Unity knows to call at specific frequencies. In this case, `Start` method gets called once when component is first executed and `Update` gets called once per frame. The `Start` method invokes the behavior to login using MSAL.

If `Assembly-CSharp.csproj` was not generated, go to **Edit** > **Preferences** > **External Tools**. Check **Embedded packages** and **Local packages**.

MSAL dependency itself has to be placed in `..\Unity\Assets\Plugins`. It has to be a .NET Standard DLL.

`..\Unity\Assets\link.xml` has an entry `<assembly fullname="Microsoft.Identity.Client" preserve="all"/>` which prevents MSAL code from being stripped when Unity builds and optimizes the project.

#### Building the project
##### Update Player settings:
1. Go to **Edit** > **Project Settings** > **Player** > **Universal Windows Platform settings** tab.
2. In **Publishing Settings** tab, under **Capabilities**, check **InternetClient**, **InternetClientServer**, and **PrivateNetworkClientServer**.

##### Build:
<img src="https://user-images.githubusercontent.com/34331512/113494838-2ec15600-94a1-11eb-9a39-283c310fb1f0.png" width="400" />

1. Go to **File** > **Build Settings**.
2. Verify **Universal Windows Platform** is selected. Click **Switch Platform** at the bottom if needed.
3. Verify **Minimum Platform Version** is  at least `10.017763.0`. Older versions will download a lot more Windows dependencies.
4. Verify **Build configuration** is `Release`.
5. In order to be able to debug the scripts when the app is running, the following options should be checked: **Copy PDB files**, **Development Build**, and **Script Debugging**.
6. Click **Build**.
7. In the dialog create an empty folder (ex. UWP) and then select it.
8. Unity will now take some time to build the project (using IL2CPP plugin) and generate a C++ project in the selected folder.
   - For now Unity does not automatically create a UWP app - these steps have to be done manually.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494891-a3949000-94a1-11eb-8708-c8dd98989647.png" width="150" />
9. Open the UnityTestApp.sln C++ project with Visual Studio.
   - Visual Studio might require to install some missing C++ frameworks at this point.
10. Change the build configuration to `Release` and `x86` or `x64`.
11. Right-click on the **UnityTestApp (Universal Windows)** project in the **Solution Explorer** and select **Deploy**.
    - It's advised to clean the contents of UWP folder between rebuilding the project from Unity.

#### Running the app
1. From the **Start** open **UnityTestApp**.<br/>
<img src="https://user-images.githubusercontent.com/34331512/113494910-caeb5d00-94a1-11eb-9722-0ae8963bd566.png" width="200" />
2. Go through the device code flow.
3. MSAL logs and Unity logs are written to `%AppData%\Local\Packages\UnityTestApp_pzq3xp76mxafg\TempState\UnityPlayer.log`.
