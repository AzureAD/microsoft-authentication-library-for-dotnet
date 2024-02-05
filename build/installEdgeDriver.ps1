# Get the complete version of Microsoft Edge
$edgeVersion = (Get-AppxPackage -Name *Microsoft.MicrosoftEdge.Stable* | ForEach-Object Version)

# Assuming the complete version can be directly used (note: this might not always work due to version format differences)
$url = "https://msedgedriver.azureedge.net/$edgeVersion/edgedriver_win64.zip"

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

# Ensure the Downloads directory exists
mkdir -Path C:\Downloads\ -Force

# Use Invoke-WebRequest for downloading the file if Get-File is not available in your environment
Invoke-WebRequest -Uri $url -OutFile $source

echo "Expanding"
Expand-Archive -LiteralPath "$source" -DestinationPath $destination -Force
