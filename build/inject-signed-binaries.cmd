set BUILD_PATH=bin\%BUILD_BUILDCONFIGURATION%
set SIGNED_TARGET=../a/SignedProductBinaries
set NUGET_TARGET=../a
set NUGET_TEMP_TARGET=NuGet-Temp
set LIBRARY_NAME=Microsoft.Identity.Client
setlocal ENABLEDELAYEDEXPANSION

md %NUGET_TEMP_TARGET%

@echo ==========================
@echo Moving symbols.nupkg file to NuGet path...
	mv *.symbols.nupkg %NUGET_TARGET%\

@echo ==========================
@echo Decompressing nuget...
for %%i in (*.nupkg) do (
	set NUGET_NAME=%%~ni
	unzip %%~ni.nupkg -d %NUGET_TEMP_TARGET%\
)

@echo Saved nuget name as %NUGET_NAME%
@echo ==========================
@echo Copying dlls from %NUGET_TEMP_TARGET%\lib to %TO_SIGN_TARGET%...
	ROBOCOPY %NUGET_TEMP_TARGET%\lib %TO_SIGN_TARGET%\ *.dll /E

@echo ==========================
@echo Copying signed files to %NUGET_TEMP_TARGET%\lib ...
ROBOCOPY %SIGNED_TARGET%\ %NUGET_TEMP_TARGET%\lib\ *.dll /E


del %NUGET_TARGET%\*.CodeAnalysisLog.xml /s
del %NUGET_TARGET%\*.lastcodeanalysissucceeded /s


@echo ==========================
@echo Creating NuGet Package....
cd %NUGET_TEMP_TARGET%
7z a ..\%NUGET_TARGET%\%NUGET_NAME%.zip -r *
cd ..
mv %NUGET_TARGET%\%NUGET_NAME%.zip %NUGET_TARGET%\%NUGET_NAME%.nupkg

@echo ====================================
@echo MSAL-NET Postbuild script complete with no errors
@echo ====================================
exit 0

:ERROREXIT
@echo **********************************************
@echo MSAL-NET Build failed to SIGN properly
@echo MSAL-NET Postbuild Script Complete WITH ERRORS
@echo **********************************************
exit 1