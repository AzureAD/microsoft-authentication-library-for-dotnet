# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory)][string] $ProjectDirectory,
    [string] $BuildConfiguration = 'Release'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$ProjectDirectory = (Resolve-Path $ProjectDirectory).Path
$repository = Split-Path (Split-Path (Split-Path $ProjectDirectory -Parent) -Parent) -Parent
$compatibilityDirectory = Join-Path $repository 'artifacts\net8-mobile'
$binaryDirectory = Join-Path $ProjectDirectory "bin\$BuildConfiguration"
$sdkVersion = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine the SDK used for package validation.' }
$sdkLine = & dotnet --list-sdks | Where-Object { $_ -match "^$([regex]::Escape($sdkVersion)) \[" } | Select-Object -First 1
if (-not $sdkLine -or $sdkLine -notmatch '\[(.+)\]$') { throw 'Cannot locate NuGet.Frameworks in the selected SDK.' }
Add-Type -Path (Join-Path $Matches[1] "$sdkVersion\NuGet.Frameworks.dll")

function Read-Metadata($Archive) {
    $entries = @($Archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [StringComparison]::Ordinal) })
    if ($entries.Count -ne 1) { throw 'Expected exactly one package manifest.' }
    $stream = $entries[0].Open()
    $reader = $null
    try {
        $settings = [Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $reader = [Xml.XmlReader]::Create($stream, $settings)
        $document = [Xml.XmlDocument]::new()
        $document.Load($reader)
        return ,$document
    }
    finally {
        if ($reader) { $reader.Dispose() }
        $stream.Dispose()
    }
}

function Assert-AssetSelection($Archive) {
    $frameworks = [NuGet.Frameworks.NuGetFramework[]]@(
        $Archive.Entries | Where-Object { $_.FullName -match '^lib/([^/]+)/Microsoft\.Identity\.Client\.dll$' } |
            ForEach-Object { [NuGet.Frameworks.NuGetFramework]::ParseFolder(($_.FullName -split '/')[1]) }
    )
    $reducer = [NuGet.Frameworks.FrameworkReducer]::new()
    foreach ($consumer in @('net8.0-android34.0', 'net9.0-android35.0', 'net8.0-ios18.0', 'net9.0-ios18.0')) {
        $requested = [NuGet.Frameworks.NuGetFramework]::ParseFolder($consumer)
        $selected = $reducer.GetNearest($requested, $frameworks)
        if (-not $selected -or $selected.Version.Major -ne 8 -or $selected.Platform -ne $requested.Platform) {
            throw "$consumer did not select the .NET 8 mobile asset."
        }
    }
    foreach ($platform in @('android', 'ios')) {
        $modern = @($frameworks | Where-Object { $_.Version.Major -eq 10 -and $_.Platform -eq $platform })
        if ($modern.Count -ne 1 -or $reducer.GetNearest($modern[0], $frameworks) -ne $modern[0]) {
            throw "Missing or ambiguous .NET 10 $platform asset."
        }
    }
}

$packages = @(Get-ChildItem $binaryDirectory -Filter 'Microsoft.Identity.Client.*.nupkg' -File)
if ($packages.Count -ne 1) { throw 'Expected exactly one unsigned core package in the build output.' }
$primaryPackage = $packages[0]
foreach ($extension in @('.nupkg', '.snupkg')) {
    $name = [IO.Path]::ChangeExtension($primaryPackage.Name, $extension)
    $primaryPath = Join-Path $binaryDirectory $name
    $compatibilityPath = Join-Path $compatibilityDirectory $name
    if (-not (Test-Path $primaryPath) -or -not (Test-Path $compatibilityPath)) {
        throw "Missing primary or .NET 8 compatibility package: $name"
    }

    $temporaryPath = "$primaryPath.$([Guid]::NewGuid().ToString('N')).tmp"
    $primary = $null
    $compatibility = $null
    $output = $null
    try {
        $primary = [IO.Compression.ZipFile]::OpenRead($primaryPath)
        $compatibility = [IO.Compression.ZipFile]::OpenRead($compatibilityPath)
        if ($primary.GetEntry('.signature.p7s') -or $compatibility.GetEntry('.signature.p7s')) {
            throw 'Mobile assets must be combined before package signing.'
        }
        $manifest = Read-Metadata $primary
        $oldManifest = Read-Metadata $compatibility
        $metadata = $manifest.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        $oldMetadata = $oldManifest.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        if ($metadata.id -cne 'Microsoft.Identity.Client' -or
            $metadata.id -cne $oldMetadata.id -or $metadata.version -cne $oldMetadata.version) {
            throw 'Core and compatibility package IDs and versions must match.'
        }
        $groups = @($oldMetadata.SelectNodes('*[local-name()="dependencies"]/*[local-name()="group"]') |
            Where-Object { $_.targetFramework -match '^net8\.0-(android34\.0|ios18\.0)$' })
        if ($extension -eq '.nupkg' -and ($groups.Count -ne 2 -or @($groups.targetFramework | Select-Object -Unique).Count -ne 2)) {
            throw 'Expected both .NET 8 mobile dependency groups.'
        }
        $dependencies = $metadata.SelectSingleNode('*[local-name()="dependencies"]')
        if (-not $dependencies) {
            $dependencies = $manifest.CreateElement('dependencies', $metadata.NamespaceURI)
            [void]$metadata.AppendChild($dependencies)
        }
        foreach ($group in $groups) {
            $existing = @($dependencies.ChildNodes | Where-Object { $_.targetFramework -eq $group.targetFramework })
            if ($existing.Count -gt 1) { throw "Duplicate framework dependency group: $($group.targetFramework)" }
            if ($existing.Count -eq 1) {
                [void]$dependencies.RemoveChild($existing[0])
            }
            [void]$dependencies.AppendChild($manifest.ImportNode($group, $true))
        }

        $output = [IO.Compression.ZipFile]::Open($temporaryPath, [IO.Compression.ZipArchiveMode]::Create)
        foreach ($entry in $primary.Entries) {
            if ($entry.FullName -match '^lib/net8\.0-(android34\.0|ios18\.0)/') { continue }
            $destination = $output.CreateEntry($entry.FullName)
            $destination.LastWriteTime = $entry.LastWriteTime
            $target = $destination.Open()
            try {
                if ($entry.FullName.EndsWith('.nuspec', [StringComparison]::Ordinal)) {
                    $manifest.Save($target)
                }
                else {
                    $source = $entry.Open()
                    try { $source.CopyTo($target) } finally { $source.Dispose() }
                }
            }
            finally { $target.Dispose() }
        }
        $mobileEntries = @($compatibility.Entries | Where-Object {
            $_.FullName -match '^lib/net8\.0-(android34\.0|ios18\.0)/[^/]+$'
        })
        foreach ($platform in @('android34.0', 'ios18.0')) {
            $required = "lib/net8.0-$platform/Microsoft.Identity.Client" + $(if ($extension -eq '.nupkg') { '.dll' } else { '.pdb' })
            if (-not $compatibility.GetEntry($required)) { throw "Missing compatibility asset: $required" }
        }
        foreach ($entry in $mobileEntries) {
            $parts = $entry.FullName -split '/'
            $framework = if ($parts[1] -eq 'net8.0-android34.0') { 'net8.0-android' } else { 'net8.0-ios' }
            $binary = Join-Path $binaryDirectory "$framework\$($parts[2])"
            if (-not (Test-Path $binary)) { throw "Missing staged mobile build output: $binary" }
            # Use staged binaries, which may have been signed after the compatibility pack was created.
            $source = [IO.File]::OpenRead($binary)
            $destination = $output.CreateEntry($entry.FullName)
            $destination.LastWriteTime = $entry.LastWriteTime
            $target = $destination.Open()
            try { $source.CopyTo($target) } finally { $target.Dispose(); $source.Dispose() }
        }
        $output.Dispose()
        $output = $null
        if ($extension -eq '.nupkg') {
            $validation = [IO.Compression.ZipFile]::OpenRead($temporaryPath)
            try { Assert-AssetSelection $validation } finally { $validation.Dispose() }
        }
        $primary.Dispose()
        $primary = $null
        Move-Item -LiteralPath $temporaryPath -Destination $primaryPath -Force
        Write-Host "Combined and validated .NET 8/.NET 10 mobile assets: $name"
    }
    finally {
        if ($output) { $output.Dispose() }
        if ($compatibility) { $compatibility.Dispose() }
        if ($primary) { $primary.Dispose() }
        if (Test-Path $temporaryPath) { Remove-Item -LiteralPath $temporaryPath }
    }
}
