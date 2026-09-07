[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-n6-contract',

    [string]$Seed = 'N6SAKIKO001',

    [ValidateRange(60, 300)]
    [int]$GameplayTimeoutSeconds = 240,

    [ValidateRange(10, 120)]
    [int]$ReloadTimeoutSeconds = 45
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$gameExecutable = Join-Path $resolvedGameRoot 'SlayTheSpire2.exe'
if (-not (Test-Path -LiteralPath $gameExecutable -PathType Leaf)) {
    throw "SlayTheSpire2.exe was not found under '$resolvedGameRoot'."
}

$deployedManifest = Join-Path $resolvedGameRoot 'mods\TogawaSakiko\TogawaSakiko.json'
if (-not (Test-Path -LiteralPath $deployedManifest -PathType Leaf)) {
    throw "TogawaSakiko is not deployed at '$deployedManifest'."
}

$resolvedArtifactRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArtifactRoot)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runRoot = Join-Path $resolvedArtifactRoot "$RunName-$timestamp"
$appDataPath = Join-Path $runRoot 'AppData\Roaming'
$localAppDataPath = Join-Path $runRoot 'AppData\Local'
$gameplayLogPath = Join-Path $appDataPath 'SlayTheSpire2\logs\godot.log'
$reloadLogPath = Join-Path $runRoot 'n6-reload.log'
$saveSnapshotPath = Join-Path $runRoot 'current_run.save.n6.snapshot'
$summaryPath = Join-Path $runRoot 'summary.json'

function Write-IsolatedSettings {
    $settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
    New-Item -ItemType Directory -Force -Path $settingsDirectory | Out-Null
    @{
        schema_version = 8
        language = 'eng'
        mod_settings = @{
            mods_enabled = $true
            mod_list = @()
        }
        seen_ea_disclaimer = $true
        skip_intro_logo = $true
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $settingsDirectory 'settings.save') -Encoding utf8
}

function Start-IsolatedGame {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    New-Item -ItemType Directory -Force -Path $appDataPath, $localAppDataPath | Out-Null
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $gameExecutable
    $startInfo.WorkingDirectory = $resolvedGameRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment['APPDATA'] = $appDataPath
    $startInfo.Environment['LOCALAPPDATA'] = $localAppDataPath
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    return [System.Diagnostics.Process]::Start($startInfo)
}

function Stop-IsolatedGame {
    param([System.Diagnostics.Process]$Process)

    if ($null -ne $Process -and -not $Process.HasExited) {
        $Process.Kill($true)
        $Process.WaitForExit()
    }
}

function Read-OpenTextFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return ''
    }

    try {
        return [string](Get-Content -Raw -LiteralPath $Path -ErrorAction Stop)
    }
    catch {
        return ''
    }
}

function Find-RunSave {
    return Get-ChildItem -LiteralPath $appDataPath -Filter 'current_run.save' -File -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

Write-IsolatedSettings

$gameplayProcess = $null
$reloadProcess = $null
$gameplayLog = ''
$reloadLog = ''
$runSave = $null
$saveReady = $false

$requiredGameplayMarkers = @(
    'Phase N6 pure contract tests passed (73 assertions).',
    'Embarking on a singleplayer TOGAWASAKIKO-TOGAWA_SAKIKO run.',
    'Phase N6 contract: Blazing Hairband boss swap, random persistent add, disabled-card exclusion, and combat idempotence passed.',
    'Phase N6 contract: Colorful Notebook combat-entry hook applied exactly 1 Dazzling.',
    'Phase N6 contract: all six potions consumed through native hooks.',
    'Phase N6 contract: Fountain Drink native reward hook passed full-slot and open-slot branches.',
    'Phase N6 contract: The Compass owner-side participant hook applied 2 self and 1 enemy Dazzling.',
    'Phase N6 contract: Golden Pocket Watch persisted its count and triggered 2 Strength on card 12.',
    'Phase N6 contract: The Doll triggered exactly once for 2 Hype on player turn 2.',
    'Phase N6 contract: The Third Movement dealt 45/45/45/15 damage and consumed exactly three uses.',
    "Phase N6 contract: Porcelain Cup forced one Heart's Barrier while preserving the Kings two-card reward.",
    'Phase N6 contract: Masquerade Mask counted only successful purges and added a native reward at 3.',
    'Phase N6 contract: native mid-combat save written with Watch=7, Mask=2, Doll=1, ThirdMovement=1, EarlGrey=1, and purge rewards.',
    'Phase N6 contract: Cute Animal Band-Aid healed exactly 1 only after successful post-combat reward selection.',
    'Phase N6 contract: all relic, potion, hook-order, reward, and save preparation checks passed. Relics=11/11, Potions=6/6.'
)
$reloadMarker = 'Phase N6 contract: reload restored Watch=7, Mask=2, Doll=1, ThirdMovement=1, EarlGrey=1, PurgeRewards='

try {
    $gameplayProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--autoslay',
        '--togawa-native-n6-contract-smoke',
        "--seed=$Seed"
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($GameplayTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline -and -not $gameplayProcess.HasExited) {
        Start-Sleep -Milliseconds 100
        $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
        $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
        if ($missingMarkers.Count -ne 0) {
            continue
        }

        $runSave = Find-RunSave
        if ($null -ne $runSave) {
            Copy-Item -LiteralPath $runSave.FullName -Destination $saveSnapshotPath -Force
            $saveReady = $true
            break
        }
    }

    $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
    $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
    if ($missingMarkers.Count -ne 0 -or -not $saveReady) {
        throw "Phase N6 gameplay contracts did not finish within $GameplayTimeoutSeconds seconds. Missing markers: $($missingMarkers -join '; '). Save captured: $saveReady. Inspect '$gameplayLogPath'."
    }

    Stop-IsolatedGame -Process $gameplayProcess
    $gameplayProcess = $null
    Copy-Item -LiteralPath $saveSnapshotPath -Destination $runSave.FullName -Force

    $reloadProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--togawa-native-reload-smoke',
        '--togawa-native-n6-contract-reload',
        '--log-file',
        $reloadLogPath,
        '--quit-after',
        '10000'
    )

    $reloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $reloadDeadline -and -not $reloadProcess.HasExited) {
        Start-Sleep -Milliseconds 100
        $reloadLog = Read-OpenTextFile -Path $reloadLogPath
        if ($reloadLog.Contains($reloadMarker)) {
            break
        }
    }

    $reloadLog = Read-OpenTextFile -Path $reloadLogPath
    if (-not $reloadLog.Contains($reloadMarker)) {
        throw "The strict Phase N6 reload marker was not found. Inspect '$reloadLogPath'."
    }

    Stop-IsolatedGame -Process $reloadProcess
    $reloadProcess = $null
}
finally {
    Stop-IsolatedGame -Process $gameplayProcess
    Stop-IsolatedGame -Process $reloadProcess
}

$managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Phase N6 actual-game contract failed|Phase N6 pure contract failed'
$gameplayIssueLines = @(
    $gameplayLog -split "`r?`n" |
        Select-String -Pattern $managedIssuePattern |
        ForEach-Object { $_.Line }
)
$reloadIssueLines = @(
    $reloadLog -split "`r?`n" |
        Select-String -Pattern $managedIssuePattern |
        ForEach-Object { $_.Line }
)

$summary = [pscustomobject]@{
    Seed = $Seed
    PureAssertionCount = 73
    GameplayMarkerCount = $requiredGameplayMarkers.Count
    GameplayMarkersPassed = $true
    RelicsVerified = 11
    PotionsVerified = 6
    SaveCaptured = $true
    ReloadPassed = $true
    GameplayManagedIssueCount = $gameplayIssueLines.Count
    ReloadManagedIssueCount = $reloadIssueLines.Count
    SaveSnapshotPath = $saveSnapshotPath
    GameplayLogPath = $gameplayLogPath
    ReloadLogPath = $reloadLogPath
    ArtifactRoot = $runRoot
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$summary

if ($gameplayIssueLines.Count -gt 0 -or $reloadIssueLines.Count -gt 0) {
    throw "Phase N6 logs contain managed exception, localization, or mod-load failures. Inspect '$runRoot'."
}
