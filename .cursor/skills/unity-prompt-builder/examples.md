# Unity Prompt Builder — examples

## Example 1: Vague bug report → focused Agent prompt

**User input:** “Pause menu doesn’t unpause sometimes after dying.”

**Your output:**

```markdown
Recommended mode: Plan

Reason:
Pause, death, and time-scale interact across multiple systems (`GameTimeScaleGate`, lose/restart flow). Root cause is unclear without tracing freeze tokens.

Cursor Prompt:
---
## Goal
Fix the case where gameplay remains frozen after death when the player returns from the pause/lose UI.

## Context
BeaverMania uses `GameTimeScaleGate.SetFreeze(token, true/false)` for pause and lose screens. Death and restart likely register freeze tokens that are not cleared on the path back to gameplay.

## Current behavior
After dying, opening pause or lose UI and resuming sometimes leaves the game frozen (movement/input dead, time not advancing). Repro is intermittent; user reports it “sometimes” after die → UI → continue.

## Expected behavior
After any death → UI → resume/restart path, all gameplay freeze tokens from pause/lose/death are cleared and `GameTimeScaleGate` returns to unpaused gameplay.

## Scope — inspect or modify
- Scripts: `Assets/Scripts/Core/` (time scale gate), pause/lose UI, player death/restart handlers
- Prefabs: pause menu, lose screen (if wired there)
- Scenes: gameplay scene with checkpoint/restart (if referenced)
- Out of scope: unrelated combat, trader, audio mixers

## Constraints
- Do not set `Time.timeScale` directly for pause/lose; use `GameTimeScaleGate` only.
- Minimal diff; do not rewrite UI layout or unrelated systems.
- Preserve serialized inspector references; list any prefab field changes in Manual inspector checklist.
- Default edits under `Assets/Scripts/**` unless wiring fix requires prefab.

## Implementation steps
1. Trace all `GameTimeScaleGate.SetFreeze` call sites for pause, lose, and death/restart.
2. Identify which token(s) remain set on the failing path (log or comment which path leaks).
3. Ensure death/restart/pause-exit paths symmetrically clear tokens (OnDisable, scene reload, or explicit clear).
4. Add a defensive clear only if it matches the real leak (no blanket “clear all” that breaks intentional multi-freeze).

## Acceptance criteria
- [ ] Die → lose/pause UI → resume: movement and time advance every time (10 consecutive tries).
- [ ] Normal pause (Esc) still freezes and unfreezes without affecting death flow.
- [ ] No direct `Time.timeScale` assignments added for pause/lose.

## Play Mode validation checklist
- [ ] Die, open pause, close pause — gameplay runs.
- [ ] Die, use restart from lose UI — gameplay runs from checkpoint/spawn.
- [ ] Pause during live gameplay — still works.
- [ ] Watch Console for warnings about missing gate references.

## Manual inspector checklist (if needed)
- [ ] (Fill after investigation) Pause/Lose prefab → components referencing `GameTimeScaleGate` or UI controllers
---
```

---

## Example 2: Feature request with media → Agent prompt

**User input:** “Shop buy button grayed out even with enough coins” + screenshot showing `TraderUI` and coin count 50, price 20.

**Your output:**

```markdown
Recommended mode: Agent

Reason:
Single UI/commerce path with observable state (coins ≥ price) but buy disabled; likely one validation or event wiring bug in trader/shop scripts.

Cursor Prompt:
---
## Goal
Enable the shop Buy button when the player has enough currency for the selected item.

## Context
Trader/shop UI (`TraderUI`). Screenshot: coin display 50, selected item price 20, Buy button appears disabled/interactable=false.

## Current behavior
Buy control stays non-interactable despite sufficient coins (50 ≥ 20). Assumption: affordability refresh does not run on coin change or item selection.

## Expected behavior
When `playerCoins >= itemPrice` for the selected catalog entry, Buy is interactable; otherwise disabled. Updates immediately on coin gain and item selection change.

## Scope — inspect or modify
- Scripts: `Assets/Scripts/NPC/` or UI trader/shop types (e.g. `TraderUI`, shop controller, inventory/currency bridge)
- Prefabs: trader shop canvas prefab (only if button wiring is wrong)
- Out of scope: combat, player movement, unrelated HUD

## Constraints
- Do not rewrite entire shop system; fix refresh/wiring only.
- Preserve serialized fields and button/list references.
- No Multitask — trader flow shares prefab references.

## Implementation steps
1. Find where Buy `interactable` is set; list conditions blocking enable.
2. Ensure coin balance and selection events call a single `RefreshAffordability()` (or equivalent).
3. Fix stale state (e.g. cached price, wrong currency source, missing listener on coin changed).
4. Log warning if currency or catalog reference is null (do not hide broken prefab wiring).

## Acceptance criteria
- [ ] 50 coins, 20 price item → Buy interactable.
- [ ] Drop below price → Buy disabled.
- [ ] Buying deducts coins and disables when insufficient afterward.

## Play Mode validation checklist
- [ ] Open trader with 50 coins, select 20-cost item — Buy works.
- [ ] Spend until coins < price — Buy grays out.
- [ ] Receive coins from pickup/quest — Buy updates without reopening shop.

## Manual inspector checklist (if needed)
- [ ] Trader prefab → Buy Button reference assigned
- [ ] Currency/inventory source field points to player wallet component
---
```
