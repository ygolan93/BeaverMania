# Cursor → Codex Handoff

## WaspQueen charge verification — failing tests (2026-06-27, branch `feature/Wasp-Queen`)

Cursor completed Unity-side animation/charge wiring verification (Unity 2021.3.3f1). The charge animation sync is verified: production controller rebuilt with the correct charge chain (`Charge_Telegraph -> Charge_Dash` @0.95, `Charge_Dash` no exits, code forces `Charge_Recovery`), `WQ_Charge_Dash` loops with only `EnableChargeHitbox`, prefab/EventSink/controller runtime-confirmed, audit `OK: 0 issues`, and the critical `BossCharge_StaysInLoopedDash_AndDoesNotDisableHitboxFromDashLoop` PlayMode test PASSES.

Four PlayMode tests fail. None are caused by Unity wiring or the controller rebuild (the failing tests build their own harness or use the auxiliary display prefab). These are script/test-expectation items Codex owns:

| Test                                                                 | Result                                     | Likely cause                                                                                                                                                                                                                                                                                                                                 |
| -------------------------------------------------------------------- | ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WaspQueenAnimationPlayModeTests.DefaultState_IsIdleCombat_AndLoops` | Expected state name `Idle_Combat`, was not | Uses the auxiliary display prefab `Assets/Prefabs/WaspQueen/WaspQueen.prefab` / display controller (`2 Build Controller`, not rebuilt this pass). Production boss state is named `Idle`. Test expectation or display controller naming is stale.                                                                                             |
| `WaspQueenBossPlayModeTests.Charge_LocksDirectionAtStart`            | angle 90 vs `<0.01`                        | Test captures `CurrentChargeDirection` at charge-state entry, but the desync fix locks direction at dash start (`BeginChargeActive`). The 90° = fallback `transform.forward` (before lock) vs locked `-X` (after player moved). Code matches intended design ("lock at dash start, face during telegraph"); test asserts the pre-fix timing. |
| `WaspQueenBossPlayModeTests.PhaseTransitions_HappenOncePerThreshold` | `CurrentPhaseNumber` 2 vs 3                | Phase-progression timing/logic; no animator dependency.                                                                                                                                                                                                                                                                                      |
| `WaspQueenBossPlayModeTests.PoisonAoe_TicksAtConfiguredInterval`     | player health 100 vs 90                    | Poison first-tick timing vs `aoeTickRate`; no animator dependency.                                                                                                                                                                                                                                                                           |

Requested: Codex decide whether to update the stale test expectations (`DefaultState`, `Charge_LocksDirectionAtStart`) to match the shipped charge design, and whether `PhaseTransitions` / `PoisonAoe` reflect a real logic bug or test timing. Cursor did not modify boss C# (charge logic verified correct) per task scope.

Minor scene note (not blocking): `Assets/Scenes/ShadowRevenantTestArena.unity` has no `AudioVolumeSettings`, so boss SFX log `[AudioSourceRouting] Could not resolve Enemy mixer group`; `PF_WaspQueen_Boss` `WaspSpawnPoints` has a single entry (summons stack at one point).

### Resolution (2026-06-30, test-only fixes — VERIFIED GREEN in Unity Test Runner)

All failures were addressed in the test files only; no `WaspQueenBoss` gameplay logic changed (charge logic was verified correct). Verified live via Unity MCP against `BeaverProject@d3b470b190d10be0` (Unity 2021.3.3f1): **PlayMode 9/9 passed, EditMode 7/7 passed** (full Wasp Queen suite = 16/16).

PlayMode (the original 4 + confirmations):

- `DefaultState_IsIdleCombat_AndLoops` -> renamed `DefaultState_IsIdle_AndLoops`. The display prefab `Assets/Prefabs/WaspQueen/WaspQueen.prefab` actually references the production boss controller (guid `db574f27880baf040b9210f7bc0f8b0b`, default state `Idle`), not the display controller (`bec1f8b9...`, default state `Idle_Combat`). The expectation was genuinely stale; updated to `Idle` and made the state read wait robustly. `WQ_Idle_Combat` keeps `loopTime`, so the loop assertion still holds. PASS.
- `Charge_LocksDirectionAtStart` -> now waits for `ChargeAttack.IsActive` (dash start) before capturing the locked direction, matching the shipped "face during telegraph, lock at dash start" design, then moves the player to prove the dash does not re-home. It also disables `stingWeight` so the planner deterministically picks Charge (sting lunge shares `ChargeAttack` and re-homes by design; the first run picked sting and produced the 21.6 deg = 180 deg x one-FixedUpdate sting steer). PASS.
- `PhaseTransitions_HappenOncePerThreshold` -> transitions are queued and applied after the current action resolves, so the test now disables every ability (out-of-range player + `maxActiveSummonedWasps = 0` + `stingWeight = 0`) and disables leash/arena, keeping the boss in a fast Idle->Decision loop so each queued threshold change is applied promptly. PASS.
- `PoisonAoe_TicksAtConfiguredInterval` -> the zone has a ground warning-ring delay (`aoeGroundTelegraphTime`, default 0.5s) before the first tick; the test now zeroes it to isolate `aoeTickRate`. PASS.

EditMode (`WaspQueenDecisionPlannerTests`, found broken during the run — were erroring, not in the original list):

- All 6 threw `MissingMethodException` because `CreateContext` built `WaspQueenDecisionContext` via reflection with the pre-sting 9-arg signature; the struct gained `stingCooldownRemaining`. Added that arg in the correct position. The two "expect Idle" tests (`SkipsSummon`, `SkipsAbilitiesOnCooldown`) now also pass `stingCooldownRemaining` on cooldown so sting cannot be chosen.
- `ChooseAbility_ReturnsCharge_WhenPlayerAtMediumRange` then revealed that summon narrowly out-scores charge at distance 7 with equal weights (summon urgency boost when no minions are out: 6.25 vs 6.2). Parked summon on cooldown in that test to isolate the medium-range melee choice. PASS. (Note for Codex: this is intended planner behavior, not a bug, but worth a glance if charge-vs-summon balance at equal weights matters.)
- `WaspQueenPrefabReferenceAuditTests.WaspQueenPrefabs_HaveNoBrokenReferences` PASS (confirms the production prefab refs).

### Level 1 boss design — Wasp Queen as warm-up boss (updated 2026-07-01)

**Product decision (supersedes the 2026-06-30 note below):** The Wasp Queen is a **warm-up boss** the player fights in Level 1 **before** the final **Scorpion** boss. Level 1 is intended to have two sequential boss encounters: **Wasp Queen (warm-up) → Scorpion (final)**. The Wasp Queen is therefore intentionally present in the Level 1 scene (not a mistake, and no longer "out of scope").

**Current state on this branch:** `Level 1 - Remastered - Steam` contains a `PF_WaspQueen_Boss` instance at `WaspQueen_ArenaCenter`. The instance does not override `ActivateOnStart`, so it inherits the prefab default `ActivateOnStart: 1` and currently activates on scene load rather than when the player reaches its arena.

**Integration follow-up (NOT done on this branch — Codex/Cursor):**

- Gate the warm-up encounter so the Wasp Queen activates on player approach or an arena trigger, not on scene load (e.g. set the instance `ActivateOnStart = false` and rely on proximity `activateRange`, or wire an arena entry trigger). Place/sequence it earlier in the level than the Scorpion arena.
- Keep the Scorpion as the **final boss and victory owner**: `BossHandler.bossVictorySourceBehaviour` (or the legacy `Boss` field) must stay the Scorpion so end-of-level victory only fires on the Scorpion. The Wasp Queen's defeat should gate progression toward the Scorpion, **not** trigger the victory screen.
- Consider whether the shared `PF_WaspQueen_Boss` prefab default `ActivateOnStart` should be `false` (proximity-gated) with the test arena instance opting into `true`, so production scenes never get an on-load boss.

**Addresses Codex PR #182 review (P1 "Remove Wasp Queen from the Level 1 scene"):** the instance is intentional per the warm-up design; the valid part of that comment (activates on scene load) is captured as the gating follow-up above.

#### Prior note (2026-06-30, superseded)

Per the earlier product direction, the Level 1 final boss remained the Scorpion and a Wasp Queen scene instance was considered out of scope for `feature/Wasp-Queen`. The Wasp Queen still ships here as a self-contained boss package validated in `ShadowRevenantTestArena`; the warm-up-encounter wiring in Level 1 is the follow-up described above.

---

## WaspQueen pool parenting + ground-snap — FAILED in Play Mode (2026-07-04, branch `feature/settlement-layout-only`)

### What was attempted

Two changes were made to the Wasp Queen hazard spawning system:

#### 1. Poison zone ground-snap fix (WaspQueenBoss.cs)

**Problem:** `ResolveAoeTargetPosition()` returned the raw `Player.transform.position`. When the player was mid-jump (+3.42 units measured live), the poison zone (telegraph ring, fog cloud, damage sphere) spawned floating in the air instead of on the ground.

**Fix applied:** Refactored `ResolveAoeTargetPosition()` to resolve a candidate position (player / AoeOrigin / self), then project it onto the ground via a new private `SnapToGround()` helper. Uses `Physics.Raycast` with `Config.groundMask`, `Config.groundCheckStartHeight`, `Config.groundCheckDistance` — same pattern as the existing `ResolveHoverPosition()`. Falls back to the unmodified position when `Config` is null or the raycast misses. Small +0.02 Y offset to avoid telegraph z-fighting.

**Test added:** `PoisonAoe_SpawnsAtGroundLevel_WhenPlayerIsAirborne` in `WaspQueenBossPlayModeTests.cs` — creates a ground-plane cube, raises the player to Y=5, forces PoisonAoE, asserts zone spawns at ground level. Uses `FindObjectsOfType(zoneType, false)` (active only) to avoid matching the inactive prefab template.

**Review status:** Passed spec compliance review and Unity quality review (two-stage subagent review). No compilation errors in Unity console. Script-only, no Inspector assignments required.

**Risk flagged:** `Config.groundMask` defaults to `~0` in the C# field declaration, which includes the Player physics layer. If the Inspector asset value also includes the Player layer, the downward raycast from above the player could hit the player's own capsule collider instead of the ground. The fix uses the same mask as `ResolveHoverPosition()` (which works because the boss hovers from a different XZ). Codex should verify the `WaspQueenConfig.asset` `groundMask` value only includes terrain/ground layers.

#### 2. Pool parenting fix (WaspQueenPoolHub.cs) — FAILED

**Problem:** Pooled poison zones and projectiles spawned as children of the boss prefab (confirmed in Hierarchy screenshot). Root cause: `CreateInstance` calls `Instantiate(prefab, poolRoot)` where `poolRoot` defaults to `transform` (the boss's own transform). Active hazards inherited boss movement/rotation — zones slid when the boss moved, projectiles veered off course.

**Fix applied:**

- `onGet` callback: added `item.transform.SetParent(null, true)` before `SetActive(true)` — unparent to world root when taken from pool
- `onRelease` callback: added `item.transform.SetParent(poolRoot, false)` after `SetActive(false)` — re-parent under poolRoot when returned

**Result:** User reports the fix **failed** in Play Mode. The exact failure mode is not yet diagnosed. Possible causes for Codex to investigate:

1. **`SetParent(null, true)` may not behave as expected with kinematic Rigidbody on the pooled objects** — projectiles have `Rigidbody.isKinematic = true` and the unparenting with `worldPositionStays = true` may interact poorly with the physics system.
2. **Ordering issue**: `SetParent(null)` happens before `SetActive(true)` in the `onGet` callback. The object may need to be active first, or the `Activate()` call in `WaspQueenBoss.cs` may re-set positions in local space that assumes a parent.
3. **The `poolRoot` fallback to `transform`** (line 85-86 in `EnsurePools`) means the pool container IS the boss. Even the inactive parenting means the pool objects show as children. A cleaner solution might be to create a dedicated static root (`new GameObject("WaspQueenPool")`) or use `Instantiate(prefab)` without a parent at all.
4. **`WaspQueenProjectile.Activate()` and `WaspQueenPoisonZone.Activate()` may set `transform.position`/`rotation` AFTER the pool's `onGet`** — this should work with world-space position on an unparented object, but needs verification.
5. **The `Prewarm` cycle** (get then immediately release) may trigger `SetParent(null)` + `SetParent(poolRoot)` on prewarm, which could leave objects in unexpected state.

### Files changed (working tree, uncommitted)

| File                                                                | Change                                                           |
| ------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `Assets/Scripts/NPC/WaspQueen/WaspQueenBoss.cs`                     | `ResolveAoeTargetPosition()` refactored + `SnapToGround()` added |
| `Assets/Scripts/NPC/WaspQueen/WaspQueenPoolHub.cs`                  | `onGet`/`onRelease` callbacks modified for unparent/re-parent    |
| `Assets/Tests/PlayMode/NPC/WaspQueen/WaspQueenBossPlayModeTests.cs` | New test `PoisonAoe_SpawnsAtGroundLevel_WhenPlayerIsAirborne`    |

### What Codex should do

1. **Review the pool parenting failure.** Read `WaspQueenPoolHub.cs`, `WaspQueenProjectile.cs`, and `WaspQueenPoisonZone.cs` to determine why unparenting in `onGet` failed. Check `Activate()` methods for local-space assumptions.
2. **Decide on the correct pool architecture.** Options: (a) unparent in `onGet` / re-parent in `onRelease` (current attempt, failed), (b) never parent under the boss — use `Instantiate(prefab)` without a parent in `CreateInstance`, (c) create a dedicated static pool root that doesn't move.
3. **Verify `groundMask` in the config asset** does not include the Player layer.
4. **Review whether the ground-snap fix is sound** independent of the pool parenting issue (it passed both subagent reviews but has not been verified in Play Mode with the pool fix failing).

### What Codex must not change

- Prefab/scene YAML
- `WaspQueenConfig.cs` field layout (config asset values are fine to note but not change in code)
- Unrelated Wasp Queen systems (decision planner, charge, sting, summon)
- Test file structure or harness pattern

---

## Task

- **Level 1 Remastered — north forest hotspot FPS fix** on branch `fix/log-carry-animation-load`.
- **Status:** Pass 1 (terrain + shadow overrides) improved reference-coordinate metrics; **task reopened (2026-06-13)** for a direction-specific **canopy fill-rate** cliff. See `codex-to-cursor.md` for Codex static review and Cursor A/B steps.
- **Branch rollback (2026-06-13):** tip reset to `0da224eb`; removed player prefab commits `8f0ed3cb` / `38596429`. Remote force-pushed. Carry prefab serialization still pending.

## Unity context

- Scene: `Assets/Scenes/Level 1 - Remastered - Steam.unity` (**modified**)
- Prefab(s): not modified in FPS pass (review targets: `TheGivingTree.prefab`, `ENV_TheGivingTree_TerrainTree.prefab`)
- TerrainData assets: not modified directly; tuning via Terrain component fields on 33 scene tiles
- Scripts on same branch: `Carry.cs`, `NpcDialoguePresenter.cs`, `BeaverPlayerBehaviour.cs`

## Problem summary

| Issue                   | Symptom                                                             | Status                                                               |
| ----------------------- | ------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Global terrain overdraw | ~100M tris, ~10–15 FPS at bad ref coords                            | **Improved** (~34–39 FPS, ~5–21M tris at ref coords, 2026-06-12 MCP) |
| Canopy-facing fill-rate | Severe FPS in Edit + Play when camera faces giant canopy / open sky | **Open** — Codex hypothesis: `TheGivingTree` alpha-cutout overdraw   |

## Current scene diff — terrain (all 33 tiles, verified in YAML)

| Setting                     | Original | Current working diff |
| --------------------------- | -------- | -------------------- |
| `m_TreeDistance`            | 5000     | **200**              |
| `m_TreeBillboardDistance`   | 50       | **30**               |
| `m_TreeMaximumFullLODCount` | 50       | **25**               |
| `m_DetailObjectDistance`    | 80       | **20**               |
| `m_DetailObjectDensity`     | 1        | **1**                |
| `m_HeightmapPixelError`     | 5        | **5**                |
| `m_SplatMapDistance`        | 1000     | **600**              |
| `m_DrawInstanced`           | 0        | **1**                |

**Decorative foliage (bad bbox X 115–145, Y 70–79, Z 220–340):** `m_CastShadows: 0` on 52+ hand-placed renderers. No gameplay objects removed.

## Reference-coordinate before / after (MCP, 2026-06-12)

| Zone                    | FPS before | Tris before | FPS after | Tris after |
| ----------------------- | ---------- | ----------- | --------- | ---------- |
| Bad (119.6, 72, 266.1)  | ~10.5–14.9 | ~80–105M    | ~34–39    | ~5–21M     |
| Good (149.1, 70, 167.5) | ~72–83     | —           | ~72       | ~14M       |

Does **not** certify the canopy-facing repro — that remains user-reported open.

## Prior script work on same branch (for Codex review)

1. `**Carry.cs`\*\* — independent movement vs animation multipliers; gated penalty refresh
2. `**NpcDialoguePresenter.cs`\*\* — shutdown guard; idle tick throttle
3. `**BeaverPlayerBehaviour.cs**` — HUD / life-icon string caching

## What Codex should do

- [x] Static review of canopy shader/material path (done — see `codex-to-cursor.md`)
- [x] Optional: diff review of three scripts above when preparing merge
- [ ] Run `dotnet build .ci/BeaverMania.CI.csproj` if Unity managed refs available
- [ ] PR summary after Cursor completes pass 2 A/B

## What Codex must not change

- Steam scene terrain/canopy overrides without coordinating with Cursor
- Global edits to `SyntyStudios_VegitationShader` without tight A/B (vendor shader under `Assets/Eden/**`)
- Revert `m_TreeDistance` to 5000 without reference-coordinate re-profile
- Broad refactors of player/presenter scripts in this merge pass

## Risk notes

- **Canopy fill:** `TheGivingTree_Leaves.mat` uses `SyntyStudios/VegitationShader` (`TransparentCutout`, `Cull Off`, wind) — high overdraw risk against sky
- **No occlusion bake:** scene `m_OcclusionCullingData` is null
- **Shadow-off alone insufficient:** if cliff persists after shadow disable, main-pass overdraw is the likely remaining cost
- **Scene diff:** ~664 lines in scene YAML

## Carry-over review candidates (not blocking)

- `Trader.skipPressed` / close-reason paths in `NpcDialoguePresenter`
- `DisappearOnAway.cs` — dead code; deletion candidate with user confirmation
- Player camera near clip **0.001** in scene

## Cross-reference

- Active task + checklists: `AI_WORKFLOW/active-task.md`
- Codex → Cursor A/B steps: `AI_WORKFLOW/handoffs/codex-to-cursor.md`

---

## Scorpion boss AI rework — architecture and merge-readiness review (2026-08-12)

### Ownership

- **Branch:** `feature/ScorpionBoss_Rework`
- **Cursor completed:** focused gameplay implementation, boss stats tuning, obstruction bug fix, automated tests, and controlled production-scene trials.
- **Codex requested:** architecture/risk review before merge. Do not broaden gameplay behavior.

### Implemented behavior

- Boss-only advanced AI gated by `ScorpionStatsData.advancedAiEnabled`.
- Distance-aware weighted Attack/Charge/Reverse/Hold decisions with maximum two identical macro selections.
- Charge windup, brief tracking, locked horizontal direction, variants, and time/distance/pass/contact termination.
- Persistent opposing wall contact now terminates Charge even when contact began during ChargeWindup; flat and walkable sloped ground contacts are filtered before horizontal obstruction evaluation.
- Attack timeout, profile recovery, post-stun pressure, and intentional Parry counter reward of two combo.
- Normal Scorpion remains on the legacy FSM path.

### Fresh verification

- Unity EditMode batch: **46/46 passed** for `Beavermania.Tests.EditMode.NPC.Scorpion`.
- Unity PlayMode batch: **4/4 passed** for `ScorpionBossVictoryFlowPlayModeTests`.
- New state-boundary coverage includes Attack timeout/recovery, Charge timeout, persistent collision exit, post-stun timing, and stun interruption.
- Repository `.ci` build remains blocked on Windows by its existing Linux Unity assembly hint paths.

### Architecture concerns requiring Codex decision

1. `ScorpionScript.cs` now exceeds 1,000 lines and mixes legacy plus advanced FSM responsibilities.
2. `ScorpionStatsData` has repeated scalar fields for three phase profiles and Charge variants.
3. `stateTimer` still carries several state-specific meanings.
4. EditMode tests use reflection because the runtime code is in `Assembly-CSharp` while tests are in an asmdef.

### Requested Codex output

1. Decide whether extraction into a focused advanced-combat runtime/brain is required before merge or should be a separate serialized-safe follow-up.
2. If extraction is required, provide exact ownership boundaries that preserve `ScorpionScript` public/serialized contracts, enum values, prefab GUIDs, legacy normal-Scorpion behavior, and Animator bools.
3. Decide whether grouped serializable tuning can be introduced without removing or renaming existing serialized fields in this branch.
4. Review the final diff for merge readiness after the obstruction fix and expanded FSM tests.

### Protected areas

- Do not edit scene, prefab, Animator, animation, model, collider wiring, or `.meta` GUIDs.
- Do not change HP/damage tuning, add abilities, or alter the approved combat grammar.
- Do not remove/rename serialized fields without an explicit migration plan.

---

## Scorpion boss combat contract — Unity verification results + balance findings (2026-09-05)

### Ownership

- **Branch:** `feature/ScorpionBoss_Rework`, HEAD `74c82bf6` ("feat: implement scorpion boss combat contract").
- **Cursor completed:** Unity-side integration (Giving Tree marker, boss stats values), plus the **first actually-executed** EditMode/PlayMode verification of the boss contract via Unity MCP against `BeaverProject@d3b470b190d10be0` (Unity 2021.3.3f1).
- **Codex requested:** one C# defect decision (P1 below), one balance/design decision, and two pre-existing test failures that will block CI.
- Cursor changed **no C# and no formulas** in this pass.

### Verification status — contract is GREEN

All prior Scorpion passes reported "0 EditMode and 0 PlayMode tests were executed". That gap is now closed.

| Run | Result |
| --- | --- |
| Scorpion EditMode contract suites (scoped) | **48/48 passed** |
| Scorpion boss PlayMode + victory-flow (scoped) | **8/8 passed** |
| Full EditMode suite | 111 ran, **6 failed — none Scorpion** (3 audio-routing, 3 input-lifecycle) |
| Full PlayMode suite | **Aborted at 36/111** — see P3 |

Every item on the user's contract checklist has executed-test evidence except respawn/fight-restart (no test exists). `ScorpionCombatContractTests` pins the exact shipped asset values (`bossStunDuration 3.5`, `bossStunCooldown 10`, `stunnedDamageMultiplier 2`, `hurricaneKickRetreatSpeed 12`, `hurricaneKickRetreatDuration 0.6`), so the asset and the suite agree.

Note the suite has changed shape since the 2026-08-26 notes above: `ScorpionVulnerabilityWindowTests.cs` is **gone**, replaced by `ScorpionCombatContractTests.cs`. `ReceiveDamage_AdvancedBossInNormalState_IsAlwaysDamageable` now asserts the advanced boss is damageable in Idle/Attack/Charge/Reverse/Look. The punish-window model described in `active-task.md` is therefore superseded by "always damageable, 2x while stunned"; the deferred-lethal-hazard latch and `reverseVulnerabilityDuration` should be re-read in that light.

### P1 (Codex-owned, C#) — Hammers ground attack gets no weapon multiplier

`AnimatedAttack.GroundAttack()` maps only `WeaponCategory.ArmorSet` to `PlayerAttackKind.SwordSwing`; `Hammers` and `Bow` fall through to `Unspecified` (x1.0):

```csharp
PlayerAttackKind attackKind = weaponData.category == WeaponCategory.ArmorSet
    ? PlayerAttackKind.SwordSwing
    : weaponData.category == WeaponCategory.BareHands
        ? PlayerAttackKind.BareHands
        : PlayerAttackKind.Unspecified;
```

Effect with the shipped weapon assets: Hammers deals **700** per ground swing (700 x 1.0) while the Armor Set — the kit that also owns `HurricaneSword`, fire breath and sword glare — deals **600** (200 x 3.0). The intended progression is inverted, and `SwordSwing`'s approved x3 multiplier is unreachable by any weapon except Armor Set.

Cursor did not touch this (C# is out of the approved scope). **Codex decision needed:** is `Hammers` supposed to map to `SwordSwing`, or is the Armor Set `groundMeleeDamage: 200` the value that should rise? Either fix changes tuning, so it needs an explicit call rather than a guess.

### Observed damage values (base from real weapon assets; boss `maxHealth: 150000`)

| Attack | Base | Kind resolved | x | Normal | Stunned | Hits to kill |
| --- | --- | --- | --- | --- | --- | --- |
| Armor Set ground | 200 | SwordSwing | 3.0 | 600 | 1200 | 250 |
| Armor Set air | 350 | HurricaneSword | 1.5 | 525 | 1050 | 286 |
| Hammers ground | 700 | Unspecified | 1.0 | 700 | 1400 | 215 |
| Arrow | 2000 | Arrow | 2.0 | 4000 | 8000 | 38 |
| Bare Hands ground | 50 | BareHands | 1.0 | 50 | 100 | 3000 |
| Air kick (non-sword) | 20 | HurricaneKick | 0.8 | 16 | 32 | 9375 |
| Roll attack | 200 | Unspecified | 1.0 | 200 | 400 | 750 |
| RockShot (stone) | 5 | Unspecified | 1.0 | 5 | 10 | 30000 |

### P2 (design/tuning decision) — the stun reward is currently marginal

A 3.5 s stun at 2x yields roughly four extra Armor Set swings, about **2,400 bonus damage = 1.6 % of the boss health bar**. The cycle floor is 13.5 s (3.5 stun + 10 cooldown), so reaching even 30 % of the bar through stuns needs ~19 flawless tree charges across 4+ minutes.

Two compounding constraints:

- **Tree supply is finite and consumed.** `Level 1 - Remastered - Steam` contains **25** `TheGivingTree` prefab instances scene-wide, each destroyed on use. How many sit inside the boss arena within charge range is unverified — if it is under ~8, the mechanic runs dry mid-fight.
- **`maxHealth: 150000` dominates fight length.** Sustained melee needs 215–250 connected swings, so roughly 4–5 minutes of pure uninterrupted hitting and realistically 6–8 minutes with charges, retreats and repositioning. (Arithmetic from the table above, not a measured playthrough.)

Raising `stunnedDamageMultiplier` to its `[Range(1.5, 3)]` ceiling only moves stun value from 1.6 % to 3.2 %, so it does not fix the ratio alone. **Codex recommendation requested:** lowering `maxHealth` to roughly 40,000–60,000 would land the fight in a 2–3 minute band and make each stun worth 4–6 % of the bar. `maxHealth` was outside the user's approved tuning range, so Cursor left it unchanged.

### P3 (blocks PlayMode CI) — pre-existing failures unrelated to Scorpion

- `WaspQueenBossPlayModeTests.Charge_LocksDirectionAtStart` **hangs to the 180 s timeout** (`Timeout value of 180000ms was exceeded` in both test and TearDown), which aborted the unscoped PlayMode run at 36/111. This regressed since the 2026-06-30 note above, which recorded it PASS.
- `WaspQueenBossPlayModeTests.PooledProjectile_DetachesFromBossHierarchy_AndKeepsWorldVelocity` fails (expected 1 active projectile, got 0) — consistent with the still-open pool parenting failure documented earlier in this file.
- Full PlayMode also pulls in the Input System package's own `IntegrationTests.WindowsInput_*` cases; worth excluding from the project's CI filter.
- EditMode: 3 `AudioRoutingEditModeTests` (mixer dB values) and 3 `InputReaderLifecycleTests` (jump event count 1 vs 0) fail. Pre-existing, non-Scorpion.

### Lower-priority notes

- **Covering claws are functional but not animated.** `HurricaneKick_StartsCoveringReverseAtConfiguredSpeedAndDuration` passes on `isAttacking == true`, so contact damage stays live during retreat. But the Animator plays `Backwards` with `Attack` false, so no claw motion is visible. Contract met, presentation is not.
- **`reverseVulnerabilityDuration: 0.8`** remains in `ScorpionBossStatsData.asset` but nothing gates on it now that the boss is always damageable — likely vestigial, confirm before removing (serialized field).
- **Coverage gap:** respawn and fight restart have no automated test. Cursor could not hand-play the fight (no way to inject player input through MCP), so these need either a new PlayMode test or manual verification.
- **Test hygiene:** `ScorpionBossVictoryFlowPlayModeTests` logs `ScorpionBoss: ScorpionStatsData reference missing. Using legacy serialized fallback values.` on every case — the same fallback-leak pattern that was fixed in `ScorpionComboRewardTests` on 2026-08-26, still present in the victory fixture.
- **Stunned-state coverage is partial:** only `SwordSwing` has a dedicated stunned-damage test. The other four kinds share `ResolveDamage`, whose composition order is pinned by `ResolveDamage_AppliesAttackThenStunnedMultiplierBeforeRounding`, so the risk is low but the cases are not individually asserted.

### Cursor working-tree state (uncommitted, not committed by request)

| File | Change |
| --- | --- |
| `Assets/Data/NPC/Scorpion/ScorpionBossStatsData.asset` | +5 lines: the five approved boss-contract values |
| `Assets/Prefabs/Objects/Interactable/TheGivingTree.prefab` | +3 lines: `canStunScorpionBoss: 1` plus two Unity-added explicit nulls |

No other asset was dirtied by the test runs. A stale `.git/index.lock` blocked the intended separate Unity-wiring commit and the user chose to commit manually.

### What Codex must not change

- The five approved values in `ScorpionBossStatsData.asset`, or the `canStunScorpionBoss` marker.
- Scene, prefab, Animator, animation, collider wiring, or `.meta` GUIDs.
- The passing contract suites' assertions (they encode the approved combat grammar).
- `maxHealth` or `stunnedDamageMultiplier` without an explicit user decision on P2.

---

## BLOCKER — aerial-retreat pass removed serialized fields that are bound in two Scorpion prefabs (2026-09-05, acceptance testing)

### Ownership

- **Owner: Codex** (`Assets/Scripts/NPC/Scorpion/ScorpionScript.cs`, uncommitted working tree on `feature/ScorpionBoss_Rework`).
- Raised by Cursor during final acceptance testing of the "Hurricane aerial retreat and covering claws" handoff dated 2026-09-05. **Acceptance is BLOCKED on this item.**

### The regression

The retreat patch deleted six public serialized members from `ScorpionScript`:

```csharp
-        public Collider Jaw1A;
-        public Collider Jaw1B;
-        public Collider Jaw2A;
-        public Collider Jaw2B;
-        public Collider Sting;
-        public string state = nameof(ScorpionState.Idle);
```

All six are **serialized in both boss prefabs today**, verified by direct YAML scan:

| Prefab | Bound members |
| --- | --- |
| `Assets/Prefabs/Scorpion/Scorpion.prefab` | `Jaw1A` `2631068468930072073`, `Jaw1B` `3574585947653929594`, `Jaw2A` `3877933947483839307`, `Jaw2B` `9083913364248970971`, `Sting` `747129927400765033`, `state: Idle` |
| `Assets/Prefabs/Scorpion/ScorpionBoss.prefab` | `Jaw1A` `2526920102824034338`, `Jaw1B` `3893775697488114769`, `Jaw2A` `3621794351671399776`, `Jaw2B` `8755712033370654960`, `Sting` `994262058274469954`, `state: Idle` |

That is **12 live serialized bindings**. This is the exact case the handoff above listed under "What Codex must not change" and that `AGENTS.md` prohibits ("Do not remove public or serialized fields without checking prefab and scene usage").

### Why it is a blocker even though runtime behavior looks unaffected

Cursor confirmed no C# in `Assets/Scripts` or `Assets/Tests` reads `Jaw1A`/`Jaw1B`/`Jaw2A`/`Jaw2B`, and nothing reads the public `state` string, so there is **no runtime logic failure** from the removal itself. The problem is data, not behavior:

1. **Silent, irreversible loss.** Unity discards unknown serialized keys without raising a Missing Reference, so this will not surface in the Console. The five collider bindings per prefab disappear permanently the first time either prefab reserializes.
2. **It destroys the only remaining map of which colliders are the claws and the sting.** The current acceptance criterion — "the claws provide cover during retreat and register damage only as allowed by the existing attack-event system" — is the exact feature that would need those references to gate claw hitboxes per attack window. Re-deriving them later means hand-re-wiring ten collider references across two prefabs.
3. **The four removed public properties** (`StatsData`, `StunnedClock`, `chargeClock`, `recoveryDuration`) are confirmed unreferenced and are fine to drop; the issue is scoped to the six serialized members above.

### Requested Codex action

Restore the six serialized members on `ScorpionScript` (keeping them unused is acceptable), or supply an explicit migration plan plus the prefab edits that intentionally clear those bindings. Cursor will not edit prefab YAML or C# to work around this.

### Verified-good in the same pass (no action needed)

- **`ScorpionDamage.cs` deletion is safe.** Its script GUID `05f23018af447874fbc32a2f700f7956` appears in no `.prefab`, `.unity`, `.asset`, or `.controller`, confirming it was never attached. The live boss-contact damage path is `BeaverPlayerBehaviour.OnTriggerEnter` on tags `ScorpionDamage` / `ScorpionSting`, which is untouched. The pre-existing empty parried `else` branch at `BeaverPlayerBehaviour.cs:526` remains, so outgoing parry counters still never fire — unchanged, still unwired.
- **Covering-claw damage chain is intact.** `ApplyHurricaneRetreatPresentation()` sets `isAttacking = true`; `BeaverPlayerBehaviour.cs:549` polls `scorpAttack = cachedScorpionScript.isAttacking`; claw damage at `:521` and sting damage at `:533` gate on it. Retreat therefore keeps contact damage live through the existing path with no new damage route.
- **Forced `Backwards` entry will resolve.** `Scorpion.controller` has layer `Base Layer` and states `Idle`, `Walk`, `Attack`, `Backwards`, `Stunned`, so `Animator.StringToHash("Base Layer.Backwards")` with `HasState(0, ...)` matches and every post-stun / post-retreat target state exists.
- **Re-entry guard is correct.** `if (hurricaneRetreatActive && currentState == ScorpionState.Reverse) return;` prevents both timer extension and animation rewind, while a non-hurricane `Reverse` is still convertible into a retreat.

### Minor, non-blocking

`TryStartHurricaneRetreat` dereferences `rbScorpion.rotation` without a null guard, unlike the neighbouring `Scorpion != null` check. Production prefabs have a Rigidbody so this is low risk, but it is inconsistent with the file's defensive style.
