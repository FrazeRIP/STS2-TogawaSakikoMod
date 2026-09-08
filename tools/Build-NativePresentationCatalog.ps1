[CmdletBinding()]
param(
    [string]$InventoryPath = "docs/FULL_PORT_PARITY_INVENTORY.json",
    [string]$OutputCatalog = "TogawaSakiko/TogawaSakiko/diagnostics/n4_presentation_catalog.json",
    [string]$ExportPreset = "TogawaSakiko/export_presets.cfg"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$nativeRoot = Join-Path $repoRoot "TogawaSakiko/TogawaSakiko"

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Read-Json([string]$Path) {
    return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json -AsHashtable
}

function Get-PngDimensions([string]$Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24 -or
        $bytes[0] -ne 137 -or $bytes[1] -ne 80 -or $bytes[2] -ne 78 -or $bytes[3] -ne 71) {
        throw "Not a PNG: $Path"
    }
    $width = (([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19])
    $height = (([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23])
    return [ordered]@{ width = $width; height = $height }
}

function ConvertTo-ResourcePath([string]$RepoRelativePath) {
    $normalized = $RepoRelativePath.Replace('\', '/')
    $prefix = "TogawaSakiko/TogawaSakiko/"
    if (-not $normalized.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Presentation path is outside the native resource root: $RepoRelativePath"
    }
    return "res://TogawaSakiko/" + $normalized.Substring($prefix.Length)
}

function ConvertTo-RepoRelativePath([string]$ResourcePath) {
    $prefix = "res://TogawaSakiko/"
    if (-not $ResourcePath.StartsWith($prefix, [StringComparison]::Ordinal)) {
        throw "Resource path is outside the Togawa namespace: $ResourcePath"
    }
    return "TogawaSakiko/TogawaSakiko/" + $ResourcePath.Substring($prefix.Length)
}

$inventoryFile = Resolve-RepoPath $InventoryPath
$catalogFile = Resolve-RepoPath $OutputCatalog
$exportPresetFile = Resolve-RepoPath $ExportPreset
$inventory = Read-Json $inventoryFile
$richTextEnergyIconResourcePath = "res://images/packed/sprite_fonts/togawa_sakiko_energy_icon.png"
$richTextEnergyIconFile = Resolve-RepoPath "TogawaSakiko/images/packed/sprite_fonts/togawa_sakiko_energy_icon.png"
if (-not (Test-Path -LiteralPath $richTextEnergyIconFile -PathType Leaf)) {
    throw "Rich-text energy compatibility icon is missing: $richTextEnergyIconFile"
}
$richTextEnergyIconDimensions = Get-PngDimensions $richTextEnergyIconFile
if ($richTextEnergyIconDimensions.width -ne 24 -or $richTextEnergyIconDimensions.height -ne 24) {
    throw "Rich-text energy compatibility icon must be 24x24."
}

$texturesByPath = [ordered]@{}
$audioByPath = [ordered]@{}
$resourcesByPath = [ordered]@{}

function Add-Texture(
    [string]$OwnerType,
    [string]$Owner,
    [string]$Slot,
    [string]$RepoRelativePath,
    [AllowNull()][string]$SourceStatus = $null
) {
    $absolute = Resolve-RepoPath $RepoRelativePath
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        throw "Presentation texture is missing: $RepoRelativePath"
    }
    $resourcePath = ConvertTo-ResourcePath $RepoRelativePath
    $dimensions = Get-PngDimensions $absolute
    if (-not $texturesByPath.Contains($resourcePath)) {
        $texturesByPath[$resourcePath] = [ordered]@{
            ownerType = $OwnerType
            owner = $Owner
            slot = $Slot
            path = $resourcePath
            width = $dimensions.width
            height = $dimensions.height
            sourceStatus = $SourceStatus
        }
    }
}

function Add-Audio([string]$Usage, [string]$RepoRelativePath) {
    $absolute = Resolve-RepoPath $RepoRelativePath
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        throw "Presentation audio is missing: $RepoRelativePath"
    }
    $resourcePath = ConvertTo-ResourcePath $RepoRelativePath
    if (-not $audioByPath.Contains($resourcePath)) {
        $audioByPath[$resourcePath] = [ordered]@{
            usage = $Usage
            path = $resourcePath
        }
    }
}

function Add-Resource([string]$Kind, [string]$ResourcePath) {
    $repoRelativePath = ConvertTo-RepoRelativePath $ResourcePath
    $absolute = Resolve-RepoPath $repoRelativePath
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        throw "Presentation resource is missing: $repoRelativePath"
    }
    if (-not $resourcesByPath.Contains($ResourcePath)) {
        $resourcesByPath[$ResourcePath] = [ordered]@{ kind = $Kind; path = $ResourcePath }
    }
}

foreach ($card in @($inventory.cards | Sort-Object sts2StableId)) {
    Add-Texture "card" ([string]$card.sts2StableId) "small" ([string]$card.art.sts2Small) ([string]$card.art.status)
    Add-Texture "card" ([string]$card.sts2StableId) "large" ([string]$card.art.sts2Large) ([string]$card.art.status)
}

foreach ($power in @($inventory.powers | Where-Object { -not $_.inheritsBasePresentation } | Sort-Object sts2StableId)) {
    Add-Texture "power" ([string]$power.sts2StableId) "small" ([string]$power.art.sts2Small) ([string]$power.art.status)
    Add-Texture "power" ([string]$power.sts2StableId) "large" ([string]$power.art.sts2Large) ([string]$power.art.status)
}

foreach ($relic in @($inventory.relics | Sort-Object sts2StableId)) {
    Add-Texture "relic" ([string]$relic.sts2StableId) "icon" ([string]$relic.art.icon) ([string]$relic.art.status)
    Add-Texture "relic" ([string]$relic.sts2StableId) "outline" ([string]$relic.art.outline) ([string]$relic.art.status)
    Add-Texture "relic" ([string]$relic.sts2StableId) "large" ([string]$relic.art.big) ([string]$relic.art.status)
}
Add-Texture "relic" "TOGAWASAKIKO-ANOTHER_MASK" "icon" "TogawaSakiko/TogawaSakiko/images/relics/anothermask.png" "Generated from user-provided white-mask reference"
Add-Texture "relic" "TOGAWASAKIKO-ANOTHER_MASK" "outline" "TogawaSakiko/TogawaSakiko/images/relics/anothermask_outline.png" "Registered alpha silhouette"
Add-Texture "relic" "TOGAWASAKIKO-ANOTHER_MASK" "large" "TogawaSakiko/TogawaSakiko/images/relics/big/anothermask.png" "Generated from user-provided white-mask reference"

foreach ($potion in @($inventory.potions | Sort-Object sts2StableId)) {
    foreach ($layer in @($potion.art.layers | Sort-Object name)) {
        Add-Texture "potion" ([string]$potion.sts2StableId) ([string]$layer.name) ([string]$layer.destination) ([string]$potion.art.status)
    }
}

foreach ($item in @($inventory.presentationFiles | Sort-Object nativePath)) {
    $repoRelativePath = "TogawaSakiko/TogawaSakiko/" + ([string]$item.nativePath).Replace('\', '/')
    Add-Texture "presentation" ([string]$item.relativePath) "source" $repoRelativePath "Original"
}

Add-Texture "character-ui" "TogawaSakiko" "map-marker" "TogawaSakiko/TogawaSakiko/images/charui/map_marker_char_name.png" "Native layout asset"
Add-Texture "character-ui" "TogawaSakiko" "rest-site" "TogawaSakiko/TogawaSakiko/images/character/presentation/rest_site.png" "Generated full figure with elevated campfire perspective"
Add-Texture "character-ui" "TogawaSakiko" "merchant" "TogawaSakiko/TogawaSakiko/images/character/presentation/merchant.png" "Generated standing merchant-room figure"
Add-Texture "character-ui" "TogawaSakiko" "another-mask" "TogawaSakiko/TogawaSakiko/images/character/image_another_mask.png" "Generated masked combat portrait variation"
Add-Texture "character-ui" "TogawaSakiko" "top-panel-icon" "TogawaSakiko/TogawaSakiko/images/ui/top_panel/character_icon_togawa_sakiko.png" "Exact native dimensions"
Add-Texture "character-ui" "TogawaSakiko" "top-panel-outline" "TogawaSakiko/TogawaSakiko/images/ui/top_panel/character_icon_togawa_sakiko_outline.png" "Exact native dimensions"
Add-Texture "character-ui" "TogawaSakiko" "character-select-icon" "TogawaSakiko/TogawaSakiko/images/packed/character_select/char_select_togawa_sakiko.png" "Exact native dimensions"
Add-Texture "character-ui" "TogawaSakiko" "character-select-locked" "TogawaSakiko/TogawaSakiko/images/packed/character_select/char_select_togawa_sakiko_locked.png" "Exact native dimensions"
Add-Texture "character-ui" "TogawaSakiko" "card-energy-icon" "TogawaSakiko/TogawaSakiko/images/card_ui/energy_togawa_sakiko.png" "Exact native dimensions"
foreach ($cardType in @("attack", "skill", "power")) {
    Add-Texture "card-frame" "TogawaSakiko" $cardType "TogawaSakiko/TogawaSakiko/images/card_ui/frame_$cardType.png" "Original STS1 piano art adapted to native portrait opening"
}
Add-Texture "character-ui" "TogawaSakiko" "transition" "TogawaSakiko/TogawaSakiko/images/ui/transitions/togawa_sakiko_transition.png" "Exact native dimensions"
Add-Texture "starting-room" "OceanOfMemories" "background" "TogawaSakiko/TogawaSakiko/images/events/ocean_of_memories.png" "Generated for the user-approved Sakiko opening room"
Add-Texture "character-ui" "TogawaSakiko" "multiplayer-point" "TogawaSakiko/TogawaSakiko/images/ui/hands/multiplayer_hand_togawa_sakiko_point.png" "Generated compatibility placeholder"
Add-Texture "character-ui" "TogawaSakiko" "multiplayer-rock" "TogawaSakiko/TogawaSakiko/images/ui/hands/multiplayer_hand_togawa_sakiko_rock.png" "Generated compatibility placeholder"
Add-Texture "character-ui" "TogawaSakiko" "multiplayer-paper" "TogawaSakiko/TogawaSakiko/images/ui/hands/multiplayer_hand_togawa_sakiko_paper.png" "Generated compatibility placeholder"
Add-Texture "character-ui" "TogawaSakiko" "multiplayer-scissors" "TogawaSakiko/TogawaSakiko/images/ui/hands/multiplayer_hand_togawa_sakiko_scissors.png" "Generated compatibility placeholder"
Add-Texture "diagnostic" "missing-content" "master" "TogawaSakiko/TogawaSakiko/images/placeholders/missing_content.png" "Generated placeholder"
Add-Texture "mod" "TogawaSakiko" "manifest" "TogawaSakiko/TogawaSakiko/mod_image.png" "Original"

foreach ($item in @($inventory.audio | Sort-Object relativePath)) {
    Add-Audio ([string]$item.usage) ([string]$item.sts2Path)
}

Add-Resource "bootstrap" "res://TogawaSakiko/bootstrap/native_bootstrap_probe.tres"
Add-Resource "scene" "res://TogawaSakiko/scenes/screens/char_select/togawa_sakiko_background.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/ui/togawa_sakiko_icon.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/creature_visuals/togawa_sakiko.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/combat/energy_counters/togawa_sakiko_energy_counter.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/rest_site/characters/togawa_sakiko_rest_site.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/merchant/characters/togawa_sakiko_merchant.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/vfx/card_trail_togawa_sakiko.tscn"
Add-Resource "scene" "res://TogawaSakiko/scenes/events/ocean_of_memories.tscn"
Add-Resource "material" "res://TogawaSakiko/materials/transitions/togawa_sakiko_transition_mat.tres"
Add-Resource "material" "res://TogawaSakiko/materials/cards/frames/card_frame_togawa_sakiko_mat.tres"

$localizationResources = @(
    Get-ChildItem -LiteralPath (Join-Path $nativeRoot "localization") -Recurse -File -Filter "*.json" |
        Sort-Object FullName |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($nativeRoot, $_.FullName).Replace('\', '/')
            "res://TogawaSakiko/$relative"
        }
)

$catalog = [ordered]@{
    schemaVersion = 1
    baseline = "Slay the Spire 2 v0.111.0 (41cef1ea)"
    textureCount = $texturesByPath.Count
    audioCount = $audioByPath.Count
    resourceCount = $resourcesByPath.Count
    localizationFileCount = $localizationResources.Count
    textures = @($texturesByPath.Values)
    audio = @($audioByPath.Values)
    resources = @($resourcesByPath.Values)
    localizationFiles = $localizationResources
    compatibilityResources = @(
        [ordered]@{
            kind = "rich-text-energy-icon"
            path = $richTextEnergyIconResourcePath
            width = 24
            height = 24
            source = "res://TogawaSakiko/images/charui/text_energy.png"
        }
    )
}

[System.IO.Directory]::CreateDirectory((Split-Path $catalogFile -Parent)) | Out-Null
$catalogJson = $catalog | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($catalogFile, $catalogJson + "`n", [System.Text.UTF8Encoding]::new($false))

$catalogResourcePath = ConvertTo-ResourcePath ([System.IO.Path]::GetRelativePath($repoRoot, $catalogFile))
$exportPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($path in @($texturesByPath.Keys + $audioByPath.Keys + $resourcesByPath.Keys + $localizationResources + $catalogResourcePath)) {
    [void]$exportPaths.Add([string]$path)
}
[void]$exportPaths.Add("res://TogawaSakiko/diagnostics/n4_character_presentation_manifest.json")
[void]$exportPaths.Add($richTextEnergyIconResourcePath)
$sortedExportPaths = @($exportPaths | Sort-Object)
$quotedPaths = @($sortedExportPaths | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' })
$replacementLine = "export_files=PackedStringArray(" + ($quotedPaths -join ", ") + ")"

$presetLines = [System.IO.File]::ReadAllLines($exportPresetFile)
$matchingIndexes = @(for ($index = 0; $index -lt $presetLines.Length; $index++) {
    if ($presetLines[$index].StartsWith("export_files=PackedStringArray(", [StringComparison]::Ordinal)) {
        $index
    }
})
if ($matchingIndexes.Count -ne 1) {
    throw "Expected exactly one export_files line in $exportPresetFile, found $($matchingIndexes.Count)."
}
$presetLines[$matchingIndexes[0]] = $replacementLine
[System.IO.File]::WriteAllLines($exportPresetFile, $presetLines, [System.Text.UTF8Encoding]::new($false))

if (@($inventory.cards).Count -ne 95) { throw "Expected 95 card records." }
if (@($inventory.powers | Where-Object { -not $_.inheritsBasePresentation }).Count -ne 25) { throw "Expected 25 in-scope player-card-relevant power records." }
if (@($inventory.relics).Count -ne 11) { throw "Expected 11 relic records." }
if (@($inventory.potions).Count -ne 6) { throw "Expected 6 potion records." }
if ($audioByPath.Count -ne 52) { throw "Expected 52 in-scope audio resources, found $($audioByPath.Count)." }

Write-Host "Native presentation catalog generated."
Write-Host "Textures: $($texturesByPath.Count); audio: $($audioByPath.Count); resources: $($resourcesByPath.Count); localization files: $($localizationResources.Count)."
Write-Host "Explicit PCK export entries: $($sortedExportPaths.Count)."
Write-Host "Catalog: $catalogFile"
Write-Host "Export preset: $exportPresetFile"
