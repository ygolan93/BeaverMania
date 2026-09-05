# Animation Parameter Tuning Rules

## Playback speed floors

- Full-body locomotion via `Animator.speed`: keep ≥ **0.75**. Recommended carry floor: **0.8**.
- Below ~0.7 a run/walk cycle reads as slow motion or a glitch; foot contacts visibly slide because root velocity no longer matches the cycle.
- Movement velocity may legitimately drop far lower (0.4–0.6×). The mismatch is acceptable in cartoon style: a _labored but normal-speed_ cycle at reduced velocity reads as "heavy", a _slowed_ cycle reads as "broken".

## What to scale, what not to scale

- Scale: `Animator.speed` (whole-controller playback), within the floor band.
- Do not scale: transition durations, animation events timing assumptions, attack animations. If combat anims must stay full speed while locomotion slows, use a state-specific speed multiplier parameter on the locomotion states instead of `Animator.speed` — in this project `Animator.speed` is currently controller-wide, so keep the floor high enough (≥0.75) that combat is not noticeably affected.

## Beavermania specifics

- Locomotion states are driven by bools (`run`, `strafeRight`, `climb`, `armor`, `midair`); there is **no `Move` float blend parameter**. The only playback-speed lever is `Otter.speed`.
- Base playback speed is not constant: it is `1` normally and `3` during goblet boost. Always express the carry penalty as a **multiplier on the base**, not an absolute subtraction, or boost mode will be over- or under-penalized.
- Inspector safety: expose floors as `[SerializeField, Range(...)]` so designers cannot set a broken value; validate cross-field consistency (`maxBalancedLogCount < maxLogCount`) in `OnValidate()` with `Debug.LogWarning(..., this)`.

## Tuning defaults that read well

| Field                    | Default | Effect at 9 logs                 |
| ------------------------ | ------- | -------------------------------- |
| `maxBalancedLogCount`    | 3       | first 3 logs are free            |
| `minMovementMultiplier`  | 0.5     | Run 12 → 6, Walk 4 → 2           |
| `minAnimationMultiplier` | 0.8     | playback 1 → 0.8 (boost 3 → 2.4) |
