# Active Task

## Task title

- NPC Dialogue HUD Alignment — Phases 1 + 2 (Market Trader + Arms Dealer)

## Branch

- `fix/npc-dialogue-ui-alignment`

## Ownership

- **Cursor + Unity Editor** (mixed code + prefab task, executed via Unity MCP)
- **Codex** — none pending

## Current phase

- Phases 1 and 2 complete and verified in automated Play Mode QA. Awaiting human Play Mode spot-check (see checklist) before commit/PR.

## What changed (uncommitted)

| File                                                       | Change                                                                                                                                                                                                                                                                                                    |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Assets/Scripts/UI/NpcDialoguePresenter.cs` (new)          | Presenter on the shared panel root; on enable clears `Dialogue.Merchant` and restarts the dialogue so the panel serves any trader per session                                                                                                                                                             |
| `Assets/Scripts/UI/Dialogue.cs`                            | Added `hasStarted` flag + public `RestartDialogue()` (no-op before `Start()` runs); no behavior change for legacy panels                                                                                                                                                                                  |
| `Assets/Prefabs/Objects/UI/NPC_DialoguePanel.prefab` (new) | Shared HUD-styled dialogue panel: `name_bar2` bg, Bangers SDF text, Continue/Shop/Skip buttons in a HorizontalLayoutGroup, bottom-center anchors (860x150 canvas units, uniform scale 1), inactive by default                                                                                             |
| `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`            | Nested `NPC_DialoguePanel` instance as direct child of root (sibling after `Dialogues`); wired button clicks to canvas `AudioSource.Play`; neutralized legacy `TraderPanel` instance (root Image disabled, `TraderDialogue`/Continue/Shop/Skip children deactivated; `Trader Shop Panel` child untouched) |
| `Assets/Prefabs/Player/PlayerPack-Drop and Play.prefab`    | Market trader (`Trader`, the Benny/Ottis trader) `DialoguePanel` override repointed from legacy `TraderPanel` to `NPC_DialoguePanel`                                                                                                                                                                      |

Notes:

- The 7 inline panels (Guard, Survivor, Watchtower, Farmer, BeaverTown, Boss, Beavus-Final) are intentionally untouched; they migrate in later phases.
- Known behavior change: closing the shop returns to the dialogue restarted from line 0 (legacy kept the last line frozen). Skip remains available.

## Phase 2 additions (Arms Dealer / Combat Panel, 2026-06-12)

| File                                                    | Change                                                                                                                                                              |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Assets/Scripts/NPC/Beaver/Trader.cs`                   | Added `[SerializeField] TraderDialogueData dialogueData` + public `DialogueData` getter (additive only)                                                             |
| `Assets/Scripts/UI/Dialogue.cs`                         | Added `SetDialogueData(TraderDialogueData)` so the shared panel can swap lines per trader                                                                           |
| `Assets/Scripts/UI/NpcDialoguePresenter.cs`             | On enable: resolves the session-active trader, assigns it as `Merchant`, applies its `DialogueData`, then restarts the dialogue                                     |
| `Assets/Prefabs/Player/PlayerPack-Drop and Play.prefab` | Arms Dealer `DialoguePanel` → `NPC_DialoguePanel`; `dialogueData` set on both traders (Arms Dealer → `MilitaryTraderDialogueData`, Market → `MarketTraderDialogue`) |
| `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`         | Combat Panel instance neutralized like TraderPanel (root Image off, CampDialogue/Continue/Shop/Skip deactivated, `Combat Trader Shop Panel` untouched)              |

Phase 2 automated verification (in-editor Play Mode, single run):

- Arms Dealer opens shared panel with military lines ("Player: Excuse me"), merchant resolves to Arms Dealer.
- Arms Market shop opens with no legacy bar visible; CloseShop returns to dialogue; Skip closes and re-locks cursor.
- Market trader afterwards shows market lines (Benny intro) — per-trader data switching proven in one run.
- GuardPanel/BossPanel/SurvivorPanel intact with Dialogue components (structural spot-check).
- Console clean of compile and runtime errors.

## Automated verification performed (2026-06-12, in-editor Play Mode)

- Compile clean after script changes (no CS errors in Console).
- Proximity prompt shows "Press E to interact" via HUD ObjectiveText.
- `OpenInteraction` → shared panel opens, typewriter runs, `Dialogue.Merchant` resolves to "Trader".
- Continue advances lines; Shop button opens Benny's Shop (shared panel hides, legacy container stays invisible — image off, dialogue children off); CloseShop returns to dialogue; Skip closes everything, cursor re-locked/hidden.
- Second interaction session restarts dialogue cleanly (no frozen text, no double-typing).
- Screenshots at free-aspect, 1366x768, and 3440x1440: panel bottom-centered, no clipping, buttons readable, HUD unobstructed.
- Regression spot-check: Arms Dealer still opens legacy Combat Panel with CampDialogue.

## Pending human Play Mode QA

- [ ] Walk to the market trader and interact with the real E key (PlayerInputReader path; automated QA invoked `OpenInteraction` directly)
- [ ] Walk to the Arms Dealer at the camp and interact with the real E key — military dialogue, Arms Market shop
- [ ] Button click SFX audible on Continue/Shop/Skip
- [ ] Trader camera transition and cursor behavior feel correct end-to-end for both traders
- [ ] Buy/sell in Benny's Shop and Arms Market still works (coins/items/weapons)
- [ ] Dialogue completion advances the objective exactly once per trader
- [ ] Quick pass on one legacy NPC (e.g. Guard or Boss) to confirm no visual regression

## Risks / follow-ups

- `PlayerCanvas.prefab` and `PlayerPack-Drop and Play.prefab` are high-risk shared assets; changes were additive/override-only and the `Dialogues` container rect was not modified.
- Shop-close now replays the trader dialogue from the start; if undesired, presenter can skip restart when the session is unchanged (small follow-up).
- Phase 3+: migrate the 7 inline panels (Guard, Survivor, Watchtower, Farmer, BeaverTown, Boss, Beavus-Final) to the shared presenter, then delete legacy dialogue bars and restyle the shops.
