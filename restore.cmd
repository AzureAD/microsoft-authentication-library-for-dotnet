echo off
echo To Restore Non WinRT, you will need the Developer Prompt/MSBuild for VS2017!
echo Usage: restore.cmd 1: r (release), d (debug, default)   2: <blank> (the core libraries), 1 (everything)
set bconfig=d
if '%1' NEQ '' (set bconfig=%1%)
REM CALL buildVS2017.cmd %bconfig% r %2

REM echo Restoring Debug...
REM START /W "Restoring debug..." CMD /buildVS2017.cmd d r
REM echo Restoring Release... 
REM START /W "Restoring release..." CMD /c buildVS2017.cmd r r 

echo Restoring debug all... 
START /W "Restoring debug all..." CMD /c buildVS2017.cmd d r 1
echo Restoring Release all... 
START /W "Restoring release all..." CMD /c buildVS2017.cmd r r 1

echo Restoring WinRT packages should be done from the VS2015 project