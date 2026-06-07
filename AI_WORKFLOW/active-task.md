# Active Task

## Task title
- Hive objective / waypoint progression fix

## Type
- Mixed (scripts + prefab/scene hive wiring + Play Mode verification)

## Owner
- **Cursor** — script + prefab/scene wiring complete
- **User** — Play Mode verification in Level 1 Remastered - Steam

## Current phase
- Implementation complete; awaiting Play Mode confirmation

## Goal
- After destroying the first hive, advance WayPoint index 0→1 and HUD objective to "Talk to the trader". Per-hive configured advancement for second nest hive (→ index 8).

## Changes made

### Scripts
- `Assets/Scripts/UI/WayPoint.cs` — `AdvanceToIndex(int)`, `AdvanceToNext()`; `OnTriggerEnter` refactored
- `Assets/Scripts/Player/ObjectiveUI.cs` — `UpdateObjective()` advances via WayPoint; cached references
- `Assets/Scripts/Objects/Hive/Static_Hive.cs` — per-hive `advancesObjectiveOnDeath`, `advanceToObjectiveIndex`, `deathHandled` guard

### Prefabs / scene
- `Assets/Prefabs/Hive/NewHive.prefab` — default objective fields (disabled by default)
- `Assets/Prefabs/Player/PlayerPack-Drop and Play.prefab` — `ObjectiveUI.i` override 12 → 0
- `Assets/Scenes/Level 1 - Remastered - Steam.unity`:
  - **NewHive** (waypoint location 0): `advancesObjectiveOnDeath=true`, `advanceToObjectiveIndex=1`
  - **NewHive (1)** (waypoint location 7): `advancesObjectiveOnDeath=true`, `advanceToObjectiveIndex=8`

## Manual Play Mode validation checklist
- [ ] Start: objective "Clear the wasp nest", compass → first hive / WASPS A
- [ ] Destroy first hive: objective → "Talk to the trader", `WayPoint.i == 1`, compass → Trader
- [ ] Trader dialogue still advances objectives via `UpdateObjective()`
- [ ] Destroy second-nest hive (NewHive (1)): objective → "Defeat Shadow Revenant", index 8
- [ ] No double-advance on hive death; no Console errors
