# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = Join-Path ([IO.Path]::GetTempPath()) "msal-mobile-merge-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $root | Out-Null

function Add-TextEntry($Archive, $Name, $Text) {
    $stream = $Archive.CreateEntry($Name).Open()
    $writer = [IO.StreamWriter]::new($stream)
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}

function Read-TextEntry($Archive, $Name) {
    $reader = [IO.StreamReader]::new($Archive.GetEntry($Name).Open())
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function New-Fixture($Name, $CompatibilityVersion = '1.2.3', [switch]$MissingIos) {
    $repository = Join-Path $root $Name
    $project = Join-Path $repository 'src\client\Microsoft.Identity.Client'
    $bin = Join-Path $project 'bin\Release'
    $compatibility = Join-Path $repository 'artifacts\net8-mobile'
    New-Item -ItemType Directory -Path $bin, $compatibility -Force | Out-Null
    foreach ($extension in @('.nupkg', '.snupkg')) {
        $assetExtension = if ($extension -eq '.nupkg') { '.dll' } else { '.pdb' }
        foreach ($old in @($false, $true)) {
            $directory = if ($old) { $compatibility } else { $bin }
            $version = if ($old) { $CompatibilityVersion } else { '1.2.3' }
            $frameworks = if ($old) {
                @('net8.0-android34.0', 'net8.0-ios18.0')
            } else {
                @('net8.0', 'net10.0', 'net10.0-android36.0', 'net10.0-ios26.0')
            }
            $archive = [IO.Compression.ZipFile]::Open(
                (Join-Path $directory "Microsoft.Identity.Client.1.2.3$extension"),
                [IO.Compression.ZipArchiveMode]::Create)
            try {
                $groups = ($frameworks | ForEach-Object {
                    "<group targetFramework=`"$_`"><dependency id=`"Microsoft.IdentityModel.Abstractions`" version=`"8.14.0`" /></group>"
                }) -join ''
                Add-TextEntry $archive 'Microsoft.Identity.Client.nuspec' "<package xmlns=`"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd`"><metadata><id>Microsoft.Identity.Client</id><version>$version</version><dependencies>$groups</dependencies></metadata></package>"
                foreach ($framework in $frameworks) {
                    if ($old -and $MissingIos -and $framework -like '*ios*') { continue }
                    Add-TextEntry $archive "lib/$framework/Microsoft.Identity.Client$assetExtension" "original-$framework"
                    if ($old) {
                        $target = if ($framework -like '*android*') { 'net8.0-android' } else { 'net8.0-ios' }
                        $output = Join-Path $bin $target
                        New-Item -ItemType Directory -Path $output -Force | Out-Null
                        Set-Content (Join-Path $output "Microsoft.Identity.Client$assetExtension") "staged-signed-$framework" -NoNewline
                    }
                }
            }
            finally { $archive.Dispose() }
        }
    }
    return $project
}

try {
    $core = Join-Path $PSScriptRoot '..\src\client\Microsoft.Identity.Client'
    $intermediate = Join-Path $core 'obj'
    New-Item -ItemType Directory -Path $intermediate -Force | Out-Null
    $oldJar = Join-Path $intermediate "sdk10-intermediate-$([Guid]::NewGuid().ToString('N')).jar"
    New-Item -ItemType File -Path $oldJar | Out-Null
    try {
        $evaluation = & dotnet msbuild (Join-Path $core 'Microsoft.Identity.Client.csproj') -nologo `
            -getItem:None -p:TargetFramework=net8.0 -p:INCLUDE_MOBILE_AND_LEGACY_TFM=true `
            -p:MobileTargetFrameworkVersion=8.0 "-p:BaseIntermediateOutputPath=$root\obj\"
        if ($LASTEXITCODE -ne 0) { throw 'Evaluating compatibility build inputs failed.' }
        $inputs = ($evaluation | ConvertFrom-Json).Items.None
        if ($inputs | Where-Object { $_.FullPath -eq [IO.Path]::GetFullPath($oldJar) }) {
            throw 'SDK 10 intermediates must not become SDK 8 source inputs.'
        }
        Write-Host 'Passed: compatibility builds exclude the existing SDK 10 intermediate tree.'
    }
    finally {
        Remove-Item -LiteralPath $oldJar
    }

    $project = New-Fixture 'valid'
    & (Join-Path $PSScriptRoot 'merge-net8-mobile-packages.ps1') -ProjectDirectory $project
    & (Join-Path $PSScriptRoot 'merge-net8-mobile-packages.ps1') -ProjectDirectory $project
    foreach ($extension in @('.nupkg', '.snupkg')) {
        $assetExtension = if ($extension -eq '.nupkg') { '.dll' } else { '.pdb' }
        $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $project "bin\Release\Microsoft.Identity.Client.1.2.3$extension"))
        try {
            foreach ($framework in @('net8.0-android34.0', 'net8.0-ios18.0')) {
                if ((Read-TextEntry $archive "lib/$framework/Microsoft.Identity.Client$assetExtension") -cne "staged-signed-$framework") {
                    throw 'The combined package must contain staged, potentially signed binaries.'
                }
            }
            if ((Read-TextEntry $archive "lib/net10.0-android36.0/Microsoft.Identity.Client$assetExtension") -cne 'original-net10.0-android36.0') {
                throw 'The modern asset was modified.'
            }
            [xml]$manifest = Read-TextEntry $archive 'Microsoft.Identity.Client.nuspec'
            if (@($manifest.package.metadata.dependencies.group).Count -ne 6) {
                throw 'Both compatibility dependency groups must be retained alongside the modern groups.'
            }
        }
        finally { $archive.Dispose() }
    }
    Write-Host 'Passed: combined binaries/symbols, preserved signed bytes and dependencies, and .NET 8/9/10 mobile asset selection.'

    foreach ($case in @('version-mismatch', 'missing-ios')) {
        $project = if ($case -eq 'version-mismatch') {
            New-Fixture $case -CompatibilityVersion '9.9.9'
        } else {
            New-Fixture $case -MissingIos
        }
        $package = Join-Path $project 'bin\Release\Microsoft.Identity.Client.1.2.3.nupkg'
        $before = (Get-FileHash $package).Hash
        $rejected = $false
        try {
            & (Join-Path $PSScriptRoot 'merge-net8-mobile-packages.ps1') -ProjectDirectory $project
        }
        catch {
            $expected = if ($case -eq 'version-mismatch') { '*IDs and versions must match*' } else { '*Missing compatibility asset*' }
            if ($_.Exception.Message -notlike $expected) { throw }
            $rejected = $true
        }
        if (-not $rejected -or (Get-FileHash $package).Hash -cne $before) {
            throw "$case must fail without modifying the primary package."
        }
        Write-Host "Passed: $case rejected without changing the primary package."
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force
}
