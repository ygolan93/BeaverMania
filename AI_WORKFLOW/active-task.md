# Active Task

> **Status (2026-06-13):** Active branch for this mixed task is `fix/scorpion-boss-victory-flow`. Codex implemented the runtime delayed-victory flow for the Scorpion boss. Cursor completed Unity wiring and Play Mode smoke validation for production scenes except the broken `BossDemo` backup scene.

## Current mixed task

- Scorpion boss delayed victory flow after boss death

## Current owner split

- **Mixed.** Codex: script-level defeat event, delayed victory trigger, time-scale token, and PlayMode coverage scaffold. Cursor: scene/prefab reference wiring plus Unity Play Mode validation. User: final acceptance on end-of-fight behavior.

## Current phase

- **Cursor wiring + smoke validation complete (2026-06-13).** Awaiting user acceptance in-editor on the real boss encounter path.

## Wiring completed by Cursor

| Scene / asset                                      | BossHandler                                                                    | VictoryScreen                                     | BossBar / BossPanel  | ChatCollider                             | Play Mode smoke                                                                          |
| -------------------------------------------------- | ------------------------------------------------------------------------------ | ------------------------------------------------- | -------------------- | ---------------------------------------- | ---------------------------------------------------------------------------------------- |
| `Assets/Scenes/Level 1 - Remastered - Steam.unity` | Added on `Player` (scene instance)                                             | `PlayerCanvas/Finish Game Panel `                 | Wired                | Left null (no boss chat source in scene) | **Pass** — delay 2s, victory once, freeze/cursor/control                                 |
| `Assets/Scenes/Level 1.unity`                      | Added on `Player` (prefab instance; old prefab BossHandler override was stale) | `PlayerCanvas/Finish Game Panel `                 | Preserved / verified | `BossFight` (inactive Arena trigger)     | **Pass**                                                                                 |
| `Assets/Scenes/BackUp Scene/BossDemo.unity`        | Not wired                                                                      | Not wired                                         | Stale overrides only | Unknown                                  | **Blocked** — `BlinkingPlayer` and `PlayerCanvas` prefab instances are missing in Editor |
| `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`    | Not modified                                                                   | `Finish Game Panel ` exists (inactive by default) | Unchanged            | N/A                                      | N/A                                                                                      |

## Play Mode evidence (Cursor / Unity MCP)

### Remastered + Level 1 (forced `handler.Boss.TakeDamage(999999999)`)

- Immediate: boss HP `0`, boss object inactive, boss bar hidden, `Time.timeScale = 1`, victory panel hidden.
- After `VictoryDelay = 2s`: exactly one `Finish Game Panel ` active, `Loose Menu Panel` inactive, `Time.timeScale = 0`, cursor visible/unlocked, `BeaverPlayerBehaviour.enabled = false`, `FreeLook.enabled = false`.
- Lose precedence (remastered): with `LooseScreen` active before delay end, victory stayed hidden.
- Console: no errors during smoke runs.

### Smoke-test note

- Use `BossHandler.Boss`, not `FindObjectOfType<ScorpionScript>()`. Remastered scene has additional non-boss `ScorpionScript` instances; killing the wrong one does not trigger victory.

## Manual Unity verification still recommended (user)

- [ ] Reach the Scorpion boss through normal gameplay in `Level 1 - Remastered - Steam` and confirm the delayed victory feels correct in context (dialogue skip, arena entry, music stop, UI timing).
- [ ] Repeat once in `Level 1.unity` if that scene remains a supported boss route.
- [ ] Confirm boss dialogue trigger still works in `Level 1` with `ChatCollider -> BossFight`.
- [ ] Skip `BossDemo` unless missing prefab GUIDs are repaired first.

## Deferred / out of scope on this branch

- `BossDemo.unity` repair (missing prefab GUIDs `b922004aea0ae754c917e6abedafb749`, `d6e224836b570c644b9ae086cef28f39`).
- Level 1 Remastered FPS / canopy work (separate branch `fix/log-carry-animation-load`).
- NPC presenter wiring (historical handoff section in `codex-to-cursor.md`).

## Handoff files

- Codex → Cursor: `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- Cursor → Codex: `AI_WORKFLOW/handoffs/cursor-to-codex.md` (FPS task; not updated for this boss flow)

## Suggested commit message (when user approves)

```
Wire Scorpion boss victory UI and validate delayed end-game flow

Assign BossHandler.VictoryScreen to Finish Game Panel in Level 1 and
Remastered scenes, restore Level 1 boss handler wiring, and verify
delayed victory, lose precedence, and control freeze in Play Mode.
```
