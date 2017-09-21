Write-Host "=========================="
Write-Host "Applying versioning to GitVersion.yml..."

$filename = "GitVersion.yml"
$newContent = Get-Content $filename

if ($env:BUILD_SOURCEBRANCHNAME -eq "master" -and $env:ReleaseVersioning -eq "true")
{
	# Release builds do not need the preview tag - but MSAL is not in GA yet, so don't do this.
	# Write-Host "Removing preview tag"
	# $newContent = $newContent -replace "tag: preview", ""
}

Set-Content $filename $newContent
Write-Host "Modifying:" $filename;