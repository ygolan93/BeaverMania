# Active Task

## Task title
- Objective flow Unity wiring (Remastered)

## Type
- Mixed (scripts + scene wiring + Play Mode verification)

## Owner
- **Cursor** — scene/prefab wiring complete
- **User** — full gameplay Play Mode walkthrough

## Current phase
- **Complete** — all plan todos implemented and verified (MCP Play Mode + PlayMode tests)

## Goal
- Wire Remastered objective HUD/waypoint/triggers safely for authoritative `ObjectiveSyncService` without broken serialized references.

## Bridge progression decision (indices 3–6)

**Decision: Defer `FirstBridgeChecker` / `NewConstructor` migration into Remastered.**

Rationale:
- Remastered scene has **zero** `FirstBridgeChecker` or `NewConstructor` instances (MCP + YAML confirmed).
- Remastered objective order starts at hive (index 0), not legacy Level 1 trader-first flow.
- `OuterCliff` exists as waypoint target index 6 for compass guidance; bridge build gameplay is not auto-wired to service triggers in this scene.
- Progression for indices 2–6 relies on: trader dialogue `+1` (`Dialogue.EndConversation`), `Carry.OnLogCollected()` keyword resolution, and manual player actions until bridge zone scripts are migrated in a follow-up task.

Follow-up (out of scope): migrate bridge zone from `Level 1.unity` if automatic log/lock/complete stages are required in Remastered.

## Changes made

### Scene
- `Assets/Scenes/Level 1 - Remastered - Steam.unity`:
  - `WayPoint.Locations[2]` → `Trader` transform (was null; fixes "Buy weapons" compass target)
  - Existing hive overrides unchanged: `NewHive` → index 1, `NewHive (1)` → index 8

### Prefabs (verified, no change required)
- `Assets/Prefabs/Player/PlayerPack-Drop and Play.prefab`:
  - `DebugReference.Player` and `DebugReference.PlayerHudState` already assigned via nested PlayerCanvas override
  - `ObjectiveUI.i` starts at 0; 13-entry objective table for Remastered

### Scripts (prior Codex work — unchanged this pass)
- `ObjectiveSyncService`, `WayPoint`, `ObjectiveUI`, `Static_Hive`, `Trader`, `Dialogue`, `DebugReference`

## Verification performed

- Unity MCP Play Mode boot: `ObjectiveSyncService.Instance` on `GameMaster`; `CanAuthoritativelyApplyObjective=true`; index 0 text/target synced to NewHive.
- `WayPoint.Locations[2]` → `Trader` confirmed in Edit Mode and Play Mode (saved to scene YAML).
- PlayMode suite `ObjectiveSyncServicePlayModeTests`: **6/6 passed** (includes index-2 "Buy weapons" waypoint + trader override refresh).
- MCP `execute_code` in-scene flow (fresh Play Mode session):
  - BOOT: index 0, "Clear the wasp nest", target NewHive
  - NewHive death → index 1, "Talk to the trader", target Trader
  - Trader override/restore via `RefreshBindingsAndReapply()` → HUD restored
  - Dialogue `+1` → index 2, "Buy weapons", target Trader
  - NewHive (1) death → index 8, "Defeat Shadow Revenant", target ShadowRevenant
- `DebugReference.Player` / `PlayerHudState` already assigned on PlayerPack PlayerCanvas override; resolved at runtime.
- Scene validate: 0 missing scripts / 0 broken prefabs.

## Optional user gameplay walkthrough
- [ ] Visual compass mark follows targets during live combat/travel (automated checks cover service state only)
- [ ] Trader proximity prompt in actual approach range (override path verified programmatically)
