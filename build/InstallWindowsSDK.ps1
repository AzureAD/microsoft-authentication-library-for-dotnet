$url = "https://go.microsoft.com/fwlink/p/?LinkID=2033908" #Android SDK Tools from https://developer.android.com/studio#downloads
$fileName = "WindowsSDK"
$source = "C:\Downloads\$fileName"
$destination = "C:\Downloads\WindowsSDK"

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

echo "Installing"
Start-Process -FilePath $source -ArgumentList /quiet

echo "Done"