[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-n5-batch',

    [string]$Seed = 'N5SAKIKO001',

    [ValidateRange(60, 300)]
    [int]$TimeoutSeconds = 180
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
$settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
$logPath = Join-Path $appDataPath 'SlayTheSpire2\logs\godot.log'
$summaryPath = Join-Path $runRoot 'summary.json'

New-Item -ItemType Directory -Force -Path $settingsDirectory, $localAppDataPath | Out-Null
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

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $gameExecutable
$startInfo.WorkingDirectory = $resolvedGameRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.Environment['APPDATA'] = $appDataPath
$startInfo.Environment['LOCALAPPDATA'] = $localAppDataPath
foreach ($argument in @('--headless', '--force-steam=off', '--autoslay', '--togawa-native-n5-batch-smoke', "--seed=$Seed")) {
    [void]$startInfo.ArgumentList.Add($argument)
}

$pureMarker = 'Phase N5 pure contract tests passed (28 assertions).'
$gameplayMarker = 'Phase N5 batch: native command card batch passed. GreetingsEnergy=5, TirednessDraw=3, MelodyDamage=15, IdealFreeAttacks=5, VoiceRoutes=3.'
$selectionMarker = 'Embarking on a singleplayer TOGAWASAKIKO-TOGAWA_SAKIKO run.'
$gameProcess = [System.Diagnostics.Process]::Start($startInfo)
$logText = ''

try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline -and -not $gameProcess.HasExited) {
        Start-Sleep -Milliseconds 250
        if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
            continue
        }
        try {
            $logText = [string](Get-Content -Raw -LiteralPath $logPath -ErrorAction Stop)
        }
        catch {
            continue
        }
        if ($logText.Contains($pureMarker) -and $logText.Contains($gameplayMarker) -and $logText.Contains($selectionMarker)) {
            break
        }
    }
}
finally {
    if (-not $gameProcess.HasExited) {
        $gameProcess.Kill($true)
        $gameProcess.WaitForExit()
    }
}

if (Test-Path -LiteralPath $logPath -PathType Leaf) {
    $logText = [string](Get-Content -Raw -LiteralPath $logPath)
}

$missingMarkers = @(@($pureMarker, $gameplayMarker, $selectionMarker) | Where-Object { -not $logText.Contains($_) })
if ($missingMarkers.Count -gt 0) {
    throw "Phase N5 batch did not finish within $TimeoutSeconds seconds. Missing: $($missingMarkers -join '; '). Inspect '$logPath'."
}

$managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Phase N5 actual-game contract failed|Phase N5 pure contract failed'
$managedIssueLines = @(
    $logText -split "`r?`n" |
        Select-String -Pattern $managedIssuePattern |
        ForEach-Object { $_.Line }
)

$summary = [pscustomobject]@{
    Seed = $Seed
    PureAssertionCount = 28
    GameplayCards = @('GreetingsCard', 'TirednessCard', 'MelodyCard', 'IdealCard')
    GameplayMarkerPassed = $true
    CharacterSelected = $true
    ManagedIssueCount = $managedIssueLines.Count
    LogPath = $logPath
    ArtifactRoot = $runRoot
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$summary

if ($managedIssueLines.Count -gt 0) {
    throw "Phase N5 batch logs contain managed exception, localization, or mod-load failures. Inspect '$runRoot'."
}
