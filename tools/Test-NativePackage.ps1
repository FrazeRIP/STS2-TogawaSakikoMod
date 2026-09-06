param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$expectedNames = @(
    'TogawaSakiko.dll',
    'TogawaSakiko.json',
    'TogawaSakiko.pck'
)
$allowedNames = @(
    'TogawaSakiko.dll',
    'TogawaSakiko.json',
    'TogawaSakiko.pck',
    'TogawaSakiko.pdb'
)

$actualFiles = @(Get-ChildItem -LiteralPath $resolvedPackagePath -File)
$actualNames = @($actualFiles.Name)
$missingNames = @($expectedNames | Where-Object { $_ -notin $actualNames })
$unexpectedNames = @($actualNames | Where-Object { $_ -notin $allowedNames })

if ($missingNames.Count -gt 0) {
    throw "Package is missing required files: $($missingNames -join ', ')"
}

if ($unexpectedNames.Count -gt 0) {
    throw "Package contains unexpected files: $($unexpectedNames -join ', ')"
}

$manifestPath = Join-Path $resolvedPackagePath 'TogawaSakiko.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

if ($manifest.id -ne 'TogawaSakiko') {
    throw "Manifest id must be TogawaSakiko; found '$($manifest.id)'."
}

if ($manifest.has_dll -ne $true -or $manifest.has_pck -ne $true) {
    throw 'Manifest must declare both has_dll and has_pck.'
}

if (@($manifest.dependencies).Count -ne 0) {
    throw 'Native package manifest must not declare dependencies.'
}

$assemblyPath = Join-Path $resolvedPackagePath 'TogawaSakiko.dll'
$assembly = [System.Reflection.Assembly]::LoadFile($assemblyPath)
$assemblyReferences = @($assembly.GetReferencedAssemblies().Name)

if ('BaseLib' -in $assemblyReferences) {
    throw 'TogawaSakiko.dll still references BaseLib.'
}

$pckPath = Join-Path $resolvedPackagePath 'TogawaSakiko.pck'
$pckLength = (Get-Item -LiteralPath $pckPath).Length
if ($pckLength -le 0) {
    throw 'TogawaSakiko.pck is empty.'
}

[pscustomobject]@{
    PackagePath = $resolvedPackagePath
    ManifestId = $manifest.id
    ManifestVersion = $manifest.version
    MinimumGameVersion = $manifest.min_game_version
    Dependencies = @($manifest.dependencies).Count
    AssemblyReferencesBaseLib = ('BaseLib' -in $assemblyReferences)
    PckBytes = $pckLength
    Files = ($actualNames | Sort-Object) -join ', '
}
