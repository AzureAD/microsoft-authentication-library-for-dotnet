# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory)][string] $RepositoryRoot,
    [Parameter(Mandatory)][string] $WorkingDirectory,
    [string] $BuildConfiguration = 'Release',
    [string] $MicrosoftIdentityClientVersion
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = (Resolve-Path $RepositoryRoot).Path
$project = Join-Path $RepositoryRoot 'src\client\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj'
$sdk = dotnet --list-sdks | Where-Object { $_ -match '^8\.0\.' } |
    ForEach-Object { ($_ -split ' ')[0] } | Sort-Object { [version]$_ } | Select-Object -Last 1
if (-not $sdk) {
    throw 'Install the .NET 8 SDK before building the compatibility mobile assets.'
}

New-Item -ItemType Directory -Path $WorkingDirectory -Force | Out-Null
$WorkingDirectory = (Resolve-Path $WorkingDirectory).Path
@{ sdk = @{ version = $sdk; rollForward = 'disable' } } |
    ConvertTo-Json | Set-Content (Join-Path $WorkingDirectory 'global.json')

Push-Location $WorkingDirectory
try {
    & dotnet workload install android ios
    if ($LASTEXITCODE -ne 0) { throw 'Installing .NET 8 mobile workloads failed.' }

    $properties = @(
        '-p:TargetFrameworks=net8.0-android%3Bnet8.0-ios',
        '-p:INCLUDE_MOBILE_AND_LEGACY_TFM=true',
        '-p:MobileTargetFrameworkVersion=8.0',
        "-p:BaseIntermediateOutputPath=$(Join-Path $WorkingDirectory 'obj')\",
        '-p:IncludeSymbols=true'
    )
    if ($MicrosoftIdentityClientVersion) {
        $properties += "-p:MicrosoftIdentityClientVersion=$MicrosoftIdentityClientVersion"
    }

    & dotnet pack $project --configuration $BuildConfiguration --output (Join-Path $RepositoryRoot 'artifacts\net8-mobile') @properties
    if ($LASTEXITCODE -ne 0) { throw 'Building and packing the .NET 8 mobile assets failed.' }
}
finally {
    Pop-Location
}
