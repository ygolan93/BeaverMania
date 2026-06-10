# Active Task

## Task title
- No active task — branch stable after roll-animation work was reverted

## Branch
- `fix/player-jump-sfx-throttle` (pushed to `origin`)

## Current phase
- Idle. Last committed work awaiting final Play Mode QA (see checklists below).

## Branch state (verified 2026-06-11)

Working tree is clean. Committed work on this branch:

| Commit | Summary | Status |
| --- | --- | --- |
| `2ea52146` | Throttle player jump SFX | Committed |
| `1a9fc660` | Fix jump SFX pitch distortion on strafe and roll transitions | Committed, Play Mode QA pending |
| `232d5edd` | Level 1 scene overrides, fence/otter materials, OtterAnim2 Consume transition condition | Committed, Play Mode QA pending |

## Reverted work (do not assume it exists)

The following was implemented in-session on 2026-06-10/11 and then **fully reverted by the user**. None of it is present on the branch:

- Inertia/airborne roll gameplay logic in `BeaverPlayerBehaviour.cs` (`PlayerInertiaRoll`, `PlayerAirborneRoll`, roll layer weights, landing-slide defer).
- Roll SFX routing in `AudioScript.cs` (ground roll cooldown channel, airborne roll wind).
- `RollBall3.0.fbx` animation export, `RollBall2.0.fbx`, and `Assets/SourceArt/BlenderScripts/export_rollball3.py`.
- Editor tooling: `RollBallFbxImportFixer.cs`, `BallRollFbxImportFixer.cs`, `BallRollAnimationExporter.cs`, `ShapekeysAnimControllerMigrator.cs`.
- `Shapekeys_Anim.controller` rewiring (RollBall3 motion on LowerBody Roll) and Roll-state transition normalization on LowerBody/UpperBody layers.

### Knowledge worth keeping if roll work is revisited

- `RollBall2.0.fbx` has a malformed armature: the armature object is named `mixamorig:Hips` with root-level bones and **no** `mixamorig:Hips` bone; avatar copy fails against it.
- Working Blender pipeline: import `NewRoll.fbx` (clean hierarchy), retarget RollBall2's `BallRoll` action by **rotation-only** per-bone copy (hips location only), export with `bake_anim=False`, `apply_scale_options='FBX_SCALE_NONE'`, `apply_unit_scale=False`. Do **not** run `transform_apply` on the armature (bakes the 0.01 scale into bone lengths, ~100x rig errors).
- Unity import: Humanoid, copy avatar from `Shapekeys_Model.fbx` (GUID `d49a26fadbee3d34ba8722eafdaf215a`), `motionNodeName = "Armature"`. This produced a warning-free import.
- `ModelImporterClipAnimation` in Unity 2021.3 has no `loopBlendOrientation`/`loopBlendPositionY`/`loopBlendPositionXZ` properties; setting them is a compile error.

## Pending Play Mode QA (committed work)

### Jump SFX pitch fix (`1a9fc660`) — Level 1 Remastered - Steam
- [ ] Single jump (Space once): exactly one Leap sound, no Console errors
- [ ] Double jump: two sounds spaced by ~0.16s cooldown (expected)
- [ ] Spam Space on ground: throttle prevents stack/clipping
- [ ] Jump from strafe and from roll: no pitch distortion
- [ ] SFX slider affects jump; music slider does not

### Scene/animator commit (`232d5edd`)
- [ ] Level 1 Remastered - Steam loads with no missing references
- [ ] Camera FOV override (45 → 40) looks correct in Play Mode
- [ ] OtterAnim2 transition gated by `Consume` condition behaves correctly (no unintended early exit)
- [ ] Fence brick and otter eye/moustache materials render correctly

## Ownership
- **Cursor + Unity Editor** — Play Mode QA above
- **Codex** — none pending

## Notes
- `Shapekeys_Anim.controller` and `Level 1 - Remastered - Steam.unity` are high-risk assets; any future edits need explicit scope and Play Mode verification.
