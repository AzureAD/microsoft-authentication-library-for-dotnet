# Get the installed version of Microsoft Edge
$edgeVersion = (Get-Item "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe").VersionInfo.ProductVersion

$url = "https://msedgedriver.azureedge.net/$edgeVersion/edgedriver_win64.zip" #Chrome Driver from https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/
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
