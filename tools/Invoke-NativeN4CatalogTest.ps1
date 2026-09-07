[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [ValidateSet('eng', 'zhs')]
    [string]$Language = 'eng',

    [string]$RunName = 'native-n4-catalog',

    [ValidateRange(5, 120)]
    [int]$TimeoutSeconds = 45
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$loaderScript = Join-Path $PSScriptRoot 'Invoke-NativeLoaderSmokeTest.ps1'
& $loaderScript `
    -GameRoot $GameRoot `
    -ArtifactRoot $ArtifactRoot `
    -RunName "$RunName-$Language" `
    -Language $Language `
    -TimeoutSeconds $TimeoutSeconds `
    -AdditionalArguments '--togawa-native-n4-catalog-smoke' `
    -RequiredMarker 'Phase N4 presentation catalog passed'
