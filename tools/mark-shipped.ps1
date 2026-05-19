[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function MarkShipped([Parameter(mandatory=$true)][string]$dir,
                     [Parameter(mandatory=$true)][string]$access) {
    $shippedFileName = $access + "API.Shipped.txt"
    $shippedFilePath = Join-Path $dir $shippedFileName
    $shipped = @()
    $shipped += Get-Content $shippedFilePath

    $unshippedFileName = $access + "API.Unshipped.txt"
    $unshippedFilePath = Join-Path $dir $unshippedFileName
    $unshipped = Get-Content $unshippedFilePath
    $removed = @()
    $removedPrefix = "*REMOVED*";
    Write-Host "Processing $dir : $access"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            if ($item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            else {
                $shipped += $item
            }
        }
    }

    $shipped | Sort-Object -Unique |Where-Object { -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    Clear-Content $unshippedFilePath
}

try {
    foreach ($file in Get-ChildItem -re -in "PublicApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir "Public"
    }

    foreach ($file in Get-ChildItem -re -in "InternalApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir "Internal"
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
