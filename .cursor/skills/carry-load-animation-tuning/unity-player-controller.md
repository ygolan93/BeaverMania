# Unity Player Controller Map (Beavermania)

Where burden/movement/animation state lives. Verify line numbers before editing; they drift.

## `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`

- `speed`, `steer`, `JumpForce`, `Walk = 4`, `Run = 12` — public movement fields (~line 35–39). `speed` is set to `Walk` or `Run` every grounded frame in the sprint block (~line 716–744), then applied as `Player.velocity = Direction.normalized * speed`.
- `AnimSpeed` — **private** field, normally `1`. Goblet boost (`GobletON`/`GobletOFF`, ~line 1998–2035) sets it to `3` and also boosts `Walk = 7`, `Run = 18`. Carry cannot read it directly.
- `Otter.speed = AnimSpeed` is reasserted at three sites (jump ~2429, beat reset ~2657, landing ~3093). These stomp any carry animation penalty; the penalty must be re-applied via reconciliation, or these sites must multiply by a carry multiplier.
- `LogCount` — public string consumed by `PlayerHudState` → `DebugReference` HUD.

## `Assets/Scripts/Player/Carry.cs` (`Beavermania.Player.Movement.Carry`)

- `i` — carried log count (0–9). Read externally by `PauseCollectiblesSummary` (`player.Load.i`) and `FirstBridgeChecker` (`playerLoad.i`). Do not rename.
- `ApplyCarryWeightPenalty()` — applies penalties to `Goal.Walk`, `Goal.Run`, `Goal.JumpForce`, `Goal.Otter.speed`. Called from `Update()` every frame plus on pickup/drop/build.
- `ResolveBaseValue(current, lastApplied, appliedPenalty)` — recovers the unpenalized base even when external code (goblet boost, jump reset) rewrites the field. Preserve this pattern when changing the penalty math.
- `CurrentWeightPenalty01` — normalized burden exposed to other systems.

## External readers of log count (do not break)

- `Assets/Scripts/UI/Menus/PauseCollectiblesSummary.cs` — `player.Load.i`
- `Assets/Scripts/FirstBridgeChecker.cs` — `playerLoad.i`
- `Assets/Scripts/Player/PlayerHudState.cs` / `Assets/Scripts/UI/DebugReference.cs` — `LogCount` string
