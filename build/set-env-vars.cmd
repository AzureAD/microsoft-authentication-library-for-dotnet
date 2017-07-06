set BUILD_PATH=bin\%BuildConfiguration%
set TO_SIGN_TARGET=ToSign
set IS_SIGNED_PATH=Signed
set TO_PACK_TARGET=ToPack
set LIBRARY_NAME=Microsoft.IdentityModel.Clients.ActiveDirectory
set PLATFORM_SPECIFIC_LIBRARY_NAME=%LIBRARY_NAME%.Platform

@echo ##vso[task.setvariable variable=BUILD_PATH]%BUILD_PATH%
@echo ##vso[task.setvariable variable=TO_SIGN_TARGET]%TO_SIGN_TARGET%
@echo ##vso[task.setvariable variable=IS_SIGNED_PATH]%IS_SIGNED_PATH%
@echo ##vso[task.setvariable variable=TO_PACK_TARGET]%TO_PACK_TARGET%
@echo ##vso[task.setvariable variable=LIBRARY_NAME]%LIBRARY_NAME%
@echo ##vso[task.setvariable variable=PLATFORM_SPECIFIC_LIBRARY_NAME]%PLATFORM_SPECIFIC_LIBRARY_NAME%

@echo ##vso[task.setvariable variable=NUSPEC_TARGET]%TO_PACK_TARGET%\%LIBRARY_NAME%.nuspec