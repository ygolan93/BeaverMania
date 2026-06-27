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
