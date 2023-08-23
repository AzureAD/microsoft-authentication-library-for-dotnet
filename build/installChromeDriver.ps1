$url = "https://chromedriver.storage.googleapis.com/114.0.5735.90/chromedriver_win32.zip" #Chrome Driver from https://chromedriver.chromium.org/downloads
$fileName = "chromedriver_win32.zip"
$source = "C:\Downloads\$fileName"
$destination = "C:\Program Files\dotnet\"

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

echo "Expanding"
Expand-Archive -LiteralPath "$source" -DestinationPath $destination -Force

#Expand-ArchiveWith7Zip -Source $source -Destination $destination