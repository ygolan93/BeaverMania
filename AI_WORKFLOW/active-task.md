# Active Task

## Task title

- Trader floating / disappearing fix (Level 1 - Remastered - Steam)

## Branch

- `fix/npc-dialogue-ui-alignment` (continued on current working branch)

## Ownership

- **Cursor + Unity Editor**: full owner. Diagnosis, scene-level transform fixes, screenshots, Play Mode verification.
- **Codex**: not required. The planned `NpcGroundSnap` helper script was ruled out (see below); no handoff written.

## Current phase

- Complete. Scene fix applied and saved; Play Mode verification recorded below.

## Diagnosis summary

| Check                                                            | Result                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Trader prefab pivots (`Trader.prefab`, `Camp Trader.prefab`)     | Clean. Root pivot at feet, capsule bottom at local Y=0, scale 0.274.                                                                                                                                                                                                                                                                                                                                                                                                      |
| Arms Dealer (Camp Trader instance in `PlayerPack-Drop and Play`) | Grounded. Root Y == terrain Y (gap 0.000), toes on terrain in Play Mode. No change made.                                                                                                                                                                                                                                                                                                                                                                                  |
| Market Trader (Trader instance in `Market` inside `PlayerPack`)  | Floating 0.835 m above its carpet in Play Mode.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Root cause of float                                              | Scene instance had `mixamorig:Hips` localPosition overridden to (0,0,0) (prefab default y=2.8475). Edit Mode showed a sunken pose, so the root was placed 0.835 too high; runtime animation normalized the pose and lifted the mesh.                                                                                                                                                                                                                                      |
| Disappearance                                                    | NOT culling: renderer culling bounds verified to enclose the baked animated mesh with margin; Animator AlwaysAnimate; camera culling mask Everything; no baked occlusion data; `DisappearOnAway` referenced nowhere. Actual mechanism: the opaque market canopy occludes the trader from rear/side angles (orbit screenshots), made worse by the float pushing his head into the canopy. Float fix resolves the abnormal part; rear occlusion is inherent stall geometry. |

## Changes made (scene only, no code, no prefab assets touched)

| Asset                                              | Change                                                                                                                                        |
| -------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `Assets/Scenes/Level 1 - Remastered - Steam.unity` | Market Trader root world Y 70.899 -> 70.064 (local Y 6.374 -> 5.539 under `Market`), applied as scene instance override via Editor with Undo. |
| `Assets/Scenes/Level 1 - Remastered - Steam.unity` | Reverted the `mixamorig:Hips` localPosition override on the Market Trader instance back to prefab default so Edit Mode pose matches runtime.  |

New repo skills added (workflow capture, not gameplay):

- `.cursor/skills/unity-prefab-debugging/SKILL.md`
- `.cursor/skills/terrain-grounding-check/SKILL.md`

## Decisions

- `NpcGroundSnap` runtime component NOT created: both traders are static (no Rigidbody/CharacterController/NavMeshAgent), placement is now measured-correct, and the root cause (misleading edit-mode bone pose) was removed. A runtime snap would mask future placement errors instead of exposing them.
- No `SkinnedMeshRenderer.updateWhenOffscreen` change: bounds were verified correct; enabling it would add cost without fixing anything.

## Verification

- Play Mode measurement before fix: Market Trader toes 0.835 m above carpet; after fix: root snapped to carpet hit point (raycast, triggers ignored).
- Post-fix Play Mode validation checklist recorded in the task report (traders grounded, visible from player approach angles, prompts working, no new console errors).

## Risks / limitations

- If the market stall or terrain under it is moved again during remaster work, the trader Y must be re-snapped (one-step task with the `terrain-grounding-check` skill).
- Rear/side views of the market trader remain blocked by the stall canopy by design.
