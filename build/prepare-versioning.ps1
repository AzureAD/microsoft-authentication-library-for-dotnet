Write-Host "=========================="
Write-Host "Applying versioning to GitVersion.yml..."

$filename = "GitVersion.yml"
$newContent = Get-Content $filename

if ($env:BUILD_SOURCEBRANCHNAME -eq "master" -and $env:ReleaseVersioning -eq "true")
{
	Write-Host "Removing preview tag"
	# Release builds do not need the preview tag
	$newContent = $newContent -replace "tag: preview", ""

	#trying to match something like: "next-version: 1.1.1"
	$m = $newContent -match 'next-version: ';
	# taking off "next-version: " to leave the version number
	$nugetVersion = $m[0].Split(" ")[1];
}

Set-Content $filename $newContent
Write-Host "Modifying:" $filename;