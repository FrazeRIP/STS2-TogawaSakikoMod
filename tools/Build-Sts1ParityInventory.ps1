[CmdletBinding()]
param(
    [string]$Sts1Root,
    [string]$OutputJson = "docs/FULL_PORT_PARITY_INVENTORY.json",
    [string]$OutputMarkdown = "docs/FULL_PORT_PARITY_INVENTORY.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ([string]::IsNullOrWhiteSpace($Sts1Root)) {
    $Sts1Root = Join-Path (Split-Path $repoRoot -Parent) "STS1-TogawaSakikoMod"
}
$Sts1Root = [System.IO.Path]::GetFullPath($Sts1Root)

if (-not (Test-Path -LiteralPath (Join-Path $Sts1Root "src/main/java/togawasakikomod"))) {
    throw "STS1 source root was not found: $Sts1Root"
}

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-RelativePathNormalized([string]$Root, [string]$Path) {
    return [System.IO.Path]::GetRelativePath($Root, $Path).Replace("\", "/")
}

function Read-JsonMap([string]$Path) {
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json -AsHashtable
}

function Remove-JavaComments([string]$Text) {
    $withoutBlocks = [regex]::Replace($Text, "/\*.*?\*/", "", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    return [regex]::Replace($withoutBlocks, "(?m)//.*$", "")
}

function ConvertTo-ModelEntry([string]$TypeName) {
    $camelSplit = [regex]::Replace($TypeName.Trim(), "([A-Za-z0-9]|\G(?!^))([A-Z])", '$1_$2')
    $whitespaceSplit = [regex]::Replace($camelSplit.ToUpperInvariant(), "\s+", "_")
    return [regex]::Replace($whitespaceSplit, "[^A-Z0-9_]", "")
}

function ConvertTo-MarkdownCell([object]$Value) {
    if ($null -eq $Value) {
        return ""
    }
    return ([string]$Value).Replace("|", "\|").Replace("`r`n", "<br>").Replace("`n", "<br>")
}

function Get-PngDimensions([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24 -or
        $bytes[0] -ne 137 -or $bytes[1] -ne 80 -or $bytes[2] -ne 78 -or $bytes[3] -ne 71) {
        throw "Not a supported PNG: $Path"
    }

    $width = (([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19])
    $height = (([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23])
    return [ordered]@{ width = $width; height = $height; text = "${width}x${height}" }
}

function Test-ExactPngSize([string]$Path, [int]$Width, [int]$Height) {
    $dimensions = Get-PngDimensions $Path
    return $null -ne $dimensions -and $dimensions.width -eq $Width -and $dimensions.height -eq $Height
}

function Get-LocRecord([hashtable]$Map, [string]$Key) {
    if ($Map.ContainsKey($Key)) {
        return $Map[$Key]
    }
    return $null
}

function Get-LocField([object]$Record, [string]$Field) {
    if ($null -eq $Record) {
        return $null
    }
    if ($Record -is [System.Collections.IDictionary] -and $Record.Contains($Field)) {
        return $Record[$Field]
    }
    return $null
}

function Get-Sts1LocalizationKeys([object]$Table) {
    if ($Table -is [System.Collections.IDictionary]) {
        return @($Table.Keys | Sort-Object)
    }
    return @(
        $Table |
            ForEach-Object {
                if ($_ -is [System.Collections.IDictionary] -and $_.Contains("ID")) {
                    [string]$_['ID']
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object
    )
}

function Test-Sts1LocalizationContainsKey([object]$Table, [string]$Key) {
    if ($Table -is [System.Collections.IDictionary]) {
        return $Table.Contains($Key)
    }
    return @($Table | Where-Object { $_ -is [System.Collections.IDictionary] -and [string]$_['ID'] -eq $Key }).Count -gt 0
}

function Get-CSharpFilesByBaseName([string]$Root, [string[]]$ExcludeNames = @()) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $Root)) {
        return $result
    }
    foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -File -Filter "*.cs") {
        if ($file.BaseName -in $ExcludeNames) {
            continue
        }
        $result[$file.BaseName] = Get-RelativePathNormalized $repoRoot $file.FullName
    }
    return $result
}

function Get-AllFlatNativeLocalization([string]$Language) {
    $directory = Join-Path $repoRoot "TogawaSakiko/TogawaSakiko/localization/$Language"
    $result = @{}
    if (-not (Test-Path -LiteralPath $directory)) {
        return $result
    }
    foreach ($file in Get-ChildItem -LiteralPath $directory -File -Filter "*.json" | Sort-Object Name) {
        $map = Read-JsonMap $file.FullName
        foreach ($key in $map.Keys) {
            if ($result.ContainsKey($key)) {
                throw "Duplicate native localization key '$key' in $($file.FullName)."
            }
            $result[$key] = $map[$key]
        }
    }
    return $result
}

function Get-NativeLocalizationTable([string]$Language, [string]$FileName) {
    $path = Join-Path $repoRoot "TogawaSakiko/TogawaSakiko/localization/$Language/$FileName"
    if (-not (Test-Path -LiteralPath $path)) {
        return @{}
    }
    return Read-JsonMap $path
}

function Get-Sts2CardTypeName([string]$Sts1Name) {
    switch ($Sts1Name) {
        "Strike" { return "StrikeTogawaSakiko" }
        "Defend" { return "DefendTogawaSakiko" }
        default { return "${Sts1Name}Card" }
    }
}

function Get-PortStatus([bool]$Native, [bool]$Legacy) {
    if ($Native) { return "Native" }
    if ($Legacy) { return "Legacy evidence only" }
    return "STS1 source only"
}

function Add-MarkdownTable(
    [System.Text.StringBuilder]$Builder,
    [string[]]$Headers,
    [System.Collections.IEnumerable]$Rows
) {
    [void]$Builder.AppendLine("| " + (($Headers | ForEach-Object { ConvertTo-MarkdownCell $_ }) -join " | ") + " |")
    [void]$Builder.AppendLine("| " + (($Headers | ForEach-Object { "---" }) -join " | ") + " |")
    foreach ($row in $Rows) {
        $cells = [object[]]$row
        [void]$Builder.AppendLine("| " + (($cells | ForEach-Object { ConvertTo-MarkdownCell $_ }) -join " | ") + " |")
    }
    [void]$Builder.AppendLine()
}

$outputJsonPath = Resolve-RepoPath $OutputJson
$outputMarkdownPath = Resolve-RepoPath $OutputMarkdown
$javaRoot = Join-Path $Sts1Root "src/main/java/togawasakikomod"
$sts1ResourceRoot = Join-Path $Sts1Root "src/main/resources/togawasakikomod"
$nativeResourceRoot = Join-Path $repoRoot "TogawaSakiko/TogawaSakiko"

$sts1Loc = @{}
foreach ($language in @("eng", "zhs")) {
    $sts1Loc[$language] = @{}
    $directory = Join-Path $sts1ResourceRoot "localization/$language"
    foreach ($file in Get-ChildItem -LiteralPath $directory -File -Filter "*.json" | Sort-Object Name) {
        $sts1Loc[$language][$file.Name] = Read-JsonMap $file.FullName
    }
}

$nativeLoc = @{
    eng = @{
        cards = Get-NativeLocalizationTable "eng" "cards.json"
        powers = Get-NativeLocalizationTable "eng" "powers.json"
        relics = Get-NativeLocalizationTable "eng" "relics.json"
        potions = Get-NativeLocalizationTable "eng" "potions.json"
    }
    zhs = @{
        cards = Get-NativeLocalizationTable "zhs" "cards.json"
        powers = Get-NativeLocalizationTable "zhs" "powers.json"
        relics = Get-NativeLocalizationTable "zhs" "relics.json"
        potions = Get-NativeLocalizationTable "zhs" "potions.json"
    }
}

$legacyCards = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/TogawaSakikoCode/Cards") @("TogawaSakikoCard")
$nativeCards = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/NativeCode/Models/Cards")
$legacyPowers = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/TogawaSakikoCode/Powers") @("TogawaSakikoPower")
$nativePowers = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/NativeCode/Models/Powers")
$legacyRelics = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/TogawaSakikoCode/Relics") @("TogawaSakikoRelic")
$nativeRelics = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/NativeCode/Models/Relics")
$legacyPotions = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/TogawaSakikoCode/Potions") @("TogawaSakikoPotion")
$nativePotions = Get-CSharpFilesByBaseName (Join-Path $repoRoot "TogawaSakiko/NativeCode/Models/Potions")

$cards = @()
$cardRoot = Join-Path $javaRoot "cards"
$cardFiles = Get-ChildItem -LiteralPath $cardRoot -Recurse -File -Filter "*.java" |
    Where-Object { $_.BaseName -notin @("BaseCard", "CustomTags") } |
    Sort-Object BaseName

foreach ($file in $cardFiles) {
    $name = $file.BaseName
    $raw = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
    $active = Remove-JavaComments $raw
    $stats = [regex]::Match(
        $active,
        "new\s+CardStats\s*\(\s*[^,]+,\s*CardType\.([A-Z_]+)\s*,\s*CardRarity\.([A-Z_]+)\s*,\s*CardTarget\.([A-Z_]+)\s*,\s*(-?\d+)",
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $stats.Success) {
        throw "Could not parse CardStats in $($file.FullName)."
    }

    $type = $stats.Groups[1].Value
    $rarity = $stats.Groups[2].Value
    $target = $stats.Groups[3].Value
    $cost = [int]$stats.Groups[4].Value
    $enabled = -not [regex]::IsMatch($active, "@CardEnable\s*\(\s*enable\s*=\s*false\s*\)")
    $sts2Type = Get-Sts2CardTypeName $name
    $entry = ConvertTo-ModelEntry $sts2Type
    $stableId = "TOGAWASAKIKO-$entry"
    $native = $nativeCards.ContainsKey($sts2Type)
    $legacy = $legacyCards.ContainsKey($sts2Type)

    $audio = @(
        [regex]::Matches($active, 'new\s+PlayAudioAction\s*\(\s*(?:([A-Za-z0-9_]+)\.class\.getSimpleName\s*\(\s*\)|"([^"]+)")') |
            ForEach-Object {
                if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value }
            } |
            Sort-Object -Unique
    )
    $imports = @(
        [regex]::Matches($active, '(?m)^\s*import\s+togawasakikomod\.([A-Za-z0-9_.]+)\s*;') |
            ForEach-Object { $_.Groups[1].Value } |
            Sort-Object -Unique
    )

    $sts1LocKey = '${modID}:' + $name
    $engRecord = Get-LocRecord $sts1Loc.eng["CardStrings.json"] $sts1LocKey
    $zhsRecord = Get-LocRecord $sts1Loc.zhs["CardStrings.json"] $sts1LocKey
    $nativeEngTitle = $nativeLoc.eng.cards.ContainsKey("$stableId.title")
    $nativeEngDescription = $nativeLoc.eng.cards.ContainsKey("$stableId.description")
    $nativeZhsTitle = $nativeLoc.zhs.cards.ContainsKey("$stableId.title")
    $nativeZhsDescription = $nativeLoc.zhs.cards.ContainsKey("$stableId.description")

    $artFolder = $type.ToLowerInvariant()
    $sts1Small = Join-Path $sts1ResourceRoot "images/cards/$artFolder/$name.png"
    $sts1Large = Join-Path $sts1ResourceRoot "images/cards/$artFolder/${name}_p.png"
    $assetName = $sts2Type.ToLowerInvariant() + ".png"
    $sts2Small = Join-Path $nativeResourceRoot "images/card_portraits/$assetName"
    $sts2Large = Join-Path $nativeResourceRoot "images/card_portraits/big/$assetName"
    $originalSmall = Test-Path -LiteralPath $sts1Small
    $originalLarge = Test-Path -LiteralPath $sts1Large
    $smallDimensions = Get-PngDimensions $sts2Small
    $largeDimensions = Get-PngDimensions $sts2Large
    $smallValid = Test-ExactPngSize $sts2Small 250 190
    $largeValid = Test-ExactPngSize $sts2Large 500 380
    $artStatus = if ($smallValid -and $largeValid) {
        if ($originalSmall -and $originalLarge) { "Original" } else { "Generated placeholder" }
    } elseif (-not (Test-Path -LiteralPath $sts2Small) -or -not (Test-Path -LiteralPath $sts2Large)) {
        "Missing"
    } else {
        "Resolution mismatch"
    }

    $differences = [System.Collections.Generic.List[string]]::new()
    if (-not $native) { $differences.Add("native behavior missing") }
    if (-not ($nativeEngTitle -and $nativeEngDescription)) { $differences.Add("native English localization missing") }
    if (-not ($nativeZhsTitle -and $nativeZhsDescription)) { $differences.Add("native Simplified Chinese localization missing") }
    if (-not ($originalSmall -and $originalLarge)) { $differences.Add("STS1 original art missing") }
    if ($artStatus -eq "Missing") { $differences.Add("STS2 art missing") }
    if ($artStatus -eq "Resolution mismatch") { $differences.Add("STS2 art resolution mismatch") }
    if (-not $enabled) { $differences.Add("disabled in STS1; must stay out of normal pools") }

    $relativeParts = (Get-RelativePathNormalized $cardRoot $file.FullName).Split("/")
    $deck = if ($relativeParts.Length -gt 1) { $relativeParts[0] } else { "Unknown" }

    $cards += [ordered]@{
        name = $name
        deck = $deck
        type = $type
        rarity = $rarity
        target = $target
        cost = $cost
        enabledInSts1 = $enabled
        sts1Id = "togawasakikomod:$name"
        sts2Type = $sts2Type
        sts2StableId = $stableId
        portStatus = Get-PortStatus $native $legacy
        nativeSource = if ($native) { $nativeCards[$sts2Type] } else { $null }
        legacyEvidence = if ($legacy) { $legacyCards[$sts2Type] } else { $null }
        sts1Source = Get-RelativePathNormalized $Sts1Root $file.FullName
        customImports = $imports
        directVoiceKeys = $audio
        localization = [ordered]@{
            sts1English = $engRecord
            sts1SimplifiedChinese = $zhsRecord
            nativeEnglishComplete = $nativeEngTitle -and $nativeEngDescription
            nativeSimplifiedChineseComplete = $nativeZhsTitle -and $nativeZhsDescription
        }
        art = [ordered]@{
            status = $artStatus
            originalSmall = if ($originalSmall) { Get-RelativePathNormalized $Sts1Root $sts1Small } else { $null }
            originalLarge = if ($originalLarge) { Get-RelativePathNormalized $Sts1Root $sts1Large } else { $null }
            sts2Small = Get-RelativePathNormalized $repoRoot $sts2Small
            sts2SmallDimensions = $smallDimensions
            sts2Large = Get-RelativePathNormalized $repoRoot $sts2Large
            sts2LargeDimensions = $largeDimensions
            requiredSmall = "250x190"
            requiredLarge = "500x380"
        }
        differences = @($differences)
    }
}

$inheritedPowerPresentation = @("PlayerFilightPower", "MonsterVigorPower")
$powers = @()
$powerRoot = Join-Path $javaRoot "powers"
$powerFiles = Get-ChildItem -LiteralPath $powerRoot -Recurse -File -Filter "*.java" |
    Where-Object BaseName -ne "BasePower" |
    Sort-Object BaseName

foreach ($file in $powerFiles) {
    $name = $file.BaseName
    $native = $nativePowers.ContainsKey($name)
    $legacy = $legacyPowers.ContainsKey($name)
    $stableId = "TOGAWASAKIKO-$(ConvertTo-ModelEntry $name)"
    $locKey = '${modID}:' + $name
    $engRecord = Get-LocRecord $sts1Loc.eng["PowerStrings.json"] $locKey
    $zhsRecord = Get-LocRecord $sts1Loc.zhs["PowerStrings.json"] $locKey
    $inheritsBasePresentation = $name -in $inheritedPowerPresentation
    $sts1Small = Join-Path $sts1ResourceRoot "images/powers/$name.png"
    $sts1Large = Join-Path $sts1ResourceRoot "images/powers/large/$name.png"
    $assetName = $name.ToLowerInvariant() + ".png"
    $sts2Small = Join-Path $nativeResourceRoot "images/powers/$assetName"
    $sts2Large = Join-Path $nativeResourceRoot "images/powers/big/$assetName"
    $smallValid = Test-ExactPngSize $sts2Small 32 32
    $largeValid = Test-ExactPngSize $sts2Large 84 84
    $artStatus = if ($inheritsBasePresentation) {
        "Reuse base-game power"
    } elseif ($smallValid -and $largeValid) {
        "Original"
    } elseif (-not (Test-Path -LiteralPath $sts2Small) -or -not (Test-Path -LiteralPath $sts2Large)) {
        "Missing"
    } else {
        "Resolution mismatch"
    }
    $nativeLocComplete = $nativeLoc.eng.powers.ContainsKey("$stableId.title") -and
        $nativeLoc.eng.powers.ContainsKey("$stableId.description") -and
        $nativeLoc.zhs.powers.ContainsKey("$stableId.title") -and
        $nativeLoc.zhs.powers.ContainsKey("$stableId.description")

    $differences = [System.Collections.Generic.List[string]]::new()
    if (-not $native) { $differences.Add("native behavior missing") }
    if ($null -eq $engRecord -or $null -eq $zhsRecord) {
        if ($inheritsBasePresentation) { $differences.Add("inherits base-game localization") }
        else { $differences.Add("STS1 localization missing") }
    }
    if (-not $nativeLocComplete -and -not $inheritsBasePresentation) { $differences.Add("native localization missing") }
    if ($artStatus -eq "Missing") { $differences.Add("custom art missing") }
    if ($artStatus -eq "Resolution mismatch") { $differences.Add("custom art resolution mismatch") }

    $powers += [ordered]@{
        name = $name
        group = $file.Directory.Name
        sts2StableId = $stableId
        portStatus = Get-PortStatus $native $legacy
        nativeSource = if ($native) { $nativePowers[$name] } else { $null }
        legacyEvidence = if ($legacy) { $legacyPowers[$name] } else { $null }
        sts1Source = Get-RelativePathNormalized $Sts1Root $file.FullName
        inheritsBasePresentation = $inheritsBasePresentation
        localization = [ordered]@{
            sts1English = $engRecord
            sts1SimplifiedChinese = $zhsRecord
            nativeComplete = $nativeLocComplete
        }
        art = [ordered]@{
            status = $artStatus
            sts1Small = if (Test-Path -LiteralPath $sts1Small) { Get-RelativePathNormalized $Sts1Root $sts1Small } else { $null }
            sts1Large = if (Test-Path -LiteralPath $sts1Large) { Get-RelativePathNormalized $Sts1Root $sts1Large } else { $null }
            sts2Small = Get-RelativePathNormalized $repoRoot $sts2Small
            sts2Large = Get-RelativePathNormalized $repoRoot $sts2Large
            requiredSmall = "32x32"
            requiredLarge = "84x84"
        }
        differences = @($differences)
    }
}

$relics = @()
$relicRoot = Join-Path $javaRoot "relics"
$relicFiles = Get-ChildItem -LiteralPath $relicRoot -File -Filter "*.java" |
    Where-Object BaseName -ne "BaseRelic" |
    Sort-Object BaseName

foreach ($file in $relicFiles) {
    $name = $file.BaseName
    $raw = Remove-JavaComments (Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName)
    $tierMatch = [regex]::Match($raw, "RelicTier\.([A-Z_]+)")
    $tier = if ($tierMatch.Success) { $tierMatch.Groups[1].Value } else { "UNKNOWN" }
    $sts2Type = if ($name -eq "MonochromeHairband") { "StarterRelicTogawaSakiko" } else { $name }
    $native = $nativeRelics.ContainsKey($sts2Type)
    $legacy = $legacyRelics.ContainsKey($sts2Type)
    $stableId = "TOGAWASAKIKO-$(ConvertTo-ModelEntry $sts2Type)"
    $locKey = '${modID}:' + $name
    $engRecord = Get-LocRecord $sts1Loc.eng["RelicStrings.json"] $locKey
    $zhsRecord = Get-LocRecord $sts1Loc.zhs["RelicStrings.json"] $locKey
    $assetStem = $name.ToLowerInvariant()
    $sts2Icon = Join-Path $nativeResourceRoot "images/relics/$assetStem.png"
    $sts2Outline = Join-Path $nativeResourceRoot "images/relics/${assetStem}_outline.png"
    $sts2Big = Join-Path $nativeResourceRoot "images/relics/big/$assetStem.png"
    $artValid = (Test-ExactPngSize $sts2Icon 128 128) -and
        (Test-ExactPngSize $sts2Outline 128 128) -and
        (Test-ExactPngSize $sts2Big 256 256)
    $nativeLocComplete = $nativeLoc.eng.relics.ContainsKey("$stableId.title") -and
        $nativeLoc.eng.relics.ContainsKey("$stableId.description") -and
        $nativeLoc.zhs.relics.ContainsKey("$stableId.title") -and
        $nativeLoc.zhs.relics.ContainsKey("$stableId.description")

    $differences = [System.Collections.Generic.List[string]]::new()
    if (-not $native) { $differences.Add("native behavior missing") }
    if (-not $nativeLocComplete) { $differences.Add("native localization missing") }
    if (-not $artValid) { $differences.Add("STS2 art missing or wrong resolution") }

    $relics += [ordered]@{
        name = $name
        tier = $tier
        sts2Type = $sts2Type
        sts2StableId = $stableId
        portStatus = Get-PortStatus $native $legacy
        nativeSource = if ($native) { $nativeRelics[$sts2Type] } else { $null }
        legacyEvidence = if ($legacy) { $legacyRelics[$sts2Type] } else { $null }
        sts1Source = Get-RelativePathNormalized $Sts1Root $file.FullName
        localization = [ordered]@{
            sts1English = $engRecord
            sts1SimplifiedChinese = $zhsRecord
            nativeComplete = $nativeLocComplete
        }
        art = [ordered]@{
            status = if ($artValid) { "Original" } else { "Missing or resolution mismatch" }
            icon = Get-RelativePathNormalized $repoRoot $sts2Icon
            outline = Get-RelativePathNormalized $repoRoot $sts2Outline
            big = Get-RelativePathNormalized $repoRoot $sts2Big
            requiredIcon = "128x128"
            requiredBig = "256x256"
        }
        differences = @($differences)
    }
}

$potions = @()
$potionRoot = Join-Path $javaRoot "potions"
$potionFiles = Get-ChildItem -LiteralPath $potionRoot -File -Filter "*.java" |
    Where-Object BaseName -ne "BasePotion" |
    Sort-Object BaseName

foreach ($file in $potionFiles) {
    $name = $file.BaseName
    $raw = Remove-JavaComments (Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName)
    $rarityMatch = [regex]::Match($raw, "PotionRarity\.([A-Z_]+)")
    $sizeMatch = [regex]::Match($raw, "PotionSize\.([A-Z_]+)")
    $rarity = if ($rarityMatch.Success) { $rarityMatch.Groups[1].Value } else { "UNKNOWN" }
    $size = if ($sizeMatch.Success) { $sizeMatch.Groups[1].Value } else { "UNKNOWN" }
    $stableId = "TOGAWASAKIKO-$(ConvertTo-ModelEntry $name)"
    $native = $nativePotions.ContainsKey($name)
    $legacy = $legacyPotions.ContainsKey($name)
    $locKey = '${modID}:' + $name
    $engRecord = Get-LocRecord $sts1Loc.eng["PotionStrings.json"] $locKey
    $zhsRecord = Get-LocRecord $sts1Loc.zhs["PotionStrings.json"] $locKey
    $sourceArtDirectory = Join-Path $sts1ResourceRoot "images/potions/$name"
    $sourceLayers = @()
    if (Test-Path -LiteralPath $sourceArtDirectory) {
        $sourceLayers = @(Get-ChildItem -LiteralPath $sourceArtDirectory -File -Filter "*.png" | Sort-Object Name)
    }
    $nativeArtDirectory = Join-Path $nativeResourceRoot "images/potions/$($name.ToLowerInvariant())"
    $layerRows = @()
    foreach ($sourceLayer in $sourceLayers) {
        $destination = Join-Path $nativeArtDirectory $sourceLayer.Name.ToLowerInvariant()
        $layerRows += [ordered]@{
            name = $sourceLayer.Name.ToLowerInvariant()
            source = Get-RelativePathNormalized $Sts1Root $sourceLayer.FullName
            destination = Get-RelativePathNormalized $repoRoot $destination
            presentAt64x64 = Test-ExactPngSize $destination 64 64
        }
    }
    $artComplete = $layerRows.Count -gt 0 -and @($layerRows | Where-Object { -not $_.presentAt64x64 }).Count -eq 0
    $nativeLocComplete = $nativeLoc.eng.potions.ContainsKey("$stableId.title") -and
        $nativeLoc.eng.potions.ContainsKey("$stableId.description") -and
        $nativeLoc.zhs.potions.ContainsKey("$stableId.title") -and
        $nativeLoc.zhs.potions.ContainsKey("$stableId.description")

    $differences = [System.Collections.Generic.List[string]]::new()
    if (-not $native) { $differences.Add("native behavior missing") }
    if (-not $nativeLocComplete) { $differences.Add("native localization missing") }
    if (-not $artComplete) { $differences.Add("STS2 potion layers missing") }

    $potions += [ordered]@{
        name = $name
        rarity = $rarity
        size = $size
        sts2StableId = $stableId
        portStatus = Get-PortStatus $native $legacy
        nativeSource = if ($native) { $nativePotions[$name] } else { $null }
        legacyEvidence = if ($legacy) { $legacyPotions[$name] } else { $null }
        sts1Source = Get-RelativePathNormalized $Sts1Root $file.FullName
        localization = [ordered]@{
            sts1English = $engRecord
            sts1SimplifiedChinese = $zhsRecord
            nativeComplete = $nativeLocComplete
        }
        art = [ordered]@{
            status = if ($artComplete) { "Original" } else { "Missing"
            }
            requiredLayerSize = "64x64"
            layers = $layerRows
        }
        differences = @($differences)
    }
}

$directCardVoiceKeys = @($cards.directVoiceKeys | ForEach-Object { $_ } | Sort-Object -Unique)
$nativeCodeText = ((Get-ChildItem -LiteralPath (Join-Path $repoRoot "TogawaSakiko/NativeCode") -Recurse -File -Filter "*.cs" |
    ForEach-Object { Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName }) -join "`n")
$nativeCardVoiceKeys = @(
    [regex]::Matches($nativeCodeText, 'TryPlayCardVoice\s*\([^,]+,\s*"([^"]+)"\s*\)') |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique
)
$audio = @()
$sourceAudioRoot = Join-Path $sts1ResourceRoot "audio"
$nativeAudioRoot = Join-Path $nativeResourceRoot "audio"
$nativeAudioByRelativeLower = @{}
if (Test-Path -LiteralPath $nativeAudioRoot) {
    foreach ($file in Get-ChildItem -LiteralPath $nativeAudioRoot -Recurse -File | Where-Object Extension -ne ".import") {
        $relative = Get-RelativePathNormalized $nativeAudioRoot $file.FullName
        $nativeAudioByRelativeLower[$relative.ToLowerInvariant()] = $relative
    }
}

foreach ($file in Get-ChildItem -LiteralPath $sourceAudioRoot -Recurse -File | Sort-Object FullName) {
    $relative = Get-RelativePathNormalized $sourceAudioRoot $file.FullName
    $lookup = $relative.ToLowerInvariant()
    $present = $nativeAudioByRelativeLower.ContainsKey($lookup)
    $stem = $file.BaseName
    $usage = if ($relative.StartsWith("sakiko/", [System.StringComparison]::OrdinalIgnoreCase)) {
        if ($stem -in $directCardVoiceKeys) { "Direct card voice" }
        elseif ($stem -match '^(Intro|Hurt\d+|General\d+|Others\d+)$') { "Character voice" }
        else { "Registered STS1 voice; no direct card call found" }
    } elseif ($relative.StartsWith("cutscene/", [System.StringComparison]::OrdinalIgnoreCase)) {
        "Ending cutscene"
    } elseif ($relative.StartsWith("music/", [System.StringComparison]::OrdinalIgnoreCase)) {
        "Custom music"
    } else {
        "Custom VFX"
    }
    $nativeReference = $nativeCodeText.Contains("audio/" + $relative.Replace("\", "/"), [System.StringComparison]::OrdinalIgnoreCase) -or
        $nativeCodeText.Contains("audio\\" + $relative.Replace("/", "\"), [System.StringComparison]::OrdinalIgnoreCase) -or
        ($relative.StartsWith("sakiko/", [System.StringComparison]::OrdinalIgnoreCase) -and
            $nativeCardVoiceKeys -contains $stem)

    $audio += [ordered]@{
        relativePath = $relative
        usage = $usage
        copiedToSts2 = $present
        sts2Path = if ($present) { "TogawaSakiko/TogawaSakiko/audio/" + $nativeAudioByRelativeLower[$lookup] } else { "TogawaSakiko/TogawaSakiko/audio/$relative" }
        wiredInNativeCode = $nativeReference
    }
}

$domainDefinitions = [ordered]@{
    actions = "Actions"
    effects = "effects"
    events = "events"
    intents = "intents"
    modifiers = "modifiers"
    monsters = "monsters"
    patches = "patches"
    rewards = "rewards"
    rooms = "rooms"
    saveable = "saveable"
    scenes = "scenes"
    screens = "screens"
}

$sharedContractMappings = @{
    "AddCardToDeckEXAction" = "PersistentDeckMutation.AddCanonicalAsync"
    "RemoveCardFromDeckAction" = "PersistentDeckMutation.RemoveExactAsync"
    "RemoveCardFromDrawPileAction" = "PersistentDeckRemovalGameAction"
    "RemoveCardFromHandAction" = "PersistentDeckRemovalGameAction"
    "ReducedPowerRecorderPatch" = "PowerChangeLedgerService"
    "BlockEventPatch" = "HypePower + HypeExplicitBlockLossPatch"
    "ObtainRewardEventPatch" = "StarterRelicTogawaSakiko.AfterCombatVictory"
}

$domains = @()
foreach ($domain in $domainDefinitions.Keys) {
    $directory = Join-Path $javaRoot $domainDefinitions[$domain]
    $items = @()
    if (Test-Path -LiteralPath $directory) {
        foreach ($file in Get-ChildItem -LiteralPath $directory -Recurse -File -Filter "*.java" | Sort-Object BaseName) {
            $items += [ordered]@{
                name = $file.BaseName
                source = Get-RelativePathNormalized $Sts1Root $file.FullName
                nativeEquivalent = if ($sharedContractMappings.ContainsKey($file.BaseName)) { $sharedContractMappings[$file.BaseName] } else { $null }
            }
        }
    }
    $domains += [ordered]@{ domain = $domain; count = $items.Count; items = $items }
}

$sourcePresentationFiles = @()
$sourceImageRoot = Join-Path $sts1ResourceRoot "images"
$nativeImagesByRelativeLower = @{}
$nativeImageRoot = Join-Path $nativeResourceRoot "images"
foreach ($file in Get-ChildItem -LiteralPath $nativeImageRoot -Recurse -File -Filter "*.png") {
    $relative = Get-RelativePathNormalized $nativeImageRoot $file.FullName
    $nativeImagesByRelativeLower[$relative.ToLowerInvariant()] = $relative
}
foreach ($file in Get-ChildItem -LiteralPath $sourceImageRoot -Recurse -File -Filter "*.png" | Sort-Object FullName) {
    $relative = Get-RelativePathNormalized $sourceImageRoot $file.FullName
    if ($relative.StartsWith("cards/") -or $relative.StartsWith("powers/") -or $relative.StartsWith("relics/")) {
        continue
    }
    $lookup = $relative.ToLowerInvariant()
    $dimensions = Get-PngDimensions $file.FullName
    $sourcePresentationFiles += [ordered]@{
        relativePath = $relative
        dimensions = $dimensions.text
        copiedAtSameRelativePath = $nativeImagesByRelativeLower.ContainsKey($lookup)
        nativePath = if ($nativeImagesByRelativeLower.ContainsKey($lookup)) { "images/" + $nativeImagesByRelativeLower[$lookup] } else { "images/$relative" }
    }
}

$localizationTables = @()
$allLocalizationFileNames = @($sts1Loc.eng.Keys + $sts1Loc.zhs.Keys | Sort-Object -Unique)
foreach ($fileName in $allLocalizationFileNames) {
    $engMap = $sts1Loc.eng[$fileName]
    $zhsMap = $sts1Loc.zhs[$fileName]
    $engKeys = @(Get-Sts1LocalizationKeys $engMap)
    $zhsKeys = @(Get-Sts1LocalizationKeys $zhsMap)
    $engOnly = @($engKeys | Where-Object { -not (Test-Sts1LocalizationContainsKey $zhsMap $_) })
    $zhsOnly = @($zhsKeys | Where-Object { -not (Test-Sts1LocalizationContainsKey $engMap $_) })
    $nativeFileName = switch ($fileName) {
        "CardStrings.json" { "cards.json" }
        "CharacterStrings.json" { "characters.json" }
        "Keywords.json" { "card_keywords.json" }
        "PotionStrings.json" { "potions.json" }
        "PowerStrings.json" { "powers.json" }
        "RelicStrings.json" { "relics.json" }
        default { $null }
    }
    $localizationTables += [ordered]@{
        sourceTable = $fileName
        englishEntries = $engMap.Count
        simplifiedChineseEntries = $zhsMap.Count
        englishOnlyKeys = $engOnly
        simplifiedChineseOnlyKeys = $zhsOnly
        nativeDestination = $nativeFileName
        nativeTableExists = $null -ne $nativeFileName -and
            (Test-Path -LiteralPath (Join-Path $nativeResourceRoot "localization/eng/$nativeFileName")) -and
            (Test-Path -LiteralPath (Join-Path $nativeResourceRoot "localization/zhs/$nativeFileName"))
    }
}

if ($cards.Count -ne 95) { throw "Expected 95 STS1 card models, found $($cards.Count)." }
if ($powers.Count -ne 36) { throw "Expected 36 STS1 power models, found $($powers.Count)." }
if ($relics.Count -ne 11) { throw "Expected 11 concrete STS1 relics, found $($relics.Count)." }
if ($potions.Count -ne 6) { throw "Expected 6 concrete STS1 potions, found $($potions.Count)." }

$summary = [ordered]@{
    cards = [ordered]@{
        sts1 = $cards.Count
        enabledInSts1 = @($cards | Where-Object enabledInSts1).Count
        disabledInSts1 = @($cards | Where-Object { -not $_.enabledInSts1 }).Count
        native = @($cards | Where-Object portStatus -eq "Native").Count
        missingNative = @($cards | Where-Object portStatus -ne "Native").Count
        originalArtPairs = @($cards | Where-Object { $_.art.status -eq "Original" }).Count
        generatedPlaceholderPairs = @($cards | Where-Object { $_.art.status -eq "Generated placeholder" }).Count
        missingOrWrongArtPairs = @($cards | Where-Object { $_.art.status -in @("Missing", "Resolution mismatch") }).Count
    }
    powers = [ordered]@{
        sts1 = $powers.Count
        native = @($powers | Where-Object portStatus -eq "Native").Count
        missingNative = @($powers | Where-Object portStatus -ne "Native").Count
        customArtPairs = @($powers | Where-Object { $_.art.status -eq "Original" }).Count
        baseGamePresentation = @($powers | Where-Object inheritsBasePresentation).Count
        missingSts1Localization = @($powers | Where-Object { $null -eq $_.localization.sts1English -or $null -eq $_.localization.sts1SimplifiedChinese }).Count
    }
    relics = [ordered]@{
        sts1Concrete = $relics.Count
        native = @($relics | Where-Object portStatus -eq "Native").Count
        missingNative = @($relics | Where-Object portStatus -ne "Native").Count
    }
    potions = [ordered]@{
        sts1Concrete = $potions.Count
        native = @($potions | Where-Object portStatus -eq "Native").Count
        missingNative = @($potions | Where-Object portStatus -ne "Native").Count
    }
    audio = [ordered]@{
        sourceFiles = $audio.Count
        copiedToSts2 = @($audio | Where-Object copiedToSts2).Count
        missingFromSts2 = @($audio | Where-Object { -not $_.copiedToSts2 }).Count
        wiredInNativeCode = @($audio | Where-Object wiredInNativeCode).Count
    }
    presentation = [ordered]@{
        nonCardPowerRelicSourcePngs = $sourcePresentationFiles.Count
        copiedAtSameRelativePath = @($sourcePresentationFiles | Where-Object copiedAtSameRelativePath).Count
        notCopiedAtSameRelativePath = @($sourcePresentationFiles | Where-Object { -not $_.copiedAtSameRelativePath }).Count
    }
}

$inventory = [ordered]@{
    schemaVersion = 1
    baseline = [ordered]@{
        targetGame = "Slay the Spire 2 v0.111.0 (41cef1ea)"
        sourceOfTruth = "STS1 Java and resources"
        repositoryRoot = $repoRoot
        sts1Root = $Sts1Root
        generatedPlaceholder = "TogawaSakiko/TogawaSakiko/images/placeholders/missing_content.png"
        placeholderPolicy = "Use source/exemplar slot dimensions and a stable destination filename so final art can replace it without code changes."
    }
    summary = $summary
    cards = $cards
    powers = $powers
    relics = $relics
    potions = $potions
    domains = $domains
    audio = $audio
    presentationFiles = $sourcePresentationFiles
    localizationTables = $localizationTables
}

$json = $inventory | ConvertTo-Json -Depth 20
[System.IO.Directory]::CreateDirectory((Split-Path $outputJsonPath -Parent)) | Out-Null
[System.IO.File]::WriteAllText($outputJsonPath, $json + "`n", [System.Text.UTF8Encoding]::new($false))

$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine("# Full Port Parity Inventory")
[void]$md.AppendLine()
[void]$md.AppendLine("> Generated by ``tools/Build-Sts1ParityInventory.ps1`` from the STS1 Java/resources and the current native worktree. Do not use the older ``CARDS_PLAN.md`` approximation table as behavior truth.")
[void]$md.AppendLine()
[void]$md.AppendLine("Baseline: Slay the Spire 2 ``v0.111.0`` / ``41cef1ea``. Behavior and player-facing source truth: the STS1 project. Stable STS2 IDs follow the preserved ``TOGAWASAKIKO-`` policy.")
[void]$md.AppendLine()
[void]$md.AppendLine("## Scope totals and current gap")
[void]$md.AppendLine()
Add-MarkdownTable $md @("Domain", "STS1 concrete", "Native now", "Missing native", "Important correction") @(
    @("Cards", $summary.cards.sts1, $summary.cards.native, $summary.cards.missingNative, "$($summary.cards.disabledInSts1) are explicitly disabled in STS1"),
    @("Powers", $summary.powers.sts1, $summary.powers.native, $summary.powers.missingNative, "34 custom-presented + 2 base-game-derived"),
    @("Relics", $summary.relics.sts1Concrete, $summary.relics.native, $summary.relics.missingNative, "11 concrete, not 12; BaseRelic is abstract scaffolding"),
    @("Potions", $summary.potions.sts1Concrete, $summary.potions.native, $summary.potions.missingNative, "6 concrete, not 7; BasePotion is abstract scaffolding")
)
[void]$md.AppendLine("## Corrections to earlier port assumptions")
[void]$md.AppendLine()
[void]$md.AppendLine("- STS1 contains 95 real card classes, not 97; ``BaseCard`` and ``CustomTags`` are infrastructure.")
[void]$md.AppendLine("- STS1 contains 11 concrete relics and 6 concrete potions. The older workflow counts included each abstract base class.")
[void]$md.AppendLine("- ``Weakness`` exists as a disabled card and has localization, but the STS1 project has no small or large portrait for it.")
[void]$md.AppendLine("- STS1 card portraits require 250x190 small art and 500x380 large art. A same-size 250x190 duplicate in ``big/`` is not a valid large pair.")
[void]$md.AppendLine("- ``PlayerFilightPower`` and ``MonsterVigorPower`` intentionally subclass base-game Flight/Vigor and have neither custom localization nor custom icons in STS1. Their port should explicitly reuse the corresponding STS2 base-game presentation.")
[void]$md.AppendLine("- Existing C# under ``TogawaSakikoCode`` is migration evidence only. Its simplified behavior and BaseLib assumptions are not parity evidence.")
[void]$md.AppendLine()
[void]$md.AppendLine("## Placeholder contract")
[void]$md.AppendLine()
[void]$md.AppendLine("The generated master placeholder is ``res://TogawaSakiko/images/placeholders/missing_content.png``. Derived placeholders must use the exact dimensions of the source/exemplar slot and the final destination filename. Replacement art can therefore overwrite the PNG without changing model code, localization, IDs, or scene layout.")
[void]$md.AppendLine()
[void]$md.AppendLine("## Cards")
[void]$md.AppendLine()
$cardRows = [System.Collections.Generic.List[object[]]]::new()
$index = 0
foreach ($card in $cards) {
    $index++
    $cardRows.Add([object[]]@(
        $index,
        $card.name,
        $card.deck,
        "$($card.type) / $($card.rarity) / $($card.cost)",
        $(if ($card.enabledInSts1) { "Enabled" } else { "Disabled" }),
        $card.portStatus,
        $card.art.status,
        $(if ($card.directVoiceKeys.Count -gt 0) { $card.directVoiceKeys -join ", " } else { "-" }),
        $card.sts2StableId,
        (@($card.differences) -join "; ")
    ))
}
Add-MarkdownTable $md @("#", "STS1 card", "Deck", "Type / rarity / cost", "STS1 pool", "Port", "Art", "Direct voice", "Stable STS2 ID", "Missing or different") $cardRows

[void]$md.AppendLine("## Powers")
[void]$md.AppendLine()
$powerRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($power in $powers) {
    $powerRows.Add([object[]]@($power.name, $power.group, $power.portStatus, $power.art.status, $power.sts2StableId, (@($power.differences) -join "; ")))
}
Add-MarkdownTable $md @("STS1 power", "Group", "Port", "Presentation", "Stable STS2 ID", "Missing or different") $powerRows

[void]$md.AppendLine("## Relics")
[void]$md.AppendLine()
$relicRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($relic in $relics) {
    $relicRows.Add([object[]]@($relic.name, $relic.tier, $relic.portStatus, $relic.art.status, $relic.sts2StableId, (@($relic.differences) -join "; ")))
}
Add-MarkdownTable $md @("STS1 relic", "Tier", "Port", "Art", "Stable STS2 ID", "Missing or different") $relicRows

[void]$md.AppendLine("## Potions")
[void]$md.AppendLine()
$potionRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($potion in $potions) {
    $potionRows.Add([object[]]@($potion.name, $potion.rarity, $potion.size, $potion.portStatus, $potion.art.status, $potion.sts2StableId, (@($potion.differences) -join "; ")))
}
Add-MarkdownTable $md @("STS1 potion", "Rarity", "Shape", "Port", "64x64 layers", "Stable STS2 ID", "Missing or different") $potionRows

[void]$md.AppendLine("## Mechanics and non-model source")
[void]$md.AppendLine()
$domainRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($domain in $domains) {
    $ported = @($domain.items | Where-Object { $null -ne $_.nativeEquivalent }).Count
    $missingNames = @($domain.items | Where-Object { $null -eq $_.nativeEquivalent } | ForEach-Object name)
    $domainRows.Add([object[]]@($domain.domain, $domain.count, $ported, $(if ($missingNames.Count -gt 0) { $missingNames -join ", " } else { "-" })))
}
Add-MarkdownTable $md @("Domain", "STS1 files", "Mapped native contract", "Still needs native review/port") $domainRows

[void]$md.AppendLine("## Audio")
[void]$md.AppendLine()
[void]$md.AppendLine("Source audio: $($summary.audio.sourceFiles). Copied into STS2: $($summary.audio.copiedToSts2). Missing from STS2: $($summary.audio.missingFromSts2). Wired by canonical native code: $($summary.audio.wiredInNativeCode).")
[void]$md.AppendLine()
$audioRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($item in $audio) {
    $audioRows.Add([object[]]@($item.relativePath, $item.usage, $(if ($item.copiedToSts2) { "Present" } else { "Missing" }), $(if ($item.wiredInNativeCode) { "Wired" } else { "Not wired" })))
}
Add-MarkdownTable $md @("STS1 audio", "STS1 use", "STS2 asset", "Native behavior") $audioRows

[void]$md.AppendLine("## Localization tables")
[void]$md.AppendLine()
$locRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($table in $localizationTables) {
    $locRows.Add([object[]]@(
        $table.sourceTable,
        $table.englishEntries,
        $table.simplifiedChineseEntries,
        $(if ($table.nativeTableExists) { $table.nativeDestination } else { "Missing native destination" }),
        $(if ($table.englishOnlyKeys.Count -gt 0) { $table.englishOnlyKeys -join ", " } else { "-" }),
        $(if ($table.simplifiedChineseOnlyKeys.Count -gt 0) { $table.simplifiedChineseOnlyKeys -join ", " } else { "-" })
    ))
}
Add-MarkdownTable $md @("STS1 table", "English", "zh-Hans", "Native table", "English-only keys", "zh-Hans-only keys") $locRows

[void]$md.AppendLine("## Presentation files outside card/power/relic art")
[void]$md.AppendLine()
[void]$md.AppendLine("This is a same-relative-path audit. A missing row may need a new STS2 scene mapping rather than a direct copy.")
[void]$md.AppendLine()
$presentationRows = [System.Collections.Generic.List[object[]]]::new()
foreach ($item in $sourcePresentationFiles) {
    $presentationRows.Add([object[]]@($item.relativePath, $item.dimensions, $(if ($item.copiedAtSameRelativePath) { "Present" } else { "Not present at canonical relative path" }), $item.nativePath))
}
Add-MarkdownTable $md @("STS1 image", "Source dimensions", "STS2", "Planned/native path") $presentationRows

[void]$md.AppendLine("## Execution order")
[void]$md.AppendLine()
[void]$md.AppendLine("1. Repair exact-resolution presentation assets and native EN/zh-Hans catalogs while keeping behaviorless models out of pools.")
[void]$md.AppendLine("2. Port the 34 custom-presented powers and the card dependency graph; explicitly map the two inherited base-game powers.")
[void]$md.AppendLine("3. Port and enable cards in starter/common/uncommon/rare/token/curse order, preserving the nine STS1-disabled exclusions.")
[void]$md.AppendLine("4. Port the remaining ten relics and all six potions through native commands and pools.")
[void]$md.AppendLine("5. Port the custom act, events, monsters, rewards, VFX, music, cutscene, and save branches after the character combat loop is complete.")
[void]$md.AppendLine("6. Run clean package, EN/zh-Hans, save/reload, full-run, vanilla, log, and declared multiplayer gates before release.")
[void]$md.AppendLine()
[void]$md.AppendLine("The machine-readable companion ``docs/FULL_PORT_PARITY_INVENTORY.json`` retains exact STS1 localization records, source paths, dependencies, destination paths, and per-item differences.")

[System.IO.Directory]::CreateDirectory((Split-Path $outputMarkdownPath -Parent)) | Out-Null
[System.IO.File]::WriteAllText($outputMarkdownPath, $md.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host "Parity inventory generated."
Write-Host "Cards: $($summary.cards.native)/$($summary.cards.sts1) native; art gaps or mismatches: $($summary.cards.missingOrWrongArtPairs)."
Write-Host "Powers: $($summary.powers.native)/$($summary.powers.sts1) native."
Write-Host "Relics: $($summary.relics.native)/$($summary.relics.sts1Concrete) native."
Write-Host "Potions: $($summary.potions.native)/$($summary.potions.sts1Concrete) native."
Write-Host "Audio: $($summary.audio.copiedToSts2)/$($summary.audio.sourceFiles) copied; $($summary.audio.wiredInNativeCode) wired."
Write-Host "JSON: $outputJsonPath"
Write-Host "Markdown: $outputMarkdownPath"
