$ErrorActionPreference = "Stop"

function ExitOnError
{
    if ($LastExitCode -ne 0) {
        # Force the PS script to exit with an error code
        $host.SetShouldExit($LastExitCode)
        exit $LastExitCode
    }
}

function Log([String] $message)
{
    Write-Host $message -Foreground Green
}

$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$sourcePath = split-path -parent $scriptPath

$configuration = "Release"

$rootPath = (Resolve-Path .).Path
$artifacts = Join-Path $rootPath "artifacts"


# Restoring the test project will restore the product project packages too
Log ("Restoring NuGet packages...")
msbuild "$sourcePath\tests\Test.MSAL.NET.Unit\Test.MSAL.NET.Unit.csproj" /m /t:restore /p:Configuration=$configuration
ExitOnError

Log ("Building product code...")
msbuild "$sourcePath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /m /t:build /p:Configuration=$configuration /p:TreatWarningsAsErrors=true
ExitOnError

Log("Building tests...")
msbuild "$sourcePath\tests\Test.MSAL.NET.Unit\Test.MSAL.NET.Unit.csproj" /m /t:build /p:Configuration=$configuration /p:TreatWarningsAsErrors=true
ExitOnError


#Log("Building Packages")
#msbuild "$sourcePath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /t:pack /p:Configuration=$configuration /p:PackageOutputPath=$artifacts /p:NoPackageAnalysis=true /p:NuGetBuildTasksPackTargets="workaround"
#ExitOnError