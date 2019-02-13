## What this script does

- clones the Azure-sample MSAL.NET samples repos in a sub folder of the folder containing the script
- updates the sample to use the  `UsedLibraryVersion` version
- builds the sample
- if the sample builds, commits the changes and pushes the `cd/updateLatestMSAL` branch

Then, you just need to go to each of the samples repos and accepts or not the PRs:

- the list of PRs to review is in `todo.txt`,
- whereas the list of projects that failed to build are in `failed.txt`

## Prerequisites**

- have NuGet.exe in the path
- change the `UsedLibraryVersion` variable to the version to which to upgrade the samples