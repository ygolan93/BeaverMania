# Active Task

## Task title
- UI/UX and game-feel improvements

## Type
- Feature

## Owner
- Mixed

## Current phase
- Phase 1 implementation / verification

## Goal
- Improve HUD clarity first, then phase in tips, objectives, footstep feedback, and carried-log game feel without replacing existing pause, input, movement, or combat systems.

## Scope
- Phase 1: compact always-visible HUD counters, keep health top-left, move stamina bottom-left, and avoid broad gameplay rewrites.

## Out of scope
- Phase 2+ systems until Phase 1 is reviewed and Unity Play Mode verified.
- Scene, dialogue, shop, animation, and input-action rewrites.

## Relevant files/assets
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/Carry.cs`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`

## Risk level
- High

## Branch
- `cursor/feature-ui-ux-gamefeel-improvements-75e4`

## Codex responsibilities
- Keep script changes narrow and compile-safe.
- Record prefab/Unity risks and required Play Mode verification.

## Cursor responsibilities
- Verify HUD layout in Unity Editor across common resolutions.
- Verify pause/menu/input/gameplay flows in Play Mode.

## Required verification
- Code-level: `dotnet build .ci/BeaverMania.CI.csproj`
- Unity Editor / Play Mode: open Level 1 Remastered, inspect `PlayerCanvas`, confirm HUD layout and existing UI flows.

## Current status
- Phase 1 script and prefab changes are implemented for review.

## Next action
- Owner: Cursor / Unity Editor
- Action: Run Play Mode validation and adjust prefab layout if needed.

## Open questions
- Whether collectibles should be fully moved into pause/diary UI in a later phase or remain as compact HUD counters.

## Handoff needed
- Yes
