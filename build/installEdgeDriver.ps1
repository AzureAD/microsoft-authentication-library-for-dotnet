$url = "https://msedgedriver.azureedge.net/119.0.2151.72/edgedriver_win64.zip" #Chrome Driver from https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/
$fileName = "edgedriver_win64.zip"
$source = "C:\Downloads\$fileName"
$destination = "C:\Program Files\dotnet\"

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
