[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$ArtifactRoot,
    [ValidateSet('eng', 'zhs')][string]$Language = 'eng',
    [switch]$Rendered,
    [ValidateRange(30, 300)][int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$testGame = (Resolve-Path -LiteralPath $GameRoot).Path
$testRoot = Join-Path ([IO.Path]::GetFullPath($ArtifactRoot)) ("ancient-$Language-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$testRoaming = Join-Path $testRoot 'AppData/Roaming'
$testLocal = Join-Path $testRoot 'AppData/Local'
$testSettings = Join-Path $testRoaming 'SlayTheSpire2/default/1'
New-Item -ItemType Directory -Force -Path $testSettings, $testLocal | Out-Null
@{
    schema_version = 8
    language = $Language
    mod_settings = @{ mods_enabled = $true; mod_list = @() }
    seen_ea_disclaimer = $true
    skip_intro_logo = $true
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $testSettings 'settings.save') -Encoding utf8
$testLog = Join-Path $testRoot 'game.log'
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = Join-Path $testGame 'SlayTheSpire2.exe'
$start.WorkingDirectory = $testGame
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$start.Environment['APPDATA'] = $testRoaming
$start.Environment['LOCALAPPDATA'] = $testLocal
if (-not $Rendered) { $start.ArgumentList.Add('--headless') }
foreach ($argument in @('--force-steam=off', '--autoslay', '--togawa-ancient-rewards-test', '--seed=ANCIENT001',
    '--log-file', $testLog, "--togawa-ancient-output=$testRoot")) {
    $start.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::Start($start)
$text = ''
try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if (Test-Path -LiteralPath $testLog) {
            $text = Get-Content -Raw -LiteralPath $testLog
            if ($text.Contains('Ancient rewards: PASS') -or $text.Contains('[AutoSlay] Run failed')) { break }
        }
    }
    if (Test-Path -LiteralPath $testLog) { $text = Get-Content -Raw -LiteralPath $testLog }
}
finally {
    if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
}
$success = [regex]::Match($text, 'Ancient rewards: PASS \((\d+) assertions\)')
$issues = @($text -split "`n" | Where-Object { $_ -match '^\[ERROR\]|^ERROR:.*Exception' })
$passed = $success.Success -and $issues.Count -eq 0 -and
    (-not $Rendered -or (Test-Path -LiteralPath (Join-Path $testRoot 'ancient-cards.png')))
$result = [ordered]@{
    Passed = $passed
    Language = $Language
    Rendered = [bool]$Rendered
    Assertions = if ($success.Success) { [int]$success.Groups[1].Value } else { 0 }
    ManagedIssues = $issues
    Log = $testLog
    PackageHashes = @(Get-ChildItem -LiteralPath (Join-Path $testGame 'mods/TogawaSakiko') -File |
        Get-FileHash -Algorithm SHA256 | Select-Object @{n='File';e={[IO.Path]::GetFileName($_.Path)}}, Hash)
}
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $testRoot 'summary.json') -Encoding utf8
$result | ConvertTo-Json -Depth 6
if (-not $passed) { throw "Ancient reward diagnostics failed. Inspect $testLog" }
