---
name: playmode-boss-smoke-test
description: Run or define a Unity Play Mode smoke test for Beavermania boss death flows. Use when validating forced boss health to zero, delayed victory UI, control freeze, cursor state, boss bar hiding, lose precedence, or scene-specific boss wiring.
---

# PlayMode Boss Smoke Test

Use this after code and scene wiring are complete. Static inspection is not enough for boss victory acceptance.

## Required Setup

1. Open the exact production scene named by the task.
2. Confirm the active boss instance, player, `BossHandler`, `GameMaster`, and `PlayerCanvas` are present.
3. Confirm the boss and UI serialized references are assigned before entering Play Mode.
4. Clear the Console, then enter Play Mode.

## Smoke Steps

1. Reach or select the boss encounter state without unrelated scene edits.
2. Force the boss health to zero through the safest available runtime path.
3. Observe that boss UI hides immediately.
4. Observe that victory does not appear until the configured delay has elapsed.
5. After the delay, verify controls freeze or transition according to the owner flow.
6. Verify cursor visibility and lock state.
7. Verify the victory UI or victory scene appears once only.
8. Repeat the delay window with lose screen already active if the task requires lose precedence.

## Evidence

Return:

- scene name and boss instance tested
- method used to force death
- configured delay and observed behavior
- boss UI behavior
- control/camera/cursor state
- victory UI/scene result
- Console errors or warnings
- whether the scene was saved after wiring
