$url = "https://dl.google.com/android/repository/commandlinetools-win-7583922_latest.zip" #Android SDK Tools from https://developer.android.com/studio#downloads
$fileName = "AndroidTools.zip"
$source = "C:\Downloads\$fileName"
$destination = "C:\Downloads\AndroidSdkTools"
$androidSdk = "C:\Program Files (x86)\Android\android-sdk"
$androidSdkVersion = "platforms;android-29"

#$ErrorActionPreference = "Stop"

if (Test-Path "$PSScriptRoot\win-installer-helper.psm1") 
{
    Import-Module "$PSScriptRoot\win-installer-helper.psm1" -DisableNameChecking
} elseif (Test-Path "$PSScriptRoot\..\..\Helpers\win-installer-helper.psm1") 
{
    Import-Module "$PSScriptRoot\..\..\Helpers\win-installer-helper.psm1" -DisableNameChecking
}

mkdir -Path C:\Downloads\ -Force
mkdir -Path "$androidSdk\licenses" -Force

Get-File -Url $url -FileName $fileName

echo "Expanding"
Expand-Archive -LiteralPath "$source" -DestinationPath $destination -Force

dir "$androidSdk\licenses"

echo "Installing licenses" #This is a workaround as it is not possible to accept licences during the build run. If there is an issue, simply accept the licenses locally and reupload them.
Copy-Item -Path microsoft-authentication-library-for-dotnet\build\AndroidSdkLicenses\* -Destination "$androidSdk\licenses" -Force

dir "$androidSdk\licenses"

echo "installing android"
C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager --licenses --sdk_root="$androidSdk"
echo y y y y y y y |C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager "$androidSdkVersion" --sdk_root="$androidSdk"
