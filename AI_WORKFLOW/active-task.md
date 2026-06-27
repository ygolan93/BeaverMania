# Active Task

> **Status (2026-06-20):** Active branch for this mixed task is `feature/Wasp-Queen`. Codex owns the Wasp Queen C# foundation and test coverage. Cursor owns prefab, scene, animator, collider, audio/VFX asset, and Play Mode wiring.

## Current mixed task

- Wasp Queen boss foundation for Level 1 combat scenes

## Current owner split

- **Codex:** new Wasp Queen runtime scripts, config asset type, local hazard/minion lifecycle, generic boss-victory contract, `BossHandler` compatibility, Scorpion compatibility shim, and automated test coverage.
- **Cursor:** create and assign the real `WaspQueenConfig.asset`, wire prefab/scene references, assign spawn points and hazard prefabs, confirm animator triggers, assign audio clips/VFX, wire `BossHandler.bossVictorySourceBehaviour`, and run Unity Play Mode validation.
- **User:** accept combat tuning and scene-level integration after Unity verification.

## Codex scope completed

- Added `Assets/Scripts/Data/NPC/WaspQueen/WaspQueenConfig.cs`.
- Added `Assets/Scripts/NPC/IBossVictorySource.cs`.
- Added `Assets/Scripts/NPC/WaspQueen/` runtime foundation:
  - `WaspQueenAbility.cs`
  - `WaspQueenState.cs`
  - `WaspQueenDecisionPlanner.cs`
  - `WaspQueenChargeAttack.cs`
  - `WaspQueenProjectile.cs`
  - `WaspQueenPoisonZone.cs`
  - `WaspQueenBoss.cs`
- Updated `Assets/Scripts/Player/BossHandler.cs` for optional generic boss victory sources while preserving the legacy `ScorpionScript Boss` path.
- Updated `Assets/Scripts/NPC/Scorpion/ScorpionScript.cs` to implement the new generic victory contract without changing the existing Scorpion-specific event path.
- Added/extended tests:
  - `Assets/Tests/EditMode/NPC/WaspQueen/WaspQueenDecisionPlannerTests.cs`
  - `Assets/Tests/PlayMode/NPC/WaspQueen/WaspQueenBossPlayModeTests.cs`
  - `Assets/Tests/PlayMode/Core/GameFlow/ScorpionBossVictoryFlowPlayModeTests.cs`

## Runtime scope now implemented

- Wasp Queen is a new boss controller under `Assets/Scripts/NPC/WaspQueen/`.
- Decision selection is gated by cooldown, distance, summon cap, and anti-repeat behavior.
- Phase thresholds queue at `<= 70%` and `<= 30%` health and transition once after the current action resolves.
- Summons use the existing `LVL1 Wasp` prefab and are tracked locally by the boss controller.
- Projectile and poison cloud hazards are local Wasp Queen hazards, not player projectile reuse and not a new global poison-status system.
- Boss death now supports the generic `IBossVictorySource` flow so `BossHandler` can trigger delayed victory from Wasp Queen without breaking current Scorpion scenes/tests.

## Cursor Unity work required next

- Create and assign the real `WaspQueenConfig.asset`.
- Assign:
  - `Config.waspPrefab` to the existing `LVL1 Wasp` prefab
  - `Config.poisonProjectilePrefab`
  - `Config.poisonZonePrefab`
  - `Config.deathExplosionPrefab`
  - `Config.fragmentPrefabs`
- Wire boss scene/prefab references:
  - `Body`
  - `Animator`
  - `HealthBar`
  - `ProjectileSpawnPoint`
  - `AoeOrigin`
  - `WaspSpawnPoints`
  - `ChargeHitbox` if used
  - optional audio clips
- Confirm animator triggers exist:
  - `RangedAttack`
  - `PoisonAoE`
  - `Charge`
  - `Summon`
  - `PhaseTransition`
  - `Die`
- Wire `BossHandler.bossVictorySourceBehaviour` to `WaspQueenBoss` in the test/production scene that should use the new boss victory flow.
- Keep boss dialogue / `ChatCollider` / `BossPanel` intro reuse out of scope for this task unless a separate dialogue task is opened.

## Manual Unity verification required

- Place Wasp Queen in a test scene and confirm inactive/idle behavior does not spam null warnings.
- Verify far prefers ranged, near prefers poison AoE, and mid-range prefers charge.
- Verify summon caps and summon counts by phase:
  - phase 1: cap `3`, summon `1`
  - phase 2: cap `5`, summon `2`
  - phase 3: cap `7`, summon `3`
- Verify charge locks direction at the start and does not home after telegraph completion.
- Verify poison AoE damages on its configured tick interval, not every frame.
- Verify boss death fires the delayed victory once only through `BossHandler.bossVictorySourceBehaviour`.
- Verify surviving summoned wasps are cleaned up on boss death/reset, but player-killed summons still use their normal death/drop flow.
- Confirm Unity Console has no missing animation-event or null-reference spam during the fight.

## Verification status

- Codex added static code and test coverage only.
- Local `dotnet build .ci/BeaverMania.CI.csproj` remains blocked in this Windows environment by missing Unity-managed assembly references.
- No Unity Editor or Play Mode verification was performed by Codex in this session.
- Needs Unity Play Mode verification.

## Wasp Queen charge animation desync fix (2026-06-27)

**Ownership:** Mixed (Cursor C# fix; Unity Play Mode verification still required).

**Root cause:** Charge FSM could start `WaspQueenChargeAttack` movement from its telegraph fallback timer while the Animator was still in `Idle` (or another non-charge clip). The `Charge` trigger only transitions from `Idle`, so entering charge during recovery/another attack left the trigger unused until later — movement and hitbox activation were not gated on `Charge_Dash`.

**Code fix (Cursor):** `WaspQueenBoss.cs` now:
- Starts the charge animator sequence via `EnsureChargeSequenceStarted()` (`Idle` → trigger, otherwise hard-play `Charge_Telegraph`).
- Defers dash movement until `Charge_Dash` is playing (`IsChargeDashAnimPlaying()` gate in `FixedUpdate`).
- Starts dash from animation sync (`Charge_Dash` state or `EnableChargeHitbox` event) before FSM timer fallback.
- Forces `Charge_Recovery` on charge end if the autonomous chain did not advance.

**Manual Unity verification still required:**
- Charge lunge always shows Telegraph → Dash → Recovery (never idle during dash).
- `EnableChargeHitbox` / `DisableChargeHitbox` still bracket the active window.
- Interrupted charge cleans up hitbox/movement.

**Status:** C# updated; Play Mode not verified in this session.


- Boss dialogue/panel intro generalization.
- `BossDialogueInteractionSource` reuse for Wasp Queen.
- Scene/prefab YAML hand edits.
- New shared poison/debuff systems.
- ShadowRevenant pool-hub reuse or broader boss framework refactors.

## Wasp Queen `WQ_AoE_Release` downward slam (re-authored 2026-06-25)

Blender source rig: `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend` (Blender 5.1.2, 30 fps). Clip
`WQ_AoE_Release` is the poison-AoE active clip (21 frames, 1-21 = ~0.67 s; `ActivateAoE` event at 48% ~= frame 10).
Worked in-place; NLA strip + frame range unchanged; the other 12 `WQ_*` clips untouched.

- **F10 re-authored to a downward slam/pulse** to match the canonical clip description ("slam/pulse downward that
  deploys the lingering poison field at `AoEOrigin`"), replacing the interim forward energy-blast / rear-back cast:
  F1 loaded windup (== `WQ_AoE_Telegraph` F36) -> F5 rise/anticipation (`torso.loc[2] +0.12`, arms held high) ->
  **F10 slam down**: head/neck/thorax crunch down (`head[0] +0.50`, `neck[0] +0.32`, `chest[0] +0.42`,
  `torso.rot[0] +0.60`), body drops (`torso.loc[2] -0.10`), gaster pump (`Abdomen[0] +0.50`), both FK arms driven
  down to the sides (`upper_arm_fk.R` X `+0.15` / Z `-0.30`, forearm down) -> F13 recoil -> F21 returns exactly to
  idle F1. Wings on the idle airborne beat (`Wing[0] +0.55/-0.5`); `root` at origin.
- **Cleanup resolved this pass:** arms rebuilt clean within the `WQ_LimitRot` caps, mirrored `L=(R.X,-R.Y,-R.Z)`;
  the stray F2/F3 arm keys are gone; `head[0]` F21 now returns to `0`. 31 fcurves.
- **Verification:** standing-rule rest-reset `BVHTree.overlap` = **0 arm-vs-body and 0 claw-vs-claw on all 21
  frames**; endpoints **F1 vs `WQ_AoE_Telegraph` F36 = 0.0** and **F21 vs `WQ_Idle_Combat` F1 = 0.0**; confirmed via
  front + side spot renders at F5/F10/F13 (clear downward crunch at F10).
- **Rig usability (kept):** `shoulder.L`/`shoulder.R` in `Arm.L (FK)`/`Arm.R (FK)` (plus `Torso`) for easy
  select/keyframe while animating the arms.
- **Status: `.blend` SAVED.** Prior poses preserved as actions `WQ_AoE_Release_CAST_BAK` (rear-back cast) and
  `WQ_AoE_Release_LIVE_BAK`. Generic FBX re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` +
  `Beavermania/WaspQueen/5 Swap Boss Model` still pending. No Unity Play Mode verification.

## Wasp Queen `WQ_Charge_Telegraph` rear-back + crouch coil (2026-06-24)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). Clip `WQ_Charge_Telegraph` is the charge windup (warn-only, no animation events).
The other 12 `WQ_*` clips and the live `WQ_AoE_Release` WIP are untouched.

Bullets reflect a read-only keyframe scan on 2026-06-25 (the clip was retimed in Blender after the earlier authored
version, which peaked ~F20 with an F30 settle toward Dash F1; nothing was changed during this scan). Current state:

- **Length:** frames 1-31 (~1.03 s at 30 fps), `use_fake_user` on, 13 fcurves / 10 bones, all keys BEZIER.
- **Read = "coils/rears back and aims at the player, sweeping the wings back like a wind-up before a lunge":** a
  continuous wind-up that reaches MAXIMUM at the final frame F31 (no mid-clip peak, no settle); F31 is the most
  coiled/reared/aimed/wings-back frame, right before the dash releases.
- **Exact keyframes (frame: value):**
  - Rear-back + crouch: `torso.rotation_euler.X` F1 `0.0` / F10 `-0.28` / F31 `-0.48`; `torso.location.Z` F1 `0.0` /
    F10 `-0.10` / F31 `-0.18`; `Abdomen.rotation_euler.X` F1 `0.22` / F31 `0.40`.
  - Aim at player: `head.rotation_euler.X` F1 `0.0` / F10 `0.35` / F31 `0.70`; `neck.rotation_euler.X` F1 `0.0` /
    F31 `0.40` (caps `head.X +/-0.97`, `neck.X +/-0.5`).
  - Wings swept BACK: `Wing_L.rotation_euler.Z` F1 `0.0` / F10 `0.40` / F31 `0.78`; `Wing_R.rotation_euler.Z` mirror
    `-0.78`; `Wing_L/R.rotation_euler.X` F1 `0.55` / F31 `0.45`.
  - Arms near rest: `upper_arm_fk.L/R.rotation_euler.X` F1 `0.0` / F29 `0.10`; `forearm_fk.L/R.rotation_euler.X`
    F1 `0.0` / F29 `0.14`.
- **Hand-off:** clip ends at maximum coil (wings fully back, `torso -0.48`, `head 0.70`), not on the `WQ_Charge_Dash` F1
  pose, so telegraph -> dash is a deliberate wind-up -> release (the dash supplies the forward thrust).
- **Collision:** standing-rule `BVHTree.overlap` gate previously passed **0 arm-vs-body / 0 claw-vs-claw** at these pose
  extremes (calibrated vs `WQ_Idle_Combat` = 0/0). Pose values are unchanged but the retime moved the extremes to F31
  (arms F29); **re-run the gate after the retime to re-confirm.** Head-vs-body is not gateable on this mesh; the
  `head.X 0.70` aim was confirmed clear of the chest by render.
- **Status: live Blender session only, NOT saved.** Pre-edit clip backed up as the action
  `WQ_Charge_Telegraph_LIVE_BAK` (fake user). Generic FBX re-export to
  `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` still pending. No
  Unity Play Mode verification.

## Wasp Queen `WQ_Charge_Dash` looping flight sequence (re-authored 2026-06-26, supersedes the Superman one-shot)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). Per user direction the dash was converted from a one-shot Superman dash into a
**seamless looping flight**: the wind-in (F1 uncoil) and wind-out (F17-21 righting) were cut, and the flying posture is
held across the whole clip with subtle looping flight motion. Frame range unchanged (1-21, ~0.7 s); edited in place; the
other 12 `WQ_*` clips untouched. Still authored strictly within `WQ_LimitRot` caps.

> **Re-authored 2026-06-27 (seamless loop restored).** A prior hand-edit had animated the upper arms and broken the loop
> seam; this pass rebuilt the clip to the plan spec so every channel seams (F1==F21). The pre-rebuild hand-arm version is
> preserved as action `WQ_Charge_Dash_HANDARMS_BAK`. NOTE: the rig armature object was renamed to `WaspQueen_Rig` (was
> `WQ_Idle_Combat`); the action/NLA are unaffected. 32 fcurves, frames 1-21, `use_fake_user` on.

- **Held flight posture (constant F1-21):** `torso.X 1.0`, `chest.X 0.5`, `head.X -0.75`, `neck.X -0.45`; arms forward
  (superman) `upper_arm_fk.R (X -0.55, Y -0.55, Z -2.0)` mirrored `L=(R.X,-R.Y,-R.Z)`, `forearm_fk.R (X 0.0, Z -0.3)`;
  legs trailing FK (`thigh_parent.L/R["IK_FK"] = 1.0`, `thigh_fk.X -0.4`, `shin_fk.X 0.2`); `Wing.Y +/-0.35`, `Wing.Z`
  held mild swept `L -0.3` / `R +0.3`.
- **Looping secondary motion (F1==F21):** wing flap `Wing_L/R.X` F1 `0.45` / F6 `-0.45` / F11 `0.45` / F16 `-0.45` /
  F21 `0.45` (2 beats); body bob `torso.loc.Z` F1 `0.05` / F6 `0.10` / F11 `0.05` / F16 `0.01` / F21 `0.05`; gaster life
  `Abdomen.X` F1 `0.10` / F11 `0.16` / F21 `0.10`; subtle antennae sway `Antenna_L/R` (X `0.05->0.13->0.05`, Z
  `+/-0.08->+/-0.12->+/-0.08`).
- **Loop seam VERIFIED:** every fcurve evaluates equal at F1 and F21 (all deltas `0.0`) -> seamless loop. Confirmed by
  side renders F1 vs F21 being identical, with F6 (wings up) vs F16 (wings down) showing the flap motion.
- **Collision:** standing-rule rest-reset `BVHTree.overlap` gate = **0 arm-vs-body and 0 claw-vs-claw on all 21 frames**.
- **Consequences (NOT handled here, follow-up):**
  - In-engine looping needs `loopTime` enabled for `WQ_Charge_Dash` in `Assets/Editor/WaspQueenSetup.cs` (currently only
    `WQ_Idle_Combat` loops). C# + reimport.
  - The clip no longer matches the telegraph (coiled-upright) or recovery (upright); the Animator chain
    `Telegraph -> Dash -> Recovery` will pop at the boundaries. Intended (transitions cut); the boss's use of this clip as
    a standalone flight loop needs an Animator/usage rethink.
  - The hitbox events (`EnableChargeHitbox`/`DisableChargeHitbox` @ 8%/92%) still fire each loop; revisit if the clip is
    repurposed as pure flight rather than an attack.
- **Status: `.blend` SAVED.** Backups preserved: `WQ_Charge_Dash_HANDARMS_BAK` (the pre-rebuild loop with hand-adjusted
  animated arms), `WQ_Charge_Dash_SUPERMAN_BAK` (the Superman one-shot), `WQ_Charge_Dash_DIVE_BAK` (forward dive),
  `WQ_Charge_Dash_LIVE_BAK` (original thin lunge). Generic FBX re-export to
  `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` still pending (the keyed
  `IK_FK = 1` leg switch must be evaluated by the bake). No Unity Play Mode verification.

## Wasp Queen `WQ_Summon_Telegraph` arms-raised "call" gesture (re-authored 2026-06-26)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). `WQ_Summon_Telegraph` is the summon windup (warn-only, no animation events; no
`WQ_Summon_Telegraph` case in `EventsFor()` in `Assets/Editor/WaspQueenSetup.cs`). It chains `Summon_Telegraph -> Summon_Release`
(the `SummonWasps` event lives at 55% of `WQ_Summon_Release`). The prior clip opened with the arms already raised
(`upper_arm_fk.R.Z 1.6`) and barely moved, so it did not read as a rising "call". Re-authored in place to build from idle
into an overhead call; frame range unchanged; the other 12 `WQ_*` clips untouched.

- **Length:** frames 1-36 (~1.2 s at 30 fps), `use_fake_user` on, **46 fcurves** across 16 bones (torso rot.X/Y/Z + loc.Z,
  chest, neck, head, Abdomen, shoulder.L/R, upper_arm_fk.L/R, forearm_fk.L/R, Wing_L/R, Antenna_L/R). All keys BEZIER /
  AUTO_CLAMPED. Mirror `L=(R.X,-R.Y,-R.Z)` on the arms.
- **Beats:** F1 clean idle-entry pose (arms down, head/torso neutral, `Abdomen 0.22`, `Wing.X 0.5`, idle antennae) so the
  Animator `Idle -> Summon_Telegraph` trigger crossfade is invisible -> F8 anticipation gather (`torso.X +0.06`,
  `loc.Z -0.04`, `head.X +0.08`, arms begin to lift `upper_arm_fk.R.Z 0.5` + `shoulder 0.2`, gaster tuck `Abdomen 0.10`)
  -> **F24 APEX "call"**: arms thrust up-and-out with the claws raised beside the head (`shoulder.X 0.9`,
  `upper_arm_fk.R = X -0.5 / Y -0.45 / Z 1.95`, `forearm_fk.R.Z -0.10`), head thrown back/up (`head.X -0.65`,
  `neck.X -0.40`), chest/torso drawn up (`torso.X -0.18`, `loc.Z +0.14`, `chest.X -0.12`), gaster raised (`Abdomen 0.32`),
  wings flared (`Wing.X 0.5`, `Wing.Y +/-0.5`, `Wing.Z +/-0.2`), antennae back (`Antenna.X -0.35`) -> F31 sustained hold/breath
  -> **F36 settles exactly onto `WQ_Summon_Release` F1** (`upper_arm_fk.R.Z 1.7`, `forearm_fk.R.Z 0.3`, `head.X -0.25`,
  `torso.X +0.10`, `Wing.Y +/-0.7`) for a seamless `Telegraph -> Release` handoff.
- **All keys within `WQ_LimitRot` caps** (`upper_arm Z +/-2.05`, `X/Y +/-0.6`; `shoulder.X` max `0.9`; `head.X +/-0.97`;
  `neck.X +/-0.5`; `Abdomen.X +/-0.75`; `forearm.R.Z [-0.35,1.55]`). No clamping surprises.
- **Verification:** standing-rule rest-reset `BVHTree.overlap` on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw on
  all 36 frames** (forearm+hand L/R 541 polys each vs 1172 spine+Abdomen polys). Endpoint **F36 vs `WQ_Summon_Release` F1
  = 0.0** on every keyed channel. Confirmed via front + side Workbench spot renders at F8/F24/F31/F36 (clear
  gather -> arms-up call with head thrown back -> present into release; symmetric, no clipping).
- **Status: `.blend` SAVED.** Prior clip preserved as action `WQ_Summon_Telegraph_LIVE_BAK` (fake user). Generic FBX
  re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` still pending
  (would also bundle the other already-edited-but-unexported clips). No Unity Play Mode verification.

### `WQ_Charge_Dash` prior interim versions (superseded by the looping flight above)

- **Superman one-shot dash (2026-06-26):** within-caps forward-leaning flight that snapped forward (F3), held, then
  righted into recovery (F21 == `WQ_Charge_Recovery` F1). Preserved as action `WQ_Charge_Dash_SUPERMAN_BAK`.
- **Explosive forward dive (2026-06-26):** committed forward-dive lunge with arms flared down-and-out. Preserved as
  action `WQ_Charge_Dash_DIVE_BAK`.
- **Original thin lunge:** the shipped one-snap `torso.X -0.3 -> +0.35` lunge. Preserved as action
  `WQ_Charge_Dash_LIVE_BAK`.

## Wasp Queen `WQ_Charge_Recovery` settle/overshoot re-author (2026-06-26)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). `WQ_Charge_Recovery` is the post-dash settle (warn-only, no animation events;
no `WQ_Charge_Recovery` case in `EventsFor()` in `Assets/Editor/WaspQueenSetup.cs`). The prior clip was a barely-readable
thin settle (only `torso.X 0.25 -> -0.06 -> 0`, a late `head.X`, and wings keyed). Re-authored in place to a deliberately
readable vulnerability window; frame range unchanged; the other 12 `WQ_*` clips untouched.

Per user direction the recovery is a **self-contained readable settle** (decoupled from the now-looping dash; the
`Dash(loop) -> Recovery` boundary blends over the controller's ~0.05 s crossfade), not a pose-match to a dash frame.

- **Length:** frames 1-24 (~0.8 s at 30 fps), `use_fake_user` on, **18 fcurves** across 12 bones (torso rot.X + loc.Z,
  chest, neck, head, Abdomen, Wing_L/R rot.X + rot.Z, Antenna_L/R rot.X + rot.Z, upper_arm_fk.L/R rot.X, forearm_fk.L/R rot.X).
- **Beats:** F1 "screech-to-a-halt" forward lurch carrying dash momentum (`torso.X +0.45`, `torso.loc.Z -0.06`,
  `chest.X +0.22`, `neck.X +0.18`, `head.X +0.28`, gaster inertia `Abdomen.X +0.45`, wings still swept
  `Wing.X 0.45`, `Wing_L.Z -0.28`/`Wing_R.Z +0.28`) -> F6-8 rebound/overshoot past neutral (`torso.X -0.18`,
  `torso.loc.Z +0.04`, `chest.X -0.10`, `neck.X -0.10`, `head.X -0.12`, wing flare `Wing.X 0.62`,
  `Wing.Z +0.10/-0.10`) -> F10-18 exposed hang (slow drift, secondary wobble) -> **F24 settles exactly onto
  `WQ_Idle_Combat` F1**. Arms kept subtle (`upper_arm_fk.X 0.12 -> 0`, `forearm_fk.X 0.10 -> 0`, mirrored
  `L=(R.X,-R.Y,-R.Z)`); all keys clamp-safe within `WQ_LimitRot` caps.
- **Verification:** standing-rule rest-reset `BVHTree.overlap` on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw
  on all 24 frames** (arm L/R 553 polys each vs 1358 body polys). Endpoint **F24 vs `WQ_Idle_Combat` F1 = 0.0** on every
  one of the 18 keyed channels (seamless `Recovery -> Idle`). Confirmed via front + side Workbench spot renders at
  F1/F7/F14 (clear forward halt -> upright overshoot -> settle; symmetric, no clipping).
- **Status: `.blend` SAVED.** Prior clip preserved as action `WQ_Charge_Recovery_LIVE_BAK` (fake user). Generic FBX
  re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` still
  pending (would also bundle the other already-edited-but-unexported clips). No Unity Play Mode verification.

## Wasp Queen `WQ_Summon_Release` summon burst (re-authored 2026-06-26)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). `WQ_Summon_Release` is the summon active clip; the `SummonWasps` event lives at
55% (`EventsFor()` in `Assets/Editor/WaspQueenSetup.cs`) and spawns wasps at `WaspSpawnPoint_1/2/3`. Re-authored in place
from a weak version (arms only `upper_arm_fk.R.Z 1.7 -> 1.4`, wings unsweep, head nod, and F18 still held the arms
raised so the `Summon_Release -> Idle` chain popped) into a readable burst. Frame range unchanged (1-18, ~0.57 s at 30
fps); the other 12 `WQ_*` clips untouched. Authored within `WQ_LimitRot` caps, mirror `L=(R.X,-R.Y,-R.Z)`.

- **Length:** frames 1-18 (~0.57 s), `use_fake_user` on, **47 fcurves** across 17 bones (torso rot.X/Y/Z + loc.Z, chest,
  neck, head, Abdomen, Stinger.X, shoulder.L/R, upper_arm_fk.L/R, forearm_fk.L/R, Wing_L/R, Antenna_L/R). All keys BEZIER /
  AUTO_CLAMPED.
- **Beats:** F1 == `WQ_Summon_Telegraph` F36 handoff (arms lowered after the call: `shoulder 0`, `upper_arm_fk.R.Z 1.7`,
  `head.X -0.25`, `torso.X 0.1`, `Abdomen.X 0.22`, `Wing.Y ±0.7`) -> **F10 BURST APEX (~55%, aligns with `SummonWasps`)**:
  arms flung up-and-out (`shoulder.R.X 0.9`, `upper_arm_fk.R = X -0.35 / Y -0.4 / Z 1.95`, forearm opened `R.Z -0.1`),
  wings flared wide (`Wing.X ~0.45`, `Wing.Y ±1.25`, `Wing.Z ∓0.3`), body punch up (`torso.X +0.25` / `loc.Z -0.05`,
  `chest.X +0.2`, `head.X +0.15`, `neck.X +0.1`), gaster pump (`Abdomen.X 0.5`), stinger pump, antennae swept back
  (`Antenna.X -0.3`) -> F13 settle/overshoot -> **F18 == `WQ_Idle_Combat` F1 exactly** (arms down, `Wing.X 0.55`, all
  neutral) for a seamless `Release -> Idle` landing. Added a `Stinger.X` channel easing `0.0` (telegraph) -> `0.08` (idle)
  so both endpoints stay seamless.
- **Rig note:** `shoulder.X` is the actual arm raiser (the `upper_arm_fk` Z axis mostly twists the claw); the interim pass
  only pushed `shoulder.X` to `0.3`, so the arms barely moved. Driving `shoulder.X` to the `0.9` cap is what makes the
  burst thrust the arms out, calibrated against the telegraph's proven within-caps arms-up apex.
- **Verification:** standing-rule rest-reset `BVHTree.overlap` on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw on
  all 18 frames** (arm L/R 553 polys each vs 1358 spine+Abdomen polys). Endpoints **F1 vs `WQ_Summon_Telegraph` F36 = 0.0**
  and **F18 vs `WQ_Idle_Combat` F1 = 0.0** on every keyed channel. Front + side Workbench spot renders at F1/F10/F14/F18
  confirm down -> up-and-out burst with wide wing flare -> settle -> idle; symmetric, no clipping.
- **Status: `.blend` SAVED.** Prior clip preserved as action `WQ_Summon_Release_LIVE_BAK` (fake user). Generic FBX
  re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` still pending
  (would also bundle the other already-edited-but-unexported clips). `SummonWasps` is already wired at `0.55` in
  `EventsFor()`; no Unity-side code change. No Unity Play Mode verification.

## Wasp Queen `WQ_Death` defeat collapse (re-authored 2026-06-26)

Blender source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp
Queen.blend` (Blender 5.1.2, 30 fps). `WQ_Death` is the death clip; it is wired from **Any State -> Death with no exit**
transition, `ExplodeFragments` fires at 87% (`EventsFor()` in `Assets/Editor/WaspQueenSetup.cs`; `0.87 * 54 ~= frame 47`),
and the body is hidden by that event. Re-authored in place from a messy version (no clean `F1` entry keys, noisy
`upper_arm_fk.*.Z` jitter F18-29, arms oddly raised to `±1.4`, no `neck`/`chest`/`forearm` channels) into a clean
readable collapse. Frame range unchanged (1-54, ~1.8 s); the other 12 `WQ_*` clips untouched. Within `WQ_LimitRot` caps,
mirror `L=(R.X,-R.Y,-R.Z)`.

- **Length:** frames 1-54 (~1.8 s), `use_fake_user` on, **27 fcurves** across 16 bones (torso rot.X/Y/Z + loc.X/Y/Z,
  chest, neck, head, Abdomen, Stinger, Wing_L/R rot.X/Y, Antenna_L/R rot.X/Z, shoulder.L/R rot.X, upper_arm_fk.L/R
  rot.X/Z, forearm_fk.L/R rot.X). All keys BEZIER / AUTO_CLAMPED. No loop (Death has no outgoing transition, so `F54`
  does NOT need to return to idle).
- **Beats:** F1 clean idle-entry pose (== `WQ_Idle_Combat` F1 on every shared channel: arms down, neutral head/neck/torso,
  `Abdomen 0.22`, `Stinger 0.08`, `Wing.X 0.55`, idle antennae) so the Any State -> Death crossfade reads as
  alive-then-collapsing -> F8 brief stagger/anticipation (tiny rear `torso.X -0.06`, `head.X -0.05`, slight settle) ->
  F18-47 progressive forward slump + sink + droop -> **F47 fullest collapse (aligns with `ExplodeFragments`)** ->
  F47-54 held collapse (subtle final sink).
- **Exact keyframes (frame: value):**
  - Forward slump + sink: `torso.rotation_euler.X` F1 `0.0` / F8 `-0.06` / F18 `0.25` / F30 `0.55` / F40 `0.78` /
    F47 `0.9` / F54 `0.92` (cap `±1.1`); `torso.location.Z` F1 `0.0` / F30 `-0.12` / F40 `-0.18` / F47 `-0.22` /
    F54 `-0.24`.
  - Spine fold: `chest.X` F1 `0.0` -> F47 `0.5` (cap `±0.6`); `neck.X` F1 `0.0` -> F47 `0.48` (cap `±0.5`);
    `head.X` F1 `0.0` / F8 `-0.05` / F30 `0.5` / F47 `0.72` (cap `±0.97`).
  - Gaster: `Abdomen.X` F1 `0.22` -> F47 `0.65` (cap `±0.75`); `Stinger.X` F1 `0.08` -> F47 `0.35`.
  - Wings droop (from raised idle to drooped): `Wing_L/R.X` F1 `0.55` (clamps to `0.5`, matches idle) / F18 `0.1` /
    F30 `-0.2` / F47 `-0.45` (cap `±0.5`); `Wing_L.Y` F1 `0.0` -> F47 `0.3`, `Wing_R.Y` mirror `-0.3`.
  - Antennae droop forward/down: `Antenna_L/R.X` F1 `0.05` -> F47 `0.8` (cap `±1.05`); `Antenna_L.Z 0.08 -> 0`,
    `Antenna_R.Z -0.08 -> 0`.
  - Arms fall limp (subtle, gate-safe): `shoulder.R.X 0 -> -0.12`, `upper_arm_fk.R.X 0 -> 0.08`,
    `upper_arm_fk.R.Z 0 -> -0.08`, `forearm_fk.R.X 0 -> 0.1` (L mirrored). Replaces the old raised `±1.4` + F18-29
    jitter with a clean monotonic settle.
- **Verification:** standing-rule rest-reset `BVHTree.overlap` on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw on
  all 54 frames** (arm L/R 553 polys each vs 1358 spine+Abdomen polys). All keys within `WQ_LimitRot` caps except the
  intentional `Wing.X 0.55` at F1 (matches idle's stored value, clamps identically to `0.5`). `F1` matches
  `WQ_Idle_Combat F1` exactly on every shared channel (clean entry); no idle-return required at `F54`. Confirmed via
  front + side Workbench spot renders at F1/F10/F30/F47 (upright idle -> slight stagger -> sinking forward fold ->
  deep drooped collapse; symmetric, no clipping).
- **Status: `.blend` SAVED.** Prior clip preserved as action `WQ_Death_LIVE_BAK` (fake user); an older `WQ_Death_BAK`
  also remains. Generic FBX re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` +
  `Beavermania/WaspQueen/5 Swap Boss Model` still pending (would bundle all already-edited-but-unexported `WQ_*` clips).
  `ExplodeFragments` is already wired at `0.87` in `EventsFor()`; no Unity-side code/controller change. No Unity Play
  Mode verification.
