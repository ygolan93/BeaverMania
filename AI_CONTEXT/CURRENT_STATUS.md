# Current Status

## Main Current Goal

Steam release candidate stability.

## Current Focus Areas

- Level 1 Remastered integration and stability.
- **Level 1 north forest FPS** — pass 1 done (terrain draw distances); pass 2 open (canopy fill-rate / open-sky sightline).
- Trader UI, dialogue, and shop flow.
- Player combat polish.
- FireBreath, SwordGlare, Bow, and Stoning regression risks.
- Audio balance and SFX/music routing.
- FPS and rendering optimization.

Large architecture work should be deferred unless it directly stabilizes the Steam release candidate.

## Recent Branch Activity (`fix/log-carry-animation-load`, as of 2026-06-13)

- **Level 1 forest FPS pass 1 (scene):** 33 terrain tiles — tree distance 5000→200, detail distance 80→20, instancing on; bad-zone decorative shadow-off. MCP: bad ref ~34–39 FPS (was ~10–15), good ref ~72 FPS at reference coordinates.
- **Level 1 forest FPS pass 2 (open):** user still reports severe FPS when camera faces giant `TheGivingTree` canopy / open sky (Edit + Play). Codex static review points to alpha-cutout fill-rate, not scripts. Cursor A/B next — see `AI_WORKFLOW/handoffs/codex-to-cursor.md`.
- **Carry animation:** independent movement vs animation multipliers in `Carry.cs`.
- **Dialogue teardown:** `NpcDialoguePresenter` shutdown guard — Play Mode exit clean.
- **FPS script quick wins:** HUD caching (`BeaverPlayerBehaviour`), presenter idle throttle (`NpcDialoguePresenter`).
- Workflow: `AI_WORKFLOW/active-task.md` | handoffs in `AI_WORKFLOW/handoffs/`

## Prior branch (`fix/player-jump-sfx-throttle`, 2026-06-11)

- Jump SFX throttling and pitch-distortion fixes committed (`2ea52146`, `1a9fc660`); Play Mode QA pending.
- Level 1 Remastered scene overrides, fence/otter materials, and an OtterAnim2 `Consume` transition condition committed (`232d5edd`).
- Roll-animation pipeline prototyped and **fully reverted**.
