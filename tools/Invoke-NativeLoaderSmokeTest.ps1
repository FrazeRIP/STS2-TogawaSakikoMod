param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-loader',

    [ValidateSet('eng', 'zhs')]
    [string]$Language = 'eng',

    [ValidateRange(5, 120)]
    [int]$TimeoutSeconds = 45,

    [string[]]$AdditionalArguments = @(),

    [string]$RequiredMarker = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$gameExecutable = Join-Path $resolvedGameRoot 'SlayTheSpire2.exe'
if (-not (Test-Path -LiteralPath $gameExecutable -PathType Leaf)) {
    throw "SlayTheSpire2.exe was not found under '$resolvedGameRoot'."
}

$resolvedArtifactRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArtifactRoot)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runRoot = Join-Path $resolvedArtifactRoot "$RunName-$timestamp"
$appDataPath = Join-Path $runRoot 'AppData\Roaming'
$localAppDataPath = Join-Path $runRoot 'AppData\Local'
$logPath = Join-Path $runRoot 'slay-the-spire-2.log'
$settingsDirectory = Join-Path $appDataPath 'SlayTheSpire2\default\1'
$settingsPath = Join-Path $settingsDirectory 'settings.save'

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
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settingsPath -Encoding utf8

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $gameExecutable
$startInfo.WorkingDirectory = $resolvedGameRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.Environment['APPDATA'] = $appDataPath
$startInfo.Environment['LOCALAPPDATA'] = $localAppDataPath
[void]$startInfo.ArgumentList.Add('--headless')
[void]$startInfo.ArgumentList.Add('--force-steam=off')
[void]$startInfo.ArgumentList.Add('--log-file')
[void]$startInfo.ArgumentList.Add($logPath)
[void]$startInfo.ArgumentList.Add('--quit-after')
[void]$startInfo.ArgumentList.Add('1200')
foreach ($argument in $AdditionalArguments) {
    [void]$startInfo.ArgumentList.Add($argument)
}

$gameProcess = [System.Diagnostics.Process]::Start($startInfo)
$bootstrapFound = $false
$verticalSliceFound = $false
$requiredMarkerFound = [string]::IsNullOrWhiteSpace($RequiredMarker)
$forcedTermination = $false
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

try {
    while ([DateTime]::UtcNow -lt $deadline -and -not $gameProcess.HasExited) {
        Start-Sleep -Milliseconds 500
        if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
            continue
        }

        $currentLog = Get-Content -Raw -LiteralPath $logPath -ErrorAction SilentlyContinue
        if ($currentLog -match 'Native bootstrap initialized') {
            $bootstrapFound = $true
        }
        if ($currentLog -match 'Native vertical slice ready') {
            $verticalSliceFound = $true
        }
        if (-not [string]::IsNullOrWhiteSpace($RequiredMarker) -and $currentLog.Contains($RequiredMarker)) {
            $requiredMarkerFound = $true
        }
        if (-not [string]::IsNullOrWhiteSpace($RequiredMarker) -and
            $requiredMarkerFound -and $bootstrapFound -and $verticalSliceFound) {
            break
        }
    }
}
finally {
    if (-not $gameProcess.HasExited) {
        $forcedTermination = $true
        $gameProcess.Kill($true)
        $gameProcess.WaitForExit()
    }
}

$logLines = @()
$keyLines = @()
$initializerCount = 0
$bootstrapCount = 0
$verticalSliceCount = 0
$requiredMarkerCount = 0
$runningModdedFound = $false
$finishedInitializationFound = $false
$preStartupLoaderIssues = @()
if (Test-Path -LiteralPath $logPath -PathType Leaf) {
    $logLines = @(Get-Content -LiteralPath $logPath)
    $keyLines = @(
        $logLines | Select-String -Pattern @(
            'TogawaSakiko',
            'Native bootstrap',
            'Native vertical slice ready',
            'Verified game baseline',
            'Verified mounted PCK',
            'RUNNING MODDED',
            'BaseLib',
            'ERROR',
            'EXCEPTION',
            'Exception',
            'Failed to load',
            'Could not load'
        ) | ForEach-Object { $_.Line }
    )

    $initializerCount = @(
        $logLines | Select-String -SimpleMatch 'Calling initializer method of type TogawaSakiko.NativeCode.Bootstrap.ModEntryPoint'
    ).Count
    $bootstrapCount = @($logLines | Select-String -SimpleMatch 'Native bootstrap initialized').Count
    $verticalSliceCount = @($logLines | Select-String -SimpleMatch 'Native vertical slice ready').Count
    if (-not [string]::IsNullOrWhiteSpace($RequiredMarker)) {
        $requiredMarkerCount = @($logLines | Select-String -SimpleMatch $RequiredMarker).Count
    }
    $runningModdedMatch = @($logLines | Select-String -SimpleMatch 'RUNNING MODDED')
    $runningModdedFound = $runningModdedMatch.Count -gt 0
    $finishedInitializationFound = @(
        $logLines | Select-String -SimpleMatch "Finished mod initialization for 'Togawa Sakiko' (TogawaSakiko)."
    ).Count -eq 1

    $startupEndIndex = -1
    for ($lineIndex = 0; $lineIndex -lt $logLines.Count; $lineIndex++) {
        if ($logLines[$lineIndex].Contains('RUNNING MODDED')) {
            $startupEndIndex = $lineIndex
            break
        }
    }
    $startupLines = if ($startupEndIndex -ge 0) {
        $logLines[0..$startupEndIndex]
    } else {
        $logLines
    }
    $preStartupLoaderIssues = @(
        $startupLines |
            Select-String -Pattern 'ERROR|EXCEPTION|Unhandled|Failed to load|Could not load' |
            ForEach-Object { $_.Line }
    )
}

[pscustomobject]@{
    BootstrapFound = $bootstrapFound
    BootstrapCount = $bootstrapCount
    VerticalSliceFound = $verticalSliceFound
    VerticalSliceCount = $verticalSliceCount
    RequiredMarker = $RequiredMarker
    RequiredMarkerFound = $requiredMarkerFound
    RequiredMarkerCount = $requiredMarkerCount
    Language = $Language
    InitializerCount = $initializerCount
    FinishedInitializationFound = $finishedInitializationFound
    RunningModdedFound = $runningModdedFound
    PreStartupLoaderIssueCount = $preStartupLoaderIssues.Count
    ProcessId = $gameProcess.Id
    ProcessExited = $gameProcess.HasExited
    ForcedTermination = $forcedTermination
    LogPath = $logPath
    KeyLines = $keyLines
}

if (-not $bootstrapFound) {
    throw "Native bootstrap marker was not found within $TimeoutSeconds seconds. Inspect '$logPath'."
}

if (-not $verticalSliceFound -or $verticalSliceCount -ne 1) {
    throw "Native vertical-slice validation did not complete exactly once for '$Language'. Inspect '$logPath'."
}

if (-not [string]::IsNullOrWhiteSpace($RequiredMarker) -and
    (-not $requiredMarkerFound -or $requiredMarkerCount -ne 1)) {
    throw "Required diagnostic marker '$RequiredMarker' did not occur exactly once for '$Language'. Inspect '$logPath'."
}

if ($bootstrapCount -ne 1 -or $initializerCount -ne 1 -or -not $finishedInitializationFound -or -not $runningModdedFound) {
    throw "The loader did not complete the expected single Togawa initialization. Inspect '$logPath'."
}

if ($preStartupLoaderIssues.Count -gt 0) {
    throw "The loader reported an error before startup completed. Inspect '$logPath'."
}
