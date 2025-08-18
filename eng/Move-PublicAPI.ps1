param(
    [Parameter(Mandatory = $false)]
    [string]$Root,

    [switch]$DryRun
)

<#!
.SYNOPSIS
    Moves all APIs from PublicAPI.Unshipped.txt files into the corresponding PublicAPI.Shipped.txt files.

.DESCRIPTION
    This script scans the repository for files named "PublicAPI.Unshipped.txt" and, for each one found,
    appends its content to the sibling "PublicAPI.Shipped.txt" file and then clears the unshipped file.

    Run this script every time a release is made to move unshipped API entries to the shipped list.

.PARAMETER Root
    Optional. The root directory to scan. Defaults to the repository root (one level above the script's folder).

.PARAMETER DryRun
    Optional. If specified, the script will only report what it would do without making any changes.

.EXAMPLE
    ./Move-PublicAPI.ps1

.EXAMPLE
    ./Move-PublicAPI.ps1 -Root C:\g\msal

.EXAMPLE
    ./Move-PublicAPI.ps1 -DryRun
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $PSBoundParameters.ContainsKey('Root') -or [string]::IsNullOrWhiteSpace($Root)) {
    # Default Root to the repo root (one level above script directory)
    $Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

Write-Host "Scanning for PublicAPI.Unshipped.txt under: $Root"

# Find all PublicAPI.Unshipped.txt files
$unshippedFiles = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'PublicAPI.Unshipped.txt' -ErrorAction Stop

if (-not $unshippedFiles -or $unshippedFiles.Count -eq 0) {
    Write-Host 'No PublicAPI.Unshipped.txt files found. Nothing to do.'
    return
}

$updatedCount = 0

foreach ($unshipped in $unshippedFiles) {
    $unshippedPath = $unshipped.FullName
    $shippedPath = Join-Path $unshipped.DirectoryName 'PublicAPI.Shipped.txt'

    # Read unshipped content
    $unshippedContent = Get-Content -LiteralPath $unshippedPath -Raw -ErrorAction Stop

    if ([string]::IsNullOrWhiteSpace($unshippedContent)) {
        Write-Host "Skipping (empty): $unshippedPath"
        continue
    }

    # Ensure shipped file exists
    if (-not (Test-Path -LiteralPath $shippedPath)) {
        if ($DryRun) {
            Write-Host "Would create missing shipped file: $shippedPath"
        }
        else {
            New-Item -ItemType File -Path $shippedPath -Force | Out-Null
        }
    }

    # Read existing shipped content if any
    $existingShipped = ''
    if (Test-Path -LiteralPath $shippedPath) {
        $existingShipped = Get-Content -LiteralPath $shippedPath -Raw -ErrorAction Stop
    }

    # Prepare content to append with reasonable separation
    $toAppend = $unshippedContent.TrimEnd("`r", "`n")

    if (-not [string]::IsNullOrEmpty($existingShipped)) {
        # Ensure at least one blank line separation between existing shipped content and new additions
        $needsTrailingNewLine = -not $existingShipped.EndsWith("`n")
        $separator = if ($needsTrailingNewLine) { "`r`n`r`n" } else { "`r`n" }
        $toAppend = $separator + $toAppend + "`r`n"
    }
    else {
        # If shipped is empty, just ensure the appended content ends with a newline
        $toAppend = $toAppend + "`r`n"
    }

    if ($DryRun) {
        $movedLines = ($unshippedContent -split "(`r`n|`n|`r)").Where({ $_ -ne '' }).Count
        Write-Host "Would move $movedLines line(s) from: $unshippedPath"`n"              to: $shippedPath"
        continue
    }

    # Append to shipped and clear unshipped
    Add-Content -LiteralPath $shippedPath -Value $toAppend -Encoding UTF8

    # Clear the unshipped file (leave file present but empty)
    Set-Content -LiteralPath $unshippedPath -Value '' -NoNewline -Encoding UTF8

    $updatedCount++
    Write-Host "Moved content from: $unshippedPath"`n"               to: $shippedPath"
}

Write-Host "Done. Files updated: $updatedCount"
