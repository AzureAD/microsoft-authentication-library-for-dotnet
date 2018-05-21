echo off
echo To Build Non WinRT, you will need the Developer Prompt/MSBuild for VS2017!
echo Usage: (Note: Building both the WinRT and non WinRT works best in a std. command-prompt)
echo param1 (configuration options): debug(d), release(r) .. default is debug
echo param2 (target options): build(b), restore (r), clean(c) .. default is build 
echo param3 (include sample apps): blank=components and utests, not blank=everything
echo Calling args: configuration: %1%, target: %2%, sample: %3%
echo .

set bconfig=debug
if '%1' NEQ '' (set bconfig=%1%)
if '%1' EQU 'd' (set bconfig=debug)
if '%1' EQU 'r' (set bconfig=release)
set btarget=build
if '%2' NEQ '' (set btarget=%2%)
if '%2' EQU 'b' (set btarget=build)
if '%2' EQU 'r' (set btarget=restore)
if '%2' EQU 'c' (set btarget=clean)
set bsampleapps=0
if '%3' NEQ '' (set bsampleapps=1)

echo Building using: target: %btarget%, configuration: %bconfig%, sample: %bsampleapps%

set msbuild15=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe
set msbuild14=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe

Rem -- WinRT
pushd adal
  Rem // msbuild14 doesn't support restore, thus for now only build is made available here...
  "%msbuild14%" ADAL.NET.VS2015.sln /t:build /p:configuration=%bconfig%
popd

Rem -- The rest
if %bsampleapps% EQU  1 (
  "%msbuild15%" msbuild Combined.NoWinRT.sln /t:%btarget% /p:configuration=%bconfig% 
 ) else (
   "%msbuild15%" CoreAndUTests.sln /t:%btarget% /p:configuration=%bconfig%
  )
)

if %IsMSBuild15DefinedHere% EQU 1 set msbuild15=
if %IsMSBuild14DefinedHere% EQU 1 set msbuild14=