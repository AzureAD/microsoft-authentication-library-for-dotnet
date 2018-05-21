@echo off

echo .
echo Android SDK's required: 25, 26 and 27
pushd %ProgramFiles(x86)%\Android\android-sdk\platforms
echo . 
echo Available SDK's:
echo %ProgramFiles(x86)%\Android\android-sdk\platforms
echo .
echo Open SDK Manager:
echo  %ProgramFiles(x86)%\Android\android-sdk\SdkManager.exe (should be started in admin mode)
echo .
echo Validating available SDK's ...
Rem .. todo: need to validate sub version requirements as well....
if not exist android-25 echo Please install Android SDK v25 (Open SDK Manager from above location)
if not exist android-26 echo Please install Android SDK v26 (Open SDK Manager from above location)
if not exist android-27 echo Please install Android SDK v27 (Open SDK Manager from above location)
echo ... validation done.
popd

Rem JDK .... when something doesn't build... perhaps this is why. Update to the latest.
Rem e.g.: C:\Program Files\Java\jdk1.8.0_152
REM HKEY_LOCAL_MACHINE\SOFTWARE\JavaSoft
Rem HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\JavaSoft\Java Development Kit\1.8.0_152
Rem Java SE Development Kit 8u172: http://download.oracle.com/otn-pub/java/jdk/8u172-b11/a58eab1ec242421181065cdc37240b08/jdk-8u172-windows-x64.exe
Rem Java SE Development Kit 8u171: http://download.oracle.com/otn-pub/java/jdk/8u171-b11/512cd62ec5174c3487ac17c61aaa89e8/jdk-8u171-windows-x64.exe


Rem TODO: Rem error MSB4057: The target "GetBuiltProjectOutputRecursive" does not exist in the project.

Rem "E:\github\azure-activedirectory-library-for-dotnet\Combined.NoWinRT.sln" (default target) (1) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\adal\devApps\XFormsApp.Droid\XFormsApp.Droid.csproj" (default target) (12) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\adal\devApps\XFormsApp\XFormsApp.csproj" (GetBuiltProjectOutputRecursive target) (11:6) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\Microsoft.IdentityModel.Clients.ActiveDirectory.csproj" (GetBuiltProjectOutputRecursive target) (2:45) ->
Rem   E:\github\azure-activedirectory-library-for-dotnet\adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\Microsoft.IdentityModel.Clients.ActiveDirectory.csproj : error MSB4057: The target "GetBuiltProjectOutputRecursive" does not exist in the project.

Rem "E:\github\azure-activedirectory-library-for-dotnet\Combined.NoWinRT.sln" (default target) (1) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\adal\devApps\XFormsApp.Droid\XFormsApp.Droid.csproj" (default target) (12) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\adal\devApps\XFormsApp\XFormsApp.csproj" (GetBuiltProjectOutputRecursive target) (11:6) ->
Rem "E:\github\azure-activedirectory-library-for-dotnet\core\src\Microsoft.Identity.Core.csproj" (GetBuiltProjectOutputRecursive target) (3:43) ->
Rem   E:\github\azure-activedirectory-library-for-dotnet\core\src\Microsoft.Identity.Core.csproj : error MSB4057: The target "GetBuiltProjectOutputRecursive" does not exist in the project.

Rem MSBuild
Rem C:\Program Files (x86)\MSBuild\14.0\bin\amd64\

Rem validate that VS is installed:
Rem HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\DevDiv\vs\Servicing\14.0
Rem HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\DevDiv\vs\Servicing\15.0


Rem validate VS components:
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\GitHubPackage
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\NetCoreToolsPackage

Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\iOSPackage
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\AndroidPackage
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\XamarinAndroidPackage
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\XamarinIOSPackage
Rem HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0_Config\InstalledProducts\XamarinShellPackage


Rem Validate that Xamarin is installed
Rem HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Xamarin


Rem Validate that the right versions of .net target packages are installed

