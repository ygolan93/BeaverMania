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
