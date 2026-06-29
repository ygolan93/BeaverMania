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

### Level 1 final boss (scope note, 2026-06-30)

Per product direction, the Level 1 final boss remains the **Scorpion**. Swapping Level 1 to the Wasp Queen (wiring `BossHandler.bossVictorySourceBehaviour`, objective/dialogue text, scene boss instance) is intentionally **out of scope for `feature/Wasp-Queen`** and will be handled on a separate branch. The Wasp Queen ships here as a self-contained boss package validated in `ShadowRevenantTestArena`.

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
