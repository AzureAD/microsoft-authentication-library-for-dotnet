$ErrorActionPreference = "Stop"

function ExitOnError
{
    if ($LastExitCode -ne 0) {
        # Force the PS script to exit with an error code, signalling the error to AppVeyor
        $host.SetShouldExit($LastExitCode)
        exit $LastExitCode
    }
}

Log ("Building product code...")
MSBuild ./src/ADAL.PCL/ADAL.PCL.csproj /t:Build /p:runcodeanalysis=false
ExitOnError
MSBuild ./src/ADAL.PCL.Android/ADAL.PCL.Android.csproj /t:Build /p:runcodeanalysis=false
ExitOnError
MSBuild ./src/ADAL.PCL.CoreCLR/ADAL.PCL.CoreCLR.csproj /t:Build /p:runcodeanalysis=false
ExitOnError
MSBuild ./src/ADAL.PCL.Desktop/ADAL.PCL.Desktop.csproj /t:Build /p:runcodeanalysis=false
ExitOnError
MSBuild ./src/ADAL.PCL.iOS/ADAL.PCL.iOS.csproj /t:Build /p:runcodeanalysis=false
ExitOnError
MSBuild ./src/ADAL.PCL.WinRT/ADAL.PCL.WinRT.csproj /t:Build /p:runcodeanalysis=false
ExitOnError

Log("Building Tests...")
MSBuild ./tests/Test.ADAL.NET.Unit/Test.ADAL.NET.Unit.csproj /t:Build /p:runcodeanalysis=false
ExitOnError