$ErrorActionPreference = "Stop"

function ExitOnError
{
    if ($LastExitCode -ne 0) {
        # Force the PS script to exit with an error code, signalling the error to AppVeyor 
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

$isAppVeyor = Test-Path -Path env:\APPVEYOR
$rootPath = (Resolve-Path .).Path
$artifacts = Join-Path $rootPath "artifacts"
$targetFrameworks = "/p:""TargetFrameworks=net45;netstandard1.1;netstandard1.3;win81;Xamarin.iOS10"""

if ($isAppVeyor)
{
    $appVeyorLogger = "/logger:""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"""
}

Log ("Building product code...")
msbuild "$sourcePath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /m /t:restore /p:Configuration=$configuration $appVeyorLogger $targetFrameworks
msbuild "$sourcePath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /m /t:build /p:Configuration=$configuration $appVeyorLogger $targetFrameworks 
ExitOnError

#Log("Building Tests...")
#msbuild "$sourcePath\tests\Test.MSAL.NET.Unit\Test.MSAL.NET.Unit.csproj" /m /t:restore /p:Configuration=$configuration $appVeyorLogger $targetFrameworks
#msbuild "$sourcePath\tests\Test.MSAL.NET.Unit\Test.MSAL.NET.Unit.csproj" /m /t:build /p:Configuration=$configuration $appVeyorLogger $targetFrameworks
#ExitOnError


Log("Building Packages")
msbuild "$sourcePath\src\Microsoft.Identity.Client\Microsoft.Identity.Client.csproj" /t:pack /p:Configuration=$configuration /p:PackageOutputPath=$artifacts /p:NoPackageAnalysis=true /p:NuGetBuildTasksPackTargets="workaround" $appVeyorLogger $targetFrameworks
ExitOnError