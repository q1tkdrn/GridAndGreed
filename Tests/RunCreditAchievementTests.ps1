$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$managerPath = Join-Path $projectRoot 'Assets/JDY/Scripts/Manager/AchievementManager.cs'
$bytes = [IO.File]::ReadAllBytes($managerPath)
try { $source = (New-Object System.Text.UTF8Encoding($false, $true)).GetString($bytes) }
catch { $source = [Text.Encoding]::GetEncoding(949).GetString($bytes) }
$sourceFile = Join-Path ([IO.Path]::GetTempPath()) ('CreditAchievement-' + [guid]::NewGuid().ToString('N') + '.cs')
try {
    [IO.File]::WriteAllText($sourceFile, $source)
    Add-Type -Path $sourceFile, (Join-Path $projectRoot 'Assets/JDY/Scripts/Data/AchievementReward.cs'), (Join-Path $PSScriptRoot 'CreditAchievementTests.cs')
    [CreditAchievementTests]::Main()
} finally {
    Remove-Item -LiteralPath $sourceFile -ErrorAction SilentlyContinue
}
