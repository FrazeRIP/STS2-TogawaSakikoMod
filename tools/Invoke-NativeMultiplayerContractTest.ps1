param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [Parameter(Mandatory = $true)][string]$ArtifactRoot,
    [string]$RunName = 'native-multiplayer-contract',
    [string]$Seed = 'SAKIKOMP001',
    [ValidateSet('vanilla', 'mixed-host', 'mixed-client', 'duplicate', 'four-player')]
    [string[]]$Scenarios = @('vanilla', 'mixed-host', 'mixed-client', 'duplicate', 'four-player'),
    [ValidateRange(120, 1800)][int]$TimeoutSeconds = 600,
    [ValidateRange(1024, 60000)][int]$FirstPort = 27851,
    [switch]$StartingEvent,
    [ValidateRange(0, 2)][int]$StartingChoiceOffset = 0,
    [switch]$VerifyReplay
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$executable = Join-Path $resolvedGameRoot 'SlayTheSpire2.exe'
$manifest = Join-Path $resolvedGameRoot 'mods\TogawaSakiko\TogawaSakiko.json'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf) -or -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw 'A deployed native Sakiko game installation is required.'
}
$resolvedArtifacts = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArtifactRoot)
$runRoot = Join-Path $resolvedArtifacts ($RunName + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$outcomes = [System.Collections.Generic.List[object]]::new()
$packageHashes = @(Get-ChildItem -LiteralPath (Split-Path -Parent $manifest) -File |
    Where-Object { $_.Extension -in '.dll', '.pck', '.json' } |
    ForEach-Object { [pscustomobject]@{ Name = $_.Name; SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
$scenarioIndex = 0

try {
    foreach ($scenario in $Scenarios) {
        $scenarioRoot = Join-Path $runRoot $scenario
        New-Item -ItemType Directory -Path $scenarioRoot -Force | Out-Null
        $peerCount = if ($scenario -eq 'four-player') { 4 } else { 2 }
        $processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
        $logs = [System.Collections.Generic.List[string]]::new()
        $port = $FirstPort + $scenarioIndex
        $scenarioIndex++
        $scenarioError = $null
        try {
            Write-Host "Starting $scenario with $peerCount separate native ENet processes on port $port."
            foreach ($peerId in 1..$peerCount) {
                $peerRoot = Join-Path $scenarioRoot "peer-$peerId"
                $roaming = Join-Path $peerRoot 'AppData\Roaming'
                $localData = Join-Path $peerRoot 'AppData\Local'
                $settingsDirectory = Join-Path $roaming 'SlayTheSpire2\default\1'
                New-Item -ItemType Directory -Path $settingsDirectory, $localData -Force | Out-Null
                @{
                    schema_version = 8; language = $(if ($peerId % 2 -eq 0) { 'zhs' } else { 'eng' })
                    mod_settings = @{ mods_enabled = $true; mod_list = @() }
                    seen_ea_disclaimer = $true; skip_intro_logo = $true
                    volume_master = 0
                } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $settingsDirectory 'settings.save') -Encoding utf8
                $log = Join-Path $peerRoot 'game.log'
                $logs.Add($log)
                $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
                $startInfo.FileName = $executable
                $startInfo.WorkingDirectory = $resolvedGameRoot
                $startInfo.UseShellExecute = $false
                $startInfo.CreateNoWindow = $true
                $startInfo.Environment['APPDATA'] = $roaming
                $startInfo.Environment['LOCALAPPDATA'] = $localData
                foreach ($argument in @('--headless', '--force-steam=off', '--togawa-native-multiplayer-contract',
                    "--togawa-mp-id=$peerId", "--togawa-mp-count=$peerCount", "--togawa-mp-port=$port",
                    "--togawa-mp-scenario=$scenario", "--togawa-mp-artifacts=$scenarioRoot", "--seed=$Seed",
                    '--log-file', $log)) {
                    $startInfo.ArgumentList.Add($argument)
                }
                if ($StartingEvent) {
                    $startInfo.ArgumentList.Add('--togawa-mp-starting-event')
                    $startInfo.ArgumentList.Add("--togawa-mp-starting-choice-offset=$StartingChoiceOffset")
                }
                $processes.Add([System.Diagnostics.Process]::Start($startInfo))
                if ($peerId -eq 1) { Start-Sleep -Milliseconds 1500 }
            }
            $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
            while ([DateTime]::UtcNow -lt $deadline) {
                $failures = @(Get-ChildItem -LiteralPath $scenarioRoot -Filter 'peer-*-failure.json' -File)
                if ($failures.Count -gt 0) {
                    throw "Native contract failure: $([System.IO.File]::ReadAllText($failures[0].FullName))"
                }
                $results = @(Get-ChildItem -LiteralPath $scenarioRoot -Filter 'peer-*-result.json' -File |
                    Where-Object Name -Match '^peer-[0-9]+-result\.json$')
                if ($results.Count -eq $peerCount) { break }
                $exited = @($processes | Where-Object HasExited)
                if ($exited.Count -gt 0) { throw "A peer exited before all $peerCount results were written. Inspect '$scenarioRoot'." }
                Start-Sleep -Milliseconds 300
            }
            $results = @(Get-ChildItem -LiteralPath $scenarioRoot -Filter 'peer-*-result.json' -File |
                Where-Object Name -Match '^peer-[0-9]+-result\.json$')
            if ($results.Count -ne $peerCount) { throw "Scenario '$scenario' timed out with $($results.Count)/$peerCount peer results." }
            $evidence = @($results | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json })
            if (@($evidence | Where-Object { -not $_.Passed -or $_.TestMode }).Count -gt 0) {
                throw "Scenario '$scenario' did not pass in actual game mode."
            }
            if ($StartingEvent) {
                foreach ($peerId in 1..$peerCount) {
                    $startingResult = Join-Path $scenarioRoot "peer-$peerId-starting-result.json"
                    if (-not (Test-Path -LiteralPath $startingResult -PathType Leaf) -or
                        -not (Get-Content -Raw -LiteralPath $startingResult | ConvertFrom-Json).Passed) {
                        throw "Native starting-event evidence is missing or failed for $scenario peer $peerId."
                    }
                }
            }
            # Native context text contains process-local object hashes; checksum IDs and values are the protocol contract.
            $checksumCopies = @($evidence | ForEach-Object {
                ConvertTo-Json -InputObject @($_.NativeChecksums | Select-Object Id, Value) -Depth 8 -Compress
            })
            if (@($checksumCopies | Select-Object -Unique).Count -ne 1) {
                throw "Scenario '$scenario' generated different native checksum sequences between processes."
            }
            if (@($evidence[0].NativeChecksums).Count -eq 0) { throw 'Zero native checksums is not a passing run.' }
            $issues = @($logs | ForEach-Object {
                if (Test-Path -LiteralPath $_) {
                    Get-Content -LiteralPath $_ | Select-String -Pattern '\[ERROR\]|Unhandled exception|Exception:|State divergence|Multiplayer contract FAILED|Failed to load mod|Could not load mod'
                }
            })
            if ($issues.Count -gt 0) { throw "Scenario '$scenario' logs contain $($issues.Count) managed or synchronization errors." }
            if ($VerifyReplay) {
                foreach ($playerIndex in 0..($peerCount - 1)) {
                    $replayPath = Join-Path $scenarioRoot ("peer-" + ($playerIndex + 1) + '.replay')
                    $replayResultPath = $replayPath + ".player-$playerIndex.result.json"
                    $replayRoot = Join-Path $scenarioRoot "replay-player-$playerIndex"
                    $replayRoaming = Join-Path $replayRoot 'AppData\Roaming'
                    $replayLocal = Join-Path $replayRoot 'AppData\Local'
                    $replaySettings = Join-Path $replayRoaming 'SlayTheSpire2\default\1'
                    New-Item -ItemType Directory -Path $replaySettings, $replayLocal -Force | Out-Null
                    @{
                        schema_version = 8; language = $(if ($playerIndex % 2 -eq 1) { 'zhs' } else { 'eng' })
                        mod_settings = @{ mods_enabled = $true; mod_list = @() }
                        seen_ea_disclaimer = $true; skip_intro_logo = $true
                    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $replaySettings 'settings.save') -Encoding utf8
                    $replayLog = Join-Path $replayRoot 'game.log'
                    $replayStart = [System.Diagnostics.ProcessStartInfo]::new()
                    $replayStart.FileName = $executable
                    $replayStart.WorkingDirectory = $resolvedGameRoot
                    $replayStart.UseShellExecute = $false
                    $replayStart.CreateNoWindow = $true
                    $replayStart.Environment['APPDATA'] = $replayRoaming
                    $replayStart.Environment['LOCALAPPDATA'] = $replayLocal
                    foreach ($argument in @('--headless', '--force-steam=off', '--togawa-native-multiplayer-replay',
                        "--togawa-mp-replay-path=$replayPath", "--togawa-mp-replay-player-index=$playerIndex", '--log-file', $replayLog)) {
                        $replayStart.ArgumentList.Add($argument)
                    }
                    Write-Host "Replaying $scenario from player slot $playerIndex using its captured native replay."
                    $replayProcess = [System.Diagnostics.Process]::Start($replayStart)
                    $processes.Add($replayProcess)
                    $replayDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
                    while ([DateTime]::UtcNow -lt $replayDeadline -and -not $replayProcess.HasExited -and
                        -not (Test-Path -LiteralPath $replayResultPath -PathType Leaf)) {
                        Start-Sleep -Milliseconds 300
                    }
                    if (-not (Test-Path -LiteralPath $replayResultPath -PathType Leaf)) {
                        throw "Native replay failed or timed out for $scenario player slot $playerIndex. Inspect '$replayLog'."
                    }
                    $replayEvidence = Get-Content -Raw -LiteralPath $replayResultPath | ConvertFrom-Json
                    if (-not $replayEvidence.Passed) { throw "Native replay did not pass for $scenario player slot $playerIndex." }
                    $replayIssues = @(Get-Content -LiteralPath $replayLog |
                        Select-String -Pattern '\[ERROR\]|Unhandled exception|Exception:|State divergence|Multiplayer contract FAILED|Failed to load mod|Could not load mod')
                    if ($replayIssues.Count -gt 0) {
                        throw "Native replay logs contain managed or synchronization errors for $scenario player slot $playerIndex."
                    }
                }
            }
            $outcomes.Add([pscustomobject]@{
                Scenario = $scenario; Passed = $true; PlayerCount = $peerCount
                NativeChecksums = @($evidence[0].NativeChecksums).Count
                Choices = @($evidence[0].PlayerChoices).Count
                Languages = @(1..$peerCount | ForEach-Object { if ($_ % 2 -eq 0) { 'zhs' } else { 'eng' } })
                StartingEventVerified = $StartingEvent.IsPresent
                StartingChoiceOffset = $StartingChoiceOffset
                EvidenceRoot = $scenarioRoot; ReplayCaptured = $true
                ReplayPlaybackVerified = $VerifyReplay.IsPresent; ReconnectVerified = $false
            })
            Write-Host "Passed ${scenario}: all $peerCount peers produced matching native checksums and state snapshots."
        }
        catch {
            $scenarioError = $_.Exception.Message
            $outcomes.Add([pscustomobject]@{ Scenario = $scenario; Passed = $false; PlayerCount = $peerCount; Error = $scenarioError; EvidenceRoot = $scenarioRoot })
            throw
        }
        finally {
            foreach ($process in $processes) {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                    $process.WaitForExit()
                }
                $process.Dispose()
            }
        }
    }
}
finally {
    [pscustomobject]@{
        Seed = $Seed; Transport = 'Native ENet loopback'; IsolatedAppDataPerProcess = $true
        PackageHashes = $packageHashes; Scenarios = @($outcomes.ToArray()); ArtifactRoot = $runRoot
        ReplayPlaybackVerified = $VerifyReplay.IsPresent -and @($outcomes | Where-Object { -not $_.Passed }).Count -eq 0
        ReconnectVerified = $false
    } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $runRoot 'summary.json') -Encoding utf8
}
$outcomes.ToArray()
