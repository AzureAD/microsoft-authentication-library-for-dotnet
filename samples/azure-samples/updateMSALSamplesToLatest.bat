echo OFF
set Usedlibrary="Microsoft.Identity.Client"
set UsedLibraryVersion="2.7.0"
cd %~dp0
echo Updating v2-wabapp-msgraph
set repo="https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2"
set folder="v2-wabapp-msgraph"
set solution="WebApp-OpenIDConnect-DotNet.sln"
git clone %repo% %folder%
cd %folder%
git checkout aspnetcore2-2-signInAndCallGraph
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-webapp-admin-restricted-scopes
set repo="https://github.com/azure-samples/active-directory-dotnet-admin-restricted-scopes-v2"
set folder="v2-webapp-admin-restricted-scopes"
set solution="GroupManager.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-wpf-msgraph
set repo="https://github.com/azure-samples/active-directory-dotnet-desktop-msgraph-v2"
set folder="v2-wpf-msgraph"
set solution="active-directory-wpf-msgraph-v2.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-wpf-webapi
set repo="https://github.com/azure-samples/active-directory-dotnet-native-aspnetcore-v2"
set folder="v2-wpf-webapi"
set solution="2. Web API now calls Microsoft Graph\Web-API-Calls-Graph.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-desktop-iwa
set repo="https://github.com/azure-samples/active-directory-dotnet-iwa-v2"
set folder="v2-desktop-iwa"
set solution="iwa-console\iwa-console.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-desktop-up
set repo="https://github.com/azure-samples/active-directory-dotnetcore-console-up-v2"
set folder="v2-desktop-up"
set solution="up-console.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-uwp
set repo="https://github.com/azure-samples/active-directory-dotnet-native-uwp-v2"
set folder="v2-uwp"
set solution="active-directory-dotnet-native-uwp-v2.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-xamarin
set repo="https://github.com/azure-samples/active-directory-xamarin-native-v2"
set folder="v2-xamarin"
set solution="active-directory-Xamarin-native-v2.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-xamarin
set repo="https://github.com/azure-samples/active-directory-xamarin-native-v2"
set folder="v2-xamarin"
set solution="active-directory-Xamarin-native-v2.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-daemon-console
set repo="https://github.com/azure-samples/active-directory-dotnetcore-daemon-v2"
set folder="v2-daemon-console"
set solution="daemon-console.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-daemon-web
set repo="https://github.com/azure-samples/active-directory-dotnet-daemon-v2"
set folder="v2-daemon-web"
set solution="UserSync.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
echo Updating v2-browserless
set repo="https://github.com/azure-samples/active-directory-dotnetcore-devicecodeflow-v2"
set folder="v2-browserless"
set solution="device-code-flow-console.sln"
git clone %repo% %folder%
cd %folder%
git checkout -b cd/updateLatestMSAL
nuget restore %solution%
msbuild /t:restore %solution%
nuget.exe update %solution% -Id %Usedlibrary% -Version %UsedLibraryVersion%
msbuild %solution%
if ERRORLEVEL 1 (echo "%folder% build failed" >> ..\failed.txt ) else (
git add *
git commit -m "Updating the solution to MSAL %UsedLibraryVersion%"
git push --set-upstream origin cd/updateLatestMSAL 2>> ..\todo.txt
)
ver > nul

cd %~dp0
