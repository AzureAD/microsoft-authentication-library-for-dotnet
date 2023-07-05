# Contributor License agreement

Please visit [https://cla.microsoft.com/](https://cla.microsoft.com/) and sign the Contributor License Agreement.  You only need to do that once. We can not look at your code until you've submitted this request.

# Setup, Building and Testing 

## Prerequisites to Build MSAL.NET

Visual Studio 2022 or higher is needed. VS Code / VS for Mac are not supported.
To build the mobile targets, install the Maui workload. To build UWP, install Windows SDK 177630.

## Build 

MSAL .Net supports many target frameworks, by default it does not enable the mobile targets and the legacy targets. Unit tests exist only for the base frameworks.
To load all targets, edit Microsoft.Identity.Client.csproj or set an env variable named INCLUDE_MOBILE_AND_LEGACY_TFM to value "1" and restart Visual Studio.

Open `LibsAndSamples.sln` for a bigger solution with lots of apps that exercise MSAL. You may have to disable some dev apps. Load `LibsNoSamples.sln` for a small solution that has the library and the tests. 

## Run tests

Microsoft contributors can run integration tests by following instructions [here](https://microsoft.sharepoint.com/teams/ADAL/_layouts/OneNote.aspx?id=%2Fteams%2FADAL%2FSiteAssets%2FDevEx%20Notebook&wd=target%28ID4S%2FMSAL.NET%2FTechnical%2FTesting.one%7C267ED97C-4551-49B4-B9C4-BA1239EC9C9F%2FMSAL%20desktop%20integration%20tests%7CF69C92B6-4A10-404A-9E9E-0FE513BF2897%2F%29).

External contributors can only run the unit tests. The CI build will run integration tests. 

## Package

To test an MSAL package, use the package command from VS. Or from the command line:

`msbuild <msal>.csproj /t:pack /p:MsalClientSemVer=1.2.3-preview`

### How the MSAL team deals with forks

The CI build will not run on a PR opened from a fork, as a security measure. The MSAL team may  move your branch from your fork to the main repository, to be able to run the CI. This will preserve the identity of the commit. 


```
# list existing remotes
git remote -v 

# add a remote to the fork of the contributor
git remote add joe joes_repo_url

# sync
git fetch joe

# checkout the contributor's branch 
git checkout joes_feature_branch

# push it to the original repository (AzureAD/MSAL)
git push origin

```



