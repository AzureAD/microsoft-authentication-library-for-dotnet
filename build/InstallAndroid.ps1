$url = "https://dl.google.com/android/repository/commandlinetools-win-7583922_latest.zip"
$fileName = "AndroidTools.zip"
$source = "C:\Downloads\$fileName"
$destination = "C:\Downloads\AndroidSdkTools"
$androidSdk = "C:\Program Files (x86)\Android\android-sdk\"

#$ErrorActionPreference = "Stop"

if (Test-Path "$PSScriptRoot\win-installer-helper.psm1") 
{
    Import-Module "$PSScriptRoot\win-installer-helper.psm1" -DisableNameChecking
} elseif (Test-Path "$PSScriptRoot\..\..\Helpers\win-installer-helper.psm1") 
{
    Import-Module "$PSScriptRoot\..\..\Helpers\win-installer-helper.psm1" -DisableNameChecking
}

mkdir -Path C:\Downloads\ -Force
mkdir -Path "C:\Program Files (x86)\Android\android-sdk\licenses" -Force

Get-File -Url $url -FileName $fileName

echo "Expanding"
Expand-Archive -LiteralPath "$source" -DestinationPath "C:\Downloads\AndroidSdkTools" -Force

dir "C:\Program Files (x86)\Android\android-sdk\licenses"

echo "Installing licenses" #This is a workaround as it is not possible to accept licences during the build run. If there is an issue, simply accept the licenses locally and reupload them.
Copy-Item -Path microsoft-authentication-library-for-dotnet\build\AndroidSdkLicenses\* -Destination "C:\Program Files (x86)\Android\android-sdk\licenses" -Force

dir "C:\Program Files (x86)\Android\android-sdk\licenses"

echo "installing android"
C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager --licenses --sdk_root="C:\Program Files (x86)\Android\android-sdk"
echo y y y y y y y |C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager "platforms;android-29" --sdk_root="C:\Program Files (x86)\Android\android-sdk"
