<#
.SYNOPSIS
    Deploys a published .NET app to an Azure App Service using the publish profile (Kudu Zip Deploy).

.DESCRIPTION
    Reads the web app publish profile XML from the PUBLISH_PROFILE_XML environment variable
    (sourced from Key Vault), extracts the SCM basic-auth credentials from the MSDeploy entry,
    zips the published output, and POSTs it to the Kudu '/api/zipdeploy' endpoint. No ARM service
    connection is required because the publish profile carries its own SCM credentials.

.PARAMETER PublishDirectory
    Directory containing the published app output to deploy.

.PARAMETER ZipPath
    Full path of the zip archive to create and upload.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string] $ZipPath
)

$ErrorActionPreference = 'Stop'

$profileXml = $env:PUBLISH_PROFILE_XML
if ([string]::IsNullOrWhiteSpace($profileXml)) {
    throw "The 'PUBLISH_PROFILE_XML' environment variable is empty. Map the publish-profile Key Vault secret to it via the task 'env:' block."
}

[xml] $publishData = $profileXml
$msDeployProfile = $publishData.publishData.publishProfile |
    Where-Object { $_.publishMethod -eq 'MSDeploy' } |
    Select-Object -First 1

if ($null -eq $msDeployProfile) {
    throw 'No MSDeploy entry found in the publish profile.'
}

$scmHost = ($msDeployProfile.publishUrl -split ':')[0]
$userName = $msDeployProfile.userName
$password = $msDeployProfile.userPWD

if (Test-Path -Path $ZipPath) {
    Remove-Item -Path $ZipPath -Force
}
Compress-Archive -Path (Join-Path $PublishDirectory '*') -DestinationPath $ZipPath -Force

$authHeader = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("$($userName):$($password)"))
$zipDeployUri = "https://$scmHost/api/zipdeploy"
$zipLength = (Get-Item -Path $ZipPath).Length

Write-Host "Deploying $zipLength bytes to $zipDeployUri"
Invoke-RestMethod -Uri $zipDeployUri -Method Post -InFile $ZipPath -ContentType 'application/zip' `
    -Headers @{ Authorization = "Basic $authHeader" } -TimeoutSec 300 | Out-Null
Write-Host 'Deployment complete.'
