param (
    [Parameter(Mandatory = $true)]
    [ValidateSet("Install", "Validate", "Cleanup")]
    [string]$Action,
    
    [string]$JavaZipPath,
    [string]$JavaInstallPath
)

function Install-Java {
    param (
        [string]$ZipPath,
        [string]$InstallPath
    )
    Write-Host "Installing Java from $ZipPath to $InstallPath"

    # Extract Java ZIP
    if (!(Test-Path $ZipPath)) {
        Write-Error "Java ZIP file not found: $ZipPath"
        exit 1
    }
    Expand-Archive -Path $ZipPath -DestinationPath $InstallPath -Force

    # Set environment variables
    [Environment]::SetEnvironmentVariable("JAVA_HOME", $InstallPath, [System.EnvironmentVariableTarget]::Machine)
    $env:JAVA_HOME = $InstallPath
    $newPath = "$env:JAVA_HOME\bin;$env:PATH"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, [System.EnvironmentVariableTarget]::Machine)
    $env:PATH = $newPath

    Write-Host "Java installed successfully at $InstallPath"
    Write-Host "JAVA_HOME set to $env:JAVA_HOME"
}

function Validate-Java {
    Write-Host "Validating Java installation"

    try {
        java -version
        Write-Host "Java validation successful."
    } catch {
        Write-Error "Java validation failed. Ensure JAVA_HOME and PATH are configured correctly."
        exit 1
    }

    if (-not $env:JAVA_HOME) {
        Write-Error "JAVA_HOME is not set."
        exit 1
    }
    Write-Host "JAVA_HOME is set to $env:JAVA_HOME"
}

function Cleanup-Java {
    param (
        [string]$ZipPath,
        [string]$InstallPath
    )
    Write-Host "Cleaning up Java installation from $InstallPath and ZIP $ZipPath"

    # Delete ZIP file
    if (Test-Path $ZipPath) {
        try {
            Remove-Item -Path $ZipPath -Force
            Write-Host "Deleted Java ZIP file: $ZipPath"
        } catch {
            Write-Error "Failed to delete Java ZIP file: $ZipPath"
            exit 1
        }
    } else {
        Write-Host "Java ZIP file not found for deletion: $ZipPath"
    }

    # Delete installation directory
    if (Test-Path $InstallPath) {
        try {
            Remove-Item -Path $InstallPath -Recurse -Force
            Write-Host "Deleted Java installation directory: $InstallPath"
        } catch {
            Write-Error "Failed to delete Java installation directory: $InstallPath"
            exit 1
        }
    } else {
        Write-Host "Java installation directory not found for deletion: $InstallPath"
    }
}

# Main Logic
switch ($Action) {
    "Install" {
        Install-Java -ZipPath $JavaZipPath -InstallPath $JavaInstallPath
    }
    "Validate" {
        Validate-Java
    }
    "Cleanup" {
        Cleanup-Java -ZipPath $JavaZipPath -InstallPath $JavaInstallPath
    }
    default {
        Write-Error "Invalid action: $Action"
    }
}
