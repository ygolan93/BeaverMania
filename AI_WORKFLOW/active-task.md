# Active Task

## Task title
- UI/UX and game-feel improvements

## Type
- Feature

## Owner
- Mixed

## Current phase
- Phase 5 carried-log weight implementation / verification

## Goal
- Improve HUD clarity first, then phase in tips, objectives, footstep feedback, and carried-log game feel without replacing existing pause, input, movement, or combat systems.

## Scope
- Phase 5 first slice: make the existing carried-log movement penalties capped and designer-tunable without replacing player movement.

## Out of scope
- Scene placement of tip trigger zones until Unity Editor follow-up.
- Dialogue, shop, animation, and input-action rewrites.

## Relevant files/assets
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/Carry.cs`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Scripts/Data/Tips/TipDefinition.cs`
- `Assets/Scripts/Data/Tips/TipRequest.cs`
- `Assets/Scripts/UI/Tips/*`
- `Assets/Scripts/UI/UIMenu.cs`
- `Assets/Scripts/Objects/Newbridge/NewConstructor.cs`
- `Assets/Scripts/UI/DebugReference.cs`
- `Assets/Scripts/UI/Objectives/ObjectiveTrackerPresenter.cs`
- `Assets/Scripts/Data/Display/FootstepSurfaceEffectProfile.cs`
- `Assets/Scripts/Display/FootstepVfxEmitter.cs`
- `Assets/Scripts/Player/SoundScripts/AudioScript.cs`

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
- Contextual bridge construction tips are wired to intended bridge construction areas via `NewConstructor`.
- Phase 3 objective tracker presentation is implemented for review.
- Phase 4 footstep VFX is implemented through `AudioScript.Step()` for review.
- Phase 5 carried-log weight penalties are centralized and capped in `Carry`.
- Phase 5 penalties now rebase when other systems temporarily change walk/run/jump/animation values.
- Phase 5 carry component now exposes current weight penalty and guards missing references for Play Mode diagnostics.

## Next action
- Owner: Cursor / Unity Editor
- Action: Run Play Mode validation, confirm log pickup/drop/build restores movement stats correctly, and tune carried-log penalty caps.

## Open questions
- Whether collectibles should be fully moved into pause/diary UI in a later phase or remain as compact HUD counters.
- Which scene locations should receive authored `TipTriggerZone` components for bridge/log guidance.

## Handoff needed
- Yes
