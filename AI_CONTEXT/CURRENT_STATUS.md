# Current Status

## Main Current Goal

Steam release candidate stability.

## Current Focus Areas

- Level 1 Remastered integration and stability.
- Trader UI, dialogue, and shop flow.
- Player combat polish.
- FireBreath, SwordGlare, Bow, and Stoning regression risks.
- Audio balance and SFX/music routing.
- FPS and rendering optimization.

Large architecture work should be deferred unless it directly stabilizes the Steam release candidate.

## Recent Branch Activity (`fix/player-jump-sfx-throttle`, as of 2026-06-11)

- Jump SFX throttling and pitch-distortion fixes committed (`2ea52146`, `1a9fc660`); Play Mode QA pending.
- Level 1 Remastered scene overrides, fence/otter materials, and an OtterAnim2 `Consume` transition condition committed (`232d5edd`).
- A roll-animation pipeline (RollBall3.0 Blender export, import fixer, Shapekeys_Anim rewiring, airborne/inertia roll gameplay logic) was prototyped and then **fully reverted**; see `AI_WORKFLOW/active-task.md` for retained findings if revisited.
