# Codex → Cursor Handoff

## Task
- UI/UX and game-feel improvements — Phase 1 HUD cleanup, Phase 2 tips, Phase 3 objective tracker

## Code changes made
- Compact collectible/ammo/log HUD text values to numbers only so existing icons carry the label meaning.
- Move `StaminaBar` from the top-left HUD cluster to bottom-left anchored screen space in `PlayerCanvas.prefab`.
- Add code-only tips subsystem with `TipDefinition`, settings persistence, cooldown/display-count/priority gating, idle/active gating, trigger-zone hooks, and an animated runtime tip card.
- Add a runtime Tips On/Off toggle to the existing pause menu through `UIMenu` startup without editing the pause menu prefab hierarchy.
- Add contextual bridge construction tips through `NewConstructor` so bridge/log guidance appears only near intended bridge construction areas.
- Add an objective tracker presenter that formats current objective text as a compact bullet list and keeps trader/objective override text flowing through the existing `DebugReference` path.
- Record mixed ownership and verification requirements in `AI_WORKFLOW/active-task.md`.

## Files changed
- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`
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

## New or changed serialized fields
- New serialized tuning fields on `TipDefinition`, `TipsService`, `PlayerIdleStateTracker`, `TipCardPresenter`, and `TipTriggerZone`.
- No existing serialized fields were renamed or removed.

## Inspector assignments required
- None expected from script changes.
- Verify existing `DebugReference` text references and `Health_Bar_Script` slider references remain assigned in the active scenes.
- Phase 2 scene authoring requires creating `TipDefinition` assets and assigning them to `TipTriggerZone` components in intended areas.

## Prefab/scene/UI/Animator follow-up required
- Inspect `PlayerCanvas.prefab` in Unity Editor and confirm `StaminaBar` is readable at bottom-left across common resolutions.
- Confirm top-right collectible/ammo/log counters still align with their icons after label removal.
- Decide in Phase 2/3 whether collectibles should move fully into pause/diary UI or remain compact HUD counters.
- Confirm the runtime `Tips: On/Off` toggle appears in the pause menu and does not overlap volume/restart controls.
- Confirm bridge tips appear near `NewConstructor` trigger areas and do not appear globally when the player is away from bridge constructors.
- Confirm the objective tracker appears middle-left, is readable, and formats current objectives as a compact list.
- Add `TipTriggerZone` components near intended bridge/log/tutorial areas after validating the runtime card and toggle.

## What Cursor should test in Play Mode
- Reproduction path: open Level 1 Remastered, enter Play Mode, move/jump/sprint, pick up coins/nuts/apples/goblets/arrows/logs, open/close pause, toggle Tips off/on.
- Functional checks: health stays top-left, stamina appears bottom-left and updates, counter text shows icon + number only, objective tracker appears middle-left, log count shows `N/9`, 9-log carry state shows `LCtrl+RMB to build`, pause menu still opens/closes, Tips toggle persists across pause reopen, bridge tips appear only near bridge construction triggers.
- Negative/regression checks: no `NullReferenceException`, trader/lose UI still locks input/cursor, objective text still updates, tips do not show while disabled, paused, in trader UI, or away from bridge construction areas.

## What Cursor should not change
- Do not edit scenes or additional prefabs for later phases until Phase 1 layout is accepted.
- Do not rewrite input, pause, movement, combat, or objective systems as part of this handoff.
- Do not scatter bridge/log tips globally; place `TipTriggerZone` only at intended bridge-building/tutorial spaces.

## Known limitations
- Unity Play Mode was not available in Cloud, so layout and serialized-reference behavior need Editor verification.
- A compact bridge-build instruction remains in the 9/9 log HUD as a fallback while contextual bridge-area tips are verified.
- Additional authored `TipDefinition` assets or scene `TipTriggerZone` placements have not been added yet.
- The objective tracker runtime container is created from code; visual padding/background must be checked in the Editor.

## Risk notes
- Enemy/NPC behavior risk: Low for Phase 1.
- Interaction/dialogue/shop flow risk: Medium because `PlayerCanvas.prefab` and pause menu runtime UI are shared with dialogue/shop/pause flows.
- Player combat/input risk: Low; input is read for gating only, not rebound or intercepted.
- Pooling/performance risk: Low; bridge tips use cooldown/attempt throttling and do not spawn particles or pooled objects.
