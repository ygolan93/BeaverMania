# Unity Prompt Builder - examples

## Example 1: Vague bug report -> focused Plan prompt

**User input:** "Pause menu doesn't unpause sometimes after dying."

**Your output:**

```markdown
Recommended mode: Plan

Reason:
Pause, death, and time-scale interact across multiple systems (`GameTimeScaleGate`, lose/restart flow). Root cause is unclear without tracing freeze-token ownership first.

Cursor Prompt:
---
## Goal
Fix the case where gameplay remains frozen after death when the player returns from the pause/lose UI.

## Context
BeaverMania uses `GameTimeScaleGate.SetFreeze(token, true/false)` for pause and lose screens. Death and restart likely register freeze tokens that are not cleared on the path back to gameplay.

## Current behavior
After dying, opening pause or lose UI and resuming sometimes leaves the game frozen. Repro is intermittent; the user reports it "sometimes" after death -> UI -> continue.

## Expected behavior
After any death -> UI -> resume/restart path, all gameplay freeze tokens from pause/lose/death are cleared and `GameTimeScaleGate` returns to normal gameplay.

## Files / Areas to Inspect
- Scripts: `Assets/Scripts/Core/` time-scale gate, pause/lose UI controllers, player death/restart handlers
- Prefabs: pause menu, lose screen (only if wiring is part of the failure)
- Scenes: gameplay scene with checkpoint/restart flow (if referenced)
- Out of scope: unrelated combat, trader, or audio systems

## Constraints
- Do not set `Time.timeScale` directly for pause/lose; use `GameTimeScaleGate` only.
- Minimal diff; do not rewrite UI layout or unrelated systems.
- Preserve serialized inspector references; list any prefab field changes in the Manual inspector checklist.
- Treat architecture or cross-system refactor questions as Codex-owned if they appear.

## Required Changes
1. Trace all `GameTimeScaleGate.SetFreeze` call sites for pause, lose, death, and restart.
2. Identify which token or path remains set on the failing route.
3. Fix the asymmetric clear path without adding a blanket "clear all" fallback.
4. Add a targeted guard or warning only if it matches the real leak.

## Validation
- Command checks: `dotnet build .ci/BeaverMania.CI.csproj` when available
- Manual Unity checks: die -> pause/lose UI -> resume; die -> restart; normal pause/unpause
- Regression checks: normal pause flow still works, restart still restores gameplay

## Manual inspector checklist (if needed)
- [ ] Pause/Lose prefab fields referencing `GameTimeScaleGate` or related controllers are still assigned

## Report Back
- Files changed
- Summary of changes
- Validation performed
- Risks / manual follow-up
---
```

## Example 2: Feature request with evidence -> focused Agent prompt

**User input:** "Shop buy button is grayed out even with enough coins." Screenshot shows `TraderUI`, coin count 50, price 20.

**Your output:**

```markdown
Recommended mode: Agent

Reason:
Single trader/shop path with observable state (coins >= price) but buy disabled. Likely a localized validation or event-refresh bug.

Cursor Prompt:
---
## Goal
Enable the shop Buy button when the player has enough currency for the selected item.

## Context
Trader/shop UI currently shows 50 coins and a selected item priced at 20, but the Buy button remains disabled.

## Current behavior
Buy stays non-interactable despite sufficient coins. Assumption: affordability refresh does not run on coin change or item selection change.

## Expected behavior
When `playerCoins >= itemPrice` for the selected catalog entry, Buy is interactable; otherwise it is disabled. The state updates immediately on coin gain and item selection change.

## Files / Areas to Inspect
- Scripts: trader/shop UI controller, affordability logic, currency source, selection-change handling
- Prefabs: trader shop canvas prefab (only if button wiring is wrong)
- Scenes: none unless the shop is scene-wired
- Out of scope: combat, movement, unrelated HUD systems

## Constraints
- Do not rewrite the shop system; fix refresh or wiring only.
- Preserve serialized fields and button/list references.
- No multitask split; trader/shop flow shares prefab and UI references.

## Required Changes
1. Find where Buy `interactable` is set and list the conditions blocking enable.
2. Ensure coin-balance and item-selection changes both drive a single affordability refresh path.
3. Fix any stale state, wrong currency source, or missing event subscription.
4. Add a warning if the currency or catalog reference is missing instead of failing silently.

## Validation
- Command checks: `dotnet build .ci/BeaverMania.CI.csproj` when available
- Manual Unity checks: 50 coins / 20 price item enables Buy; dropping below price disables; gaining coins re-enables without reopening
- Regression checks: purchasing still deducts currency correctly and disables Buy when funds become insufficient

## Manual inspector checklist (if needed)
- [ ] Trader prefab -> Buy Button reference assigned
- [ ] Currency source field points to the correct player wallet component

## Report Back
- Files changed
- Summary of changes
- Validation performed
- Risks / manual follow-up
---
```
