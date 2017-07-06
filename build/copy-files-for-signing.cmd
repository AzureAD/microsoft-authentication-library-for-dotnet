@echo ==========================
@echo Cleaning signing staging folder
md %TO_SIGN_TARGET%
del /q %TO_SIGN_TARGET%\*.*

@echo ==========================
@echo Copying files to staging folder for signing
xcopy /y src\ADAL.PCL\%BUILD_PATH%\netstandard1.1\%LIBRARY_NAME%.* %TO_SIGN_TARGET%\
xcopy /y src\ADAL.PCL.Android\%BUILD_PATH%\%PLATFORM_SPECIFIC_LIBRARY_NAME%.* %TO_SIGN_TARGET%\Android\
xcopy /y src\ADAL.PCL.Desktop\%BUILD_PATH%\%PLATFORM_SPECIFIC_LIBRARY_NAME%.* %TO_SIGN_TARGET%\Desktop\
xcopy /y src\ADAL.PCL.CoreCLR\%BUILD_PATH%\netstandard1.3\%PLATFORM_SPECIFIC_LIBRARY_NAME%.* %TO_SIGN_TARGET%\CoreCLR\
xcopy /y src\ADAL.PCL.iOS\bin\iPhone\%BuildConfiguration%\%PLATFORM_SPECIFIC_LIBRARY_NAME%.* %TO_SIGN_TARGET%\iOS\
xcopy /y src\ADAL.PCL.WinRT\%BUILD_PATH%\%PLATFORM_SPECIFIC_LIBRARY_NAME%.* %TO_SIGN_TARGET%\WinRT\

@echo ==========================
@echo Cleaning signed files destination
md %IS_SIGNED_PATH%
del /q %IS_SIGNED_PATH%\*.*

@echo ====================================
@echo Copying files for signing DONE
@echo ====================================
exit 0