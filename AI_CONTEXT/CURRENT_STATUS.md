# Current Status

## Main Current Goal

Steam release candidate stability.

## Current Focus Areas

- Level 1 Remastered integration and stability.
- **Level 1 north forest FPS** — pass 1 done (terrain draw distances); pass 2 open (canopy fill-rate / open-sky sightline).
- **FPS script-level remediation (2026-07-03)** — Phase 1–3 implemented (see below). Phase 4 Play Mode verification pending.
- Trader UI, dialogue, and shop flow.
- Player combat polish.
- FireBreath, SwordGlare, Bow, and Stoning regression risks.
- Audio balance and SFX/music routing.

Large architecture work should be deferred unless it directly stabilizes the Steam release candidate.

## FPS Performance Remediation (2026-07-03, Cursor-owned)

Script-level hot-path optimizations across Level 1 Remastered. All changes are behavior-preserving.

**Completed:**

- **Task A — BeaverPlayerBehaviour:** 19 cached animator hash IDs (~68 string→int SetBool/GetBool replacements), `RaycastNonAlloc` for bow aim, cached `ScorpionScript` in `OnTriggerStay`, fixed double `Camera.main`.
- **Task B — NPC_Basic:** static `HashSet<NPC_Basic>` wasp registry replacing `FindGameObjectsWithTag("NPC")` O(n²), `OnEnable`/`OnDisable` lifecycle, 3 cached animator hashes (13 call sites).
- **Task C — AnimatedAttack + FootstepVfxEmitter:** `OverlapSphereNonAlloc` (32-element buffer), `RaycastNonAlloc` with closest-hit scan, cached `WaitForSeconds`.
- **Task D — Polling throttles:** `EnemyHealthBarVisibility` 0.25s with randomized offset, `UpdatePlattering` dirty-check, `BossHandler` 1s resolve throttle, `RotateUI` 2s camera retry.
- **Task E — WaspQueenPoolHub:** new `ObjectPool<T>` hub for projectiles and poison zones (mirrors `ShadowRevenantPoolHub`). `WaspQueenProjectile` and `WaspQueenPoisonZone` support pool release callbacks. Summoned wasps remain `Instantiate`/`Destroy` (NPC_Basic.Death self-destructs).
- **Task G — QualitySettings:** Standalone default changed from Ultra (index 2) to Medium (index 1). Runtime `FrameBudgetGovernor` can step up if headroom exists.

**Manual work remaining:**

- **Task F — Debris colliders (user):** Replace `MeshCollider` (convex) with `SphereCollider` on `NewHive.prefab` and ScorpionBoss explosion debris children.
- **Task E inspector wiring:** Add `WaspQueenPoolHub` component to `PF_WaspQueen_Boss` prefab, assign `config` and `poolRoot`.
- **Play Mode verification (Task H):** Full checklist in plan file `fps_perf_implementation_bb9e18cf.plan.md`.

**CI note:** `dotnet build .ci/BeaverMania.CI.csproj` fails with pre-existing `UnityEngine could not be found` assembly reference errors (affects all files including untouched ones). No new compilation errors introduced.

## Recent Branch Activity (`fix/log-carry-animation-load`, as of 2026-06-13)

- **Branch tip:** `0da224eb` after rollback; remote force-pushed. Removed prefab commits `8f0ed3cb` / `38596429` (player camera + carry serialization pass).
- **Level 1 forest FPS pass 1 (scene):** 33 terrain tiles — tree distance 5000→200, detail distance 80→20, instancing on; bad-zone decorative shadow-off. MCP: bad ref ~34–39 FPS (was ~10–15), good ref ~72 FPS at reference coordinates.
- **Level 1 canopy pass (committed):** performance controller, high/proxy clusters, culled proxy assets in `ce591d28`.
- **Level 1 forest FPS pass 2 (open):** user still reports severe FPS when camera faces giant `TheGivingTree` canopy / open sky (Edit + Play). Codex static review points to alpha-cutout fill-rate, not scripts. Cursor A/B next — see `AI_WORKFLOW/handoffs/codex-to-cursor.md`.
- **Carry animation:** independent movement vs animation multipliers in `Carry.cs` (script only; prefab tuning deferred after rollback).
- **Dialogue teardown:** `NpcDialoguePresenter` shutdown guard — Play Mode exit clean.
- **FPS script quick wins:** HUD caching (`BeaverPlayerBehaviour`), presenter idle throttle (`NpcDialoguePresenter`).
- Draft PR #180 tracks the rolled-back branch (3 feature/doc commits on top of `main`).
- Workflow: `AI_WORKFLOW/active-task.md` | handoffs in `AI_WORKFLOW/handoffs/`

## Prior branch (`fix/player-jump-sfx-throttle`, 2026-06-11)

- Jump SFX throttling and pitch-distortion fixes committed (`2ea52146`, `1a9fc660`); Play Mode QA pending.
- Level 1 Remastered scene overrides, fence/otter materials, and an OtterAnim2 `Consume` transition condition committed (`232d5edd`).
- Roll-animation pipeline prototyped and **fully reverted**.
