# Cursor → Codex Handoff

## Task

- **Level 1 Remastered — north forest hotspot FPS fix** on branch `fix/log-carry-animation-load`.
- **Status:** Pass 1 (terrain + shadow overrides) improved reference-coordinate metrics; **task reopened (2026-06-13)** for a direction-specific **canopy fill-rate** cliff. See `codex-to-cursor.md` for Codex static review and Cursor A/B steps.

## Unity context

- Scene: `Assets/Scenes/Level 1 - Remastered - Steam.unity` (**modified**)
- Prefab(s): not modified in FPS pass (review targets: `TheGivingTree.prefab`, `ENV_TheGivingTree_TerrainTree.prefab`)
- TerrainData assets: not modified directly; tuning via Terrain component fields on 33 scene tiles
- Scripts on same branch: `Carry.cs`, `NpcDialoguePresenter.cs`, `BeaverPlayerBehaviour.cs`

## Problem summary

| Issue | Symptom | Status |
|-------|---------|--------|
| Global terrain overdraw | ~100M tris, ~10–15 FPS at bad ref coords | **Improved** (~34–39 FPS, ~5–21M tris at ref coords, 2026-06-12 MCP) |
| Canopy-facing fill-rate | Severe FPS in Edit + Play when camera faces giant canopy / open sky | **Open** — Codex hypothesis: `TheGivingTree` alpha-cutout overdraw |

## Current scene diff — terrain (all 33 tiles, verified in YAML)

| Setting | Original | Current working diff |
|---------|----------|---------------------|
| `m_TreeDistance` | 5000 | **200** |
| `m_TreeBillboardDistance` | 50 | **30** |
| `m_TreeMaximumFullLODCount` | 50 | **25** |
| `m_DetailObjectDistance` | 80 | **20** |
| `m_DetailObjectDensity` | 1 | **1** |
| `m_HeightmapPixelError` | 5 | **5** |
| `m_SplatMapDistance` | 1000 | **600** |
| `m_DrawInstanced` | 0 | **1** |

**Decorative foliage (bad bbox X 115–145, Y 70–79, Z 220–340):** `m_CastShadows: 0` on 52+ hand-placed renderers. No gameplay objects removed.

## Reference-coordinate before / after (MCP, 2026-06-12)

| Zone | FPS before | Tris before | FPS after | Tris after |
|------|------------|-------------|-----------|------------|
| Bad (119.6, 72, 266.1) | ~10.5–14.9 | ~80–105M | ~34–39 | ~5–21M |
| Good (149.1, 70, 167.5) | ~72–83 | — | ~72 | ~14M |

Does **not** certify the canopy-facing repro — that remains user-reported open.

## Prior script work on same branch (for Codex review)

1. **`Carry.cs`** — independent movement vs animation multipliers; gated penalty refresh
2. **`NpcDialoguePresenter.cs`** — shutdown guard; idle tick throttle
3. **`BeaverPlayerBehaviour.cs`** — HUD / life-icon string caching

## What Codex should do

- [x] Static review of canopy shader/material path (done — see `codex-to-cursor.md`)
- [ ] Optional: diff review of three scripts above when preparing merge
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
