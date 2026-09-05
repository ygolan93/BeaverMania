# Codex to Cursor Handoff

## 2026-09-05 - PR #185 review fixes

### Scope and ownership

- Codex gated typed attack multipliers to advanced Scorpions and added focused EditMode regression coverage. Ordinary Scorpions now take the supplied base damage for every typed attack, preserving their legacy damage behavior.
- Codex restored the `Level 1 - Remastered - Steam` player prefab override to `(431.64, 70.74, 228.66)` and removed the scene's `victoryDelay: 10` override so the Scorpion prefab's inherited two-second delay applies.
- No Inspector assignments, prefab changes, or new serialized fields are required.

### Unity verification required

1. Load `Assets/Scenes/Level 1 - Remastered - Steam.unity` and confirm the player starts in the progression area rather than inside the boss's detection range.
2. Defeat the Scorpion boss and confirm the victory screen appears after the inherited two-second delay, with no ten-second stall.
3. Hit an ordinary Scorpion with Arrow, Sword Swing, Hurricane Kick, and Hurricane Sword routes and confirm each uses its legacy base damage while the advanced boss retains its configured typed multipliers.

### Verification status

- Scoped `git diff --check` passed.
- The repository's `.NET` compilation check could not run because `dotnet` is not installed in this environment.
- Unity Editor and Play Mode were not run. Needs Unity Play Mode verification.

## 2026-09-05 - Hurricane aerial retreat and covering claws

### Scope and ownership

- Mixed: Codex owns `ScorpionScript.cs` and focused combat tests; Cursor owns final in-scene animation/contact verification. Do not modify the user's existing unrelated scene/prefab/asset changes.
- Both explicit `HurricaneKick` and `HurricaneSword` attacks now trigger the same advanced-boss retreat. Damage multipliers remain 0.8x and 1.5x respectively.
- Movement and `isAttacking` start immediately. The boss faces the attacker and retreats in the locked opposite direction at the existing 12 speed for 0.6 seconds. Further Hurricane hits do not change the direction/timer or rewind the animation.
- Start the existing `Base Layer.Backwards` state once. The shipped controller already plays this looping clip at 2x; forcing entry avoids Walk's 1.2476-second exit blend swallowing the retreat. Do not add another animation-speed multiplier or duplicate animation events.
- Preserve the serialized `hurricaneKickRetreatSpeed` / `hurricaneKickRetreatDuration` field names; both Hurricane kinds use them. No Inspector assignments or asset edits are required by the script patch.
- Normal scorpions, Stunned/Dead rejection, Tree Stun/cooldown, damage pipeline, Boost registration, and victory/death flow are unchanged.

### Unity verification required

1. Hit a walking/charging/attacking boss with each aerial Hurricane: it immediately faces the player, moves backwards, and visibly cycles its covering claws throughout the retreat.
2. Approach its moving claw colliders and confirm existing player contact damage occurs. Check player invulnerability/parry behavior and existing claw SFX; no duplicated events or new damage path should be introduced.
3. Land further Kick/Sword hits during the same retreat: the 0.6-second timer and animation phase must not restart. Confirm normal animation and movement resume afterwards.
4. While Tree-Stunned or dead, neither Hurricane can start retreat. Verify normal scorpions retain their ordinary combo stun behavior.
5. Controller/clip: `Assets/Prefabs/Scorpion/BossAnimations/Scorpion.controller`, `Scorpion_Backwards.fbx`. Automated Animator-state coverage does not prove bone motion, physical contact, or visual quality in the production scene.

### Verification status

- Unity 2021.3.3f1 compiled the script changes; no C# compiler errors reported after refresh.
- Six Scorpion EditMode suites: **94/94 passed**, job `8df3a523d03f4ca0a0658771aabd4984`. The first run exposed two fixture-state failures; owned inactive Boost dependencies and explicit target isolation fixed them without changing gameplay.
- Scorpion combat/controller and victory PlayMode: job `4c81eb58e9ba4928902ed0835af95566` **succeeded**, **10 completed**, **zero reported failures**. The bridge reconnected after a domain reload but did not retain the detailed result payload; the 124 discovery count is not the executed test count.
- Read-only clip inspection confirms `Scorpion_Backwards` loops over 0.666667 seconds at state speed 2, with 17 varying rotation keys on each `Jaw_Left.001` / `Jaw_Right.001`. Actual model motion, collider contact, and SFX still need the in-scene checks above.
- Scoped `git diff --check` passed. The Windows `.ci` build shim remains unavailable due to its unresolved Unity assembly paths; the live Unity compile and selected tests were used. Full unrelated suites were not rerun for this follow-up.
- No Prefab, Scene, Animator Controller, animation clip, `.asset`, or `.meta` file was changed for this follow-up.

> **Status (2026-07-04, Wasp Queen hazard pool detachment):** Codex implemented the script-side fix so pooled Wasp Queen poison projectiles and poison zones no longer use a boss-child transform as their runtime storage root. Unity Play Mode verification is still required because the local batchmode test run crashed with `HandleProjectAlreadyOpenInAnotherInstance` while the project was already open.

## Wasp Queen hazard pool detachment - Cursor verification required

### What Codex changed

- `Assets/Scripts/NPC/WaspQueen/WaspQueenPoolHub.cs`
  - Resolves a stable scene-root `WaspQueenHazardPool_<instanceId>` when `poolRoot` is null, the hub transform, or any child of `PF_WaspQueen_Boss`.
  - Keeps external non-boss `poolRoot` assignments valid.
  - Detaches active pooled hazards to world root on get.
  - Parks inactive pooled hazards under the stable pool root on release.
  - Releases active hazards before clearing pools if the hub is destroyed.
  - Destroys the runtime-created pool root with the hub.
- `Assets/Scripts/NPC/WaspQueen/WaspQueenProjectile.cs`
  - Applies world position/rotation before activating the pooled projectile.
- `Assets/Scripts/NPC/WaspQueen/WaspQueenPoisonZone.cs`
  - Applies world position before activating the pooled poison zone.
- `Assets/Scripts/NPC/WaspQueen/WaspQueenBoss.cs`
  - Auto-caches `WaspQueenPoolHub` and initializes it from `Config` when present.
- `Assets/Tests/PlayMode/NPC/WaspQueen/WaspQueenBossPlayModeTests.cs`
  - Adds pooled poison-zone parent/world-position regression coverage.
  - Adds pooled projectile parent/velocity regression coverage.
  - Makes the ground-snap test use explicit `Default` ground mask and `Character` player layer.

### Cursor / Unity checks

- Inspect `PF_WaspQueen_Boss.prefab` and clear or replace `WaspQueenPoolHub.poolRoot`; do not assign a boss child or nested model transform.
- Run the Wasp Queen PlayMode suite, especially:
  - `PooledPoisonZone_DetachesFromBossHierarchy_AndReturnsToSceneRootPool`
  - `PooledProjectile_DetachesFromBossHierarchy_AndKeepsWorldVelocity`
  - `PoisonAoe_SpawnsAtGroundLevel_WhenPlayerIsAirborne`
- In Play Mode, trigger ranged poison and poison AoE:
  - Active `PF_WaspQueen_PoisonProjectile(Clone)` and `PF_WaspQueen_PoisonZone(Clone)` should not appear under `PF_WaspQueen_Boss`.
  - Inactive pooled clones may appear under `WaspQueenHazardPool_<instanceId>` at scene root.
  - Moving/turning the boss after AoE spawn must not move the zone.
  - Projectile trajectory must not bend when the boss rotates.
- Verify `WaspQueenConfig.asset` `groundMask` excludes `Character` and `Enemy` while including actual terrain/ground layers.

### Codex verification

- `git diff --check` passed on the touched C# script/test files.
- `dotnet build .ci/BeaverMania.CI.csproj` remains blocked by unresolved Unity assemblies in this Windows checkout.
- Direct Unity batchmode PlayMode verification was attempted with Unity 2021.3.3f1 but crashed because the project was already open in another Unity instance.

> **Status (2026-06-20):** Active mixed-task handoff is the **Wasp Queen boss foundation** on branch `feature/Wasp-Queen`. Codex completed the script-side foundation and tests. Cursor now owns Unity wiring and Play Mode proof.

## What Codex changed

### New runtime/data files

- `Assets/Scripts/NPC/IBossVictorySource.cs`
- `Assets/Scripts/Data/NPC/WaspQueen/WaspQueenConfig.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenAbility.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenState.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenDecisionPlanner.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenChargeAttack.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenProjectile.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenPoisonZone.cs`
- `Assets/Scripts/NPC/WaspQueen/WaspQueenBoss.cs`

### Compatibility / shared boss-victory changes

- `Assets/Scripts/Player/BossHandler.cs`
  - added optional `bossVictorySourceBehaviour`
  - keeps legacy `ScorpionScript Boss` field
  - subscribes to `IBossVictorySource` when configured
  - preserves current delayed-victory flow and lose-screen precedence behavior
- `Assets/Scripts/NPC/Scorpion/ScorpionScript.cs`
  - now also implements `IBossVictorySource`
  - preserves the existing `Action<ScorpionScript> Defeated` path for current scenes/tests

### Test coverage added or extended

- `Assets/Tests/EditMode/NPC/WaspQueen/WaspQueenDecisionPlannerTests.cs`
  - far prefers ranged
  - near prefers AoE
  - mid prefers charge
  - summon blocked at cap
  - cooldown suppression
  - anti-repeat behavior
- `Assets/Tests/PlayMode/NPC/WaspQueen/WaspQueenBossPlayModeTests.cs`
  - phase 2/3 transitions happen once
  - defeat signal fires once
  - charge locks direction
  - poison AoE ticks by interval
- `Assets/Tests/PlayMode/Core/GameFlow/ScorpionBossVictoryFlowPlayModeTests.cs`
  - extended generic boss-victory-source path while preserving Scorpion flow coverage

## Runtime design now expected

1. `WaspQueenBoss` activates either when explicitly triggered or when the player enters `Config.activateRange`.
2. `Update()` owns state progression, cooldowns, phase checks, and decision selection.
3. `FixedUpdate()` is limited to hover maintenance and active charge movement.
4. Threshold crossings at `<= 70%` and `<= 30%` queue one pending phase transition each.
5. The planner filters illegal actions first, then chooses the highest weighted legal action while discouraging repeats.
6. Summons instantiate the existing `LVL1 Wasp` prefab from configured spawn points and are tracked locally by the boss.
7. Projectile and poison hazards damage `BeaverPlayerBehaviour` directly, respect `Rolling` / `isParried`, and clean themselves up locally.
8. On death, Wasp Queen raises the generic boss defeat contract, clears surviving hazards/summons, and reuses `PooledOneShotVfx` / `PooledDeathDebris` for death presentation.

## What Cursor must wire in Unity

### Create/configure assets

1. Create a real `WaspQueenConfig.asset`.
2. Seed or verify the intended per-phase defaults:
   - phase 1 summon cap/count `3 / 1`
   - phase 2 summon cap/count `5 / 2`
   - phase 3 summon cap/count `7 / 3`
3. Assign:
   - `waspPrefab` -> existing `LVL1 Wasp`
   - `poisonProjectilePrefab`
   - `poisonZonePrefab`
   - `deathExplosionPrefab`
   - `fragmentPrefabs`

### Wire boss prefab / scene instance

1. Add `WaspQueenBoss` to the intended boss object if not already present.
2. Assign:
   - `Config`
   - `Body`
   - `Animator`
   - `HealthBar`
   - `ProjectileSpawnPoint`
   - `AoeOrigin`
   - `WaspSpawnPoints`
   - `ChargeHitbox` if used
   - optional telegraph / attack / hit / death clips
3. Confirm `AudioSource` is present and routed correctly at runtime.
4. Confirm the animator contains triggers:
   - `RangedAttack`
   - `PoisonAoE`
   - `Charge`
   - `Summon`
   - `PhaseTransition`
   - `Die`

### Wire generic victory flow

1. On the intended scene `BossHandler`, assign `bossVictorySourceBehaviour` to the `WaspQueenBoss` component.
2. Leave legacy `BossHandler.Boss` usage intact for existing Scorpion scenes unless you are intentionally testing Wasp Queen.
3. Do not widen this task into `BossPanel`, `BossDialogueInteractionSource`, or `ChatCollider` intro wiring.

## Play Mode validation Cursor owns

- Confirm boss activation and idle state produce no null or missing-reference spam.
- Verify each attack telegraph and action resolves once per decision.
- Verify summon limits by phase: `3 / 5 / 7`.
- Verify summon counts by phase: `1 / 2 / 3`.
- Verify charge direction locks at dash start and does not re-home mid-charge.
- Verify poison zone damage ticks by configured interval, not every frame.
- Verify boss death triggers delayed victory exactly once through `bossVictorySourceBehaviour`.
- Verify lose-screen precedence still beats victory when the delay overlaps a lose state.
- Verify surviving summoned wasps are despawned on boss death/reset.
- Verify player-killed summons still use their normal kill/drop behavior.
- Verify Console has no missing animation-event warnings.

## Constraints to preserve

- Do not hand-edit scene or prefab YAML unless absolutely necessary.
- Do not remove or rename serialized Scorpion boss fields while wiring Wasp Queen.
- Do not expand this pass into boss dialogue generalization.
- Do not replace `LVL1 Wasp` AI with a new summon-specific controller in this task.
- Report any inspector references that code cannot safely infer instead of patching around them in C#.

## Verification completed by Codex

- Static code review of the new boss controller, local hazards, summon ownership, and generic boss victory path.
- Added automated tests for decision planning, boss runtime behavior, and generic `BossHandler` victory-source handling.

## Verification blocked in this environment

- `dotnet build .ci/BeaverMania.CI.csproj` is blocked here by missing Unity-managed assembly references on this Windows machine.
- No Unity Play Mode or serialized-reference verification was performed by Codex.
- Needs Unity Play Mode verification.
