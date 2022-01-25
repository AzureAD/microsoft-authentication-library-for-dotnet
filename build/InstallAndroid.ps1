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

Get-File -Url $url -FileName $fileName
#Expand-ArchiveWith7Zip -Source $source -Destination $destination


echo "Expanding"
Expand-Archive -LiteralPath "$source" -DestinationPath "C:\Downloads\AndroidSdkTools" -Force

echo "installing android"
echo y y y y y y |C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager --licenses --sdk_root="C:\Program Files (x86)\Android\android-sdk"
echo y |C:\Downloads\AndroidSdkTools\cmdline-tools\bin\.\sdkmanager "platforms;android-29" --sdk_root="C:\Program Files (x86)\Android\android-sdk"
