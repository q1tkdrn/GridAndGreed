$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Assets/Sanggu/Scripts/Building/UnitTemp.cs'), (Join-Path $PSScriptRoot 'UnitSkinTests.cs')
[UnitSkinTests]::Main()
