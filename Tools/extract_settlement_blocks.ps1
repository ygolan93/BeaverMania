# Extracts settlement PrefabInstance blocks (market stalls + children) from the
# feature branch scene and appends them to the current (main-based) scene.
# One-shot migration helper for feature/settlement-layout-only.
param(
    [string]$BranchScene = "$env:TEMP\branch_scene.unity",
    [string]$TargetScene = "Assets\Scenes\Level 1 - Remastered - Steam.unity",
    [string]$MarketFolder = "Assets\Low-Poly Medieval Market",
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# 1. Market package GUID set (from imported .meta files on disk)
$marketGuids = New-Object System.Collections.Generic.HashSet[string]
Get-ChildItem -Recurse -Filter *.prefab.meta $MarketFolder | ForEach-Object {
    $g = (Select-String -Path $_.FullName -Pattern '^guid: ([a-f0-9]{32})').Matches[0].Groups[1].Value
    [void]$marketGuids.Add($g)
}
Write-Host "Market prefab GUIDs: $($marketGuids.Count)"

# 2. Parse branch scene into blocks
$lines = Get-Content $BranchScene
$blockStarts = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^--- !u!(\d+) &(\d+)( stripped)?$') {
        $blockStarts += [pscustomobject]@{ Line = $i; Type = [int]$Matches[1]; Id = $Matches[2]; Stripped = [bool]$Matches[3] }
    }
}
Write-Host "Total blocks: $($blockStarts.Count)"

function Get-BlockText([int]$idx) {
    $start = $blockStarts[$idx].Line
    $end = if ($idx + 1 -lt $blockStarts.Count) { $blockStarts[$idx + 1].Line - 1 } else { $lines.Count - 1 }
    return $lines[$start..$end]
}

# 3. Seed: PrefabInstance blocks sourced from market package prefabs
$selected = New-Object System.Collections.Generic.HashSet[int]
$instanceIds = New-Object System.Collections.Generic.HashSet[string]
for ($i = 0; $i -lt $blockStarts.Count; $i++) {
    if ($blockStarts[$i].Type -ne 1001) { continue }
    $text = Get-BlockText $i
    $src = ($text | Select-String -Pattern 'm_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]{32})')
    if ($src -and $marketGuids.Contains($src.Matches[0].Groups[1].Value)) {
        [void]$selected.Add($i)
        [void]$instanceIds.Add($blockStarts[$i].Id)
    }
}
Write-Host "Seed market instances: $($selected.Count)"

# 4. Fixpoint: stripped transforms of selected instances, then child instances parented to them
$anchorIds = New-Object System.Collections.Generic.HashSet[string]
do {
    $changed = $false
    for ($i = 0; $i -lt $blockStarts.Count; $i++) {
        if ($selected.Contains($i)) { continue }
        $b = $blockStarts[$i]
        $text = Get-BlockText $i
        if ($b.Type -eq 4 -and $b.Stripped) {
            $pi = ($text | Select-String -Pattern 'm_PrefabInstance: \{fileID: (\d+)\}')
            if ($pi -and $instanceIds.Contains($pi.Matches[0].Groups[1].Value)) {
                [void]$selected.Add($i); [void]$anchorIds.Add($b.Id); $changed = $true
            }
        }
        elseif ($b.Type -eq 1001) {
            $tp = ($text | Select-String -Pattern 'm_TransformParent: \{fileID: (\d+)\}')
            if ($tp -and $anchorIds.Contains($tp.Matches[0].Groups[1].Value)) {
                [void]$selected.Add($i); [void]$instanceIds.Add($b.Id); $changed = $true
            }
        }
    }
} while ($changed)
Write-Host "Total selected blocks: $($selected.Count)"

# 5. Report all source prefab GUIDs used (for dependency verification)
$usedGuids = New-Object System.Collections.Generic.HashSet[string]
foreach ($i in $selected) {
    $text = Get-BlockText $i
    ($text | Select-String -Pattern 'guid: ([a-f0-9]{32})' -AllMatches) | ForEach-Object {
        $_.Matches | ForEach-Object { [void]$usedGuids.Add($_.Groups[1].Value) }
    }
}
Write-Host "--- GUIDs referenced by selected blocks ---"
$usedGuids | Sort-Object | ForEach-Object { Write-Host $_ }

# 6. Collision check: selected anchor IDs must not exist in target scene
$targetIds = New-Object System.Collections.Generic.HashSet[string]
foreach ($l in (Get-Content $TargetScene)) {
    if ($l -match '^--- !u!\d+ &(\d+)') { [void]$targetIds.Add($Matches[1]) }
}
$collisions = @()
foreach ($i in $selected) {
    if ($targetIds.Contains($blockStarts[$i].Id)) { $collisions += $blockStarts[$i].Id }
}
if ($collisions.Count -gt 0) {
    throw "fileID collisions with target scene: $($collisions -join ', ')"
}
Write-Host "No fileID collisions."

# 7. Append blocks to target scene in original order (LF endings, no trailing extras)
$ordered = $selected | Sort-Object
$out = New-Object System.Collections.Generic.List[string]
foreach ($i in $ordered) { (Get-BlockText $i) | ForEach-Object { $out.Add($_) } }
Write-Host "Blocks: $($ordered.Count), lines to append: $($out.Count)"
if ($DryRun) {
    Write-Host "--- DRY RUN: block headers ---"
    foreach ($i in $ordered) { Write-Host $lines[$blockStarts[$i].Line] }
    exit 0
}
$existing = [System.IO.File]::ReadAllText((Resolve-Path $TargetScene))
$nl = if ($existing.Contains("`r`n")) { "`r`n" } else { "`n" }
if (-not $existing.EndsWith("`n")) { $existing += $nl }
$payload = ($out -join $nl) + $nl
[System.IO.File]::WriteAllText((Resolve-Path $TargetScene), $existing + $payload)
Write-Host "Done."
