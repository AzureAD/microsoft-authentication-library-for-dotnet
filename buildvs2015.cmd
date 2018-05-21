echo off
echo To Build WinRT, you will need the Developer Prompt/MSBuild for VS2015!
Rem echo msbuild ADAL.NET.VS2015.sln /t:restore /p:configuration=debug /p:configuration=release
Rem echo msbuild ADAL.NET.VS2015.sln /t:build /p:configuration=debug /p:configuration=release
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

Rem -- WinRT
REM     set msbuild14=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe
Rem if EXIST "%msbuild14%" (
  pushd adal
  Rem // msbuild14 doesn't support restore, thus for now only build is made available here...
  Rem "%msbuild14%" ADAL.NET.VS2015.sln /t:build /p:configuration=%bconfig%
  msbuild ADAL.NET.VS2015.sln /t:build /p:configuration=%bconfig%
  popd
Rem )