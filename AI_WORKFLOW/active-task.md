# Active Task

## Task title
- UI/UX and game-feel improvements

## Type
- Feature

## Owner
- Mixed

## Current phase
- Phase 2 first implementation slice / verification

## Goal
- Improve HUD clarity first, then phase in tips, objectives, footstep feedback, and carried-log game feel without replacing existing pause, input, movement, or combat systems.

## Scope
- Phase 2 first slice: add a modular tips subsystem, runtime pause-menu Tips On/Off toggle, idle/active gating, cooldown/display-count spam control, and trigger-zone hooks.

## Out of scope
- Scene placement of tip trigger zones until Unity Editor follow-up.
- Dialogue, shop, animation, and input-action rewrites.

## Relevant files/assets
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/Carry.cs`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Scripts/Data/Tips/TipDefinition.cs`
- `Assets/Scripts/UI/Tips/*`
- `Assets/Scripts/UI/UIMenu.cs`

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
- Phase 1 HUD cleanup is implemented.
- Phase 2 tips subsystem code is implemented for review.

## Next action
- Owner: Cursor / Unity Editor
- Action: Run Play Mode validation, confirm the runtime Tips toggle appears in pause/settings, and place `TipTriggerZone` components only after layout/functionality is accepted.

## Open questions
- Whether collectibles should be fully moved into pause/diary UI in a later phase or remain as compact HUD counters.
- Which scene locations should receive authored `TipTriggerZone` components for bridge/log guidance.

## Handoff needed
- Yes
