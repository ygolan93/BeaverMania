# Active Task

## Task title
- Level 1 FPS Pass 2 — Area Hotspots to 60 FPS

## Type
- Mixed (profiling + scripts + ProjectSettings + scene/prefab + Editor tools)

## Owner
- **Cursor** — implementation complete pending Unity verification
- **User** — Play Mode + Standalone build validation on target PC

## Current phase
- Pass 2D — **Needs Unity Play Mode verification**

## Goal
- Sustained ≥60 FPS across Level 1 Remastered demo route.

## Zone cost map (Pass 2A)

| Zone | Bottleneck class | Baseline metrics | Pass 2 target |
|------|------------------|------------------|---------------|
| **Global** | GPU draw calls | 2,802 draw / 2,594 batches / ~10.9M tris | ≥30% draw-call reduction in village |
| **P0 Village/market** | Static batching + materials | Markets non-static | Static prefabs + scene instances |
| **P1 Open terrain** | Trees/detail/basemap | tree 5000 / detail 80 / splat 1000 | tree 2500 / detail 40 / splat 600 |
| **Hive/wasp** | CPU Instantiate burst | 30 wasps same frame | 6 per batch, 0.12s interval |
| **Boss arenas** | Shadows + skinned meshes | Medium shadows 2 cascades | 1 cascade, 2 pixel lights |
| **Spawn/coins** | Script Update/GC | ~25 KB GC/frame (Pass 1 mitigated) | Maintain Pass 1 wins |

### Top 3 ranked zones
1. **P0 Village/market** — GPU draw calls (static batching underused)
2. **P1 Open terrain** — foliage/detail distance
3. **P2 Global occlusion/LOD** — occlusion data not baked (`m_OcclusionCullingData: {fileID: 0}`)

## Pass 2 deliverables (implemented)

### Pass 2B — Scripts / settings
- `PerformanceBootstrap.cs` — Medium GPU tuning at runtime, dev low-FPS logging (draw calls in Editor), quality fallback to Fast after 4s <45 FPS
- `TerrainPerformanceBootstrap.cs` — runtime terrain distance cap on all tiles
- `Static_Hive.cs` — staggered wasp spawn (6/batch, 0.12s, max 30)
- `GameplayTriggerVisualBootstrap.cs` — spread trigger mesh hide across frames (96/frame)
- `QualitySettings.asset` — Medium: pixel lights 2, shadow cascades 1, softVegetation off

### Pass 2C — Scene / prefab
- **P0:** Marked static on market prefabs (`Bakery_market_with_food`, `Fish_market_with_sections`, `Meat_market_with_objects`, `lamp_post`) — all `m_StaticEditorFlags: 2147483647`
- **P1:** Scene terrain tuning on 33 tiles (tree/detail/basemap/heightmap pixel error)
- **P2:** `LevelPerformanceEditorTools.cs` — menu items for village static pass, terrain tune, occlusion bake, LOD candidate report

### Pass 2D — Verification status
- Unity MCP reprofile **blocked** (Editor busy/timeouts during occlusion bake attempt)
- `dotnet build .ci/BeaverMania.CI.csproj` **failed locally** (Unity managed assemblies unavailable — environment issue)
- **Needs user Play Mode** on demo route + Standalone Windows build smoke test

## Manual Unity steps (required)

1. Let Unity finish reimport/compile after pull.
2. Open `Level 1 - Remastered - Steam.unity` and **Save** (terrain + prefab static changes).
3. Run **Beavermania → Performance → Mark Village Environment Static** (Eden fence/wall instances in scene).
4. Run **Beavermania → Performance → Bake Occlusion Culling** (Edit Mode; wait for bake; save scene).
5. Run **Beavermania → Performance → Report Large Props Missing LOD** — add LODGroups manually for logged props.
6. Play Mode: walk spawn → village → bridge → camp → hive → bosses; confirm FPS ≥60 and no regressions.

## Play Mode checklist
- [ ] Spawn 3 min: FPS ≥60
- [ ] Village/market: FPS ≥60; shadows/LOD OK
- [ ] Bridge + camp: FPS ≥60
- [ ] Hive destruction: no multi-second freeze
- [ ] Boss arenas: FPS ≥60 in combat
- [ ] Trader open/close: FPS ≥60
- [ ] Standalone build at Medium quality matches Editor

## Result
- **Implementation complete; acceptance criteria pending user Play Mode + build verification**
