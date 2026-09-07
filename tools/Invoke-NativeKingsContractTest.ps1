[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-kings-contract',

    [string]$Seed = 'KINGSSAKIKO001',

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
$reloadLogPath = Join-Path $runRoot 'kings-reload.log'
$saveSnapshotPath = Join-Path $runRoot 'current_run.save.kings-pending.snapshot'
$saveProbePath = Join-Path $runRoot 'latest-save-probe.json'
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

function Find-RunSaves {
    return Get-ChildItem -LiteralPath $appDataPath -Filter 'current_run.save*' -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @('current_run.save', 'current_run.save.backup') }
}

function Test-SavedKingsPending {
    param([Parameter(Mandatory = $true)][string]$SaveText)

    $save = $SaveText | ConvertFrom-Json
    $players = @($save.players | Where-Object {
        $_.character_id -eq 'CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO'
    })
    if ($players.Count -ne 1) {
        return $false
    }

    $serializedRelics = if ($null -ne $players[0].PSObject.Properties['relics']) {
        @($players[0].relics)
    }
    else {
        @()
    }
    $relics = @($serializedRelics | Where-Object {
        $_.id -eq 'RELIC.TOGAWASAKIKO-STARTER_RELIC_TOGAWA_SAKIKO'
    })
    $kingsCards = @($players[0].deck | Where-Object {
        $_.id -eq 'CARD.TOGAWASAKIKO-KINGS_CARD'
    })
    if ($relics.Count -ne 0 -or $kingsCards.Count -ne 0) {
        return $false
    }

    $carriers = @($save.modifiers | Where-Object {
        $_.id -eq 'MODIFIER.TOGAWASAKIKO-KINGS_REWARD_CARRIER_MODIFIER'
    })
    if ($carriers.Count -ne 1 -or $null -eq $carriers[0].props) {
        return $false
    }

    $pendingProperties = @($carriers[0].props.strings | Where-Object {
        $_.name -eq 'PendingPlayerIds'
    })
    if ($pendingProperties.Count -ne 1) {
        return $false
    }

    $pendingPlayerIds = @([string]$pendingProperties[0].value -split ',')
    return $pendingPlayerIds -contains [string]$players[0].net_id
}

function Get-SavedKingsProbe {
    param([Parameter(Mandatory = $true)][string]$SaveText)

    $save = $SaveText | ConvertFrom-Json
    $players = @($save.players | Where-Object {
        $_.character_id -eq 'CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO'
    })
    if ($players.Count -ne 1) {
        return [pscustomobject]@{
            Passed = $false
            PlayerCount = $players.Count
            StarterRelicCount = -1
            PersistentKingsCardCount = -1
            CarrierCount = -1
            PendingValue = $null
            PlayerNetId = $null
        }
    }

    $serializedRelics = if ($null -ne $players[0].PSObject.Properties['relics']) {
        @($players[0].relics)
    }
    else {
        @()
    }
    $relics = @($serializedRelics | Where-Object {
        $_.id -eq 'RELIC.TOGAWASAKIKO-STARTER_RELIC_TOGAWA_SAKIKO'
    })
    $kingsCards = @($players[0].deck | Where-Object {
        $_.id -eq 'CARD.TOGAWASAKIKO-KINGS_CARD'
    })
    $carriers = @($save.modifiers | Where-Object {
        $_.id -eq 'MODIFIER.TOGAWASAKIKO-KINGS_REWARD_CARRIER_MODIFIER'
    })
    $pendingProperties = if ($carriers.Count -eq 1 -and $null -ne $carriers[0].props) {
        @($carriers[0].props.strings | Where-Object { $_.name -eq 'PendingPlayerIds' })
    }
    else {
        @()
    }
    $pendingValue = if ($pendingProperties.Count -eq 1) {
        [string]$pendingProperties[0].value
    }
    else {
        $null
    }
    $playerNetId = [string]$players[0].net_id
    $pendingPlayerIds = if ($null -ne $pendingValue) { @($pendingValue -split ',') } else { @() }

    return [pscustomobject]@{
        Passed = $relics.Count -eq 0 -and
            $kingsCards.Count -eq 0 -and
            $carriers.Count -eq 1 -and
            $pendingProperties.Count -eq 1 -and
            $pendingPlayerIds -contains $playerNetId
        PlayerCount = $players.Count
        StarterRelicCount = $relics.Count
        PersistentKingsCardCount = $kingsCards.Count
        CarrierCount = $carriers.Count
        PendingValue = $pendingValue
        PlayerNetId = $playerNetId
    }
}

Write-IsolatedSettings

$gameplayProcess = $null
$reloadProcess = $null
$gameplayLog = ''
$reloadLog = ''
$runSave = $null
$saveReady = $false

$requiredGameplayMarkers = @(
    'Phase N5 pure contract tests passed (725 assertions).',
    'Kings lifecycle: verified one hidden native run carrier for the Sakiko player.',
    'Kings lifecycle: removed the starter relic and verified no persistent Kings card before victory.',
    'Kings lifecycle: armed one nonstacking Kings power through native PowerCmd.Apply.',
    'Kings lifecycle: AfterCombatEnd persisted pending state before power teardown.',
    'Kings lifecycle: AfterCombatVictory observed pending state after power teardown and before native save.',
    'Kings lifecycle: native encounter reward generated with 2 options while pending state remained active.',
    'Kings lifecycle: native card reward consumption cleared pending state.'
)
$requiredReloadMarkers = @(
    'Kings lifecycle: reload restored pending state and generated a 2-option combat card reward.',
    'Kings lifecycle: reload consumed the native card reward and cleared pending state.'
)

try {
    $gameplayProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--autoslay',
        '--togawa-native-kings-contract-smoke',
        "--seed=$Seed"
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($GameplayTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline -and -not $gameplayProcess.HasExited) {
        Start-Sleep -Milliseconds 50
        $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath

        if (-not $saveReady) {
            $candidateSaves = @(Find-RunSaves | Sort-Object @{ Expression = { $_.Name -ne 'current_run.save' } })
            $saveProbes = @()
            foreach ($candidateSave in $candidateSaves) {
                try {
                    $saveText = Read-OpenTextFile -Path $candidateSave.FullName
                    $saveProbe = Get-SavedKingsProbe -SaveText $saveText
                    $saveProbes += [pscustomobject]@{
                        Name = $candidateSave.Name
                        Probe = $saveProbe
                    }
                    if ($saveProbe.Passed) {
                        Copy-Item -LiteralPath $candidateSave.FullName -Destination $saveSnapshotPath -Force
                        $runSave = $candidateSaves | Where-Object Name -eq 'current_run.save' | Select-Object -First 1
                        $saveReady = $true
                        break
                    }
                }
                catch {
                    $saveProbes += [pscustomobject]@{
                        Name = $candidateSave.Name
                        Error = $_.Exception.Message
                    }
                }
            }

            $saveProbes | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $saveProbePath -Encoding utf8
        }

        $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
        if ($saveReady -and $missingMarkers.Count -eq 0) {
            break
        }
    }

    $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
    $missingMarkers = @($requiredGameplayMarkers | Where-Object { -not $gameplayLog.Contains($_) })
    if ($missingMarkers.Count -ne 0 -or -not $saveReady) {
        throw "Kings gameplay lifecycle did not finish within $GameplayTimeoutSeconds seconds. Missing markers: $($missingMarkers -join '; '). Saved pending state captured: $saveReady. Inspect '$gameplayLogPath'."
    }

    Stop-IsolatedGame -Process $gameplayProcess
    $gameplayProcess = $null

    $saveCaptureDeadline = [DateTime]::UtcNow.AddSeconds(5)
    while ([DateTime]::UtcNow -lt $saveCaptureDeadline -and -not $saveReady) {
        $candidateSaves = @(Find-RunSaves | Sort-Object @{ Expression = { $_.Name -ne 'current_run.save' } })
        $saveProbes = @()
        foreach ($candidateSave in $candidateSaves) {
            try {
                $saveText = Read-OpenTextFile -Path $candidateSave.FullName
                $saveProbe = Get-SavedKingsProbe -SaveText $saveText
                $saveProbes += [pscustomobject]@{
                    Name = $candidateSave.Name
                    Probe = $saveProbe
                }
                if ($saveProbe.Passed) {
                    Copy-Item -LiteralPath $candidateSave.FullName -Destination $saveSnapshotPath -Force
                    $runSave = $candidateSaves | Where-Object Name -eq 'current_run.save' | Select-Object -First 1
                    $saveReady = $true
                    break
                }
            }
            catch {
                $saveProbes += [pscustomobject]@{
                    Name = $candidateSave.Name
                    Error = $_.Exception.Message
                }
            }
        }

        $saveProbes | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $saveProbePath -Encoding utf8
        if (-not $saveReady) {
            Start-Sleep -Milliseconds 50
        }
    }
    if (-not $saveReady -or $null -eq $runSave) {
        throw "Kings gameplay lifecycle finished, but its pending native save was not captured. Inspect '$saveProbePath'."
    }

    Copy-Item -LiteralPath $saveSnapshotPath -Destination $runSave.FullName -Force

    $reloadProcess = Start-IsolatedGame -Arguments @(
        '--headless',
        '--force-steam=off',
        '--togawa-native-kings-contract-reload',
        '--log-file',
        $reloadLogPath,
        '--quit-after',
        '10000'
    )

    $reloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $reloadDeadline -and -not $reloadProcess.HasExited) {
        Start-Sleep -Milliseconds 100
        $reloadLog = Read-OpenTextFile -Path $reloadLogPath
        $missingReloadMarkers = @($requiredReloadMarkers | Where-Object { -not $reloadLog.Contains($_) })
        if ($missingReloadMarkers.Count -eq 0) {
            break
        }
    }

    $reloadLog = Read-OpenTextFile -Path $reloadLogPath
    $missingReloadMarkers = @($requiredReloadMarkers | Where-Object { -not $reloadLog.Contains($_) })
    if ($missingReloadMarkers.Count -ne 0) {
        throw "The strict Kings save/reload lifecycle did not finish. Missing markers: $($missingReloadMarkers -join '; '). Inspect '$reloadLogPath'."
    }

    Stop-IsolatedGame -Process $reloadProcess
    $reloadProcess = $null
}
finally {
    Stop-IsolatedGame -Process $gameplayProcess
    Stop-IsolatedGame -Process $reloadProcess
}

$managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Kings lifecycle actual-game contract failed|Phase N5 pure contract failed'
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
    PureAssertionCount = 725
    GameplayMarkerCount = $requiredGameplayMarkers.Count
    GameplayMarkersPassed = $true
    SavedPendingStateCaptured = $true
    NoPersistentCardOrRelicCarrierPassed = $true
    NaturalRewardOptionCount = 2
    NaturalRewardClearPassed = $true
    ReloadMarkerCount = $requiredReloadMarkers.Count
    ReloadedRewardOptionCount = 2
    ReloadPassed = $true
    GameplayManagedIssueCount = $gameplayIssueLines.Count
    ReloadManagedIssueCount = $reloadIssueLines.Count
    SaveSnapshotPath = $saveSnapshotPath
    SaveProbePath = $saveProbePath
    GameplayLogPath = $gameplayLogPath
    ReloadLogPath = $reloadLogPath
    ArtifactRoot = $runRoot
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$summary

if ($gameplayIssueLines.Count -gt 0 -or $reloadIssueLines.Count -gt 0) {
    throw "Kings lifecycle logs contain managed exception, localization, or mod-load failures. Inspect '$runRoot'."
}
