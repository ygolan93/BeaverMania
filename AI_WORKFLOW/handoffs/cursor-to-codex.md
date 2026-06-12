# Cursor → Codex Handoff

## Task

- Context handoff after Cursor-owned session: Trader grounding fix + console error cleanup (Level 1 - Remastered - Steam). No blocking work for Codex; items below are review/cleanup candidates discovered during runtime debugging.

## Unity context

- Scene(s): `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- Prefab(s): `Assets/Prefabs/BeaverNPC/Trader.prefab`, `Assets/Prefabs/BeaverNPC/Camp Trader.prefab` (read-only, not modified); new `Assets/Prefabs/Objects/ENV_TheGivingTree_TerrainTree.prefab` (visual-only terrain tree)
- UI panel(s): none touched; presenter prompt flow exercised in Play Mode only
- Animator/animation context: trader idle animation normalizes bone pose at runtime; edit-mode bone overrides are misleading (see notes)

## What was changed in Editor

- Market Trader scene instance: root Y snapped to its carpet (world 70.899 -> 70.064); stale `mixamorig:Hips` localPosition override reverted to prefab default.
- Terrain tree prototype `TheGivingTree` (interactable prefab, wrong shader, no LODGroup) swapped on two TerrainData assets to new `ENV_TheGivingTree_TerrainTree.prefab` (same mesh/materials/scale + LODGroup, LOD0 cutoff 0.002). Skyline verified pixel-identical.
- Script edits (already applied, compile-verified clean): removed CS0414 fields `Trader.traderOfferPresentationActive`, `Trader.interactKey`, `NpcDialoguePresenter.isShopOpen` (presenter copy only; `Trader.isShopOpen` is used and kept).

## What was observed in Play Mode

- Reproduction path: teleport player within `PanelPopUp` (3 m) of each trader; check `IsInteractionAvailable` and HUD prompt.
- Expected: prompt appears in range; traders grounded.
- Actual: both pass post-fix; zero console errors/warnings after full recompile + terrain tree-database rebuild on all 33 tiles.
- Frequency: deterministic.

## Suspected code issue (review candidates, not bugs blocking release)

- `Trader.skipPressed` becomes `true` via `OnDialogueSessionClosed(ExternalCancel/SourceDisabled)` without any player interaction (observed when an unrelated respawn yanked the player out of range mid-registration). It only resets after fully leaving `PanelPopUp` range. Worth a look at whether `PlayerLeftRange` vs `SourceDisabled` close reasons are assigned correctly in `NpcDialoguePresenter.UnregisterSource` paths.
- `Assets/Scripts/DisappearOnAway.cs` is dead code: referenced by zero scenes/prefabs (GUID-verified). Also unsafe if ever revived (`Update()`-driven `SetActive` with FindGameObjectWithTag in Start, null-unsafe `Item`). Deletion candidate — but confirm with user before removing the file + .meta.
- Player camera `near clip plane` is overridden to 0.001 in the scene (PlayerPack camera). Depth-precision hazard (z-fighting at distance); consider restoring a sane near plane (0.1+) if visual artifacts are reported.

## Files/scripts likely involved

- `Assets/Scripts/NPC/Beaver/Trader.cs` (skipPressed lifecycle)
- `Assets/Scripts/UI/NpcDialoguePresenter.cs` (close-reason assignment on unregister)
- `Assets/Scripts/DisappearOnAway.cs` (dead code)

## What Cursor needs from Codex

- Nothing required. Optional: code-level review of the close-reason/skipPressed flow above; if a script change results, hand back for Play Mode verification.

## What Codex must not change

- Protected assets (scene/prefab/UI/animation): `Trader.prefab`, `Camp Trader.prefab`, `Market.prefab`, `PlayerPack-Drop and Play.prefab`, the Steam scene, both modified TerrainData assets, `ENV_TheGivingTree_TerrainTree.prefab`.
- Behavioral constraints: do not re-add removed CS0414 fields; orphaned `interactKey: 101` YAML entries in the two trader prefabs are intentionally left (Unity ignores them; cleaning them requires prefab edits, not worth the risk). Do not add a runtime ground-snap component — ruled out deliberately (static NPCs, measured-correct placement; a snap would mask future placement errors).

## Required verification after Codex changes

- Unity checks Cursor will run: trader prompt/dialogue/shop open-close in Play Mode, skipPressed reset after leaving range, console clean.
- Script-level checks Codex should run: `dotnet build .ci/BeaverMania.CI.csproj` where available; confirm no new references to removed fields.

## Risk notes

- Trader/dialogue/shop risk: low — only unused-field removals touched these scripts; presenter flow verified working in Play Mode post-change.
- Inspector-reference risk: none added; no serialized fields renamed or repurposed.
- Animation/state-machine risk: none; reminder that edit-mode bone poses can carry stale instance overrides — always verify placement in Play Mode.
- Audio-routing risk: untouched.
- Scene-integration risk: TerrainData prototype swap affects only background tiles z 1000-2000 and an empty tile; gameplay tile untouched.
