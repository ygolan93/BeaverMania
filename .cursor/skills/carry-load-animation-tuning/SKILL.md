---
name: carry-load-animation-tuning
description: Tune carried-log (or other burden) slowdown so physical movement speed and animation playback speed are controlled independently in Beavermania. Use when carried logs, carry weight, or burden penalties make player animation look too slow, broken, or frozen, when adjusting Carry.cs penalties, Animator.speed floors, movement/sprint multipliers, or when separating movement slowdown from animation slowdown.
---

# Carry Load Animation Tuning

Fix and tune burden-based slowdown so the game can slow the player's _movement_ heavily while keeping _animation playback_ readable.

## Core rule

Physical slowdown and animation slowdown are two separate channels with separate floors:

- **Movement channel**: `Walk`, `Run`, `JumpForce` on `BeaverPlayerBehaviour` — may drop steeply (multiplier floor ~0.4–0.6).
- **Animation channel**: `Animator.speed` (`Goal.Otter.speed`) — must never drop below **0.75–0.85**. Below that, locomotion looks broken.

Never derive one channel from the other. Compute both from the same normalized load and clamp independently.

## Multiplier model

```csharp
float load01 = Mathf.Clamp01((float)(logCount - maxBalancedLogCount) / (maxLogCount - maxBalancedLogCount));
float movementMultiplier = Mathf.Lerp(1f, minMovementMultiplier, load01);
float animationMultiplier = Mathf.Lerp(1f, minAnimationMultiplier, load01);
```

Serialized tuning fields (on `Carry`):

- `maxBalancedLogCount` — logs carried with zero penalty (default 3)
- `minMovementMultiplier` — movement multiplier at max load (default 0.5)
- `minAnimationMultiplier` — animation multiplier at max load (default 0.8, `[Range(0.75f, 0.85f)]`)

## Project-specific constraints

- `BeaverPlayerBehaviour` overwrites `Otter.speed = AnimSpeed` after jumps/landings, and the goblet boost changes `AnimSpeed`, `Walk`, `Run`. Any carry penalty must reconcile against an externally changing base (see `ResolveBaseValue` in `Carry.cs`). Apply multipliers to the _recovered base_, never to the already-penalized current value (compounding bug).
- Locomotion animation is bool-driven (`run`, `strafeRight`, `climb`); there is no `Move` float parameter. Playback speed control is `Animator.speed` only.
- No per-frame string allocations: `Goal.LogCount = i + "/9"` in `Update()` must only rebuild when the count changes. No per-frame `Debug.Log`.

## Validation matrix (Play Mode)

Always verify all four rows:

| Load             | Expected movement              | Expected `Animator.speed`                     |
| ---------------- | ------------------------------ | --------------------------------------------- |
| 0 logs           | base Walk/Run                  | base (1, or 3 during goblet boost)            |
| medium (~5)      | proportionally reduced         | ≥ ~0.9                                        |
| max (9)          | `minMovementMultiplier` × base | `minAnimationMultiplier` × base, never < 0.75 |
| sprint with logs | reduced Run velocity           | run cycle readable, no foot-slide freeze      |

## Embedded references

- [unity-player-controller.md](unity-player-controller.md) — where movement, sprint, AnimSpeed, and carry penalties live in this codebase
- [animation-parameter-tuning.md](animation-parameter-tuning.md) — animation playback tuning rules and floor rationale
- [profiler-aware-refactor.md](profiler-aware-refactor.md) — allocation rules and profiler validation checklist
