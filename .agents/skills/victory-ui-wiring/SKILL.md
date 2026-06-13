---
name: victory-ui-wiring
description: Wire and verify Beavermania victory or end-game UI references. Use when assigning victory panels, end screens, boss-win UI, cursor/end-state behavior, PlayerCanvas links, or BossHandler.VictoryScreen.
---

# Victory UI Wiring

Use this when connecting a gameplay victory trigger to UI. The purpose is to prove the correct UI object is shown by the correct owner, not to redesign the UI.

## Workflow

1. Locate the runtime owner that activates victory UI.
2. Locate the dedicated victory/end-game object under `PlayerCanvas` or the scene UI root.
3. Confirm the object starts inactive unless the existing flow requires otherwise.
4. Assign the serialized field on the runtime owner to that dedicated object.
5. Preserve existing lose, pause, boss bar, dialogue, and shop UI references.
6. Verify cursor visibility/lock state and player control state in Play Mode.

## Boss Victory Guardrails

- `BossHandler.VictoryScreen` must point at a victory panel or victory scene-flow object, not a lose screen, boss bar, dialogue panel, or generic canvas root.
- Victory should appear once after the configured boss delay.
- If lose flow wins during the delay, victory must stay hidden.
- The boss UI should hide immediately on boss death, before the victory delay completes.
- Do not solve missing scene wiring by adding duplicate canvases, duplicate EventSystems, or duplicate manager objects.

## Report

Return:

- selected victory UI object path
- assigned owner/component/field
- existing UI references preserved
- cursor/control behavior observed
- whether victory appeared once
- whether lose-precedence behavior was checked
