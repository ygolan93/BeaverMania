# Active Task

## Task title
- Demo Settlement Layout Pass (Island 1 Village + Island 2 Forward Camp)

## Type
- Mixed (Unity scene layout + user Play Mode verification)

## Owner
- **Cursor** — scene hierarchy, prefab placement, markers, light probes (complete)
- **User** — terrain sculpt/paint, NavMesh bake, Play Mode validation

## Current phase
- Layout pass complete; **placement correction pass complete (2026-06-06)** — awaiting user Play Mode + terrain follow-up

## Goal
- Narrative settlement/layout pass for 30–40 min demo flow: village hub → bridge objective → forward camp → hive escalation → red boss.

## Scene
- **Edited:** `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- **Backup:** `Assets/Scenes/BackUp Scene/Level 1 - Remastered - Steam - LD Backup 2026-06-06.unity`

## Constraints preserved
- `PlayerPack-Drop and Play`, `Market/Trader`, `Camp/Camp Trader` not moved
- Checkpoints, bosses, hives untouched
- No gameplay script changes

## Branch
- `feature/demo-settlement-layout-pass`

## Placement correction pass (2026-06-06, Cursor-owned)
- Downscaled oversized props: `PRP_I1_Cart_01` (0.35), `PRP_I1_FarmSack_01` (0.38), `PRP_I1_CrateCluster_03` (0.4), dock crates/barrel (0.55–0.6), blacksmith anvil/hammer (0.45)
- Disabled white placeholder `pPlane1` child on farm sack instance
- Repositioned cart off bridge path; bridge dressing scaled/side-offset
- Moved path markers (`MRK_I1/I2_MainPath_A`, `INT_I2_HiveTrigger_01`) off walk lanes
- Rescaled/repositioned I2 dead trees, fences, barricades to path edges
- Marked corrected props Static
- **Needs Unity Play Mode verification**

## Manual follow-up (user)
- [ ] Terrain pad flatten + path texture paint (dock, village paths, hive/boss approach)
- [ ] Rebake light probes (empty groups placed — run Generate Lighting or manual probe placement)
- [ ] NavMesh rebake if enemy pathing breaks
- [ ] Play Mode: trader prompts, bridge build colliders near `PRP_I1_BridgePart_*`, boss arena clearance
- [ ] Verify decorative `NewBridgePart` instances do not block bridge gameplay

## Result
- **Layout pass: complete (Needs Unity Play Mode verification)**
- **Placement correction pass: complete (Needs Unity Play Mode verification)**
