set BUILD_PATH=bin\%BuildConfiguration%


set TO_PACK_TARGET=ToPack
set LIBRARY_NAME=Microsoft.IdentityModel.Clients.ActiveDirectory
set COPY_TO_NUGET_LIBFOLDER=build/copy-to-nuget-libfolder.cmd

set PLATFORM_SPECIFIC_LIBRARY_NAME=%LIBRARY_NAME%.Platform

@echo ==========================
@echo Cleaning packaging staging folder
md %TO_PACK_TARGET%
del /q %TO_PACK_TARGET%\*.*

@echo ==========================
@echo Copying nuspec file...
copy /y build\%LIBRARY_NAME%.nuspec %TO_PACK_TARGET%
@echo ##vso[task.setvariable variable=TO_PACK_TARGET]%TO_PACK_TARGET%
@echo ##vso[task.setvariable variable=NUSPEC_TARGET]%TO_PACK_TARGET%\%LIBRARY_NAME%.nuspec

@echo ==========================
@echo Copying source files for NuGet symbols package
xcopy /ys src\* NuGet\src\src\

@echo ==========================
@echo Copying signed files to the NuGet lib path
set PORTABLE_LIB=portable-net45+win

md %TO_PACK_TARGET%\lib\%PORTABLE_LIB%\
copy /y  %SIGNED%\*.dll "%NUGET_TARGET%\lib\%PORTABLE_LIB%\"
copy /y %TO_SIGN_TARGET%\*.pdb "%NUGET_TARGET%\lib\%PORTABLE_LIB%\"
copy /y %TO_SIGN_TARGET%\*.xml "%NUGET_TARGET%\lib\%PORTABLE_LIB%\"

call %COPY_TO_NUGET_LIBFOLDER% Desktop net45
call %COPY_TO_NUGET_LIBFOLDER% CoreCLR netstandard1.3
call %COPY_TO_NUGET_LIBFOLDER% WinRT netcore45
call %COPY_TO_NUGET_LIBFOLDER% Android MonoAndroid10
call %COPY_TO_NUGET_LIBFOLDER% iOS Xamarin.iOS10

copy /y "%TO_PACK_TARGET%\lib\%PORTABLE_LIB%\*.*" %TO_PACK_TARGET%\lib\net45\
copy /y "%TO_PACK_TARGET%\lib\%PORTABLE_LIB%\*.*" %TO_PACK_TARGET%\lib\netstandard1.3
copy /y "%TO_PACK_TARGET%\lib\%PORTABLE_LIB%\*.*" %TO_PACK_TARGET%\lib\netcore45\
copy /y "%TO_PACK_TARGET%\lib\%PORTABLE_LIB%\*.*" %TO_PACK_TARGET%\lib\MonoAndroid10\
copy /y "%TO_PACK_TARGET%\lib\%PORTABLE_LIB%\*.*" %TO_PACK_TARGET%\lib\Xamarin.iOS10\

del %TO_PACK_TARGET%\*.CodeAnalysisLog.xml /s
del %TO_PACK_TARGET%\*.lastcodeanalysissucceeded /s

@echo ====================================
@echo Copying files for packaging DONE
@echo ====================================
exit 0