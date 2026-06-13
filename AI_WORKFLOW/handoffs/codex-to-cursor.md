# Codex to Cursor Handoff

> **Status (2026-06-13 rollback):** Branch `fix/log-carry-animation-load` was reset to `0da224eb`. Removed player prefab commits `8f0ed3cb` and `38596429`; shared player camera prefabs match merge-base again. `Carry.cs` script changes from `d636a525` remain, but prefab serialized carry tuning is deferred. Remote branch force-pushed to match.

> **Status (2026-06-13):** Codex implemented the scene-local canopy mitigation pass for `Level 1 - Remastered - Steam`. Cursor now owns Unity-side hierarchy verification, locked-route performance proof, and gameplay sanity checks on the supported minimum-spec machine.

## Active implementation handoff — Level 1 Remastered 60 FPS floor pass

### What Codex changed

- Added `Assets/Scripts/Data/Display/PerformanceBudgetProfile.cs`
- Added `Assets/Scripts/Display/FrameBudgetGovernor.cs`
- Added `Assets/Scripts/Display/Level1RemasteredPerformanceController.cs`
- Added `Assets/Data/Display/Level1RemasteredPerformanceBudgetProfile.asset`
- Added edit-mode coverage in `Assets/Tests/EditMode/Core/GameFlow/FrameBudgetGovernorEditModeTests.cs`
- Added play-mode lifecycle coverage in `Assets/Tests/PlayMode/Core/GameFlow/Level1RemasteredPerformanceControllerPlayModeTests.cs`
- Added Beavermania-owned canopy assets:
  - `Assets/Materials/TheGivingTree/BeavermaniaVegetationCulled.shader`
  - `Assets/Materials/TheGivingTree/TheGivingTree_Leaves_Culled.mat`
  - `Assets/Prefabs/Objects/ENV_TheGivingTree_PerfProxy.prefab`
- Updated `Assets/Scenes/Level 1 - Remastered - Steam.unity` with:
  - `Perf_CanopyCluster_High`
  - `Perf_CanopyCluster_Proxy`
  - `Level1RemasteredPerformanceController`
  - eight decorative `TheGivingTree` full instances reparented under the high cluster
  - eight matching proxy instances under the proxy cluster

### Controller/runtime design now in scene

- `PerformanceBudgetProfile` is scene-local config with a 60-frame rolling window, `17.2 ms` degrade threshold, `14.9 ms` recover threshold, and `1.0 s` tier cooldown.
- `FrameBudgetGovernor` is the pure C# tier-state core.
- `Level1RemasteredPerformanceController` snapshots incoming quality/terrain state, starts at `Balanced`, applies the ordered tiers, and restores previous global settings on disable/destroy.
- The controller now also handles programmatic setup safely before first enable so the lifecycle tests and runtime-injected setups do not restore zeroed terrain data.

### Tier values implemented

| Tier | Quality | High cluster | Proxy cluster | treeDistance | billboardDistance | max full LOD | detail distance | detail density | pixel error | splat distance | shadow distance |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| High | Medium | on | off | 200 | 30 | 25 | 20 | 1.0 | 5 | 600 | 40 |
| Balanced | Medium | off | on | 180 | 28 | 16 | 12 | 0.75 | 8 | 500 | 30 |
| Fast | Fast | off | on | 140 | 22 | 10 | 4 | 0.5 | 12 | 400 | 18 |
| CanopySafe | Fast | off | off | 110 | 18 | 6 | 0 | 0.25 | 15 | 300 | 12 |

### Decorative canopy cluster Codex targeted

Only skyline/decorative full `TheGivingTree` instances in the failing sightline were moved into the high/proxy swap path:

- `TheGivingTree (12)` at `(120.46, 74.75, 300.71)`
- `TheGivingTree (3)` at `(129.05, 73.40, 290.68)`
- `TheGivingTree (1)` at `(131.38, 74.75, 275.44)`
- `TheGivingTree` at `(132.98, 70.02, 252.57)`
- `TheGivingTree (8)` at `(137.42, 69.81, 246.45)`
- `TheGivingTree (9)` at `(142.10, 71.00, 260.50)`
- `TheGivingTree (2)` at `(148.11, 69.57, 243.81)`
- `TheGivingTree (10)` at `(153.32, 69.59, 250.37)`

### What Cursor must verify in Unity

1. Open `Level 1 - Remastered - Steam` and confirm the new root objects exist and the eight full trees / eight proxies are parented to the expected clusters.
2. Confirm `Level1RemasteredPerformanceController` has:
   - the new profile asset assigned
   - the terrain cache populated
   - `Perf_CanopyCluster_High` and `Perf_CanopyCluster_Proxy` wired
3. Use the locked acceptance route only:
   - hold 5 seconds at the good ref `(149.1, 70.0, 167.5)`
   - hold 5 seconds at the bad ref `(119.6, 72.0, 266.1)`
   - hold 5 seconds at the exact canopy-facing screenshot repro transform
   - traverse south-to-north through the hotspot for 20 seconds with normal movement
4. Pass only if Play Mode never drops below `60 FPS` on the three holds and never sustains below `58 FPS` during traversal.
5. Re-run gameplay sanity after the pass:
   - trader camp visibility
   - log pickup/build route
   - bridge zone
   - clean Play Mode exit

### Risks / limitations

- Codex could not use Unity MCP in this session; the scene-side verification route still belongs to Cursor/Unity.
- `dotnet build .ci/BeaverMania.CI.csproj` is blocked on this Windows machine because the project references `/opt/unity/...` assemblies that are not present here.
- The scene YAML now contains the intended proxy/high-cluster structure, but Unity Editor should be treated as the source of truth for hierarchy reconstruction and serialized binding.
- Needs Unity Play Mode verification.

> **Status (2026-06-13):** Reopening the **Level 1 forest FPS fix** task. Cursor's 2026-06-12 terrain/shadow pass improved the original hotspot, but the user still reports a severe FPS drop in both Edit Mode and Play Mode when the camera faces the giant canopy / open-sky direction from the latest screenshot. The diagnostic handoff below is the active item. The NPC presenter refactor section is historical context only.

## Active diagnostic handoff — Level 1 Remastered canopy-facing FPS drop

### What Codex reviewed

- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/cursor-to-codex.md`
- Current working diff for `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- `Assets/Prefabs/Objects/Interactable/TheGivingTree.prefab`
- `Assets/Prefabs/Objects/ENV_TheGivingTree_TerrainTree.prefab`
- `Assets/Materials/TheGivingTree/TheGivingTree_Leaves.mat`
- `Assets/Materials/TheGivingTree/TheGivingTree_Bark.mat`
- `Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_VegitationShader.shader`

### Static findings

1. **Current working scene diff** (verified in YAML; also recorded in `active-task.md` and `cursor-to-codex.md`):
   - all 33 terrains: `m_TreeDistance: 200`, `m_TreeMaximumFullLODCount: 25`, `m_DetailObjectDistance: 20`
   - unchanged from original: `m_DetailObjectDensity: 1`, `m_HeightmapPixelError: 5`
   - an intermediate Cursor pass briefly used `15` / `0` for max LOD / detail distance; that is **not** what the working diff contains

2. `TheGivingTree` canopy materials are on **`SyntyStudios/VegitationShader`**, which is:
   - `TransparentCutout`
   - `Cull Off`
   - wind-deformed in the vertex stage
   - shadow-capable

3. `Level 1 - Remastered - Steam.unity` still has **no baked occlusion data** (`m_OcclusionCullingData: {fileID: 0}`).

4. The current scene diff already adds many `m_CastShadows: 0` overrides on decorative instances, including `TheGivingTree` renderers. If the camera-facing FPS cliff still reproduces, that pushes suspicion away from shadow-map cost and toward **main-pass canopy overdraw / fill-rate**.

### Root-cause hypothesis

The unresolved hotspot is most likely **screen-space overdraw from giant `TheGivingTree` canopies against the bright sky**, not scripts and not the already-disabled shadow pass. The screenshot direction is consistent with a canopy-dominant fill problem: large alpha-cutout leaf coverage, double-sided rendering, open sky behind it, and no baked occlusion.

### What Cursor should A/B next

- Reproduce at the reported facing direction and watch Stats/Profiler.
- Temporarily disable the nearest giant `TheGivingTree` renderer(s) in that sightline.
  - If FPS jumps immediately, the canopy is the dominant cost.
- Repeat once with Scene View **Effects** off.
  - If Scene View recovers mostly with Effects off, split post-processing/sky from geometry cost.
- If the canopy A/B is positive, prefer a **decorative proxy fix** over more global terrain tuning:
  - simpler decorative prefab or LOD variant for the skyline trees
  - leaf material/proxy that uses backface culling
  - fewer overlapping giant canopies in the exact bad sightline

### What Codex is not recommending yet

- No more script work unless Profiler shows CPU/script time.
- No global vegetation shader edit yet; `Cull Off` is suspicious, but changing the shader without a tight A/B is too broad for this repo.
- No broad scene retune based only on the older 2026-06-12 handoff values; those values are stale relative to the current working scene diff.

## Task

- Shared NPC prompt presenter and dialogue-session refactor for trader, legacy NPC dialogue, and boss dialogue flows

## Codex scope completed

- Added repo-local embedded skills:
  - `.agents/skills/unity-ui-controller/SKILL.md`
  - `.agents/skills/csharp-event-driven-ui/SKILL.md`
  - `.agents/skills/safe-refactor/SKILL.md`
- Added `INpcDialogueInteractionSource` and `NpcDialogueSessionContext` so NPCs report availability and session payloads to a shared presenter.
- Reworked `NpcDialoguePresenter` into the single runtime owner for prompt selection, interact handling, dialogue binding, shop swapping, and cleanup.
- Updated `Dialogue` so shared buttons route through the presenter and legacy `Trader` lookup is only a fallback path.
- Migrated `Trader` to the shared presenter contract while preserving public/serialized compatibility shims such as `DialoguePanel`, `Shop`, and `activateSkip()`.
- Converted `ActivateDialogue` into a legacy-content adapter that no longer toggles scene UI directly.
- Added `BossDialogueInteractionSource` and narrowed `BossHandler` so boss dialogue enters the same shared presenter flow before combat starts.
- Narrowed trader camera/presentation re-entry in `BeaverPlayerBehaviour` so the presenter path is authoritative.

## Files changed by Codex

- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- `.agents/skills/unity-ui-controller/SKILL.md`
- `.agents/skills/csharp-event-driven-ui/SKILL.md`
- `.agents/skills/safe-refactor/SKILL.md`
- `Assets/Scripts/UI/INpcDialogueInteractionSource.cs`
- `Assets/Scripts/UI/NpcDialogueSessionContext.cs`
- `Assets/Scripts/UI/NpcDialoguePresenter.cs`
- `Assets/Scripts/UI/Dialogue.cs`
- `Assets/Scripts/NPC/Beaver/Trader.cs`
- `Assets/Scripts/UI/ActivateDialogue.cs`
- `Assets/Scripts/Player/BossDialogueInteractionSource.cs`
- `Assets/Scripts/Player/BossHandler.cs`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`

## New or changed serialized fields

- `NpcDialoguePresenter`
  - `dialogue`
  - `promptRoot`
  - `promptText`
  - `dialogueRoot`
- `Dialogue`
  - `ShopButton`
- `Trader`
  - existing fields preserved
  - additive `dialogueData`
  - additive `interactionPromptMessage`
- `ActivateDialogue`
  - additive `interactionPromptMessage`
  - additive `interactionRange`
- `BossDialogueInteractionSource`
  - `bossHandler`
  - `legacyBossPanel`
  - `promptMessage`

No serialized or public fields were removed in this pass.

## Unity wiring Cursor owns

- `PlayerCanvas.prefab`
  - Add a dedicated prompt widget under the HUD.
  - Assign the prompt widget root to `NpcDialoguePresenter.promptRoot`.
  - Assign the prompt text label to `NpcDialoguePresenter.promptText`.
  - Keep `NPC_DialoguePanel` as the shared session root.
  - Inside `NPC_DialoguePanel`, keep dialogue content and shop content as sibling branches so closing shop returns to the same shared session path.
- Shared dialogue buttons
  - `Continue` -> shared `Dialogue.Continue`
  - `Skip` -> shared `Dialogue.EndConversation` or the existing shared close path
  - `Shop` -> shared `Dialogue.OpenShop`
  - shop close button -> shared `Dialogue.CloseShop`
  - Remove per-NPC panel/button routing where the shared panel is now authoritative.
- NPC prefab and scene references
  - Trader-family NPCs should point `DialoguePanel` and optional `Shop` references at shared `PlayerCanvas` children, not scene-owned inline bars.
  - Legacy non-shop NPCs using `ActivateDialogue` can keep their old dialogue object only as a content source until fully migrated.
  - Boss flow should keep the old boss dialogue content as source data only; do not leave direct panel-open behavior active in parallel.

## Expected runtime flow after wiring

1. Source enters range and registers with `NpcDialoguePresenter`.
2. Presenter selects the nearest available source and shows a single prompt.
3. Interact input is consumed centrally by the presenter.
4. Presenter binds a `NpcDialogueSessionContext` into the shared `Dialogue`.
5. Source callback applies NPC-specific camera/cursor presentation.
6. Shared buttons drive continue, shop open, shop close, and end/skip through the presenter only.
7. Leaving range, disabling the source, skip, dialogue completion, or external close all go through presenter cleanup and source callbacks.

## What Cursor should test in Play Mode

- Market trader:
  - one prompt only
  - interact opens shared panel
  - shop opens and closes without switching back to a legacy inline panel
  - skip restores cursor and camera cleanly
- Arms dealer:
  - same shared prompt path
  - correct dialogue content
  - correct shop content
- One legacy non-shop NPC using `ActivateDialogue`:
  - shared prompt
  - shared panel opens with legacy lines
  - no shop button shown
- Boss dialogue:
  - shared prompt/session opens
  - skip or completion still transitions into boss combat correctly
- Overlap case:
  - two NPC sources in range still show exactly one prompt, with nearest source winning
- External close case:
  - guard disable or `DoorShut` path closes the session cleanly with no orphaned UI
- Fallback case:
  - missing prompt widget still uses objective override
  - missing dialogue data logs once and fails closed with no null refs

## Verification completed by Codex

- `git diff --check` passed aside from line-ending normalization warnings from Git.
- Manual code-path review completed for presenter registration, session binding, shop swap, compatibility shims, and first-open shared dialogue initialization.

## Verification blocked in this environment

- `dotnet build .ci/BeaverMania.CI.csproj` is blocked on this Windows machine because the Unity-managed assembly references expected by the CI project are not available here.
- No Unity Editor or Play Mode proof was performed by Codex.

## Constraints Cursor should preserve

- Do not remove legacy panels or serialized fields in the same pass as wiring.
- Do not rename `MonoBehaviour` classes or move prefab-bound scripts.
- Keep `activateSkip()` and other legacy button-entry shims alive until each migrated NPC family is verified in Play Mode.
- Keep Cursor as owner of prefab, scene, button, and serialized-reference changes.

## Known limitations

- Prompt and shared panel behavior are code-complete but not Unity-verified yet.
- Legacy traders and NPCs still depend on prefab/scene references being repointed to the shared `PlayerCanvas` structure.
- Needs Unity Play Mode verification.
