$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Assets/Sanggu/Scripts/Building/ItemBattleState.cs'), (Join-Path $PSScriptRoot 'ItemBattleStateTests.cs')
[ItemBattleStateTests]::Main()
