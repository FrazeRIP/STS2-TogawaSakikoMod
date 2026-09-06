param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$RunName = 'native-gameplay',

    [string]$TogawaSeed = 'N2SAKIKO001',

    [string]$VanillaSeed = 'N2VANILLA001',

    [ValidateRange(30, 300)]
    [int]$GameplayTimeoutSeconds = 180,

    [ValidateRange(10, 120)]
    [int]$ReloadTimeoutSeconds = 45,

    [ValidateRange(30, 300)]
    [int]$VanillaTimeoutSeconds = 120
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
    throw "The staged TogawaSakiko mod is not deployed at '$deployedManifest'."
}

$resolvedArtifactRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArtifactRoot)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runRoot = Join-Path $resolvedArtifactRoot "$RunName-$timestamp"
$togawaAppDataPath = Join-Path $runRoot 'togawa\AppData\Roaming'
$togawaLocalAppDataPath = Join-Path $runRoot 'togawa\AppData\Local'
$vanillaAppDataPath = Join-Path $runRoot 'vanilla\AppData\Roaming'
$vanillaLocalAppDataPath = Join-Path $runRoot 'vanilla\AppData\Local'
$reloadLogPath = Join-Path $runRoot 'togawa-reload.log'
$saveSnapshotPath = Join-Path $runRoot 'current_run.save.snapshot'

function Write-IsolatedSettings {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppDataPath
    )

    $settingsDirectory = Join-Path $AppDataPath 'SlayTheSpire2\default\1'
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
        [string]$AppDataPath,

        [Parameter(Mandatory = $true)]
        [string]$LocalAppDataPath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    New-Item -ItemType Directory -Force -Path $AppDataPath, $LocalAppDataPath | Out-Null

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $gameExecutable
    $startInfo.WorkingDirectory = $resolvedGameRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment['APPDATA'] = $AppDataPath
    $startInfo.Environment['LOCALAPPDATA'] = $LocalAppDataPath

    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    return [System.Diagnostics.Process]::Start($startInfo)
}

function Stop-IsolatedGame {
    param(
        [System.Diagnostics.Process]$Process
    )

    if ($null -ne $Process -and -not $Process.HasExited) {
        $Process.Kill($true)
        $Process.WaitForExit()
    }
}

function Read-OpenTextFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

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
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppDataPath
    )

    return Get-ChildItem -LiteralPath $AppDataPath -Filter 'current_run.save' -File -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Get-Count {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [string]$Needle
    )

    return [regex]::Matches($Text, [regex]::Escape($Needle)).Count
}

Write-IsolatedSettings -AppDataPath $togawaAppDataPath
Write-IsolatedSettings -AppDataPath $vanillaAppDataPath

$togawaLogPath = Join-Path $togawaAppDataPath 'SlayTheSpire2\logs\godot.log'
$vanillaLogPath = Join-Path $vanillaAppDataPath 'SlayTheSpire2\logs\godot.log'
$togawaProcess = $null
$reloadProcess = $null
$vanillaProcess = $null
$runSave = $null
$saveText = ''
$togawaLog = ''
$reloadLog = ''
$vanillaLog = ''

$selectionMarker = 'Selecting character: CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO'
$visualMarker = 'Native gameplay smoke: created the static combat portrait.'
$moonlightMarker = 'Native gameplay smoke: Moonlight Sonata added a card-removal reward.'
$dazzlingMarker = 'Native gameplay smoke: Dazzling dealt'
$hairbandMarker = 'Native gameplay smoke: Monochrome Hairband added Desire to the deck; success=True.'
$rewardsMarker = '[AutoSlay] Finished screen: NRewardsScreen'
$reloadMarker = 'Native reload smoke: deserialized Togawa Sakiko with Monochrome Hairband and'
$vanillaSelectionMarker = 'Embarking on a singleplayer IRONCLAD run.'
$vanillaCombatMarker = '[AutoSlay] Finished Monster room'
$saveNeedles = @(
    'CHARACTER.TOGAWASAKIKO-TOGAWA_SAKIKO',
    'RELIC.TOGAWASAKIKO-STARTER_RELIC_TOGAWA_SAKIKO',
    'CARD.TOGAWASAKIKO-DESIRE_CARD'
)
$saveSnapshotValid = $false
$snapshotText = ''

try {
    Write-Host "Starting Togawa AutoSlay smoke with seed $TogawaSeed."
    $togawaArguments = @(
        '--headless',
        '--force-steam=off',
        '--autoslay',
        '--togawa-native-gameplay-smoke',
        "--seed=$TogawaSeed"
    )
    $togawaProcess = Start-IsolatedGame -AppDataPath $togawaAppDataPath -LocalAppDataPath $togawaLocalAppDataPath -Arguments $togawaArguments

    $gameplayDeadline = [DateTime]::UtcNow.AddSeconds($GameplayTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $gameplayDeadline -and -not $togawaProcess.HasExited) {
        Start-Sleep -Milliseconds 500
        $togawaLog = Read-OpenTextFile -Path $togawaLogPath
        $runSave = Find-RunSave -AppDataPath $togawaAppDataPath
        if ($null -ne $runSave) {
            $saveText = Read-OpenTextFile -Path $runSave.FullName
        }

        $saveContainsAllIds = $saveNeedles.Where({ -not $saveText.Contains($_) }).Count -eq 0
        if ($saveContainsAllIds -and -not $saveSnapshotValid) {
            try {
                $null = $saveText | ConvertFrom-Json
                Copy-Item -LiteralPath $runSave.FullName -Destination $saveSnapshotPath -Force
                $snapshotText = Read-OpenTextFile -Path $saveSnapshotPath
                $null = $snapshotText | ConvertFrom-Json
                $saveSnapshotValid = $saveNeedles.Where({ -not $snapshotText.Contains($_) }).Count -eq 0
            }
            catch {
                $saveSnapshotValid = $false
            }
        }

        $togawaComplete =
            $togawaLog.Contains($selectionMarker) -and
            $togawaLog.Contains($visualMarker) -and
            $togawaLog.Contains($moonlightMarker) -and
            $togawaLog.Contains($dazzlingMarker) -and
            (Get-Count -Text $togawaLog -Needle $hairbandMarker) -ge 1 -and
            $togawaLog.Contains($rewardsMarker) -and
            $saveSnapshotValid

        if ($togawaComplete) {
            break
        }
    }

    $togawaLog = Read-OpenTextFile -Path $togawaLogPath
    $runSave = Find-RunSave -AppDataPath $togawaAppDataPath
    if ($null -ne $runSave) {
        $saveText = Read-OpenTextFile -Path $runSave.FullName
    }

    $selectionFound = $togawaLog.Contains($selectionMarker)
    $visualFound = $togawaLog.Contains($visualMarker)
    $moonlightFound = $togawaLog.Contains($moonlightMarker)
    $dazzlingFound = $togawaLog.Contains($dazzlingMarker)
    $hairbandCount = Get-Count -Text $togawaLog -Needle $hairbandMarker
    $rewardsFound = $togawaLog.Contains($rewardsMarker)
    $saveContainsAllIds = $saveSnapshotValid -and
        $saveNeedles.Where({ -not $snapshotText.Contains($_) }).Count -eq 0

    if (-not $selectionFound -or -not $visualFound -or -not $moonlightFound -or
        -not $dazzlingFound -or $hairbandCount -lt 1 -or -not $rewardsFound -or
        -not $saveContainsAllIds) {
        throw "The Togawa gameplay smoke did not complete within $GameplayTimeoutSeconds seconds. Inspect '$togawaLogPath'."
    }

    Stop-IsolatedGame -Process $togawaProcess
    $togawaProcess = $null
    Copy-Item -LiteralPath $saveSnapshotPath -Destination $runSave.FullName -Force
    Write-Host 'Togawa combat, rewards, and post-victory save markers passed.'

    Write-Host 'Starting isolated save reload smoke.'
    $reloadArguments = @(
        '--headless',
        '--force-steam=off',
        '--togawa-native-reload-smoke',
        '--log-file',
        $reloadLogPath,
        '--quit-after',
        '10000'
    )
    $reloadProcess = Start-IsolatedGame -AppDataPath $togawaAppDataPath -LocalAppDataPath $togawaLocalAppDataPath -Arguments $reloadArguments

    $reloadDeadline = [DateTime]::UtcNow.AddSeconds($ReloadTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $reloadDeadline -and -not $reloadProcess.HasExited) {
        Start-Sleep -Milliseconds 500
        $reloadLog = Read-OpenTextFile -Path $reloadLogPath
        if ($reloadLog.Contains($reloadMarker)) {
            break
        }
    }

    $reloadLog = Read-OpenTextFile -Path $reloadLogPath
    $reloadFound = $reloadLog.Contains($reloadMarker)
    if (-not $reloadFound) {
        throw "The native save reload marker was not found within $ReloadTimeoutSeconds seconds. Inspect '$reloadLogPath'."
    }

    Stop-IsolatedGame -Process $reloadProcess
    $reloadProcess = $null
    Write-Host 'The game deserialized the custom character, relic, and generated card.'

    Write-Host "Starting vanilla Ironclad AutoSlay smoke with seed $VanillaSeed."
    $vanillaArguments = @(
        '--headless',
        '--force-steam=off',
        '--autoslay',
        '--togawa-native-vanilla-smoke',
        "--seed=$VanillaSeed"
    )
    $vanillaProcess = Start-IsolatedGame -AppDataPath $vanillaAppDataPath -LocalAppDataPath $vanillaLocalAppDataPath -Arguments $vanillaArguments

    $vanillaDeadline = [DateTime]::UtcNow.AddSeconds($VanillaTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $vanillaDeadline -and -not $vanillaProcess.HasExited) {
        Start-Sleep -Milliseconds 500
        $vanillaLog = Read-OpenTextFile -Path $vanillaLogPath
        if ($vanillaLog.Contains($vanillaSelectionMarker) -and
            $vanillaLog.Contains($vanillaCombatMarker)) {
            break
        }
    }

    $vanillaLog = Read-OpenTextFile -Path $vanillaLogPath
    $vanillaSelectionFound = $vanillaLog.Contains($vanillaSelectionMarker)
    $vanillaCombatFound = $vanillaLog.Contains($vanillaCombatMarker)
    if (-not $vanillaSelectionFound -or -not $vanillaCombatFound) {
        throw "The vanilla gameplay smoke did not complete within $VanillaTimeoutSeconds seconds. Inspect '$vanillaLogPath'."
    }

    Stop-IsolatedGame -Process $vanillaProcess
    $vanillaProcess = $null
    Write-Host 'Vanilla Ironclad character selection and combat passed with Togawa loaded.'
}
finally {
    Stop-IsolatedGame -Process $togawaProcess
    Stop-IsolatedGame -Process $reloadProcess
    Stop-IsolatedGame -Process $vanillaProcess
}

$togawaIssueLines = @(
    $togawaLog -split "`r?`n" |
        Select-String -Pattern '\[AutoSlay\].*Run failed|Unhandled exception|System\.[A-Za-z]+Exception|Failed to load mod|Could not load mod|Localization formatting error' |
        ForEach-Object { $_.Line }
)
$reloadIssueLines = @(
    $reloadLog -split "`r?`n" |
        Select-String -Pattern 'Unhandled exception|System\.[A-Za-z]+Exception|Failed to load mod|Could not load mod' |
        ForEach-Object { $_.Line }
)
$vanillaIssueLines = @(
    $vanillaLog -split "`r?`n" |
        Select-String -Pattern '\[AutoSlay\].*Run failed|Unhandled exception|System\.[A-Za-z]+Exception|Failed to load mod|Could not load mod' |
        ForEach-Object { $_.Line }
)

[pscustomobject]@{
    TogawaSeed = $TogawaSeed
    TogawaCharacterSelected = $selectionFound
    StaticCombatPortraitCreated = $visualFound
    MoonlightRewardCreated = $moonlightFound
    DazzlingTriggered = $dazzlingFound
    HairbandVictoryCount = $hairbandCount
    RewardScreenFinished = $rewardsFound
    SaveContainsCharacterRelicAndDesire = $saveContainsAllIds
    ReloadDeserializedCustomModels = $reloadFound
    VanillaSeed = $VanillaSeed
    VanillaIroncladSelected = $vanillaSelectionFound
    VanillaCombatFinished = $vanillaCombatFound
    TogawaIssueCount = $togawaIssueLines.Count
    ReloadIssueCount = $reloadIssueLines.Count
    VanillaIssueCount = $vanillaIssueLines.Count
    RunSavePath = $runSave.FullName
    SaveSnapshotPath = $saveSnapshotPath
    TogawaLogPath = $togawaLogPath
    ReloadLogPath = $reloadLogPath
    VanillaLogPath = $vanillaLogPath
    ArtifactRoot = $runRoot
}

if ($togawaIssueLines.Count -gt 0 -or $reloadIssueLines.Count -gt 0 -or $vanillaIssueLines.Count -gt 0) {
    throw "One or more gameplay logs contain exception or mod-load failure markers. Inspect '$runRoot'."
}
