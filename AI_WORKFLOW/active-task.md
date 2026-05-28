# Active Task

## Task title
- UI/UX and game-feel improvements

## Type
- Feature

## Owner
- Mixed (code complete; Unity verification pending)

## Current phase
- Verification and merge readiness

## Goal
- Improve HUD clarity, tips, objectives, footstep feedback, and carried-log game feel without replacing pause, input, movement, or combat systems.

## Scope
- All five implementation phases (HUD, tips, objectives, footsteps, log weight) plus pause-menu collectibles summary.

## Out of scope
- Scene placement of `TipTriggerZone` until Unity Editor follow-up.
- Combat-speed penalty wiring (`GroundBeat`/`AirBeat` are not attack gates).
- Dialogue, shop, animation, and input-action rewrites.

## Relevant files/assets
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`, `Carry.cs`, `SoundScripts/AudioScript.cs`
- `Assets/Scripts/UI/UIMenu.cs`, `DebugReference.cs`, `Menus/PauseCollectiblesSummary.cs`
- `Assets/Scripts/UI/Tips/*`, `Assets/Scripts/Data/Tips/*`
- `Assets/Scripts/UI/Objectives/ObjectiveTrackerPresenter.cs`
- `Assets/Scripts/Display/FootstepVfxEmitter.cs`, `Assets/Scripts/Data/Display/FootstepSurfaceEffectProfile.cs`
- `Assets/Scripts/Objects/Newbridge/NewConstructor.cs`

## Risk level
- High (prefab + runtime UI + movement penalties)

## Branch
- `cursor/feature-ui-ux-gamefeel-improvements-75e4` (local commits present; remote upstream deleted — re-push before PR)

## Codex responsibilities
- Done: script-level implementation and handoff notes.

## Cursor responsibilities
- Run Unity Play Mode checklist (below).
- Re-push branch and open/refresh PR if merging.

## Required verification
- Code-level: `dotnet build .ci/BeaverMania.CI.csproj` (requires Unity managed DLL paths on the machine; failed locally without Unity install — use Unity Editor compile or CI with Unity refs).
- Unity Editor / Play Mode: Level 1 Remastered, `PlayerCanvas`, pause/tips/objectives/footsteps/carry flows.

## Current status
- Phase 1–5 implementation complete on branch.
- Pause-menu collectibles summary implemented.
- PR review fixes applied (bridge hint, footstep material/player gating, carry rebase/penalty readout).

## Next action
- Owner: User / Unity Editor
- Action: Complete Play Mode checklist; re-push branch; merge when verified.

## Open questions
- Hide compact HUD collectibles after pause summary is accepted?
- Which scenes get authored `TipDefinition` + `TipTriggerZone` placements?

## Handoff needed
- No further code handoff unless Play Mode finds regressions.
