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

$pureMarker = 'Phase N5 pure contract tests passed (725 assertions).'
$gameplayMarker = 'Phase N5 batch: native card batches passed. GreetingsEnergy=5, TirednessDraw=3, MelodyDamage=15, IdealFreeAttacks=5, Artifact=5, Dazzling=10, KindnessCurrentAndPriorLossSelection=passed, Keys=Black5+White5, MantraDivinity=PlayerEnemyTriple+Voice10+InnerCry7, MementoMori=BaseExhaust6+UpgradePurge5+CombatOnly1+Triple60, PersistentAdds=Tiredness2+Radiance2+Ideal2+Voice2+Amoris2+Mortis2, CommonDamage=Phantom24+24+28+Symbol28, Regen=9, SymbolDraw=7, StrengthTrade=Dark27+ActualSteal3+Georgette27+EnemyStrength2+EnemyHype1, HeartsBarrier=DeckSizedBlock+Retain, SelectionCards=Daten30+ExactPersistentPurge2+Kill19+DesireRetrieve3+EarthProjectedBlockDamage, Quaerere=Scry7+9+DiscardBlock7, Kings=Damage34+Single+Reward2+Reroll2+Clear3+Saved, CommonTail=Accomplice5+CarefreeRetain2+DesuWaDrawPriority3+EdgeFrail3+PurgeGeneratedAndPersistent+MasqueradeDamage8+SavedGrowth+WeaknessDiscard, UncommonDirect=Gold35+Tiredness2+Dazzling16+Plating16+Mutsumi12AndBlock12+Protection2+SoyoAoE14+Kindness2+RhinoBlock17WithoutDexterity+CountingBuffTypes, Curses=AmorisRetain+DolorisBlockable2+MortisInjury+OblivionisHandExhaust+TimorisVulnerable, RemainingUncommon=20Cards+10Powers, VoiceRoutes=25.'
$rareCoreMarker = 'Phase N5 batch: rare core passed. BandInvitation=4+5, Crychic=X1+X2Upgrade, Imprisoned=Damage8+DesireEnergy, Perfection=FreeSelection, Sora=Damage12+OrderedTop2, StayElegance=PotionProcured.'
$rarePowersMarker = 'Phase N5 batch: rare powers passed. Charismatic=EnemyHype+ActualStrengthCopy+PrideRejected, Cruelty=Vulnerable2+1+DesireDraw2, WishGoodLuck=FullBlockThorns2, Worldview=ExactUnplayableReplacement, CrychicPower=FreePhantom+Decrement, Pride=DrawPriority2+DelayedTopReturn.'
$rareMutationsMarker = 'Phase N5 batch: rare mutations passed. AsYourHeartDesires=SavedStatCopy+History+ExactDeckVersionCleanup, Ether=Base4+Upgrade9+LinkedOblivionis, Masks=DazzlingMinus2+Strength2+ExactTransform, SpringSunlight=LiveThresholdCost+Damage40.'
$rareFinaleMarker = 'Phase N5 batch: rare finale passed. AveMujica=Base+Upgrade+PersistentSelection, BlackBirthday=Base28+Upgrade36+AllPowerRemoval.'
$waterLifecycleMarker = 'Phase N5 batch: Symbol III Water extra-turn lifecycle passed. TurnIncrement=1, RoundPreserved=true, AmbergrisConsumed=1.'
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
        if ($logText.Contains($pureMarker) -and $logText.Contains($gameplayMarker) -and $logText.Contains($rareCoreMarker) -and $logText.Contains($rarePowersMarker) -and $logText.Contains($rareMutationsMarker) -and $logText.Contains($rareFinaleMarker) -and $logText.Contains($waterLifecycleMarker) -and $logText.Contains($selectionMarker)) {
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

$missingMarkers = @(@($pureMarker, $gameplayMarker, $rareCoreMarker, $rarePowersMarker, $rareMutationsMarker, $rareFinaleMarker, $waterLifecycleMarker, $selectionMarker) | Where-Object { -not $logText.Contains($_) })
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
    PureAssertionCount = 725
    GameplayCards = @('GreetingsCard', 'TirednessCard', 'MelodyCard', 'IdealCard', 'ProtectionCard', 'RadianceCard', 'KindnessCard', 'AmorisCard', 'DolorisCard', 'MortisCard', 'OblivionisCard', 'TimorisCard', 'BlackKeysCard', 'WhiteKeysCard', 'BlackAndWhiteKeysCard', 'VoiceCard', 'InnerCryCard', 'MementoMoriCard', 'BudgetBentoCard', 'PhantomOfSakikoCard', 'PhantomOfTakiCard', 'PhantomOfTomoriCard', 'SymbolIIAirCard', 'DarkHeavenCard', 'GeorgetteMeGeorgetteYouCard', 'HeartsBarrierCard', 'DatenCard', 'KillKiSSCard', 'SymbolIVEarthCard', 'QuaerereLuminaCard', 'KingsCard', 'AccompliceCard', 'CarefreeCard', 'DesuWaCard', 'EdgeOfBreakdownCard', 'MasqueradeRhapsodyRequestCard', 'WeaknessCard', 'ClockOutCard', 'FallenFlowersCard', 'HachibouseiDanceCard', 'PhantomOfMutsumiCard', 'PhantomOfSoyoCard', 'RhinocerosBeetleCard', 'CountingStarsCard', 'AleaIactaEstCard', 'AnglesCard', 'ChoirSChoirCard', 'CrucifixXCard', 'CuriosityCard', 'EnduranceCard', 'FearlessCard', 'KaoCard', 'OurSongCard', 'PerdereOmniaCard', 'PrimoDieInScaenaCard', 'SeizeTheFateCard', 'SharedDestinyCard', 'SymbolIFireCard', 'SymbolIIIWaterCard', 'TheGirlWithFlaxenHairCard', 'UtopiaCard', 'VeritasCard', 'WishFulfilledCard', 'WishToBecomeHumanCard', 'BandInvitationCard', 'CrychicCard', 'ImprisonedXIICard', 'PerfectionCard', 'SoraNoMusicaCard', 'StayEleganceCard', 'CharismaticFormCard', 'CrueltyCard', 'WishYouGoodLuckCard', 'WorldviewCard', 'PrideCard', 'AsYourHeartDesiresCard', 'EtherCard', 'MasksCard', 'SpringSunlightCard', 'AveMujicaCard', 'BlackBirthdayCard')
    GameplayMarkerPassed = $true
    RareCorePassed = $true
    RarePowersPassed = $true
    RareMutationsPassed = $true
    RareFinalePassed = $true
    WaterLifecyclePassed = $true
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
