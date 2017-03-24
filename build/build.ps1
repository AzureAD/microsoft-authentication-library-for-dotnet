MSBuild ADAL.NET.sln /t:Clean
MSBuild ./src/ADAL.PCL/ADAL.PCL.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./src/ADAL.PCL.Android/ADAL.PCL.Android.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./src/ADAL.PCL.CoreCLR/ADAL.PCL.CoreCLR.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./src/ADAL.PCL.Desktop/ADAL.PCL.Desktop.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./src/ADAL.PCL.iOS/ADAL.PCL.iOS.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./src/ADAL.PCL.WinRT/ADAL.PCL.WinRT.csproj /t:Build /p:runcodeanalysis=false
MSBuild ./tests/Test.ADAL.NET.Unit/Test.ADAL.NET.Unit.csproj /t:Build /p:runcodeanalysis=false