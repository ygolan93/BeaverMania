# Profiler-Aware Refactor Rules

## Allocation rules for per-frame carry code

- No string building in `Update()` unless the value changed. `Goal.LogCount = i + "/9"` boxes/concats every frame — cache the last count and rebuild only on change. Hint strings like `"9/9  LCtrl+RMB to build"` are constants; assign, don't rebuild.
- No `Debug.Log` in per-frame paths, even behind a condition that is usually true. One-shot warning flags (the existing `loggedMissing*` pattern) are fine.
- No LINQ, `GetComponent`, `Find*`, or closures in `Update`/`FixedUpdate`. Cache references in `Awake`.
- Penalty math (`Mathf.Lerp`/`Clamp01` on floats) is allocation-free; calling it every frame is acceptable when reconciliation against external writers requires it.

## Profiler validation checklist

Run with the Profiler attached in Play Mode, CPU module, Hierarchy view:

1. Search `Carry.Update`. **GC Alloc must read 0 B** while standing still with logs carried.
2. Pick up and drop logs; allocation spikes only on those frames (string rebuild, `Instantiate`) — acceptable.
3. Sprint for ~10 s with max logs: `Carry.Update` self time should be negligible (< 0.05 ms) and flat.
4. Console: no repeated log entries while carrying.
5. Animator module (or inspector on the `Otter` Animator): confirm `Speed` shows the expected multiplied value at 0 / medium / max load, and snaps back to base after dropping all logs and after a jump landing.
6. Toggle goblet boost with max logs: `Animator.speed` should be `3 × minAnimationMultiplier`, never the unboosted floor.
