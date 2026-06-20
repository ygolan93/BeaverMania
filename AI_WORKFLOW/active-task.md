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

## Explicitly out of scope in this branch

- Boss dialogue/panel intro generalization.
- `BossDialogueInteractionSource` reuse for Wasp Queen.
- Scene/prefab YAML hand edits.
- New shared poison/debuff systems.
- ShadowRevenant pool-hub reuse or broader boss framework refactors.
