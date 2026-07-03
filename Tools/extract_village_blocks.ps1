# Ports village environment blocks from the feature branch scene into the current scene.
# - Appends missing PrefabInstances whose source prefab GUID is in the env whitelist
#   (plus their stripped transforms/GameObjects, attached components, and child instances).
# - Replaces shared-but-modified env instances with the branch version.
# - Applies branch RenderSettings fog/skybox and LightingSettings reference.
# One-shot migration helper for feature/settlement-layout-only (village parity pass).
param(
    [string]$BranchScene = "$env:TEMP\branch_scene.unity",
    [string]$TargetScene = "Assets\Scenes\Level 1 - Remastered - Steam.unity",
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$envGuids = @(
    '02a4a6df6ce5d82468957911b0be5933', # wall-medieval-wood
    '1adbdd5ea9a76d247a3f262569b3e059', # SM_Env_Rock_Cliff_03
    'b6aaf38f0afb8a54092d2a20210a2b58', # Bg Working
    '62afc438d988a6c48aca0dba28ba4cea', # Bg Back Squat
    '86ab4554563daf5429bc7abd10263b90', # Bg Sleeping
    'e83bae9063810944a9da23a91643f6ba', # Bg SadIdle
    '5f42a379456d8f948bb52392d16c505f', # Bg Pray
    '557012e4ad677074aaf3d319f0934a0a', # BG_Chatting2_Guard
    'ac84c0fbd0e66654d8169eb03696e4a4', # Beaver Millitary
    '6b40117f44be6114eaa8219c883f7578', # SM_Prop_Gate_Small_01
    'd836b24472f564f629d8cae908c35681', # dryer-outside
    '91ee05bce3671ee44a7037a0ffa5ebdb', # Hammer decor
    'a66031aa94a7b4d5b91f09a4f0a41e3f', # fence-classic
    '8826618c95a0bd442b046c7854cae1d1', # shield-medieval
    '0bd3cd68ddcbf744ea73b61ba2b530e8', # water-tower-western
    '21e07dfeb4eb0d94fbc8af00d646feef', # DoubleBurlapBags
    'f0290686e4170fc4b8421f6a07049fad', # SM_Bld_Windmill_01
    '802b1f55c39ca7e4ea648bc9808989bb', # CrateA
    'faa232351140d09468c55b9d5a0ec242', # SM_Bld_Stone_Cabin_01
    'e0a4cf137d4f89347ba5bd2d670cee35', # RegularBucket
    '98e9db8b534d7a54a9a80e97f818e4dc', # DeadTreeA
    'cf4270f67cf9f624fae518b037c0e0bc', # DeadTreeB
    '038c019db003bfd40ac5f59bf358472b', # SM_Prop_HandCart_01
    '58a5427cb8bfc0c40b7f4d6acc630e9c', # SM_Prop_Camp_Crate_01
    'c7144c40c33f3ff4a80577ffd7629ac1', # SM_Prop_Camp_Fire_Tripod_01
    'eaba0d5d851714d4ebdefa3ab9809493', # SM_Prop_Camp_Tent_01
    '8315b183e5a8d85499990e6c5df0ed1f'  # child prop of Bg character
)
$envSet = New-Object System.Collections.Generic.HashSet[string]
$envGuids | ForEach-Object { [void]$envSet.Add($_) }

function Get-Blocks([string[]]$lines) {
    $order = New-Object System.Collections.Generic.List[string]
    $blocks = @{}
    $starts = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!(\d+) &(\d+)( stripped)?$') {
            $starts += ,@($i, [int]$Matches[1], $Matches[2], [bool]$Matches[3])
        }
    }
    for ($j = 0; $j -lt $starts.Count; $j++) {
        $s = $starts[$j]
        $end = if ($j + 1 -lt $starts.Count) { $starts[$j + 1][0] - 1 } else { $lines.Count - 1 }
        $blocks[$s[2]] = @{ Type = $s[1]; Stripped = $s[3]; Text = $lines[$s[0]..$end]; Order = $j }
        $order.Add($s[2])
    }
    return @{ Blocks = $blocks; Order = $order }
}

function Get-SourceGuid($block) {
    $m = ($block.Text | Select-String -Pattern 'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})')
    if ($m) { return $m.Matches[0].Groups[1].Value }
    return $null
}

$branchLines = Get-Content $BranchScene
$bp = Get-Blocks $branchLines
$bb = $bp.Blocks
$currentText = [System.IO.File]::ReadAllText((Resolve-Path $TargetScene))
$currentLines = $currentText -split "\r?\n"
$cp = Get-Blocks $currentLines
$cb = $cp.Blocks

Write-Host "branch blocks: $($bb.Count), current blocks: $($cb.Count)"

# --- Phase 1: seed missing env PrefabInstances ---
$selected = New-Object System.Collections.Generic.HashSet[string]
foreach ($id in $bb.Keys) {
    if ($cb.ContainsKey($id)) { continue }
    $b = $bb[$id]
    if ($b.Type -ne 1001) { continue }
    $g = Get-SourceGuid $b
    if ($g -and $envSet.Contains($g)) { [void]$selected.Add($id) }
}
Write-Host "seed env instances: $($selected.Count)"

# --- Phase 2: fixpoint over stripped anchors, components, child instances ---
$anchorTransforms = New-Object System.Collections.Generic.HashSet[string]
$strippedGos = New-Object System.Collections.Generic.HashSet[string]
do {
    $changed = $false
    foreach ($id in $bb.Keys) {
        if ($selected.Contains($id) -or $cb.ContainsKey($id)) { continue }
        $b = $bb[$id]
        if ($b.Stripped) {
            $pi = ($b.Text | Select-String -Pattern 'm_PrefabInstance: \{fileID: (\d+)\}')
            if ($pi -and $selected.Contains($pi.Matches[0].Groups[1].Value)) {
                [void]$selected.Add($id)
                if ($b.Type -eq 4) { [void]$anchorTransforms.Add($id) }
                if ($b.Type -eq 1) { [void]$strippedGos.Add($id) }
                $changed = $true
            }
        }
        elseif ($b.Type -eq 1001) {
            $tp = ($b.Text | Select-String -Pattern 'm_TransformParent: \{fileID: (\d+)\}')
            if ($tp -and $anchorTransforms.Contains($tp.Matches[0].Groups[1].Value)) {
                $g = Get-SourceGuid $b
                if ($g -and $envSet.Contains($g)) { [void]$selected.Add($id); $changed = $true }
            }
        }
        else {
            # component attached to a selected stripped GameObject
            $go = ($b.Text | Select-String -Pattern 'm_GameObject: \{fileID: (\d+)\}')
            if ($go -and $strippedGos.Contains($go.Matches[0].Groups[1].Value)) {
                [void]$selected.Add($id); $changed = $true
            }
        }
    }
} while ($changed)
Write-Host "total selected for append: $($selected.Count)"

# --- Phase 3: shared-but-modified env instances to replace ---
$replace = New-Object System.Collections.Generic.List[string]
foreach ($id in $bb.Keys) {
    if (-not $cb.ContainsKey($id)) { continue }
    $b = $bb[$id]
    if ($b.Type -ne 1001) { continue }
    $g = Get-SourceGuid $b
    if (-not ($g -and $envSet.Contains($g))) { continue }
    $bt = ($b.Text -join "`n")
    $ct = ($cb[$id].Text -join "`n")
    if ($bt -ne $ct) { $replace.Add($id) }
}
Write-Host "shared env instances to replace: $($replace.Count)"

# --- Report GUID usage of appended blocks ---
$usedGuids = New-Object System.Collections.Generic.HashSet[string]
foreach ($id in $selected) {
    ($bb[$id].Text | Select-String -Pattern 'guid: ([a-f0-9]{32})' -AllMatches) | ForEach-Object {
        $_.Matches | ForEach-Object { [void]$usedGuids.Add($_.Groups[1].Value) }
    }
}
Write-Host "--- GUIDs referenced by appended blocks ---"
$usedGuids | Sort-Object | ForEach-Object { Write-Host $_ }

if ($DryRun) {
    Write-Host "--- DRY RUN: appended block headers ---"
    foreach ($id in ($selected | Sort-Object { $bb[$_].Order })) {
        Write-Host $bb[$id].Text[0]
    }
    Write-Host "--- DRY RUN: replaced block ids ---"
    $replace | ForEach-Object { Write-Host $_ }
    exit 0
}

$nl = if ($currentText.Contains("`r`n")) { "`r`n" } else { "`n" }

# --- Apply replacements (in-memory text swap per block) ---
foreach ($id in $replace) {
    $old = ($cb[$id].Text -join $nl)
    $new = ($bb[$id].Text -join $nl)
    $oldLf = ($cb[$id].Text -join "`n")
    # normalize: current text on disk may use CRLF; match via joined text using detected newline
    if ($currentText.Contains($old)) {
        $currentText = $currentText.Replace($old, $new)
    } elseif ($currentText.Contains($oldLf)) {
        $currentText = $currentText.Replace($oldLf, ($bb[$id].Text -join "`n"))
    } else {
        throw "Replacement source text not found for block $id"
    }
}

# --- Atmosphere: RenderSettings + LightmapSettings field swaps ---
$atmos = @(
    @('m_FogColor: {r: 0.7924528, g: 0.7551769, b: 0.69900316, a: 1}', 'm_FogColor: {r: 0.7803044, g: 0.9217606, b: 0.990566, a: 0.62352943}'),
    @('m_FogDensity: 0.005', 'm_FogDensity: 0.004'),
    @('m_SkyboxMaterial: {fileID: 2100000, guid: 5b0298777acafa0468f925daa2f7b12e, type: 2}', 'm_SkyboxMaterial: {fileID: 2100000, guid: d9dca9ae1786c394f9e79c637aa9a7bf, type: 2}'),
    @('m_IndirectSpecularColor: {r: 1.5548949, g: 1.8895764, b: 1.970018, a: 1}', 'm_IndirectSpecularColor: {r: 1.9187237, g: 2.2873554, b: 2.3303552, a: 1}'),
    @('m_LightingSettings: {fileID: 4890085278179872738, guid: cea1ee256a4a5084991a3ccf9586be9f, type: 2}', 'm_LightingSettings: {fileID: 4890085278179872738, guid: b6a7bc64394a59142b54f8dfa777c4c9, type: 2}')
)
foreach ($pair in $atmos) {
    if (-not $currentText.Contains($pair[0])) { throw "Atmosphere source not found: $($pair[0])" }
    $currentText = $currentText.Replace($pair[0], $pair[1])
}
Write-Host "atmosphere fields applied: $($atmos.Count)"

# --- Append selected blocks in branch order ---
$out = New-Object System.Collections.Generic.List[string]
foreach ($id in ($selected | Sort-Object { $bb[$_].Order })) {
    $bb[$id].Text | ForEach-Object { $out.Add($_) }
}
if (-not $currentText.EndsWith("`n")) { $currentText += $nl }
$currentText += (($out -join $nl) + $nl)

[System.IO.File]::WriteAllText((Resolve-Path $TargetScene), $currentText)
Write-Host "Appended $($out.Count) lines; replaced $($replace.Count) blocks. Done."
