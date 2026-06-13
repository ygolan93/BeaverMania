# Active Task

## Task title

- Level 1 Remastered — north forest hotspot FPS fix (terrain + foliage + canopy fill-rate)

## Branch

- `fix/log-carry-animation-load`

## Ownership

- **Mixed.** Codex: scene-local performance-controller implementation, proxy asset creation, tests, and handoff. Cursor: Unity hierarchy verification, Play Mode / Stats proof, and any editor-only cleanup. User: final acceptance on the locked route.

## Current phase

### 2026-06-13 branch rollback

- Branch tip reset locally and on remote to `0da224eb` (`Refresh BeaverMania workflow and Cursor guidance docs`).
- Removed commits:
  - `8f0ed3cb` — player prefab carry serialization + camera lens edits
  - `38596429` — camera prefab revert follow-up
- **In scope on branch now:** `ce591d28` (Level 1 performance/canopy), `d636a525` (carry/presenter script hot-path), `0da224eb` (workflow docs).
- **Out of scope again until revisited:** `Player.prefab` / `PlayerPack-Drop and Play.prefab` edits. `Carry.cs` script migration remains; prefab serialized tuning still needs a dedicated Unity pass.
- Draft PR #180 updated to match the rolled-back branch history.

### 2026-06-13 implementation update

- Codex implemented the scene-local canopy pass: `PerformanceBudgetProfile`, `FrameBudgetGovernor`, `Level1RemasteredPerformanceController`, a Beavermania-owned culled leaves shader/material, and a decorative `TheGivingTree` proxy prefab.
- The remastered scene now contains `Perf_CanopyCluster_High`, `Perf_CanopyCluster_Proxy`, and `Level1RemasteredPerformanceController`.
- Cursor is the next owner for Unity verification, locked-route FPS proof, and any editor-only cleanup.

- **Implementation complete in Codex (2026-06-13).** The remastered scene now has a decorative canopy high/proxy split plus a scene-local adaptive performance controller with four fixed tiers.
- **Next owner:** Cursor — follow A/B steps in `AI_WORKFLOW/handoffs/codex-to-cursor.md` (canopy disable test, Scene View Effects off, decorative proxy if positive).
- **Codex handoff:** static review complete; no further script work unless Profiler shows CPU/script time.

## Problem

### Original (2026-06-12 baseline)

Standing in north forest (~119, 72, 266) yielded **~10–14 FPS** and **~100M triangles** (~21k batches). South/open reference (~149, 70, 167) was **~72–83 FPS**. Root cause: all 33 terrain tiles used **`m_TreeDistance: 5000`** plus dense grass details at **`m_DetailObjectDistance: 80`**.

### Remaining (2026-06-13 user report)

Direction-specific FPS cliff when viewing **giant `TheGivingTree` canopies against open sky**. Suspected **alpha-cutout fill-rate / overdraw** (`SyntyStudios/VegitationShader`, `Cull Off`), not scripts. Scene has **no baked occlusion** (`m_OcclusionCullingData: {fileID: 0}`).

### Current implementation (2026-06-13)

- Added `Perf_CanopyCluster_High` and `Perf_CanopyCluster_Proxy` roots in `Level 1 - Remastered - Steam`.
- Added `ENV_TheGivingTree_PerfProxy.prefab` using a Beavermania-owned culled leaves shader/material.
- Added `Level1RemasteredPerformanceController` and `Level1RemasteredPerformanceBudgetProfile`.
- Targeted eight skyline/decorative `TheGivingTree` instances in the failing sightline for high/proxy swapping.

## Reference-coordinate results (Unity MCP, 2026-06-12)

Ground-snapped teleport, **8s settle** — valid for the **original** hotspot pass; may not cover the **canopy-facing** repro:

| Zone         | Position             | FPS (before)  | Tris (before) | FPS (after pass 1) | Tris (after pass 1) |
| ------------ | -------------------- | ------------- | ------------- | ------------------ | ------------------- |
| **Bad ref**  | (119.6, 72.0, 266.1) | ~10.5–14.9    | ~80–105M      | **~34–39**         | **~5–21M**          |
| **Good ref** | (149.1, 70.0, 167.5) | ~72–83 (user) | —             | **~72**            | **~14M**            |

**Pass 1 acceptance (reference coords only):** bad ≥30 FPS ✓ | bad tris <50M ✓ | good ≥70 FPS ✓

**Pass 2 acceptance (canopy-facing repro):** **not met** — user report pending Cursor A/B.

## Scene changes — current working diff (`Level 1 - Remastered - Steam.unity`)

Applied to **all 33 Terrain** components (verified in scene YAML):

| Setting                     | Original | Current working diff |
| --------------------------- | -------- | -------------------- |
| `m_TreeDistance`            | 5000     | **200**              |
| `m_TreeBillboardDistance`   | 50       | **30**               |
| `m_TreeMaximumFullLODCount` | 50       | **25**               |
| `m_DetailObjectDistance`    | 80       | **20**               |
| `m_DetailObjectDensity`     | 1        | **1** (unchanged)    |
| `m_HeightmapPixelError`     | 5        | **5** (unchanged)    |
| `m_SplatMapDistance`        | 1000     | **600**              |
| `m_DrawInstanced`           | 0        | **1**                |

**Bad-zone decorative foliage (X 115–145, Y 70–79, Z 220–340):** `m_CastShadows: 0` on **52+** decorative `MeshRenderer` instances (including `TheGivingTree` clusters). No gameplay objects removed.

> **Note:** An intermediate Cursor pass briefly set `m_DetailObjectDistance: 0` and `m_TreeMaximumFullLODCount: 15`; the **committed working diff** uses **20 / 25** instead.

## Prior script work on same branch

- Carry animation multiplier fix (`Carry.cs`) — script only; prefab serialization not applied after rollback
- Dialogue host teardown fix (`NpcDialoguePresenter.cs`)
- FPS script quick wins (`BeaverPlayerBehaviour`, `Carry`, `NpcDialoguePresenter`)

## Verification performed

- Unity MCP Play Mode (2026-06-12): reference-coordinate sampling at bad/good refs
- Codex static review (2026-06-13): shader/material/occlusion audit — see `codex-to-cursor.md`
- Play Mode enter/exit: no cleanup errors after presenter shutdown fix

## Manual Unity verification (user / Cursor)

### Locked route

- [ ] Confirm `Perf_CanopyCluster_High`, `Perf_CanopyCluster_Proxy`, and `Level1RemasteredPerformanceController` are present and wired in the remastered scene
- [ ] Hold 5 seconds at good ref `(149.1, 70.0, 167.5)` and stay at or above `60 FPS`
- [ ] Hold 5 seconds at bad ref `(119.6, 72.0, 266.1)` and stay at or above `60 FPS`
- [ ] Hold 5 seconds at the exact canopy-facing screenshot repro transform and stay at or above `60 FPS`
- [ ] Traverse south-to-north through the hotspot for 20 seconds and never sustain below `58 FPS`
- [ ] Scene View no longer shows a large direction-specific cliff at the canopy repro

- [ ] Reproduce canopy-facing FPS cliff from user screenshot direction
- [ ] A/B: disable nearest giant `TheGivingTree` renderer(s) in sightline — FPS jump?
- [ ] A/B: Scene View **Effects** off — post/sky vs geometry split
- [ ] Reference coords still ≥30 FPS bad / ≥70 FPS good after any canopy fix
- [ ] Trader camp, log route, bridge build zone intact
- [ ] Exit Play Mode — console clean

## Deferred

- Per-tile tree distance tuning
- Selective grass re-enable on play tile only (detail distance 15–25)
- Occlusion bake for open-vista directions
- Decorative canopy proxy / LOD variant / culling-friendly leaf material
- Carry prefab serialization alignment + feel pass (after FPS stable; rolled back with `8f0ed3cb`)

## Handoff files

- Cursor → Codex: `AI_WORKFLOW/handoffs/cursor-to-codex.md`
- Codex → Cursor (active A/B): `AI_WORKFLOW/handoffs/codex-to-cursor.md`

## Suggested commit message (when pass 2 complete)

```
Reduce Level 1 north forest geometry and canopy fill cost at FPS hotspots

Lower terrain tree/detail draw, disable decorative shadows in the bad zone,
and address giant-canopy overdraw in the open-sky sightline without
south-zone regression.
```
