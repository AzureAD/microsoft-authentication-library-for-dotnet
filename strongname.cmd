@echo off
REM ******************************************
REM ** 
REM ** Locate folders and disable strongname 
REM ** 
REM ******************************************

echo options: debug (default), release
echo args: %1%

set bconfig=debug
if '%1' NEQ '' (set bconfig=%1%)
if '%1' EQU 'd' (set bconfig=debug)

Rem echo config: %gotoFolder%

Rem ADAL and Core
set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\monoandroid7
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\netstandard1.1
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\netstandard1.3
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\xamarin.ios10
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\uap10.0
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=adal\src\Microsoft.IdentityModel.Clients.ActiveDirectory\bin\%bconfig%\net45
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

Rem MSAL

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\monoandroid7
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\netstandard1.1
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\netstandard1.3
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\xamarin.ios10
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\uap10.0
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\net45
pushd %gotoFolder%
sn.exe -Vr Microsoft.IdentityModel.Clients.ActiveDirectory.dll
sn.exe -Vr Microsoft.Identity.Core.dll
popd

delete ....

set gotoFolder=msal\src\Microsoft.Identity.Client\bin\%bconfig%\net45
pushd %gotoFolder%
sn.exe -Vr Microsoft.Identity.Client.dll
popd
