# Wasp Queen Boss — Animation Asset Contract (Cursor → Codex)

Status: animation/asset side complete and verified. The runtime boss controller and the
`WaspQueenAnimationEvents` bridge are **Codex-owned** and not yet created. This document is the
exact contract those scripts must satisfy so the existing animator + clip events resolve.

## Assets produced

- Model + clips (FBX): `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx`
  - Rig: **Generic**, root motion **off**, ~1.3 m tall, faces +Z (Unity forward), pivot at body/hip height.
  - 13 clips, exact names: `WQ_Idle_Combat` (loops), `WQ_Intro_Roar`, `WQ_Ranged_Telegraph`,
    `WQ_Ranged_Fire`, `WQ_AoE_Telegraph`, `WQ_AoE_Release`, `WQ_Charge_Telegraph`, `WQ_Charge_Dash`,
    `WQ_Charge_Recovery`, `WQ_Summon_Telegraph`, `WQ_Summon_Release`, `WQ_Phase_Transition`, `WQ_Death`.
- Animator: `Assets/Prefabs/WaspQueen/Animations/WaspQueen.controller`
- Prefab: `Assets/Prefabs/WaspQueen/WaspQueen.prefab` (model + Animator + controller, root motion off,
  child anchors + charge hitbox). No gameplay scripts attached yet.
- Build tooling (re-runnable): `Assets/Editor/WaspQueenSetup.cs`
  (menu `Beavermania/WaspQueen/Build All`, plus `Verify Clips`).

## Animator parameters (exact names / types)

- Triggers: `Intro`, `RangedAttack`, `PoisonAoE`, `Charge`, `Summon`, `PhaseTransition`, `Die`, `HitLight`
- Bools: `IsActive`, `IsDead`, `IsFlying`, `IsEnraged`
- Int: `Phase`
- Floats: `MoveSpeed`, `DistanceToPlayer`

The boss FSM drives state transitions by setting one trigger per ability. Each ability self-chains
windup → release → (recovery) → `Idle_Combat` via **Has Exit Time** transitions, so the FSM only needs
to fire the entry trigger and then read animator state / events for timing. `Die` is wired from
**Any State** → `Death`; `Death` has **no exit transition**. Set `IsDead = true` / `IsActive = false`
on death so no further attacks are issued.

## Animation event method contract

Animation events are placed on the **release/active** clips only (telegraph clips warn-only).
Each event calls a parameterless `public void` method **by name** on a component on the **prefab root**
(same GameObject as the `Animator`). Create `WaspQueenAnimationEvents : MonoBehaviour` on the root that
forwards each call to the boss controller (mirrors `SwordEffects.FireSword()` → player).

| Clip                  | Method (exact)        | Fires at    | Purpose                                               |
| --------------------- | --------------------- | ----------- | ----------------------------------------------------- |
| `WQ_Ranged_Fire`      | `FireProjectile`      | 45%         | spawn poison projectile from `ProjectileSpawnPoint`   |
| `WQ_AoE_Release`      | `ActivateAoE`         | 48%         | activate poison AoE at `AoEOrigin`                    |
| `WQ_Charge_Dash`      | `EnableChargeHitbox`  | start (≈6%) | enable `ChargeHitbox` collider                        |
| `WQ_Charge_Dash`      | `DisableChargeHitbox` | end (≈92%)  | disable `ChargeHitbox` collider                       |
| `WQ_Summon_Release`   | `SummonWasps`         | 55%         | spawn wasps at `WaspSpawnPoint_1/2/3`                 |
| `WQ_Phase_Transition` | `PhasePulse`          | 50%         | phase pulse VFX/SFX                                   |
| `WQ_Death`            | `ExplodeFragments`    | 87%         | death explosion + fragments, hide/disable body (once) |
| `WQ_Intro_Roar`       | `QueenScream`         | 27%         | optional SFX cue (safe no-op if unused)               |

Until `WaspQueenAnimationEvents` exists, these events log "has no receiver" warnings at runtime —
expected, not an error.

## Prefab anchors already present (local space, tune freely)

- `ProjectileSpawnPoint` (front, chest/head height)
- `AoEOrigin` (near base)
- `WaspSpawnPoint_1` / `_2` / `_3` (around/above the queen)
- `ChargeHitbox` (front; `BoxCollider`, `isTrigger`, GameObject starts **inactive** — toggled by the events)

## What Codex still owns

- `WaspQueenController` (FSM, phases at 70%/30% HP, projectile, poison AoE, summon, charge, death/fragments),
  modeled on `ShadowRevenantController` / `ScorpionScript`.
- `WaspQueenAnimationEvents` bridge with the 8 methods above.
- Victory integration via `BossHandler` (`Defeated` event or `bossVictorySourceBehaviour`), like `ScorpionScript`.
- Materials: the FBX imported with missing source PBR textures (originals not on disk); assign Wasp Queen materials/textures in Unity.

## Verification performed (Cursor)

- All 13 clips import with correct lengths and loop flags; events verified in-range (e.g. `ExplodeFragments`
  @1.566s of a 1.8s clip). The importer meta shows a stale "2.82s" warning string that does not reflect the
  actual (correct) imported event time — cosmetic only.
- Controller has all 19 parameters, 13 states, chained ability transitions, and `Death` with empty transitions.
- Prefab references the controller, root motion off, anchors + trigger hitbox present, no missing scripts.
- PlayMode smoke test: `Assets/Tests/PlayMode/Core/NPC/WaspQueenAnimationPlayModeTests.cs`.

## Standing rule — arms must never intersect the body or each other

Any animation edit that keys `shoulder` / `upper_arm_fk` / `forearm_fk` (L or R) must keep the FK arms outside the body
exterior **and** keep the left and right claws from intersecting each other. Treat it as an acceptance gate: after the
edit, run a deterministic `BVHTree.overlap` check on the evaluated `WaspQueen_Body`, **resetting all pose bones to rest
before applying the action** (clips do not key every bone, so naive evaluation inherits the previous clip's pose). Check
arm-vs-body (`DEF-forearm.*`/`DEF-hand.*` vs `DEF-spine*`+`DEF-Abdomen`) and claw-vs-claw (`DEF-*.L` vs `DEF-*.R`).
Required result: **0 and 0 on every frame** (an `upper_arm`-root sternum seam is an acceptable seam, not a gate).

## `WQ_AoE_Release` downward slam (Blender source, re-authored 2026-06-25)

Source rig: `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend` (Blender 5.1.2, 30 fps). `WQ_AoE_Release`
is the poison-AoE active clip, 21 frames (1-21 = ~0.67 s), `ActivateAoE` at 48% (~frame 10). Edited in place; NLA strip
/ frame range unchanged; other 12 `WQ_*` clips untouched.

- **F10 pose = downward slam/pulse** (matches the canonical description "slam/pulse downward that deploys the lingering
  poison field at `AoEOrigin`"), replacing the interim forward energy-blast / rear-back cast: F1 windup
  (== `WQ_AoE_Telegraph` F36) -> F5 rise/anticipation -> **F10 slam down** (`head[0] +0.50`, `neck[0] +0.32`,
  `chest[0] +0.42`, `torso.rot[0] +0.60`, body drop `torso.loc[2] -0.10`, gaster pump `Abdomen[0] +0.50`, both FK arms
  driven down to the sides) -> F13 recoil -> F21 exactly idle F1. Wings on the idle beat (`Wing[0] +0.55/-0.5`), `root`
  at origin.
- **Standing rule re-verified:** rest-reset `BVHTree.overlap` scan = **0 arm-vs-body and 0 claw-vs-claw on all 21
  frames**. Arms rebuilt clean within the `WQ_LimitRot` caps (mirrored `L=(R.X,-R.Y,-R.Z)`); prior stray F2/F3 keys
  removed; `head[0]` F21 returns to `0`. Endpoints: F1 vs `WQ_AoE_Telegraph` F36 = 0.0, F21 vs `WQ_Idle_Combat` F1 = 0.0.
- **Rig usability:** `shoulder.L`/`shoulder.R` in `Arm.L (FK)`/`Arm.R (FK)` collections (kept in `Torso`) so they are
  visible/keyframable with the FK-arm controls.
- **Status:** `.blend` **SAVED**. Prior poses preserved as actions `WQ_AoE_Release_CAST_BAK` (rear-back cast) and
  `WQ_AoE_Release_LIVE_BAK`. Generic FBX re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` +
  `Beavermania/WaspQueen/5 Swap Boss Model` pending; no Unity Play Mode verification.

## `WQ_Charge_Telegraph` rear-back + crouch coil (Blender source, 2026-06-24)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Charge_Telegraph` is the charge windup (warn-only, no animation events). The other 12
`WQ_*` clips and the live `WQ_AoE_Release` WIP are untouched.

The bullets below reflect a read-only keyframe scan of the action on 2026-06-25 (the clip was retimed in Blender after
the earlier authored version, which had a mid-clip peak ~F20 and an F30 settle toward Dash F1). Current actual state:

- **Length:** frames 1-31 (~1.03 s at 30 fps), `use_fake_user` on. 13 fcurves across 10 bones, all keys BEZIER.
- **Read = "coils/rears back and aims at the player, sweeping the wings back like a wind-up before a lunge."** It is a
  continuous wind-up that reaches its MAXIMUM at the final frame F31 (no mid-clip peak, no settle-back); F31 is the most
  coiled/reared/aimed/wings-back frame, immediately before the dash releases.
- **Exact keyframes (frame: value):**
  - Rear-back + crouch: `torso.rotation_euler.X` F1 `0.0`, F10 `-0.28`, F31 `-0.48`; `torso.location.Z` F1 `0.0`,
    F10 `-0.10`, F31 `-0.18`; `Abdomen.rotation_euler.X` F1 `0.22`, F31 `0.40`.
  - Aim at player (face pitches forward/down despite the rear-back): `head.rotation_euler.X` F1 `0.0`, F10 `0.35`,
    F31 `0.70`; `neck.rotation_euler.X` F1 `0.0`, F31 `0.40` (within caps `head.X +/-0.97`, `neck.X +/-0.5`).
  - Wings swept BACK (axis `Wing_L +Z` / `Wing_R -Z` = rearward; `Wing.Y` roll not keyed): `Wing_L.rotation_euler.Z`
    F1 `0.0`, F10 `0.40`, F31 `0.78`; `Wing_R.rotation_euler.Z` F1 `0.0`, F10 `-0.40`, F31 `-0.78`;
    `Wing_L/R.rotation_euler.X` F1 `0.55`, F31 `0.45` (kept raised).
  - Arms near rest (clear of body): `upper_arm_fk.L/R.rotation_euler.X` F1 `0.0`, F29 `0.10`;
    `forearm_fk.L/R.rotation_euler.X` F1 `0.0`, F29 `0.14`.
- **Hand-off:** because the clip ends at maximum coil (wings fully back, torso `-0.48`, head `0.70`) rather than on the
  `WQ_Charge_Dash` F1 launch pose, the telegraph -> dash transition is a deliberate wind-up -> release (the dash provides
  the forward thrust), not a pose match.
- **Collision status:** the standing-rule `BVHTree.overlap` gate passed **0 arm-vs-body / 0 claw-vs-claw** at these pose
  extremes when last run (calibrated against `WQ_Idle_Combat` = 0/0; note shipped `WQ_Charge_Dash` does not pass
  arm-vs-body because its forearms rest on the torso). The peak pose values are unchanged, but the retime moved the
  extremes to F31 (arms to F29); **re-run the gate after this retime to re-confirm.** Head-vs-body is not gateable on
  this mesh (face has no own vertex group; only antennae do), so the `head.X 0.70` aim was confirmed clear of the chest
  by render (side + three-quarter).
- **Status:** live Blender session only, **NOT saved**; pre-edit clip backed up as the action
  `WQ_Charge_Telegraph_LIVE_BAK` (fake user). Generic FBX re-export + `Beavermania/WaspQueen/5 Swap Boss Model` pending;
  no Unity Play Mode verification.

## `WQ_Charge_Dash` looping flight sequence (Blender source, re-authored 2026-06-26)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Charge_Dash` is now a **seamless looping flight** (per user): the wind-in/wind-out were cut
and the flying posture is held across the whole clip with subtle looping motion. Frames 1-21 unchanged; within
`WQ_LimitRot` caps; other 12 `WQ_*` clips untouched.

Re-authored 2026-06-27: a prior hand-edit had animated the upper arms and broken the loop seam; the clip was rebuilt to
spec so every channel seams again (F1==F21). The pre-rebuild hand-arm version is preserved as `WQ_Charge_Dash_HANDARMS_BAK`.
(The rig armature object was renamed `WQ_Idle_Combat` -> `WaspQueen_Rig`; action/NLA unaffected.) 32 fcurves, frames 1-21.

- **Held posture (constant F1-21):** `torso.X 1.0`, `chest.X 0.5`, `head.X -0.75`, `neck.X -0.45`; arms forward
  `upper_arm_fk.R (X -0.55, Y -0.55, Z -2.0)` mirrored; `forearm_fk.R (X 0.0, Z -0.3)`; legs FK trailing
  (`thigh_parent.IK_FK = 1.0`, `thigh_fk.X -0.4`, `shin_fk.X 0.2`); `Wing.Y +/-0.35`, `Wing.Z` mild swept `L -0.3`/`R +0.3`.
- **Looping motion (F1==F21):** wing flap `Wing.X` 2 beats (`0.45/-0.45/0.45/-0.45/0.45`); body bob `torso.loc.Z`
  `0.05->0.10->0.05->0.01->0.05`; gaster `Abdomen.X 0.10->0.16->0.10`; subtle antennae sway.
- **Loop seam VERIFIED:** all fcurves equal at F1 and F21 (deltas `0.0`); side renders F1 vs F21 identical, F6/F16 show
  the wing flap. Standing-rule `BVHTree.overlap` gate = **0 arm-vs-body and 0 claw-vs-claw on all 21 frames**.
- **Follow-up (not handled):** enable `loopTime` for `WQ_Charge_Dash` in `Assets/Editor/WaspQueenSetup.cs` for in-engine
  looping; the clip no longer matches telegraph/recovery so the `Telegraph->Dash->Recovery` chain will pop (transitions
  intentionally cut) and the clip's role as a standalone flight loop needs an Animator/usage rethink; hitbox events still
  fire each loop.
- **Status:** `.blend` **SAVED**. Backups: `WQ_Charge_Dash_HANDARMS_BAK` (pre-rebuild loop w/ hand-adjusted arms),
  `WQ_Charge_Dash_SUPERMAN_BAK`, `WQ_Charge_Dash_DIVE_BAK`, `WQ_Charge_Dash_LIVE_BAK`. Generic FBX re-export +
  `Beavermania/WaspQueen/5 Swap Boss Model` pending (bake must evaluate the `IK_FK = 1` leg switch); no Unity Play Mode
  verification.

### `WQ_Charge_Dash` prior interim versions (superseded by the looping flight above)

- **Superman one-shot dash (2026-06-26):** within-caps forward-leaning flight that snapped forward (F3), held, then
  righted to match `WQ_Charge_Recovery` F1; arms forward via the wide `upper_arm_fk.Z` axis, legs FK-switched. Preserved
  as action `WQ_Charge_Dash_SUPERMAN_BAK`.
- **Explosive forward dive (2026-06-26):** committed forward-dive lunge, arms flared down-and-out. Preserved as action
  `WQ_Charge_Dash_DIVE_BAK`.
- **Original thin lunge:** the shipped one-snap `torso.X -0.3 -> +0.35`. Preserved as action `WQ_Charge_Dash_LIVE_BAK`.

## `WQ_Charge_Recovery` settle/overshoot (Blender source, re-authored 2026-06-26)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Charge_Recovery` is the post-dash settle (warn-only, no animation events). Re-authored in
place from a barely-readable thin settle into a deliberately readable vulnerability window; frames 1-24 unchanged; other
12 `WQ_*` clips untouched. Per user direction it is a **self-contained settle** (decoupled from the now-looping dash;
the `Dash(loop) -> Recovery` boundary blends over the controller's ~0.05 s crossfade), not a pose-match to a dash frame.

- **Length:** frames 1-24 (~0.8 s), `use_fake_user` on, 18 fcurves / 12 bones.
- **Read = "post-dash settle/overshoot back to neutral; intentionally readable punish window":** F1 forward "screech-to-a-halt"
  lurch carrying momentum (`torso.X +0.45`, `loc.Z -0.06`, `chest.X +0.22`, `neck.X +0.18`, `head.X +0.28`,
  `Abdomen.X +0.45`, wings swept `Wing.X 0.45` / `Wing_L.Z -0.28` / `Wing_R.Z +0.28`) -> F6-8 rebound past neutral
  (`torso.X -0.18`, `loc.Z +0.04`, `chest.X -0.10`, `neck.X -0.10`, `head.X -0.12`, wing flare `Wing.X 0.62`,
  `Wing.Z +/-0.10`) -> F10-18 exposed hang/wobble -> **F24 == `WQ_Idle_Combat` F1 exactly** (seamless `Recovery -> Idle`).
  Arms subtle (`upper_arm_fk.X 0.12 -> 0`, `forearm_fk.X 0.10 -> 0`, mirror `L=(R.X,-R.Y,-R.Z)`); all keys within
  `WQ_LimitRot` caps.
- **Standing rule re-verified:** rest-reset `BVHTree.overlap` scan on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw
  on all 24 frames**. Endpoint F24 vs `WQ_Idle_Combat` F1 = 0.0 across all 18 keyed channels. Front + side Workbench spot
  renders at F1/F7/F14 confirm the halt -> overshoot -> settle reads and is symmetric/clip-free.
- **Status:** `.blend` **SAVED**. Prior clip preserved as action `WQ_Charge_Recovery_LIVE_BAK`. Generic FBX re-export to
  `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` pending; no Unity Play
  Mode verification.

## `WQ_Summon_Telegraph` arms-raised "call" gesture (Blender source, re-authored 2026-06-26)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Summon_Telegraph` is the summon windup (warn-only, no animation events). It chains
`Summon_Telegraph -> Summon_Release` (the `SummonWasps` event is at 55% of `WQ_Summon_Release`). The prior clip opened with
the arms already raised and barely moved; re-authored in place to build from idle into an overhead "call". Frames 1-36
unchanged; other 12 `WQ_*` clips untouched. Authored strictly within `WQ_LimitRot` caps, mirror `L=(R.X,-R.Y,-R.Z)`.

- **Length:** frames 1-36 (~1.2 s), `use_fake_user` on, 46 fcurves / 16 bones (torso rot + loc.Z, chest, neck, head,
  Abdomen, shoulder.L/R, upper_arm_fk.L/R, forearm_fk.L/R, Wing_L/R, Antenna_L/R). Keys BEZIER / AUTO_CLAMPED.
- **Read = "arms raised overhead, head up, a 'call' announcing reinforcements":** F1 clean idle-entry pose (arms down,
  neutral head/torso) so the Animator `Idle -> Summon_Telegraph` crossfade is invisible -> F8 anticipation gather (slight
  crouch/look-in, arms begin to lift) -> **F24 APEX**: arms thrust up-and-out with the claws raised beside the head
  (`shoulder.X 0.9`, `upper_arm_fk.R = X -0.5 / Y -0.45 / Z 1.95`), head thrown back/up (`head.X -0.65`, `neck.X -0.40`),
  chest/torso drawn up (`torso.X -0.18`, `loc.Z +0.14`), gaster raised (`Abdomen 0.32`), wings flared, antennae swept back
  -> F31 sustained hold -> **F36 == `WQ_Summon_Release` F1** (`upper_arm_fk.R.Z 1.7`, `forearm_fk.R.Z 0.3`, `head.X -0.25`,
  `torso.X +0.10`, `Wing.Y +/-0.7`). Note: with the `upper_arm` Z axis capped at `+/-2.05`, the readable maximum is a wide
  raised "call" (claws beside the head), not a straight-overhead reach; this also keeps the claws well apart.
- **Standing rule re-verified:** rest-reset `BVHTree.overlap` scan on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw
  on all 36 frames** (forearm+hand L/R 541 polys each vs 1172 spine+Abdomen polys). Endpoint F36 vs `WQ_Summon_Release` F1
  = 0.0 across all keyed channels. Front + side Workbench spot renders at F8/F24/F31/F36 confirm gather -> arms-up call ->
  present reads and is symmetric/clip-free.
- **Status:** `.blend` **SAVED**. Prior clip preserved as action `WQ_Summon_Telegraph_LIVE_BAK`. Generic FBX re-export to
  `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` pending; no Unity Play
  Mode verification.

## `WQ_Summon_Release` summon burst (Blender source, re-authored 2026-06-26)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Summon_Release` is the summon active clip; the `SummonWasps` event is at 55% (spawns wasps at
`WaspSpawnPoint_1/2/3`). Re-authored in place from a weak version (arms only `upper_arm_fk.R.Z 1.7 -> 1.4`, wings unsweep,
head nod, F18 still held the arms raised so `Summon_Release -> Idle` popped) into a readable burst. Frames 1-18 (~0.57 s)
unchanged; other 12 `WQ_*` clips untouched. Within `WQ_LimitRot` caps, mirror `L=(R.X,-R.Y,-R.Z)`.

- **Length:** frames 1-18 (~0.57 s), `use_fake_user` on, 47 fcurves / 17 bones (torso rot + loc.Z, chest, neck, head,
  Abdomen, Stinger.X, shoulder.L/R, upper_arm_fk.L/R, forearm_fk.L/R, Wing_L/R, Antenna_L/R). Keys BEZIER / AUTO_CLAMPED.
- **Read = "summon burst, arms thrust out, wing flare":** F1 == `WQ_Summon_Telegraph` F36 handoff (arms lowered after the
  call) -> **F10 apex (~55%, aligns with `SummonWasps`)**: arms flung up-and-out (`shoulder.R.X 0.9`,
  `upper_arm_fk.R = X -0.35 / Y -0.4 / Z 1.95`, forearm open `R.Z -0.1`), wings flared wide (`Wing.X ~0.45`, `Wing.Y ±1.25`,
  `Wing.Z ∓0.3`), body punch up (`torso.X +0.25` / `loc.Z -0.05`, `chest.X +0.2`, `head.X +0.15`, `neck.X +0.1`), gaster
  pump (`Abdomen.X 0.5`), stinger pump, antennae back -> F13 settle/overshoot -> **F18 == `WQ_Idle_Combat` F1 exactly**
  (seamless `Release -> Idle`). A `Stinger.X` channel eases `0.0` (telegraph) -> `0.08` (idle) so both endpoints match. Note:
  `shoulder.X` is the real arm raiser (the `upper_arm` Z axis mostly twists the claw), driven to the `0.9` cap to make the
  arms thrust out; `Wing.X` is capped at `±0.5`, so the flare comes from `Wing.Y`/`Wing.Z`.
- **Standing rule re-verified:** rest-reset `BVHTree.overlap` on `WaspQueen_Body` = **0 arm-vs-body and 0 claw-vs-claw on
  all 18 frames** (arm L/R 553 polys each vs 1358 spine+Abdomen polys). Endpoints F1 vs `WQ_Summon_Telegraph` F36 = 0.0 and
  F18 vs `WQ_Idle_Combat` F1 = 0.0 across all keyed channels. Front + side Workbench spot renders at F1/F10/F14/F18 confirm
  down -> up-and-out burst with wide wing flare -> settle -> idle; symmetric, clip-free.
- **Status:** `.blend` **SAVED**. Prior clip preserved as action `WQ_Summon_Release_LIVE_BAK`. Generic FBX re-export to
  `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` + `Beavermania/WaspQueen/5 Swap Boss Model` pending; `SummonWasps`
  already wired at `0.55` in `EventsFor()` (no Unity code change); no Unity Play Mode verification.

## `WQ_Death` defeat collapse (Blender source, re-authored 2026-06-26)

Source rig: generated Rigify object `WQ_Idle_Combat` in `D:\Projects\Blender Projects\BeavermaniaFiles\Wasp Queen.blend`
(Blender 5.1.2, 30 fps). `WQ_Death` is the death clip, wired from **Any State -> Death with no exit** transition;
`ExplodeFragments` fires at 87% (`EventsFor()`; `0.87 * 54 ~= frame 47`) and hides/disables the body. Re-authored in
place from a messy version (no clean `F1` entry keys, noisy `upper_arm_fk.*.Z` jitter F18-29, arms oddly raised to
`±1.4`, missing `neck`/`chest`/`forearm`) into a clean readable collapse. Frames 1-54 unchanged; other 12 `WQ_*` clips
untouched. Within `WQ_LimitRot` caps, mirror `L=(R.X,-R.Y,-R.Z)`.

- **Length:** frames 1-54 (~1.8 s), `use_fake_user` on, 27 fcurves / 16 bones (torso rot.X/Y/Z + loc.X/Y/Z, chest, neck,
  head, Abdomen, Stinger, Wing_L/R rot.X/Y, Antenna_L/R rot.X/Z, shoulder.L/R rot.X, upper_arm_fk.L/R rot.X/Z,
  forearm_fk.L/R rot.X). Keys BEZIER / AUTO_CLAMPED. No loop; `F54` does not need to return to idle (no outgoing
  transition, body hidden at F47).
- **Read = "the defeat collapse: she slumps forward, wings and antennae droop, body sinks":** F1 clean idle-entry pose
  (== `WQ_Idle_Combat` F1 on every shared channel) so the Any State -> Death crossfade reads as alive-then-collapsing
  -> F8 brief stagger/anticipation (tiny rear `torso.X -0.06`, `head.X -0.05`) -> F18-47 progressive forward slump
  (`torso.X 0 -> 0.9`), sink (`torso.loc.Z 0 -> -0.22`), spine fold (`chest.X -> 0.5`, `neck.X -> 0.48`,
  `head.X -> 0.72`), gaster sag (`Abdomen.X 0.22 -> 0.65`, `Stinger.X -> 0.35`), wings droop from the raised idle
  (`Wing.X 0.55 -> -0.45`, `Wing_L.Y -> 0.3` / `Wing_R.Y -> -0.3`), antennae droop forward (`Antenna.X 0.05 -> 0.8`),
  and FK arms fall limp (`shoulder.R.X -> -0.12`, `upper_arm_fk.R.X -> 0.08` / `.Z -> -0.08`, `forearm_fk.R.X -> 0.1`,
  L mirrored) -> **F47 fullest collapse (aligns with `ExplodeFragments`)** -> F47-54 held collapse (subtle final sink).
- **Standing rule re-verified:** rest-reset `BVHTree.overlap` scan on `WaspQueen_Body` = **0 arm-vs-body and 0
  claw-vs-claw on all 54 frames** (arm L/R 553 polys each vs 1358 spine+Abdomen polys). All keys within `WQ_LimitRot`
  caps except the intentional `Wing.X 0.55` at F1 (matches idle's stored value, clamps identically to `0.5`). `F1`
  matches `WQ_Idle_Combat F1` exactly on every shared channel. Front + side Workbench spot renders at F1/F10/F30/F47
  confirm upright idle -> slight stagger -> sinking forward fold -> deep drooped collapse; symmetric, clip-free.
- **Status:** `.blend` **SAVED**. Prior clip preserved as action `WQ_Death_LIVE_BAK` (an older `WQ_Death_BAK` also
  remains). Generic FBX re-export to `Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx` +
  `Beavermania/WaspQueen/5 Swap Boss Model` pending (would bundle all already-edited-but-unexported `WQ_*` clips).
  `ExplodeFragments` already wired at `0.87` in `EventsFor()` (no Unity code/controller change); no Unity Play Mode
  verification.
