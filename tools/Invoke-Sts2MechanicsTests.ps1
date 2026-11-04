[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'sts2-mechanics',

    [string]$Seed = 'MECHANICS001',

    [ValidateSet('eng', 'zhs')]
    [string]$Language = 'eng',

    # Runs one last-hit scenario instead of the regular suite: no permanent card may be added after lethal damage.
    [ValidateSet('', 'phantom:sakiko', 'phantom:tomori', 'phantom:taki', 'phantom:mutsumi', 'phantom:soyo',
        'element:fire', 'element:air', 'element:water', 'element:earth', 'element:ether')]
    [string]$Lethal = '',

    [ValidateRange(30, 300)]
    [int]$TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Regular-suite count; each lethal scenario runs 2 Monochrome Hairband checks plus its 2 lethal checks.
$expectedAssertions = if ($Lethal) { 4 } else { 538 }

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
$scenarioName = if ($Lethal) { '-' + $Lethal.Replace(':', '-') } else { '' }
$runRoot = Join-Path $resolvedArtifactRoot ("$RunName-$Language$scenarioName-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$appDataPath = Join-Path $runRoot 'AppData\Roaming'
$localAppDataPath = Join-Path $runRoot 'AppData\Local'
$gameplayLogPath = Join-Path $appDataPath 'SlayTheSpire2\logs\godot.log'
$summaryPath = Join-Path $runRoot 'summary.json'

$settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
New-Item -ItemType Directory -Force -Path $settingsDirectory, $localAppDataPath | Out-Null
@{
    schema_version = 8
    language = $Language
    mod_settings = @{
        mods_enabled = $true
        mod_list = @()
    }
    seen_ea_disclaimer = $true
    skip_intro_logo = $true
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $settingsDirectory 'settings.save') -Encoding utf8

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

$requiredMarkers = @(
    'Embarking on a singleplayer TOGAWASAKIKO-TOGAWA_SAKIKO run.',
    "Native gameplay smoke: STS2 mechanics: PASS ($expectedAssertions assertions)"
)
$failureMarker = 'InvalidOperationException: STS2 mechanics: '

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $gameExecutable
$startInfo.WorkingDirectory = $resolvedGameRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.Environment['APPDATA'] = $appDataPath
$startInfo.Environment['LOCALAPPDATA'] = $localAppDataPath
$arguments = @('--headless', '--force-steam=off', '--autoslay', '--togawa-sts2-mechanics-test', "--seed=$Seed")
if ($Lethal) {
    $arguments += "--togawa-sts2-mechanics-lethal=$Lethal"
}
foreach ($argument in $arguments) {
    [void]$startInfo.ArgumentList.Add($argument)
}

$process = [System.Diagnostics.Process]::Start($startInfo)
$gameplayLog = ''
try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
        Start-Sleep -Milliseconds 250
        $gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
        if (@($requiredMarkers | Where-Object { -not $gameplayLog.Contains($_) }).Count -eq 0 -or
            $gameplayLog -match 'InvalidOperationException: STS2 mechanics: |\[AutoSlay\] Run failed') {
            break
        }
    }
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }
}

$gameplayLog = Read-OpenTextFile -Path $gameplayLogPath
$missingMarkers = @($requiredMarkers | Where-Object { -not $gameplayLog.Contains($_) })
$observedCount = [regex]::Match($gameplayLog, 'STS2 mechanics: PASS \((\d+) assertions\)')
$managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Phase N\d pure contract failed'
$issueLines = @(
    $gameplayLog -split "`r?`n" |
        Select-String -Pattern $managedIssuePattern |
        ForEach-Object { $_.Line }
)

$summary = [pscustomobject]@{
    Seed = $Seed
    Language = $Language
    Lethal = $Lethal
    ExpectedAssertions = $expectedAssertions
    ObservedAssertions = if ($observedCount.Success) { [int]$observedCount.Groups[1].Value } else { $null }
    MissingMarkers = $missingMarkers
    ManagedIssueCount = $issueLines.Count
    GameplayLogPath = $gameplayLogPath
    ArtifactRoot = $runRoot
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$summary

if ($missingMarkers.Count -ne 0) {
    $failure = @($gameplayLog -split "`r?`n" | Where-Object { $_.Contains($failureMarker) } | Select-Object -First 1)
    throw "STS2 mechanics markers missing: $($missingMarkers -join '; '). First failure: $($failure -join ''). Inspect '$gameplayLogPath'."
}
if ($issueLines.Count -gt 0) {
    throw "STS2 mechanics log contains managed exception, localization, or mod-load failures. Inspect '$gameplayLogPath'."
}
