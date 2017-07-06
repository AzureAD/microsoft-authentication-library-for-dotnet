set BUILD_PATH=bin\%BuildConfiguration%
set TO_SIGN_TARGET=ToSign
set IS_SIGNED_PATH=Signed
set TO_PACK_TARGET=ToPack
set LIBRARY_NAME=Microsoft.IdentityModel.Clients.ActiveDirectory
set PLATFORM_SPECIFIC_LIBRARY_NAME=%LIBRARY_NAME%.Platform

@echo ==========================
@echo Setting build agent variables

@echo ##vso[task.setvariable variable=BUILD_PATH]%BUILD_PATH%
@echo ##vso[task.setvariable variable=TO_SIGN_TARGET]%TO_SIGN_TARGET%
@echo ##vso[task.setvariable variable=IS_SIGNED_PATH]%IS_SIGNED_PATH%
@echo ##vso[task.setvariable variable=TO_PACK_TARGET]%TO_PACK_TARGET%
@echo ##vso[task.setvariable variable=LIBRARY_NAME]%LIBRARY_NAME%
@echo ##vso[task.setvariable variable=PLATFORM_SPECIFIC_LIBRARY_NAME]%PLATFORM_SPECIFIC_LIBRARY_NAME%

@echo ##vso[task.setvariable variable=NUSPEC_TARGET]%TO_PACK_TARGET%\%LIBRARY_NAME%.nuspec

@echo ==========================
@echo Cleaning signing staging folder
md %TO_SIGN_TARGET%
del /q %TO_SIGN_TARGET%\*.*

@echo ==========================
@echo Cleaning signed files destination
md %IS_SIGNED_PATH%
del /q %IS_SIGNED_PATH%\*.*

@echo ==========================
@echo Cleaning packaging staging folder
md %TO_PACK_TARGET%
del /q %TO_PACK_TARGET%\*.*

@echo ==========================
@echo Copying .nuspec file
copy /y build\%LIBRARY_NAME%.nuspec %TO_PACK_TARGET%