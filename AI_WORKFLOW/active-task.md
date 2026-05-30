# Active Task

## Task title
- First Trader + Military Trader scene/UI re-integration (Level 1 Remastered)

## Type
- Bugfix / Unity wiring

## Owner
- Mixed (serialization fix applied; Unity Play Mode verification pending)

## Current phase
- Verification

## Goal
- Restore First Trader and Military Trader proximity, dialogue, shop, camera lock, and objective progression after NPCs were removed/re-added.

## Scope
- FirstTraderItems + PlayerCanvas TraderPanel aliases (done)
- Military Trader via Camp Trading Point prefab + Combat Panel UI aliases (done)
- Scene wiring in `Level 1 - Remastered - Steam.unity`

## Relevant files/assets
- `Assets/Prefabs/Objects/FirstTraderItems.prefab`
- `Assets/Prefabs/BeaverNPC/Camp Trading Point.prefab`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Prefabs/Objects/UI/Combat Panel.prefab`
- `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- `Assets/Data/Dialogue/MilitaryTraderDialogueData.asset`

## Risk level
- High (trader/shop/dialogue serialized references; Military Trader placed near Camp Checkpoint)

## Current status
- YAML/prefab wiring restored for both traders.
- Military Trader placed at ~(1440, 599.5, -1720) near Camp Checkpoint — **verify position in Scene view**.
- Unity MCP unavailable during implementation.

## Next action
- Owner: User / Unity Editor
- Action: Open scene, confirm MilitaryTrader placement and all Inspector refs; run Play Mode checklist for both traders.

## Required verification
- Unity Play Mode on 2nd island: military trader prompt, CampDialogue, combat shop, clean exit.

## Handoff needed
- No Codex handoff unless Play Mode reveals script-level regressions.
