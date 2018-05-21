@echo off
echo Info:
echo To Build Non WinRT, you will need the Developer Prompt/MSBuild for VS2017!
echo To Build WinRT, you will need the Developer Prompt/MSBuild for VS2015 (and point to the vs2015 build script)!
echo .
echo Usage:
echo   %%1  (configuration options): debug(d), release(r) .. default is debug
echo   %%2  (target options)       : build(b), restore (r), clean(c) .. default is build 
echo   %%3  (include sample apps)  : blank=components and utests, not blank=everything
echo Calling args: configuration: %1%, target: %2%, sample: %3%

if '%1' EQU 'h' (goto :eof) 

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

if %bsampleapps% EQU  1 (
    msbuild Combined.NoWinRT.sln /m /t:%btarget% /p:configuration=%bconfig% 
 ) else (
    msbuild CoreAndUTests.sln /m /t:%btarget% /p:configuration=%bconfig%
  )
)