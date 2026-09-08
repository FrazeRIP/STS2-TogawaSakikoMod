param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-n3-contract',

    [string]$Seed = 'N3SAKIKO001',

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
$reloadLogPath = Join-Path $runRoot 'n3-reload.log'
$saveSnapshotPath = Join-Path $runRoot 'current_run.save.snapshot'
$summaryPath = Join-Path $runRoot 'summary.json'

function Write-IsolatedSettings {
    $settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
    $settingsPath = Join-Path $settingsDirectory 'settings.save'
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
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settingsPath -Encoding utf8
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

function Get-ValidatedDeckIds {
    param([Parameter(Mandatory = $true)][string]$SaveText)

    $save = $SaveText | ConvertFrom-Json
    $players = @($save.players | Where-Object {
        $_.character_id -eq 'CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO'
    })
    if ($players.Count -ne 1) {
        throw "Expected one Togawa player in the save, found $($players.Count)."
    }

    return @($players[0].deck | ForEach-Object { [string]$_.id })
}

function Get-ExactCount {
    param(
        [Parameter(Mandatory = $true)][string[]]$Values,
        [Parameter(Mandatory = $true)][string]$Expected
    )

    return @($Values | Where-Object { $_ -eq $Expected }).Count
}

Write-IsolatedSettings

$gameplayProcess = $null
$reloadProcess = $null
$gameplayLog = ''
$reloadLog = ''
$runSave = $null
$saveReady = $false
$deckIds = @()

$requiredGameplayMarkers = @(
    'Phase N3 pure contract tests passed (31 assertions).',
    'Phase N3 contract: starting synchronized acceptance action',
    'Phase N3 contract: deck identity, five-pile, prevention, hook, history, and state checks passed.',
    'Phase N3 contract: signed power ledger event, filter, round, removal, and reset checks passed.',
    'Phase N3 contract: power copy stack, instance, dynamic state, duration, and rejection checks passed.',
    'Phase N3 contract: Hype explicit loss, turn clear, damage, retention, owner, and teardown setup checks passed.',
    'Phase N3 contract: visual play-queue node and stale play action cleanup passed.',
    'Phase N3 contract: all post-combat reset, outside removal, Hairband idempotence, and pre-save checks passed.',
    'Native gameplay smoke: Monochrome Hairband added Desire to the deck; success=True.'
)
$reloadMarker = 'Phase N3 contract: reload preserved exactly one Hairband Desire, one added Silent Farewell, and no removed Two Moons.'

try {
    Write-Host "Starting isolated Phase N3 contract run with seed $Seed."
    $gameplayProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--autoslay',
        '--togawa-native-n3-contract-smoke',
        "--seed=$Seed"
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($GameplayTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline -and -not $gameplayProcess.HasExited) {
        Start-Sleep -Milliseconds 250
        $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
        $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
        if ($missingMarkers.Count -ne 0) {
            continue
        }

        $runSave = Find-RunSave
        if ($null -eq $runSave) {
            continue
        }

        try {
            $saveText = Read-OpenTextFile -Path $runSave.FullName
            $candidateIds = Get-ValidatedDeckIds -SaveText $saveText
            $desireCount = Get-ExactCount -Values $candidateIds -Expected 'CARD.TOGAWASAKIKO-DESIRE_CARD'
            $silentFarewellCount = Get-ExactCount -Values $candidateIds -Expected 'CARD.TOGAWASAKIKO-SILENT_FAREWELL_CARD'
            $twoMoonsCount = Get-ExactCount -Values $candidateIds -Expected 'CARD.TOGAWASAKIKO-TWO_MOONS_CARD'
            if ($desireCount -eq 1 -and $silentFarewellCount -eq 1 -and $twoMoonsCount -eq 0) {
                Copy-Item -LiteralPath $runSave.FullName -Destination $saveSnapshotPath -Force
                $snapshotText = Read-OpenTextFile -Path $saveSnapshotPath
                $deckIds = Get-ValidatedDeckIds -SaveText $snapshotText
                $saveReady = $true
                break
            }
        }
        catch {
            $saveReady = $false
        }
    }

    $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
    $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
    if ($missingMarkers.Count -ne 0 -or -not $saveReady) {
        $missingText = $missingMarkers -join '; '
        throw "Phase N3 gameplay contracts did not finish within $GameplayTimeoutSeconds seconds. Missing markers: $missingText. Inspect '$gameplayLogPath'."
    }

    Stop-IsolatedGame -Process $gameplayProcess
    $gameplayProcess = $null
    Copy-Item -LiteralPath $saveSnapshotPath -Destination $runSave.FullName -Force
    Write-Host 'Actual-game command, hook-order, teardown, and post-victory save checks passed.'

    Write-Host 'Starting strict Phase N3 save reload check.'
    $reloadProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--togawa-native-reload-smoke',
        '--togawa-native-n3-contract-reload',
        '--log-file',
        $reloadLogPath,
        '--quit-after',
        '10000'
    )

    $reloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $reloadDeadline -and -not $reloadProcess.HasExited) {
        Start-Sleep -Milliseconds 250
        $reloadLog = Read-OpenTextFile -Path $reloadLogPath
        if ($reloadLog.Contains($reloadMarker)) {
            break
        }
    }

    $reloadLog = Read-OpenTextFile -Path $reloadLogPath
    if (-not $reloadLog.Contains($reloadMarker)) {
        throw "The strict Phase N3 reload marker was not found. Inspect '$reloadLogPath'."
    }

    Stop-IsolatedGame -Process $reloadProcess
    $reloadProcess = $null
    Write-Host 'Strict native save reload check passed.'
}
finally {
    Stop-IsolatedGame -Process $gameplayProcess
    Stop-IsolatedGame -Process $reloadProcess
}

$managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Phase N3 actual-game contract failure|Phase N3 pure contract failure'
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
    PureAssertionCount = 31
    GameplayMarkerCount = $requiredGameplayMarkers.Count
    GameplayMarkersPassed = $true
    ExactDesireCount = Get-ExactCount -Values $deckIds -Expected 'CARD.TOGAWASAKIKO-DESIRE_CARD'
    ExactSilentFarewellCount = Get-ExactCount -Values $deckIds -Expected 'CARD.TOGAWASAKIKO-SILENT_FAREWELL_CARD'
    ExactTwoMoonsCount = Get-ExactCount -Values $deckIds -Expected 'CARD.TOGAWASAKIKO-TWO_MOONS_CARD'
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
    throw "Phase N3 logs contain managed exception, localization, or mod-load failures. Inspect '$runRoot'."
}
