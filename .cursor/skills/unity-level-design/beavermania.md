# BeaverMania Level Design Constraints

Apply when working in the BeaverProject repository.

## Scope

- Scene and prefab edits require **explicit user scope** — do not casually edit `.unity` or `.prefab` YAML.
- Prefer Unity MCP (`manage_scene`, `manage_gameobject`, `manage_asset`) for inspection and controlled edits when the Editor is connected.
- Script changes stay under `Assets/Scripts/**` per project rules unless the user requests asset work.

## High-Risk Areas

Do not move or break references in these without understanding impact:

- Level 1 scenes (`Assets/Scenes/Level 1*.unity`)
- `PlayerCanvas.prefab`, player prefabs under `Assets/Prefabs/OtterPlayer/`
- `Trader.prefab`, `TraderPanel.prefab`
- Checkpoints, spawn points, bridge build zones
- NPC/trader dialogue and shop triggers

## Gameplay Systems to Preserve

- **Input**: `PlayerInputReader` — do not add parallel raw input paths.
- **Pause**: `GameTimeScaleGate` — never set `Time.timeScale` directly for menus.
- **Player**: `BeaverPlayerBehaviour` APIs for movement/combat state.
- **Trader**: Proximity prompts only; dialogue/shop must restore UI, camera, cursor, and control.

## Hierarchy Conventions

If the scene already uses BeaverMania parent groups, follow them. Do not replace with a competing structure unless requested.

Suggested prefixes align with project level-design naming:

- `ENV_` — environment decoration
- `GP_` — gameplay objects
- `COL_` — collectibles
- `BND_` — boundaries

## Verification

After scene changes:

1. Unity Play Mode — player traversal, camera, interactions, combat spacing.
2. Check trader/NPC approach and prompt visibility.
3. Confirm no blocked checkpoints, bridges, or objectives.
4. Run `dotnet build .ci/BeaverMania.CI.csproj` only if scripts were also changed.

Report: "Needs Unity Play Mode verification" when Editor testing was not performed.
