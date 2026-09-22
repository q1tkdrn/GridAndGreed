$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$boss = Get-Content (Join-Path $projectRoot 'Assets/Sanggu/Scripts/BossTemp.cs') -Raw
$start = $boss.IndexOf('public static class AfterlifePassiveRules')
if ($start -lt 0) { throw 'AfterlifePassiveRules class was not found' }
$rulesFile = Join-Path ([IO.Path]::GetTempPath()) ('BossRules-' + [guid]::NewGuid().ToString('N') + '.cs')
try {
    [IO.File]::WriteAllText($rulesFile, $boss.Substring($start))
    Add-Type -Path $rulesFile, (Join-Path $projectRoot 'Assets/Sanggu/Scripts/BossPassiveState.cs'), (Join-Path $PSScriptRoot 'BossPassiveStateTests.cs')
    [BossPassiveStateTests]::Main()
} finally {
    Remove-Item -LiteralPath $rulesFile -ErrorAction SilentlyContinue
}
