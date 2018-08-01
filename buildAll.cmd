echo off
echo To Build Non WinRT, you will need the Developer Prompt/MSBuild for VS2017!

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

echo Restoring debug all... 
START /W "Restoring debug all..." CMD /c buildVS2017.cmd %bconfig% r 1
echo Building debug... 
START /W "Building debug..." CMD /c buildVS2017.cmd %bconfig% b
echo Strongnaming... 
START /W "Strongnaming..." CMD /c strongname.cmd %bconfig%
echo Building debug all... 
START /W "Building debug all..." CMD /c buildVS2017.cmd %bconfig% b 1
echo Done.

