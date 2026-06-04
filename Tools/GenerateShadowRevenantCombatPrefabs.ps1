# Creates Shadow Revenant VFX prefabs (from HurtEffect template + PooledOneShotVfx) and wires ShadowRevenantConfig.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root "Assets"
$prefabDir = Join-Path $assets "Prefabs\NPC\ShadowRevenant"
$configPath = Join-Path $assets "Data\NPC\ShadowRevenant\ShadowRevenantConfig.asset"
$sourceVfx = Join-Path $assets "Prefabs\ProjectEffects\HurtEffect.prefab"
$pooledVfxGuid = "777c4870b7c64ef58222e26bb76f87bf"
$shadeGuid = "5c846133f4ff4fe580352d620964ffd3"
$shadeComponentFileId = "9000007"

function New-Guid32 { return ([guid]::NewGuid().ToString("N")).ToLower() }

function Write-MetaPrefab($path, $guid) {
    $meta = @"
fileFormatVersion: 2
guid: $guid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
    Set-Content -Path ($path + ".meta") -Value $meta -NoNewline
}

function Add-PooledOneShotVfx($prefabPath, $componentFileId) {
    $lines = Get-Content $prefabPath
    $out = New-Object System.Collections.Generic.List[string]
    $injected = $false
    foreach ($line in $lines) {
        $out.Add($line)
        if (-not $injected -and $line -match '^\s+- component: \{fileID: 3400547379593215124\}') {
            $out.Add("  - component: {fileID: $componentFileId}")
            $injected = $true
        }
    }
    if (-not $injected) {
        throw "Could not inject PooledOneShotVfx into $prefabPath"
    }

    $out.Add("")
    $out.Add("--- !u!114 &$componentFileId")
    $out.Add("MonoBehaviour:")
    $out.Add("  m_ObjectHideFlags: 0")
    $out.Add("  m_CorrespondingSourceObject: {fileID: 0}")
    $out.Add("  m_PrefabInstance: {fileID: 0}")
    $out.Add("  m_PrefabAsset: {fileID: 0}")
    $out.Add("  m_GameObject: {fileID: 3400547379593215125}")
    $out.Add("  m_Enabled: 1")
    $out.Add("  m_EditorHideFlags: 0")
    $out.Add("  m_Script: {fileID: 11500000, guid: $pooledVfxGuid, type: 3}")
    $out.Add("  m_Name: ")
    $out.Add("  m_EditorClassIdentifier: ")

    Set-Content -Path $prefabPath -Value $out
}

$vfxEntries = @(
    @{ File = "ShadowRevenantHitVFX.prefab"; Guid = (New-Guid32); PoolId = "910001001" },
    @{ File = "ShadowRevenantDeathVFX.prefab"; Guid = (New-Guid32); PoolId = "910002001" },
    @{ File = "ShadowRevenantPhaseVFX.prefab"; Guid = (New-Guid32); PoolId = "910003001" },
    @{ File = "ShadowRevenantLightBreakVFX.prefab"; Guid = (New-Guid32); PoolId = "910004001" }
)

$refs = @{}
foreach ($entry in $vfxEntries) {
    $dest = Join-Path $prefabDir $entry.File
    Copy-Item -Path $sourceVfx -Destination $dest -Force
    $content = Get-Content $dest -Raw
    $objectName = [System.IO.Path]::GetFileNameWithoutExtension($entry.File)
    $content = $content -replace 'm_Name: HurtEffect', "m_Name: $objectName"
    Set-Content -Path $dest -Value $content -NoNewline
    Add-PooledOneShotVfx -prefabPath $dest -componentFileId $entry.PoolId
    Write-MetaPrefab -path $dest -guid $entry.Guid
    $refs[$objectName] = @{ Guid = $entry.Guid; RootId = "3400547379593215125" }
    Write-Host "Created $($entry.File) guid=$($entry.Guid)"
}

$config = Get-Content $configPath -Raw
$config = $config -replace 'shadeMinionPrefab: \{fileID: 9000005,', "shadeMinionPrefab: {fileID: $shadeComponentFileId,"
$config = $config -replace 'hitVfxPrefab: \{fileID: 0\}', "hitVfxPrefab: {fileID: $($refs.ShadowRevenantHitVFX.RootId), guid: $($refs.ShadowRevenantHitVFX.Guid), type: 3}"
$config = $config -replace 'deathVfxPrefab: \{fileID: 0\}', "deathVfxPrefab: {fileID: $($refs.ShadowRevenantDeathVFX.RootId), guid: $($refs.ShadowRevenantDeathVFX.Guid), type: 3}"
$config = $config -replace 'phaseVfxPrefab: \{fileID: 0\}', "phaseVfxPrefab: {fileID: $($refs.ShadowRevenantPhaseVFX.RootId), guid: $($refs.ShadowRevenantPhaseVFX.Guid), type: 3}"
$config = $config -replace 'lightBreakVfxPrefab: \{fileID: 0\}', "lightBreakVfxPrefab: {fileID: $($refs.ShadowRevenantLightBreakVFX.RootId), guid: $($refs.ShadowRevenantLightBreakVFX.Guid), type: 3}"
Set-Content -Path $configPath -Value $config -NoNewline

Write-Host "Updated ShadowRevenantConfig.asset (shade minion fileID $shadeComponentFileId, four VFX prefabs)."
