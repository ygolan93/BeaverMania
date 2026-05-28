# Codex → Cursor Handoff

## Task
- UI/UX and game-feel improvements — Phase 1 HUD cleanup

## Code changes made
- Compact collectible/ammo/log HUD text values to numbers only so existing icons carry the label meaning.
- Move `StaminaBar` from the top-left HUD cluster to bottom-left anchored screen space in `PlayerCanvas.prefab`.
- Record mixed ownership and verification requirements in `AI_WORKFLOW/active-task.md`.

## Files changed
- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/Carry.cs`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`

## New or changed serialized fields
- None.

## Inspector assignments required
- None expected from script changes.
- Verify existing `DebugReference` text references and `Health_Bar_Script` slider references remain assigned in the active scenes.

## Prefab/scene/UI/Animator follow-up required
- Inspect `PlayerCanvas.prefab` in Unity Editor and confirm `StaminaBar` is readable at bottom-left across common resolutions.
- Confirm top-right collectible/ammo/log counters still align with their icons after label removal.
- Decide in Phase 2/3 whether collectibles should move fully into pause/diary UI or remain compact HUD counters.

## What Cursor should test in Play Mode
- Reproduction path: open Level 1 Remastered, enter Play Mode, move/jump/sprint, pick up coins/nuts/apples/goblets/arrows/logs, open/close pause.
- Functional checks: health stays top-left, stamina appears bottom-left and updates, counter text shows icon + number only, log count shows `N/9`, 9-log carry state shows `LCtrl+RMB to build`, pause menu still opens/closes.
- Negative/regression checks: no `NullReferenceException`, trader/lose UI still locks input/cursor, objective text still updates, bridge construction hinting is not treated as complete until the tips subsystem phase.

## What Cursor should not change
- Do not edit scenes or additional prefabs for later phases until Phase 1 layout is accepted.
- Do not rewrite input, pause, movement, combat, or objective systems as part of this handoff.

## Known limitations
- Unity Play Mode was not available in Cloud, so layout and serialized-reference behavior need Editor verification.
- A compact bridge-build instruction remains in the 9/9 log HUD until Phase 2 can replace it with contextual tips.

## Risk notes
- Enemy/NPC behavior risk: Low for Phase 1.
- Interaction/dialogue/shop flow risk: Medium because `PlayerCanvas.prefab` is shared with dialogue/shop/pause panels.
- Player combat/input risk: Low; no input/combat logic changed.
- Pooling/performance risk: None in Phase 1.
