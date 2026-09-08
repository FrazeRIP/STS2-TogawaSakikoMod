[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [ValidateRange(0, 2)]
    [int[]]$Choice = @(0, 1, 2),

    [ValidateSet('eng', 'zhs')]
    [string]$Language = 'eng',

    [switch]$CaptureScreenshot,

    [switch]$CaptureFeedback,

    [string]$RunName = 'native-starting-room',

    [ValidateRange(30, 300)]
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
$runRoot = Join-Path $resolvedArtifactRoot "$RunName-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$choiceKeys = @('ANOTHER_MASK', 'THE_THIRD_MOVEMENT', 'BLAZING_HAIRBAND')
$results = @()

foreach ($choiceIndex in $Choice) {
    $choiceKey = $choiceKeys[$choiceIndex]
    $choiceRoot = Join-Path $runRoot $choiceKey
    $appDataPath = Join-Path $choiceRoot 'AppData\Roaming'
    $localAppDataPath = Join-Path $choiceRoot 'AppData\Local'
    $settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
    $logPath = Join-Path $appDataPath 'SlayTheSpire2\logs\godot.log'
    $screenshotPath = Join-Path $choiceRoot 'starting-room.png'
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

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $gameExecutable
    $startInfo.WorkingDirectory = $resolvedGameRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.Environment['APPDATA'] = $appDataPath
    $startInfo.Environment['LOCALAPPDATA'] = $localAppDataPath
    $arguments = @('--force-steam=off', '--autoslay', '--togawa-native-starting-room-smoke', "--togawa-starting-choice=$choiceIndex", '--seed=OCEANSTART001')
    if ($CaptureScreenshot -or $CaptureFeedback) {
        $arguments += @('--windowed', '--resolution', '1920x1080', "--togawa-starting-screenshot=$screenshotPath")
    }
    else {
        $arguments += '--headless'
    }
    if ($CaptureFeedback) {
        $arguments += @('--togawa-feedback-visuals', "--togawa-feedback-output=$choiceRoot")
    }
    foreach ($argument in $arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $uses = if ($choiceIndex -eq 1) { 3 } else { 0 }
    $marker = "Starting room contract: passed. Choice=$choiceKey, Deck=9, Repeat=blocked, SavedChoice=exact, Reload=finished, ThirdMovementUses=$uses, Vanilla=preserved, FullscreenArtwork=resolved."
    $completionMarker = if ($CaptureFeedback) { 'Starting room contract: feedback visual rooms captured.' } else { $marker }
    $managedIssuePattern = '\[ERROR\]|Unhandled exception|[A-Za-z0-9_.]+Exception:|Failed to load mod|Could not load mod|Localization formatting error|Starting room contract failed'
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
            if ($logText.Contains($completionMarker) -or $logText -match $managedIssuePattern) {
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
    $managedIssues = @($logText -split "`r?`n" | Select-String -Pattern $managedIssuePattern | ForEach-Object { $_.Line })
    $result = [pscustomobject]@{
        Choice = $choiceKey
        Language = $Language
        Passed = $logText.Contains($marker) -and $logText.Contains($completionMarker)
        StartingDeckCount = 9
        ThirdMovementUses = $uses
        ManagedIssueCount = $managedIssues.Count
        Screenshot = if ($CaptureScreenshot -or $CaptureFeedback) { $screenshotPath } else { $null }
        FeedbackScreenshots = if ($CaptureFeedback) { $choiceRoot } else { $null }
        LogPath = $logPath
        ArtifactRoot = $choiceRoot
    }
    $result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $choiceRoot 'summary.json') -Encoding utf8
    $results += $result
    if (-not $result.Passed -or $managedIssues.Count -gt 0) {
        throw "Starting room choice $choiceKey failed or did not finish within $TimeoutSeconds seconds. Inspect '$logPath'."
    }
    if (($CaptureScreenshot -or $CaptureFeedback) -and -not (Test-Path -LiteralPath $screenshotPath -PathType Leaf)) {
        throw "The requested starting-room screenshot was not captured at '$screenshotPath'."
    }
}

$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runRoot 'summary.json') -Encoding utf8
$results
