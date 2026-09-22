$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Assets/Sanggu/Scripts/Building/FormationSave.cs'), (Join-Path $projectRoot 'Assets/Sanggu/Scripts/Building/UnitTemp.cs'), (Join-Path $projectRoot 'Assets/JDY/Scripts/Data/ItemData.cs'), (Join-Path $PSScriptRoot 'FormationSaveTests.cs')
[FormationSaveTests]::Main()
