$url = "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/116.0.5845.96/win64/chromedriver-win64.zip" #Chrome Driver from https://chromedriver.chromium.org/downloads
$fileName = "chromedriver-win64.zip"
$source = "C:\Downloads\$fileName"
$destination = "C:\Program Files\dotnet\"
$driverSource = "C:\Program Files\dotnet\chromedriver-win64\chromedriver.exe"

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

Move-Item -Path $driverSource -Destination $destination -Force