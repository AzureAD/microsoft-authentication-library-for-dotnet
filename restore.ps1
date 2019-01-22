$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
& $scriptDir\build\vsenv.ps1

& $scriptDir\buildVS2017.cmd d r 1
& $scriptDir\buildVS2017.cmd r r 1
