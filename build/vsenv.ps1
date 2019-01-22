$vsInstallDir = vswhere -latest -property installationPath
$vsToolsDir = $vsInstallDir + "\Common7\Tools"

pushd $vsToolsDir
cmd /c "VsDevCmd.bat&set" |
foreach {
  if ($_ -match "=") {
    $v = $_.split("="); set-item -force -path "ENV:\$($v[0])"  -value "$($v[1])"
  }
}
popd
Write-Host "`nVisual Studio 2017 Command Prompt variables set." -ForegroundColor Yellow