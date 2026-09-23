$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$plate = Get-Content (Join-Path $projectRoot 'Assets/JSW/Script/Plate.cs') -Raw
$start = $plate.IndexOf('public static class BattlePatternRules')
if ($start -lt 0) { throw 'BattlePatternRules class was not found' }
$tests = Get-Content (Join-Path $PSScriptRoot 'AfterlifePatternTests.cs') -Raw
Add-Type -TypeDefinition ("using UnityEngine;`nusing Random = UnityEngine.Random;`n" + $tests + "`n" + $plate.Substring($start))
[AfterlifePatternTests]::Main()
