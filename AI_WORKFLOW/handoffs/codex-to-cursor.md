# Codex to Cursor Handoff

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
