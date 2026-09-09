[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-n7-full-run',

    [string]$Seed = 'N2SAKIKO001',

    [string]$ExistingRunRoot,

    [ValidateRange(120, 1800)]
    [int]$TimeoutSeconds = 1500,

    [ValidateRange(15, 120)]
    [int]$ReloadTimeoutSeconds = 60
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
$reuseCompletedRun = -not [string]::IsNullOrWhiteSpace($ExistingRunRoot)
$runRoot = if ($reuseCompletedRun) {
    (Resolve-Path -LiteralPath $ExistingRunRoot).Path
} else {
    Join-Path $resolvedArtifactRoot "$RunName-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
}
$appDataPath = Join-Path $runRoot 'AppData\Roaming'
$localAppDataPath = Join-Path $runRoot 'AppData\Local'
$snapshotDirectory = Join-Path $runRoot 'save-matrix'
$gameLogPath = Join-Path $appDataPath 'SlayTheSpire2\logs\godot.log'
if ($reuseCompletedRun) {
    if (-not (Test-Path -LiteralPath $gameLogPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $snapshotDirectory -PathType Container)) {
        throw "The existing run root '$runRoot' does not contain the expected game log and save matrix."
    }
} else {
    New-Item -ItemType Directory -Force -Path $appDataPath, $localAppDataPath, $snapshotDirectory | Out-Null
}

$disabledCardIds = @(
    'CARD.TOGAWASAKIKO-MOMENT_MEMORY_CARD',
    'CARD.TOGAWASAKIKO-NOVA_HISTORIA_CARD',
    'CARD.TOGAWASAKIKO-ARE_THESE_LYRICS_CARD',
    'CARD.TOGAWASAKIKO-AUTHORITY_RESTORATION_CARD',
    'CARD.TOGAWASAKIKO-CAREFREE_CARD',
    'CARD.TOGAWASAKIKO-I_WANT_TO_BE_YOUR_GOD_CARD',
    'CARD.TOGAWASAKIKO-NEVER_GIVE_YOU_UP_CARD',
    'CARD.TOGAWASAKIKO-NUMBERS_AND_FACES_CARD',
    'CARD.TOGAWASAKIKO-PASSION_CARD',
    'CARD.TOGAWASAKIKO-RAISE_THE_BET_CARD',
    'CARD.TOGAWASAKIKO-WEAKNESS_CARD'
)

function Write-IsolatedSettings {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetAppDataPath
    )

    $settingsDirectory = Join-Path $TargetAppDataPath 'SlayTheSpire2\default\1'
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
        [string]$TargetAppDataPath,

        [Parameter(Mandatory = $true)]
        [string]$TargetLocalAppDataPath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    New-Item -ItemType Directory -Force -Path $TargetAppDataPath, $TargetLocalAppDataPath | Out-Null
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $gameExecutable
    $startInfo.WorkingDirectory = $resolvedGameRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment['APPDATA'] = $TargetAppDataPath
    $startInfo.Environment['LOCALAPPDATA'] = $TargetLocalAppDataPath
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
    param([Parameter(Mandatory = $true)][string]$TargetAppDataPath)
    return Get-ChildItem -LiteralPath $TargetAppDataPath -Filter 'current_run.save' -File -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Copy-ValidSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Stage,
        [Parameter(Mandatory = $true)][string]$LogText,
        [Parameter(Mandatory = $true)][scriptblock]$Ready
    )

    $destination = Join-Path $snapshotDirectory "$Stage.current_run.save"
    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        return $destination
    }

    $runSave = Find-RunSave -TargetAppDataPath $appDataPath
    if ($null -eq $runSave) {
        return $null
    }

    $saveText = Read-OpenTextFile -Path $runSave.FullName
    try {
        $saveJson = $saveText | ConvertFrom-Json
    }
    catch {
        return $null
    }

    $containsDisabledCard = @($disabledCardIds | Where-Object { $saveText.Contains($_) }).Count -gt 0
    if (-not $saveText.Contains('CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO') -or $containsDisabledCard) {
        return $null
    }

    if (-not (& $Ready $LogText $saveJson $saveText)) {
        return $null
    }

    Copy-Item -LiteralPath $runSave.FullName -Destination $destination -Force
    $copiedText = Read-OpenTextFile -Path $destination
    $null = $copiedText | ConvertFrom-Json
    if (-not $copiedText.Contains('CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO')) {
        throw "The '$Stage' snapshot did not preserve the Sakiko character ID."
    }
    return $destination
}

function Get-ManagedIssues {
    param([Parameter(Mandatory = $true)][string]$Text)
    return @(
        $Text -split "`r?`n" |
            Select-String -Pattern '\[AutoSlay\].*Run failed|Unhandled exception|System\.[A-Za-z]+Exception|Failed to load mod|Could not load mod|Localization formatting error|Error while invoking' |
            ForEach-Object { $_.Line }
    )
}

$stagePredicates = [ordered]@{
    'new-run' = {
        param($log, $save, $text)
        $log.Contains('Selecting character: CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO') -and $save.current_act_index -eq 0
    }
    'normal-combat-reward' = {
        param($log, $save, $text)
        $log.Contains('[AutoSlay] Finished Monster room') -and
        $log.Contains('[AutoSlay] Finished screen: NRewardsScreen') -and
        $text.Contains('CARD.TOGAWASAKIKO-DESIRE_CARD')
    }
    'shop' = {
        param($log, $save, $text)
        $log.Contains('[AutoSlay] Finished Shop room')
    }
    'rest-site' = {
        param($log, $save, $text)
        $log.Contains('[AutoSlay] Finished RestSite room')
    }
    'boss-reward' = {
        param($log, $save, $text)
        $log.Contains('[AutoSlay] Finished Boss room') -and
        $null -ne $save.pre_finished_room -and
        [string]$save.pre_finished_room.room_type -eq 'boss'
    }
    'act-transition' = {
        param($log, $save, $text)
        $log.Contains('[AutoSlay] Post-boss transition:') -and $save.current_act_index -ge 1
    }
}

$fullRunProcess = $null
$fullLog = ''
if ($reuseCompletedRun) {
    Write-Host "Reusing completed natural run at '$runRoot'."
    $fullLog = Read-OpenTextFile -Path $gameLogPath
} else {
    Write-IsolatedSettings -TargetAppDataPath $appDataPath
    try {
        Write-Host "Starting natural Sakiko AutoSlay run with seed $Seed."
        $arguments = @(
            '--headless',
            '--force-steam=off',
            '--autoslay',
            '--togawa-native-n7-full-run',
            "--seed=$Seed"
        )
        $fullRunProcess = Start-IsolatedGame -TargetAppDataPath $appDataPath -TargetLocalAppDataPath $localAppDataPath -Arguments $arguments
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while ([DateTime]::UtcNow -lt $deadline -and -not $fullRunProcess.HasExited) {
            Start-Sleep -Milliseconds 100
            $fullLog = Read-OpenTextFile -Path $gameLogPath
            foreach ($stage in $stagePredicates.Keys) {
                $null = Copy-ValidSnapshot -Stage $stage -LogText $fullLog -Ready $stagePredicates[$stage]
            }
        }

        if (-not $fullRunProcess.HasExited) {
            throw "The natural AutoSlay run exceeded $TimeoutSeconds seconds."
        }
        $fullRunProcess.WaitForExit()
        $fullLog = Read-OpenTextFile -Path $gameLogPath
        foreach ($stage in $stagePredicates.Keys) {
            $null = Copy-ValidSnapshot -Stage $stage -LogText $fullLog -Ready $stagePredicates[$stage]
        }
        if ($fullRunProcess.ExitCode -ne 0) {
            throw "The natural AutoSlay process exited with code $($fullRunProcess.ExitCode). Inspect '$gameLogPath'."
        }
    }
    finally {
        Stop-IsolatedGame -Process $fullRunProcess
    }
}

$requiredLogMarkers = @(
    'Selecting character: CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO',
    '[AutoSlay] Finished Monster room',
    '[AutoSlay] Finished Elite room',
    '[AutoSlay] Finished Boss room',
    '[AutoSlay] Finished Shop room',
    '[AutoSlay] Finished RestSite room',
    '[AutoSlay] Post-boss transition:',
    '[AutoSlay] Action: Victory! Run completed and returned to main menu',
    "[AutoSlay] Run completed successfully with seed=$Seed"
)
$missingMarkers = @($requiredLogMarkers | Where-Object { -not $fullLog.Contains($_) })
if ($missingMarkers.Count -gt 0) {
    throw "The natural run is missing required markers: $($missingMarkers -join ', '). Inspect '$gameLogPath'."
}

$missingSnapshots = @($stagePredicates.Keys | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $snapshotDirectory "$_.current_run.save") -PathType Leaf)
})
if ($missingSnapshots.Count -gt 0) {
    throw "The save matrix is missing snapshots: $($missingSnapshots -join ', '). Inspect '$runRoot'."
}

$fullRunIssues = @(Get-ManagedIssues -Text $fullLog)
if ($fullRunIssues.Count -gt 0) {
    throw "The natural run log contains managed issues. Inspect '$gameLogPath'."
}

$reloadResults = @()
foreach ($stage in $stagePredicates.Keys) {
    $snapshotPath = Join-Path $snapshotDirectory "$stage.current_run.save"
    $reloadRoot = Join-Path $runRoot "reload-$stage"
    $reloadAppData = Join-Path $reloadRoot 'AppData\Roaming'
    $reloadLocalAppData = Join-Path $reloadRoot 'AppData\Local'
    $reloadLogPath = Join-Path $reloadRoot 'reload.log'
    Write-IsolatedSettings -TargetAppDataPath $reloadAppData
    $saveDirectory = Join-Path $reloadAppData 'SlayTheSpire2\default\1\modded\profile1\saves'
    New-Item -ItemType Directory -Force -Path $saveDirectory | Out-Null
    Copy-Item -LiteralPath $snapshotPath -Destination (Join-Path $saveDirectory 'current_run.save') -Force

    $reloadProcess = $null
    $reloadLog = ''
    try {
        $reloadProcess = Start-IsolatedGame -TargetAppDataPath $reloadAppData -TargetLocalAppDataPath $reloadLocalAppData -Arguments @(
            '--headless',
            '--force-steam=off',
            '--togawa-native-n7-reload',
            '--log-file',
            $reloadLogPath,
            '--quit-after',
            '10000'
        )
        $reloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
        while ([DateTime]::UtcNow -lt $reloadDeadline -and -not $reloadProcess.HasExited) {
            Start-Sleep -Milliseconds 250
            $reloadLog = Read-OpenTextFile -Path $reloadLogPath
            if ($reloadLog.Contains('Phase N7: reload deserialized Togawa Sakiko.')) {
                break
            }
        }
        $reloadLog = Read-OpenTextFile -Path $reloadLogPath
        if (-not $reloadLog.Contains('Phase N7: reload deserialized Togawa Sakiko.')) {
            throw "The '$stage' save did not pass native reload validation. Inspect '$reloadLogPath'."
        }
        if (@(Get-ManagedIssues -Text $reloadLog).Count -gt 0) {
            throw "The '$stage' reload log contains managed issues. Inspect '$reloadLogPath'."
        }
    }
    finally {
        Stop-IsolatedGame -Process $reloadProcess
    }

    $reloadResults += [pscustomobject]@{
        Stage = $stage
        SnapshotPath = $snapshotPath
        ReloadLogPath = $reloadLogPath
        ReloadPassed = $true
    }
}

$currentRunAfterVictory = Find-RunSave -TargetAppDataPath $appDataPath
$profileFiles = @(Get-ChildItem -LiteralPath $appDataPath -File -Recurse -ErrorAction SilentlyContinue)
$finalPersistenceFiles = @($profileFiles | Where-Object {
    $_.Name -match 'progress|history|run' -and $_.Length -gt 0
})
if ($null -ne $currentRunAfterVictory) {
    throw "A completed-victory current_run.save still exists at '$($currentRunAfterVictory.FullName)'."
}
if ($finalPersistenceFiles.Count -eq 0) {
    throw "No final-victory profile or run-history persistence file was found under '$appDataPath'."
}

$finalVictoryReloadLogPath = Join-Path $runRoot 'final-victory-reload.log'
$finalVictoryReloadProcess = $null
try {
    $finalVictoryReloadProcess = Start-IsolatedGame -TargetAppDataPath $appDataPath -TargetLocalAppDataPath $localAppDataPath -Arguments @(
        '--headless',
        '--force-steam=off',
        '--log-file',
        $finalVictoryReloadLogPath,
        '--quit-after',
        '1800'
    )
    $finalReloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $finalReloadDeadline -and -not $finalVictoryReloadProcess.HasExited) {
        Start-Sleep -Milliseconds 250
    }
    if (-not $finalVictoryReloadProcess.HasExited) {
        throw "The final-victory profile reload exceeded $ReloadTimeoutSeconds seconds."
    }
    $finalVictoryReloadProcess.WaitForExit()
    if ($finalVictoryReloadProcess.ExitCode -ne 0) {
        throw "The final-victory profile reload exited with code $($finalVictoryReloadProcess.ExitCode)."
    }
    $finalVictoryReloadLog = Read-OpenTextFile -Path $finalVictoryReloadLogPath
    if (-not $finalVictoryReloadLog.Contains('Native bootstrap initialized.')) {
        throw "The final-victory profile reload did not initialize Togawa Sakiko. Inspect '$finalVictoryReloadLogPath'."
    }
    if (@(Get-ManagedIssues -Text $finalVictoryReloadLog).Count -gt 0) {
        throw "The final-victory profile reload log contains managed issues. Inspect '$finalVictoryReloadLogPath'."
    }
}
finally {
    Stop-IsolatedGame -Process $finalVictoryReloadProcess
}

$result = [pscustomobject]@{
    Seed = $Seed
    CharacterSelected = $true
    VictoryReached = $true
    RoomTypes = @('Monster', 'Elite', 'Boss', 'Shop', 'RestSite')
    SaveMatrixStages = @($stagePredicates.Keys)
    SaveReloadPassCount = $reloadResults.Count
    DisabledCardsGenerated = 0
    FullRunManagedIssueCount = $fullRunIssues.Count
    CurrentRunRemovedAfterVictory = $true
    FinalPersistenceFileCount = $finalPersistenceFiles.Count
    FinalVictoryReloadPassed = $true
    FinalVictoryReloadLogPath = $finalVictoryReloadLogPath
    GameLogPath = $gameLogPath
    SnapshotDirectory = $snapshotDirectory
    ReloadResults = $reloadResults
    ArtifactRoot = $runRoot
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runRoot 'summary.json') -Encoding utf8
$result
