function ExitOnMSBuildError
{
    if ($LastExitCode -ne 0) {
        # Force the PS script to exit with an error code, signalling the error to AppVeyor 
        $host.SetShouldExit($LastExitCode)
        throw "MSBuild failed with code $LastExitCode - see the log for details."
    }
}

$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition

$configuration = "Release"

$isAppVeyor = Test-Path -Path env:\APPVEYOR
$rootPath = (Resolve-Path .).Path
$artifacts = Join-Path $rootPath "artifacts"

New-Item -ItemType Directory -Force -Path $artifacts

Write-Host "Restoring packages for $scriptPath\MSAL.sln" -Foreground Green
msbuild "$scriptPath\MSAL.sln" /m /t:restore /p:Configuration=$configuration 
ExitOnMSBuildError


Write-Host "Building $scriptPath\MSAL.sln" -Foreground Green
msbuild "$scriptPath\MSAL.sln" /m /t:build /p:Configuration=$configuration
ExitOnMSBuildError

Write-Host "Building Packages" -Foreground Green
msbuild "$scriptPath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /t:pack /p:Configuration=$configuration /p:PackageOutputPath=$artifacts /p:NoPackageAnalysis=true /p:NuGetBuildTasksPackTargets="workaround"
ExitOnMSBuildError