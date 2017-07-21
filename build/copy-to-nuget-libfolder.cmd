set SOURCE=%1
set TARGET=%2
xcopy /y  %SIGNED%\%SOURCE%\* %NUGET_TARGET%\lib\%TARGET%\
xcopy /y %TO_SIGN_TARGET%\%SOURCE%\*.pdb %NUGET_TARGET%\lib\%TARGET%\
xcopy /y %TO_SIGN_TARGET%\%SOURCE%\*.xml %NUGET_TARGET%\lib\%TARGET%\