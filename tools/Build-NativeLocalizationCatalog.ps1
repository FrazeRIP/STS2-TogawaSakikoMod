[CmdletBinding()]
param(
    [string]$Sts1Root,
    [string]$InventoryPath = "docs/FULL_PORT_PARITY_INVENTORY.json",
    [string]$ReportJson = "docs/LOCALIZATION_SOURCE_DIFFERENCES.json",
    [string]$ReportMarkdown = "docs/LOCALIZATION_SOURCE_DIFFERENCES.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ([string]::IsNullOrWhiteSpace($Sts1Root)) {
    $Sts1Root = Join-Path (Split-Path $repoRoot -Parent) "STS1-TogawaSakikoMod"
}
$Sts1Root = [System.IO.Path]::GetFullPath($Sts1Root)
$sts1LocRoot = Join-Path $Sts1Root "src/main/resources/togawasakikomod/localization"
$nativeLocRoot = Join-Path $repoRoot "TogawaSakiko/TogawaSakiko/localization"

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Read-Json([string]$Path) {
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json -AsHashtable
}

function Write-Json([string]$Path, [System.Collections.IDictionary]$Value) {
    $resolved = Resolve-RepoPath $Path
    [System.IO.Directory]::CreateDirectory((Split-Path $resolved -Parent)) | Out-Null
    $json = $Value | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($resolved, $json + "`n", [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-ModelEntry([string]$TypeName) {
    $withPlus = $TypeName.Trim().Replace("+", "Plus")
    $camelSplit = [regex]::Replace($withPlus, "([A-Za-z0-9]|\G(?!^))([A-Z])", '$1_$2')
    $whitespaceSplit = [regex]::Replace($camelSplit.ToUpperInvariant(), "\s+", "_")
    return [regex]::Replace($whitespaceSplit, "[^A-Z0-9_]", "")
}

function Convert-ColorToken([System.Text.RegularExpressions.Match]$Match) {
    $colors = @{
        b = "blue"
        g = "green"
        r = "red"
        y = "gold"
        p = "purple"
    }
    $color = $colors[$Match.Groups[1].Value.ToLowerInvariant()]
    $token = $Match.Groups[2].Value
    $trailing = ""
    while ($token.Length -gt 0 -and $token[-1] -in @('.', ',', '!', '?', ';', ':', ')', ']', '。', '，', '！', '？', '；', '：', '）')) {
        $trailing = [string]$token[-1] + $trailing
        $token = $token.Substring(0, $token.Length - 1)
    }
    if ($token.Length -eq 0) {
        return $Match.Value
    }
    return "[$color]$token[/$color]$trailing"
}

function Repair-ProvableSourceDefects([string]$Text, [string]$Language) {
    if ($Language -eq "eng") {
        $Text = $Text.Replace("repalce", "replace")
        $Text = $Text.Replace("begining", "beginning")
        $Text = $Text.Replace("enemires", "enemies")
        $Text = $Text.Replace("apperance", "appearance")
    }
    if ($Language -eq "zhs") {
        $Text = $Text.Replace("获得 !M! 层 脆弱", "获得 !M! 层 易伤")
    }
    return $Text
}

function ConvertTo-NativeTitle([string]$Text, [string]$Language) {
    $result = Repair-ProvableSourceDefects $Text $Language
    return $result.Replace('*', '')
}

function ConvertTo-NativeMarkup([AllowNull()][string]$Text, [string]$Language) {
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $Text
    }

    $result = (Repair-ProvableSourceDefects $Text $Language).Replace("`r`n", "`n")
    $result = $result.Replace('!${modID}:Strength!', '{Strength:diff()}')
    $result = $result.Replace('!D!', '{Damage:diff()}')
    $result = $result.Replace('!B!', '{Block:diff()}')
    $result = $result.Replace('!M!', '{MagicNumber:diff()}')
    $result = $result.Replace('%d', '{Potency}')
    $result = $result.Replace('[E]', '{energyPrefix:energyIcons(1)}')
    $result = $result.Replace('#Block', '[gold]Block[/gold]')
    $result = [regex]::Replace($result, '\bNL\b', "`n")
    $result = [regex]::Replace(
        $result,
        'togawasakikomod:([\p{L}\p{N}_''+\-]+)',
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            $name = $match.Groups[1].Value.Replace('_', ' ')
            return "[gold]$name[/gold]"
        })
    $result = [regex]::Replace(
        $result,
        '#([bgryp])([^\s]+)',
        [System.Text.RegularExpressions.MatchEvaluator]{ param($match) Convert-ColorToken $match })
    $result = [regex]::Replace(
        $result,
        '\*([^*\r\n]+)\*',
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            return "[gold]$($match.Groups[1].Value)[/gold]"
        })
    $result = [regex]::Replace(
        $result,
        '\*([\p{L}\p{N}_''+\-]+)',
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            $name = $match.Groups[1].Value.Replace('_', ' ')
            return "[gold]$name[/gold]"
        })
    do {
        $beforeDeduplication = $result
        $result = [regex]::Replace($result, '\[(gold|blue|green|red|purple)\]\[\1\](.*?)\[/\1\]\[/\1\]', '[$1]$2[/$1]')
    } while ($result -ne $beforeDeduplication)
    $lines = @($result -split "`n", -1 | ForEach-Object {
        $line = [regex]::Replace($_, '[ \t]+', ' ').Trim()
        if ($Language -eq "zhs") {
            $line = [regex]::Replace($line, '\s+([，。！？；：、）])', '$1')
            $line = [regex]::Replace($line, '（\s+', '（')
        }
        $line
    })
    return ($lines -join "`n").Trim()
}

function Merge-UpgradeDescription([string]$Normal, [AllowNull()][string]$Upgraded) {
    if ([string]::IsNullOrWhiteSpace($Upgraded) -or $Normal -eq $Upgraded) {
        return $Normal
    }
    return "{IfUpgraded:show:$Upgraded|$Normal}"
}

function Get-SourceTokens([AllowNull()][string]$Text) {
    if ([string]::IsNullOrEmpty($Text)) {
        return @()
    }
    return @(
        [regex]::Matches($Text, '!\$\{modID\}:Strength!|![DBM]!|\[E\]') |
            ForEach-Object Value |
            Sort-Object -Unique
    )
}

function Get-ExistingFlatMap([string]$Language, [string]$FileName) {
    $path = Join-Path $nativeLocRoot "$Language/$FileName"
    if (-not (Test-Path -LiteralPath $path)) {
        return @{}
    }
    return Read-Json $path
}

function Add-Entry([System.Collections.IDictionary]$Map, [string]$Key, [AllowNull()][object]$Value) {
    if ($Map.Contains($Key)) {
        throw "Duplicate native localization key: $Key"
    }
    $Map[$Key] = $Value
}

$inventoryFile = Resolve-RepoPath $InventoryPath
if (-not (Test-Path -LiteralPath $inventoryFile)) {
    throw "Parity inventory was not found: $inventoryFile"
}
if (-not (Test-Path -LiteralPath $sts1LocRoot)) {
    throw "STS1 localization root was not found: $sts1LocRoot"
}
$inventory = Read-Json $inventoryFile

$languages = @("eng", "zhs")
$sourceLanguageProperty = @{
    eng = "sts1English"
    zhs = "sts1SimplifiedChinese"
}
$preservedLiveCardIds = @(
    $inventory.cards |
        Where-Object { $null -ne $_.nativeSource } |
        ForEach-Object { [string]$_.sts2StableId }
)
$preservedLivePowerIds = @(
    "TOGAWASAKIKO-DAZZLING_POWER",
    "TOGAWASAKIKO-DOLORIS_POWER",
    "TOGAWASAKIKO-HYPE_POWER",
    "TOGAWASAKIKO-KINGS_POWER",
    "TOGAWASAKIKO-MORTIS_POWER",
    "TOGAWASAKIKO-OBLIVIONIS_POWER",
    "TOGAWASAKIKO-MONSTER_DIVINITY_POWER",
    "TOGAWASAKIKO-TIMORIS_POWER"
)
$preservedLiveRelicIds = @("TOGAWASAKIKO-STARTER_RELIC_TOGAWA_SAKIKO")

$existing = @{}
foreach ($language in $languages) {
    $existing[$language] = @{
        cards = Get-ExistingFlatMap $language "cards.json"
        powers = Get-ExistingFlatMap $language "powers.json"
        relics = Get-ExistingFlatMap $language "relics.json"
    }
}

$generated = @{}
foreach ($language in $languages) {
    $generated[$language] = @{
        cards = [ordered]@{}
        powers = [ordered]@{}
        relics = [ordered]@{}
        potions = [ordered]@{}
        card_keywords = [ordered]@{}
    }

    foreach ($card in @($inventory.cards | Sort-Object sts2StableId)) {
        $id = [string]$card.sts2StableId
        $titleKey = "$id.title"
        $descriptionKey = "$id.description"
        if ($id -in $preservedLiveCardIds) {
            if (-not $existing[$language].cards.Contains($titleKey) -or -not $existing[$language].cards.Contains($descriptionKey)) {
                throw "Live localization override is missing for $id ($language)."
            }
            Add-Entry $generated[$language].cards $descriptionKey $existing[$language].cards[$descriptionKey]
            $extraKeys = @(
                $existing[$language].cards.Keys |
                    Where-Object {
                        $_.StartsWith("$id.", [StringComparison]::Ordinal) -and
                        $_ -ne $descriptionKey -and
                        $_ -ne $titleKey
                    } |
                    Sort-Object
            )
            foreach ($extraKey in $extraKeys) {
                Add-Entry $generated[$language].cards $extraKey $existing[$language].cards[$extraKey]
            }
            Add-Entry $generated[$language].cards $titleKey $existing[$language].cards[$titleKey]
            continue
        }

        $source = $card.localization[$sourceLanguageProperty[$language]]
        $title = ConvertTo-NativeTitle ([string]$source.NAME) $language
        if ($language -eq "eng" -and $card.name -eq "MasqueradeRhapsodyRequest") {
            $title = "Masquerade Rhapsody Request"
        }
        $normal = ConvertTo-NativeMarkup ([string]$source.DESCRIPTION) $language
        $upgraded = if ($source.Contains("UPGRADE_DESCRIPTION")) {
            ConvertTo-NativeMarkup ([string]$source.UPGRADE_DESCRIPTION) $language
        } else {
            $null
        }
        if ($card.name -eq "HeartsBarrier") {
            $currentBlock = ConvertTo-NativeMarkup ([string]@($source.EXTENDED_DESCRIPTION)[0]) $language
            $currentBlock = $currentBlock.Replace('{Block:diff()}', '{CalculatedBlock:diff()}')
            $normal = "$normal`n$currentBlock"
            $upgraded = "$upgraded`n$currentBlock"
        }
        if ($card.name -eq "SymbolIVEarth") {
            $currentDamage = ConvertTo-NativeMarkup ([string]@($source.EXTENDED_DESCRIPTION)[0]) $language
            $currentDamage = $currentDamage.Replace('{Damage:diff()}', '{CalculatedDamage:diff()}')
            $normal = "$normal`n$currentDamage"
            if (-not [string]::IsNullOrWhiteSpace($upgraded)) {
                $upgraded = "$upgraded`n$currentDamage"
            }
        }
        Add-Entry $generated[$language].cards $descriptionKey (Merge-UpgradeDescription $normal $upgraded)
        Add-Entry $generated[$language].cards $titleKey $title
    }

    foreach ($power in @($inventory.powers | Sort-Object sts2StableId)) {
        if ($power.inheritsBasePresentation) {
            continue
        }
        $id = [string]$power.sts2StableId
        $titleKey = "$id.title"
        $descriptionKey = "$id.description"
        $smartDescriptionKey = "$id.smartDescription"
        if ($id -in $preservedLivePowerIds) {
            foreach ($key in @($descriptionKey, $smartDescriptionKey, $titleKey)) {
                if (-not $existing[$language].powers.Contains($key)) {
                    throw "Live localization override is missing for $key ($language)."
                }
                Add-Entry $generated[$language].powers $key $existing[$language].powers[$key]
            }
            continue
        }

        $source = $power.localization[$sourceLanguageProperty[$language]]
        $fragments = @($source.DESCRIPTIONS)
        if ($fragments.Count -eq 0) {
            throw "Power source localization is empty for $id ($language)."
        }
        $rawDescription = if ($fragments.Count -eq 1) {
            [string]$fragments[0]
        } else {
            ([string]$fragments[0]) + "{Amount}" + (($fragments | Select-Object -Skip 1) -join "")
        }
        $description = ConvertTo-NativeMarkup $rawDescription $language
        Add-Entry $generated[$language].powers $descriptionKey $description
        Add-Entry $generated[$language].powers $smartDescriptionKey $description
        Add-Entry $generated[$language].powers $titleKey (ConvertTo-NativeTitle ([string]$source.NAME) $language)
    }

    $mantraLocalization = if ($language -eq "eng") {
        [ordered]@{
            "TOGAWASAKIKO-MANTRA_POWER.description" = "At 10 Mantra, lose 10 Mantra and enter Divinity."
            "TOGAWASAKIKO-MANTRA_POWER.smartDescription" = "At 10 Mantra, lose 10 Mantra and enter Divinity."
            "TOGAWASAKIKO-MANTRA_POWER.title" = "Mantra"
        }
    } else {
        [ordered]@{
            "TOGAWASAKIKO-MANTRA_POWER.description" = "达到10层真言时，失去10层真言并进入神格。"
            "TOGAWASAKIKO-MANTRA_POWER.smartDescription" = "达到10层真言时，失去10层真言并进入神格。"
            "TOGAWASAKIKO-MANTRA_POWER.title" = "真言"
        }
    }
    foreach ($key in $mantraLocalization.Keys) {
        Add-Entry $generated[$language].powers $key $mantraLocalization[$key]
    }

    $curiosityLocalization = if ($language -eq "eng") {
        [ordered]@{
            "TOGAWASAKIKO-CURIOSITY_POWER.description" = "Whenever you play a Power card, gain [blue]{Amount}[/blue] Strength."
            "TOGAWASAKIKO-CURIOSITY_POWER.smartDescription" = "Whenever you play a Power card, gain [blue]{Amount}[/blue] Strength."
            "TOGAWASAKIKO-CURIOSITY_POWER.title" = "Curiosity"
        }
    } else {
        [ordered]@{
            "TOGAWASAKIKO-CURIOSITY_POWER.description" = "每打出1张能力牌，你便获得[blue]{Amount}[/blue]点力量。"
            "TOGAWASAKIKO-CURIOSITY_POWER.smartDescription" = "每打出1张能力牌，你便获得[blue]{Amount}[/blue]点力量。"
            "TOGAWASAKIKO-CURIOSITY_POWER.title" = "好奇"
        }
    }
    foreach ($key in $curiosityLocalization.Keys) {
        Add-Entry $generated[$language].powers $key $curiosityLocalization[$key]
    }

    $fearlessPrompt = if ($language -eq "eng") {
        "Choose up to 1 card in your hand to Purge."
    } else {
        "选择至多1张手牌并移除。"
    }
    Add-Entry $generated[$language].powers "TOGAWASAKIKO-FEARLESS_POWER.selectionScreenPrompt" $fearlessPrompt

    $wishFulfilledPrompt = if ($language -eq "eng") {
        "Choose a card to Purge."
    } else {
        "选择1张卡牌并移除。"
    }
    if (-not $generated[$language].cards.Contains("TOGAWASAKIKO-WISH_FULFILLED_CARD.selectionScreenPrompt")) {
        Add-Entry $generated[$language].cards "TOGAWASAKIKO-WISH_FULFILLED_CARD.selectionScreenPrompt" $wishFulfilledPrompt
    }

    $rareSelectionPrompts = if ($language -eq "eng") {
        [ordered]@{
            "TOGAWASAKIKO-AS_YOUR_HEART_DESIRES_CARD.selectionScreenPrompt" = "Copy and add to your deck."
            "TOGAWASAKIKO-MASKS_CARD.selectionScreenPrompt" = "Choose a card to turn into a copy of Masks."
            "TOGAWASAKIKO-PERFECTION_CARD.selectionScreenPrompt" = "Choose a card to add to your hand; it costs 0 energy this turn."
            "TOGAWASAKIKO-SORA_NO_MUSICA_CARD.selectionScreenPrompt" = "Choose cards to move to the top of the Draw Pile."
        }
    } else {
        [ordered]@{
            "TOGAWASAKIKO-AS_YOUR_HEART_DESIRES_CARD.selectionScreenPrompt" = "复制并加入卡组。"
            "TOGAWASAKIKO-MASKS_CARD.selectionScreenPrompt" = "选择一张牌，将其变成假面的复制。"
            "TOGAWASAKIKO-PERFECTION_CARD.selectionScreenPrompt" = "选择一张牌加入手中，本回合消耗为0能量。"
            "TOGAWASAKIKO-SORA_NO_MUSICA_CARD.selectionScreenPrompt" = "选择卡牌并移动到抽牌堆顶部。"
        }
    }
    foreach ($key in $rareSelectionPrompts.Keys) {
        if (-not $generated[$language].cards.Contains($key)) {
            Add-Entry $generated[$language].cards $key $rareSelectionPrompts[$key]
        }
    }

    foreach ($relic in @($inventory.relics | Sort-Object sts2StableId)) {
        $id = [string]$relic.sts2StableId
        $titleKey = "$id.title"
        $descriptionKey = "$id.description"
        $flavorKey = "$id.flavor"
        if ($id -in $preservedLiveRelicIds) {
            foreach ($key in @($descriptionKey, $flavorKey, $titleKey)) {
                if (-not $existing[$language].relics.Contains($key)) {
                    throw "Live localization override is missing for $key ($language)."
                }
                Add-Entry $generated[$language].relics $key $existing[$language].relics[$key]
            }
            continue
        }

        $source = $relic.localization[$sourceLanguageProperty[$language]]
        $fragments = @($source.DESCRIPTIONS)
        $description = ConvertTo-NativeMarkup ([string]$fragments[0]) $language
        if ($relic.name -eq "BlazingHairband") {
            $description = ConvertTo-NativeMarkup (($fragments | ForEach-Object { [string]$_ }) -join "") $language
        }
        Add-Entry $generated[$language].relics $descriptionKey $description
        Add-Entry $generated[$language].relics $flavorKey (ConvertTo-NativeMarkup ([string]$source.FLAVOR) $language)
        Add-Entry $generated[$language].relics $titleKey (ConvertTo-NativeTitle ([string]$source.NAME) $language)
        if ($relic.name -eq "TheThirdMovement" -and $fragments.Count -gt 1) {
            Add-Entry $generated[$language].relics "$id.completedDescription" (ConvertTo-NativeMarkup ([string]$fragments[1]) $language)
        }
    }

    foreach ($potion in @($inventory.potions | Sort-Object sts2StableId)) {
        $id = [string]$potion.sts2StableId
        $source = $potion.localization[$sourceLanguageProperty[$language]]
        Add-Entry $generated[$language].potions "$id.description" (ConvertTo-NativeMarkup ([string]@($source.DESCRIPTIONS)[0]) $language)
        Add-Entry $generated[$language].potions "$id.title" (ConvertTo-NativeTitle ([string]$source.NAME) $language)
    }
}

$keywordSources = @{}
foreach ($language in $languages) {
    $keywordSources[$language] = @{}
    $sourceKeywords = @(Read-Json (Join-Path $sts1LocRoot "$language/Keywords.json"))
    foreach ($record in $sourceKeywords) {
        $semanticName = ([string]$record.ID) -replace '^\{modID\}:', ''
        if ($language -eq "zhs" -and $semanticName -eq "Remove") {
            $semanticName = "Purge"
        }
        $keywordSources[$language][$semanticName] = $record
    }
}

if (-not $keywordSources.eng.Contains("SymbolPlus")) {
    $symbol = $keywordSources.eng.Symbol
    $keywordSources.eng.SymbolPlus = [ordered]@{
        ID = "{modID}:Symbol+"
        PROPER_NAME = "Symbol+"
        NAMES = @("Symbol+")
        DESCRIPTION = "[gold]Symbol I: Fire+[/gold], [gold]Symbol II: Air+[/gold], [gold]Symbol III: Water+[/gold], [gold]Symbol IV: Earth+[/gold], and [gold]Ether+[/gold]."
    }
}
if ($keywordSources.zhs.Contains("Symbol+")) {
    $keywordSources.zhs.SymbolPlus = $keywordSources.zhs["Symbol+"]
    $keywordSources.zhs.Remove("Symbol+")
}

$allKeywordNames = @($keywordSources.eng.Keys + $keywordSources.zhs.Keys | Sort-Object -Unique)
foreach ($semanticName in $allKeywordNames) {
    foreach ($language in $languages) {
        if (-not $keywordSources[$language].Contains($semanticName)) {
            throw "No aligned keyword source for '$semanticName' ($language)."
        }
        $record = $keywordSources[$language][$semanticName]
        $entryName = ConvertTo-ModelEntry $semanticName
        $id = "TOGAWASAKIKO-$entryName"
        $title = if ($record.Contains("PROPER_NAME")) { [string]$record.PROPER_NAME } else { [string]@($record.NAMES)[0] }
        Add-Entry $generated[$language].card_keywords "$id.description" (ConvertTo-NativeMarkup ([string]$record.DESCRIPTION) $language)
        Add-Entry $generated[$language].card_keywords "$id.title" $title
    }
}

$legacyMarkerPattern = '!\$\{modID\}:|![DBM]!|\[E\]|\bNL\b|togawasakikomod:|#[bgryp][^\s]|\*[^\s]|%d'
$unconverted = [System.Collections.Generic.List[object]]::new()
foreach ($language in $languages) {
    foreach ($tableName in @("cards", "powers", "relics", "potions", "card_keywords")) {
        foreach ($key in $generated[$language][$tableName].Keys) {
            $value = [string]$generated[$language][$tableName][$key]
            if ($value -match $legacyMarkerPattern) {
                $unconverted.Add([ordered]@{ language = $language; table = $tableName; key = $key; value = $value })
            }
        }
    }
}
if ($unconverted.Count -gt 0) {
    $unconverted | ConvertTo-Json -Depth 6 | Write-Error
    throw "Generated localization contains $($unconverted.Count) unconverted STS1 marker(s)."
}

$languageKeyMismatches = [System.Collections.Generic.List[object]]::new()
foreach ($tableName in @("cards", "powers", "relics", "potions", "card_keywords")) {
    $engKeys = @($generated.eng[$tableName].Keys | Sort-Object)
    $zhsKeys = @($generated.zhs[$tableName].Keys | Sort-Object)
    $engOnly = @($engKeys | Where-Object { -not $generated.zhs[$tableName].Contains($_) })
    $zhsOnly = @($zhsKeys | Where-Object { -not $generated.eng[$tableName].Contains($_) })
    if ($engOnly.Count -gt 0 -or $zhsOnly.Count -gt 0) {
        $languageKeyMismatches.Add([ordered]@{ table = $tableName; englishOnly = $engOnly; simplifiedChineseOnly = $zhsOnly })
    }
}
if ($languageKeyMismatches.Count -gt 0) {
    throw "Generated English and Simplified Chinese localization keys do not match."
}

foreach ($language in $languages) {
    foreach ($tableName in @("cards", "powers", "relics", "potions", "card_keywords")) {
        Write-Json "TogawaSakiko/TogawaSakiko/localization/$language/$tableName.json" $generated[$language][$tableName]
    }
}

$sourceTokenDifferences = [System.Collections.Generic.List[object]]::new()
foreach ($card in @($inventory.cards | Sort-Object name)) {
    $en = $card.localization.sts1English
    $zh = $card.localization.sts1SimplifiedChinese
    foreach ($field in @("DESCRIPTION", "UPGRADE_DESCRIPTION")) {
        $enText = if ($en.Contains($field)) { [string]$en[$field] } else { $null }
        $zhText = if ($zh.Contains($field)) { [string]$zh[$field] } else { $null }
        $enTokens = @(Get-SourceTokens $enText)
        $zhTokens = @(Get-SourceTokens $zhText)
        if (@(Compare-Object $enTokens $zhTokens).Count -gt 0) {
            $sourceTokenDifferences.Add([ordered]@{
                card = $card.name
                field = $field
                englishTokens = $enTokens
                simplifiedChineseTokens = $zhTokens
            })
        }
    }
}

$sourceKeywordKeys = @{
    eng = @((Read-Json (Join-Path $sts1LocRoot "eng/Keywords.json")) | ForEach-Object { [string]$_.ID } | Sort-Object)
    zhs = @((Read-Json (Join-Path $sts1LocRoot "zhs/Keywords.json")) | ForEach-Object { [string]$_.ID } | Sort-Object)
}

$knownCorrections = @(
    [ordered]@{
        scope = "English card title"
        source = "Mas?uerade Rhapsody Re?uest"
        native = "Masquerade Rhapsody Request"
        reason = "The class, art filename, and related relic consistently establish the corrupted letters as q."
    },
    [ordered]@{
        scope = "English spelling"
        source = "repalce / begining / enemires / apperance"
        native = "replace / beginning / enemies / appearance"
        reason = "Provable spelling corrections; behavior and meaning are unchanged."
    },
    [ordered]@{
        scope = "Edge of Breakdown English behavior text"
        source = "Vulnerable"
        native = "Frail"
        reason = "The Java implementation applies FrailPower and the Simplified Chinese source says 脆弱, so the English source text is incorrect."
    },
    [ordered]@{
        scope = "Hachibousei Dance base-power terminology"
        source = "Plated Armor / 多层护甲"
        native = "Plating / 覆甲"
        reason = "STS2 renamed and revised the corresponding native base-game power; the port uses PlatingPower and its current localized title."
    },
    [ordered]@{
        scope = "Rhinoceros Beetle dynamic Block token"
        source = "MagicNumber"
        native = "CalculatedBlock"
        reason = "The displayed value is calculated from base Block plus floor(Dazzling / 2), while deliberately bypassing Dexterity."
    },
    [ordered]@{
        scope = "Keyword key alignment"
        source = "English Purge versus Simplified Chinese Remove"
        native = "TOGAWASAKIKO-PURGE"
        reason = "Both records describe the same permanent deck-removal mechanic."
    },
    [ordered]@{
        scope = "English upgraded Symbol keyword"
        source = "Missing"
        native = "Synthesized Symbol+ entry"
        reason = "Simplified Chinese and upgraded AveMujica behavior prove the upgraded keyword exists; English text is derived from the five upgraded Symbol card names."
    },
    [ordered]@{
        scope = "Abstract localization scaffolding"
        source = "RelicID and PotionID"
        native = "Excluded"
        reason = "The matching Java classes are abstract bases, not player-facing models."
    },
    [ordered]@{
        scope = "Custom-enemy compatibility powers"
        source = "PlayerFilightPower and StrengthUpPower"
        native = "Excluded"
        reason = "These models support excluded custom-enemy behavior and no in-scope player card depends on them."
    },
    [ordered]@{
        scope = "Custom world content"
        source = "Custom act, events, enemies, encounters, intents, and exclusive ending text"
        native = "Excluded"
        reason = "These content families are intentionally outside the STS2 port scope and are not emitted into canonical localization."
    }
)

$pendingSourceFamilyFiles = @(
    "CharacterStrings.json",
    "UIStrings.json",
    "OrbStrings.json",
    "CreditStrings.json"
)
$outOfScopePendingKeys = @{
    "CharacterStrings.json" = @('${modID}:AltNeowEvent', '${modID}:AltNeowReward')
    "UIStrings.json" = @('${modID}:MutsumiAttackIntent', '${modID}:TheOblivion')
}
$templateScaffoldKeys = @(
    '${modID}:OrbID',
    '${modID}:Example'
)
$pendingSourceFamilies = [System.Collections.Generic.List[object]]::new()
foreach ($fileName in $pendingSourceFamilyFiles) {
    $englishSource = Read-Json (Join-Path $sts1LocRoot "eng/$fileName")
    $simplifiedChineseSource = Read-Json (Join-Path $sts1LocRoot "zhs/$fileName")
    $englishKeys = @($englishSource.Keys | Sort-Object)
    $simplifiedChineseKeys = @($simplifiedChineseSource.Keys | Sort-Object)
    $excludedKeys = if ($outOfScopePendingKeys.ContainsKey($fileName)) { $outOfScopePendingKeys[$fileName] } else { @() }
    $englishKeys = @($englishKeys | Where-Object { $_ -notin $excludedKeys })
    $simplifiedChineseKeys = @($simplifiedChineseKeys | Where-Object { $_ -notin $excludedKeys })
    $englishOnly = @($englishKeys | Where-Object { -not $simplifiedChineseSource.Contains($_) })
    $simplifiedChineseOnly = @($simplifiedChineseKeys | Where-Object { -not $englishSource.Contains($_) })
    $scaffolds = @($englishKeys | Where-Object { $_ -in $templateScaffoldKeys })
    $pendingSourceFamilies.Add([ordered]@{
        file = $fileName
        englishRecordCount = $englishKeys.Count
        simplifiedChineseRecordCount = $simplifiedChineseKeys.Count
        englishOnlyKeys = $englishOnly
        simplifiedChineseOnlyKeys = $simplifiedChineseOnly
        excludedOutOfScopeKeys = $excludedKeys
        templateScaffoldKeys = $scaffolds
        behaviorLinkedRecordCount = $englishKeys.Count - $scaffolds.Count
        status = "Inventoried source only; emit in the phase that implements the owning behavior."
    })
}

$report = [ordered]@{
    schemaVersion = 1
    generatedFrom = [ordered]@{
        inventory = [System.IO.Path]::GetRelativePath($repoRoot, $inventoryFile).Replace('\', '/')
        sts1Localization = $sts1LocRoot
    }
    generatedCountsPerLanguage = [ordered]@{
        cards = $generated.eng.cards.Count
        powers = $generated.eng.powers.Count
        relics = $generated.eng.relics.Count
        potions = $generated.eng.potions.Count
        cardKeywords = $generated.eng.card_keywords.Count
    }
    sourceKeywordKeys = $sourceKeywordKeys
    sourceCardDynamicTokenDifferences = $sourceTokenDifferences
    knownCorrectionsAndPolicies = $knownCorrections
    pendingSourceFamilies = @($pendingSourceFamilies)
    unconvertedLegacyMarkers = @($unconverted)
    generatedLanguageKeyMismatches = @($languageKeyMismatches)
}
Write-Json $ReportJson $report

$reportMarkdownPath = Resolve-RepoPath $ReportMarkdown
$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine("# Localization Source Differences")
[void]$md.AppendLine()
[void]$md.AppendLine("> Generated by ``tools/Build-NativeLocalizationCatalog.ps1``. The STS1 English and Simplified Chinese files remain the wording source; this record captures only necessary native-syntax conversion, provable source repairs, and source-language asymmetries.")
[void]$md.AppendLine()
[void]$md.AppendLine("## Native catalog result")
[void]$md.AppendLine()
[void]$md.AppendLine("- Cards: $($generated.eng.cards.Count) keys per language for 95 models.")
[void]$md.AppendLine("- Powers: $($generated.eng.powers.Count) keys per language for 25 in-scope player-card-relevant STS1 models plus the native Mantra support model.")
[void]$md.AppendLine("- Relics: $($generated.eng.relics.Count) keys per language for 11 concrete models.")
[void]$md.AppendLine("- Potions: $($generated.eng.potions.Count) keys per language for 6 concrete models.")
[void]$md.AppendLine("- Custom keyword records: $($generated.eng.card_keywords.Count) keys per language.")
[void]$md.AppendLine("- Unconverted STS1 markers: $($unconverted.Count). English/zh-Hans key mismatches: $($languageKeyMismatches.Count).")
[void]$md.AppendLine()
[void]$md.AppendLine("## Corrections and explicit policies")
[void]$md.AppendLine()
[void]$md.AppendLine("| Scope | STS1 source | Native catalog | Reason |")
[void]$md.AppendLine("| --- | --- | --- | --- |")
foreach ($item in $knownCorrections) {
    $cells = @($item.scope, $item.source, $item.native, $item.reason) | ForEach-Object { ([string]$_).Replace('|', '\|') }
    [void]$md.AppendLine("| " + ($cells -join " | ") + " |")
}
[void]$md.AppendLine()
[void]$md.AppendLine("## Source-language dynamic-token differences")
[void]$md.AppendLine()
if ($sourceTokenDifferences.Count -eq 0) {
    [void]$md.AppendLine("None.")
} else {
    [void]$md.AppendLine("These are source facts, not automatically treated as errors. The eventual model implementation must follow Java behavior and expose the variables required by the selected native wording.")
    [void]$md.AppendLine()
    [void]$md.AppendLine("| Card | Field | English tokens | zh-Hans tokens |")
    [void]$md.AppendLine("| --- | --- | --- | --- |")
    foreach ($item in $sourceTokenDifferences) {
        [void]$md.AppendLine("| $($item.card) | $($item.field) | $(@($item.englishTokens) -join ', ') | $(@($item.simplifiedChineseTokens) -join ', ') |")
    }
}
[void]$md.AppendLine()
[void]$md.AppendLine("## Deferred source-family inventory")
[void]$md.AppendLine()
[void]$md.AppendLine("These in-scope source records are inventoried but deliberately not emitted into the live STS2 localization package before their owning behavior exists.")
[void]$md.AppendLine()
[void]$md.AppendLine("| STS1 file | English records | zh-Hans records | Behavior-linked | Template scaffolds | Key mismatch |")
[void]$md.AppendLine("| --- | ---: | ---: | ---: | --- | --- |")
foreach ($family in $pendingSourceFamilies) {
    $scaffoldText = if (@($family.templateScaffoldKeys).Count -eq 0) { "None" } else { @($family.templateScaffoldKeys) -join ", " }
    $mismatchCount = @($family.englishOnlyKeys).Count + @($family.simplifiedChineseOnlyKeys).Count
    [void]$md.AppendLine("| $($family.file) | $($family.englishRecordCount) | $($family.simplifiedChineseRecordCount) | $($family.behaviorLinkedRecordCount) | $scaffoldText | $mismatchCount |")
}
[void]$md.AppendLine()
[void]$md.AppendLine("The two template records are ``OrbID`` and ``Example``. They are source scaffolding, not missing concrete port models. The remaining records cover Sakiko, eleven behavior UI entries, and credits. Event/monster tables and alternate-Neow, custom-intent, and custom-act keys are excluded by scope.")
[void]$md.AppendLine()
[void]$md.AppendLine("## Native conversion rules")
[void]$md.AppendLine()
[void]$md.AppendLine("- ``!D!``, ``!B!``, and ``!M!`` become STS2 dynamic variables ``Damage``, ``Block``, and ``MagicNumber`` with upgrade-difference formatting.")
[void]$md.AppendLine("- The Raise the Bet custom Strength variable becomes ``Strength``. STS1 energy icons become the native energy-icon formatter.")
[void]$md.AppendLine("- STS1 ``NL``, color prefixes, custom keyword prefixes, and card-name stars become native newlines and BBCode.")
[void]$md.AppendLine("- Source upgrade descriptions are represented by the native ``IfUpgraded`` formatter. Existing Phase N2/N3 live entries retain their already-validated semantic variable names.")
[void]$md.AppendLine("- No model is enabled by this catalog generation; normal pools remain behavior-gated.")

[System.IO.Directory]::CreateDirectory((Split-Path $reportMarkdownPath -Parent)) | Out-Null
[System.IO.File]::WriteAllText($reportMarkdownPath, $md.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host "Native localization catalogs generated."
Write-Host "Cards: $($generated.eng.cards.Count) keys; powers: $($generated.eng.powers.Count); relics: $($generated.eng.relics.Count); potions: $($generated.eng.potions.Count); keywords: $($generated.eng.card_keywords.Count)."
Write-Host "Source dynamic-token differences: $($sourceTokenDifferences.Count)."
Write-Host "Unconverted markers: $($unconverted.Count); generated language key mismatches: $($languageKeyMismatches.Count)."
Write-Host "Report: $reportMarkdownPath"
