[CmdletBinding()]
param(
    [string]$Sts1Root
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ([string]::IsNullOrWhiteSpace($Sts1Root)) {
    $Sts1Root = Join-Path (Split-Path $repoRoot -Parent) "STS1-TogawaSakikoMod"
}
$Sts1Root = [System.IO.Path]::GetFullPath($Sts1Root)

$sourceRoot = Join-Path $Sts1Root "src/main/resources/togawasakikomod"
$sourceJavaRoot = Join-Path $Sts1Root "src/main/java/togawasakikomod"
$destinationRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "TogawaSakiko/TogawaSakiko"))
$placeholderMaster = Join-Path $destinationRoot "images/placeholders/missing_content.png"
$outOfScopeImagePrefixes = @(
    "character/cutscene/",
    "character/ending/",
    "events/",
    "intents/",
    "monsters/",
    "ui/map/boss/",
    "ui/map/bossOutline/"
)
$outOfScopeAudioPrefixes = @("cutscene/", "music/")
$outOfScopePowerAssetStems = @(
    "earnestcrypower",
    "forwardresolvepower",
    "restlessidealpower",
    "silentwoundpower",
    "strengthuppower",
    "temporallongingpower",
    "unclaimedpromisepower",
    "unfadingyearningpower",
    "voicedgazepower"
)

foreach ($requiredPath in @($sourceRoot, $sourceJavaRoot, $destinationRoot, $placeholderMaster)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required presentation source was not found: $requiredPath"
    }
}

Add-Type -AssemblyName System.Drawing.Common

function Assert-DestinationPath([string]$Path) {
    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $destinationRoot.TrimEnd("\") + "\"
    if (-not $resolved.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside the Godot resource root: $resolved"
    }
    return $resolved
}

function Ensure-ParentDirectory([string]$Path) {
    [System.IO.Directory]::CreateDirectory((Split-Path $Path -Parent)) | Out-Null
}

function Copy-PresentationFile([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Presentation source is missing: $Source"
    }
    $safeDestination = Assert-DestinationPath $Destination
    Ensure-ParentDirectory $safeDestination
    Copy-Item -LiteralPath $Source -Destination $safeDestination -Force
}

function Remove-OutOfScopeDestination([string]$RelativePath) {
    $safeDestination = Assert-DestinationPath (Join-Path $destinationRoot $RelativePath)
    if (Test-Path -LiteralPath $safeDestination) {
        Remove-Item -LiteralPath $safeDestination -Recurse -Force
        return 1
    }
    return 0
}

function Get-PngDimensions([string]$Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24 -or
        $bytes[0] -ne 137 -or $bytes[1] -ne 80 -or $bytes[2] -ne 78 -or $bytes[3] -ne 71) {
        throw "Not a supported PNG: $Path"
    }
    return @(
        (([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]),
        (([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23])
    )
}

function Export-PngAtExactSize(
    [string]$Source,
    [string]$Destination,
    [int]$Width,
    [int]$Height,
    [bool]$PreserveAspect = $false
) {
    $safeDestination = Assert-DestinationPath $Destination
    Ensure-ParentDirectory $safeDestination

    $sourceImage = [System.Drawing.Image]::FromFile($Source)
    try {
        $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $bitmap.SetResolution(96, 96)
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

                if ($PreserveAspect) {
                    $scale = [Math]::Min(($Width * 0.90) / $sourceImage.Width, ($Height * 0.90) / $sourceImage.Height)
                    $drawWidth = [Math]::Max(1, [int][Math]::Round($sourceImage.Width * $scale))
                    $drawHeight = [Math]::Max(1, [int][Math]::Round($sourceImage.Height * $scale))
                    $x = [int][Math]::Floor(($Width - $drawWidth) / 2)
                    $y = [int][Math]::Floor(($Height - $drawHeight) / 2)
                    $destinationRectangle = [System.Drawing.Rectangle]::new($x, $y, $drawWidth, $drawHeight)
                } else {
                    $destinationRectangle = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
                }

                $graphics.DrawImage(
                    $sourceImage,
                    $destinationRectangle,
                    0,
                    0,
                    $sourceImage.Width,
                    $sourceImage.Height,
                    [System.Drawing.GraphicsUnit]::Pixel)
            } finally {
                $graphics.Dispose()
            }
            $bitmap.Save($safeDestination, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $bitmap.Dispose()
        }
    } finally {
        $sourceImage.Dispose()
    }
}

function Remove-JavaComments([string]$Text) {
    $withoutBlocks = [regex]::Replace($Text, "/\*.*?\*/", "", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    return [regex]::Replace($withoutBlocks, "(?m)//.*$", "")
}

function Get-Sts2CardTypeName([string]$Sts1Name) {
    switch ($Sts1Name) {
        "Strike" { return "StrikeTogawaSakiko" }
        "Defend" { return "DefendTogawaSakiko" }
        default { return "${Sts1Name}Card" }
    }
}

$removedOutOfScopePaths = 0
foreach ($relativePath in @(
    "audio/cutscene",
    "audio/music",
    "images/character/cutscene",
    "images/character/ending",
    "images/events",
    "images/intents",
    "images/monsters",
    "images/ui/map/boss",
    "images/ui/map/bossOutline"
)) {
    $removedOutOfScopePaths += Remove-OutOfScopeDestination $relativePath
}
foreach ($assetStem in $outOfScopePowerAssetStems) {
    foreach ($relativePath in @(
        "images/powers/$assetStem.png",
        "images/powers/$assetStem.png.import",
        "images/powers/big/$assetStem.png",
        "images/powers/big/$assetStem.png.import"
    )) {
        $removedOutOfScopePaths += Remove-OutOfScopeDestination $relativePath
    }
}

$cardSourceRoot = Join-Path $sourceJavaRoot "cards"
$cardFiles = Get-ChildItem -LiteralPath $cardSourceRoot -Recurse -File -Filter "*.java" |
    Where-Object { $_.BaseName -notin @("BaseCard", "CustomTags") } |
    Sort-Object BaseName

$originalCardPairs = 0
$placeholderCardPairs = 0
$resizedOriginals = 0

foreach ($cardFile in $cardFiles) {
    $name = $cardFile.BaseName
    $active = Remove-JavaComments (Get-Content -Raw -Encoding UTF8 -LiteralPath $cardFile.FullName)
    $typeMatch = [regex]::Match($active, "new\s+CardStats\s*\(\s*[^,]+,\s*CardType\.([A-Z_]+)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $typeMatch.Success) {
        throw "Could not parse card type in $($cardFile.FullName)."
    }

    $sourceFolder = $typeMatch.Groups[1].Value.ToLowerInvariant()
    $sourceSmall = Join-Path $sourceRoot "images/cards/$sourceFolder/$name.png"
    $sourceLarge = Join-Path $sourceRoot "images/cards/$sourceFolder/${name}_p.png"
    $assetName = (Get-Sts2CardTypeName $name).ToLowerInvariant() + ".png"
    $destinationSmall = Join-Path $destinationRoot "images/card_portraits/$assetName"
    $destinationLarge = Join-Path $destinationRoot "images/card_portraits/big/$assetName"

    if ((Test-Path -LiteralPath $sourceSmall) -and (Test-Path -LiteralPath $sourceLarge)) {
        $smallDimensions = Get-PngDimensions $sourceSmall
        if ($smallDimensions[0] -ne 250 -or $smallDimensions[1] -ne 190) {
            throw "STS1 small portrait has an unexpected size: $sourceSmall ($($smallDimensions -join 'x'))."
        }
        Copy-PresentationFile $sourceSmall $destinationSmall

        $largeDimensions = Get-PngDimensions $sourceLarge
        if ($largeDimensions[0] -eq 500 -and $largeDimensions[1] -eq 380) {
            Copy-PresentationFile $sourceLarge $destinationLarge
        } else {
            Export-PngAtExactSize $sourceLarge $destinationLarge 500 380 $false
            $resizedOriginals++
        }
        $originalCardPairs++
    } else {
        Export-PngAtExactSize $placeholderMaster $destinationSmall 250 190 $true
        Export-PngAtExactSize $placeholderMaster $destinationLarge 500 380 $true
        $placeholderCardPairs++
    }
}

if ($originalCardPairs -ne 94 -or $placeholderCardPairs -ne 1) {
    throw "Unexpected card-art source result: original=$originalCardPairs, placeholder=$placeholderCardPairs."
}

$otherImageSourceRoot = Join-Path $sourceRoot "images"
$otherImageCount = 0
foreach ($sourceFile in Get-ChildItem -LiteralPath $otherImageSourceRoot -Recurse -File -Filter "*.png" | Sort-Object FullName) {
    $relative = [System.IO.Path]::GetRelativePath($otherImageSourceRoot, $sourceFile.FullName).Replace("\", "/")
    if ($relative.StartsWith("cards/") -or $relative.StartsWith("powers/") -or $relative.StartsWith("relics/")) {
        continue
    }
    if (@($outOfScopeImagePrefixes | Where-Object { $relative.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
        continue
    }

    $destinationRelative = $relative
    if ($relative.StartsWith("potions/", [System.StringComparison]::OrdinalIgnoreCase)) {
        $parts = $relative.Split("/")
        $destinationRelative = "potions/$($parts[1].ToLowerInvariant())/$($parts[2].ToLowerInvariant())"
    }
    $destination = Join-Path (Join-Path $destinationRoot "images") $destinationRelative
    Copy-PresentationFile $sourceFile.FullName $destination
    $otherImageCount++
}

$audioSourceRoot = Join-Path $sourceRoot "audio"
$audioCount = 0
foreach ($sourceFile in Get-ChildItem -LiteralPath $audioSourceRoot -Recurse -File | Sort-Object FullName) {
    $relative = [System.IO.Path]::GetRelativePath($audioSourceRoot, $sourceFile.FullName).Replace("\", "/")
    if (@($outOfScopeAudioPrefixes | Where-Object { $relative.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
        continue
    }
    $parts = $relative.Split("/")
    if ($parts[0].Equals("sakiko", [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = "sakiko/$($parts[1].ToLowerInvariant())"
    }
    $destination = Join-Path (Join-Path $destinationRoot "audio") $relative
    Copy-PresentationFile $sourceFile.FullName $destination
    $audioCount++
}

$weaknessSmall = Join-Path $destinationRoot "images/card_portraits/weaknesscard.png"
$weaknessLarge = Join-Path $destinationRoot "images/card_portraits/big/weaknesscard.png"
foreach ($check in @(
    @($weaknessSmall, 250, 190),
    @($weaknessLarge, 500, 380)
)) {
    $dimensions = Get-PngDimensions $check[0]
    if ($dimensions[0] -ne $check[1] -or $dimensions[1] -ne $check[2]) {
        throw "Generated placeholder has the wrong size: $($check[0]) ($($dimensions -join 'x'))."
    }
}

Write-Host "STS1 presentation assets synchronized."
Write-Host "Card pairs: $originalCardPairs original, $placeholderCardPairs generated placeholder."
Write-Host "Original large portraits normalized to 500x380: $resizedOriginals."
Write-Host "Other source PNGs copied: $otherImageCount."
Write-Host "Audio files copied: $audioCount."
Write-Host "Out-of-scope act/event/enemy paths removed: $removedOutOfScopePaths."
Write-Host "Weakness placeholders: 250x190 and 500x380, using final destination filenames."
