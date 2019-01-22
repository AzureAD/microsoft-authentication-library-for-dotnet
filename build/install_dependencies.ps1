#requires -version 4.0
#requires –runasadministrator

function Test-ChocolateyPackageInstalled {
  Param (
    [ValidateNotNullOrEmpty()]
    [string]
    $Package
  )
  
  Process {
    $pkgResult = choco list --local-only --id-only --limit-output --exact $Package
    
    return (-Not ([string]::IsNullOrEmpty($pkgResult)))
  }
}

function Test-IsChocolateyInstalled {
  $ChocoInstalled = $false
  if (Get-Command choco.exe -ErrorAction SilentlyContinue) {
      $ChocoInstalled = $true
  }
  
  return $ChocoInstalled
}

# Set NUNIT_HACK environment variable on dev machines so that NUnit runner does the right thing for automation tests.
# Also set it locally so that it's immediately available.
[Environment]::SetEnvironmentVariable("NUNIT_HACK", "1", "User")
$env:NUNIT_HACK = "1"


if (-Not (Test-IsChocolateyInstalled)) {
  Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
}

$ChocoPackages = 'chromedriver'

ForEach ($PackageName in $ChocoPackages) {
  if (-Not (Test-ChocolateyPackageInstalled($PackageName))) {
    choco install $PackageName -y
  }
}

choco upgrade all -y --limit-output