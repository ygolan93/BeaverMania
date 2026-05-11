# Performance / Steam Release Readiness Checklist

Use this checklist before every Steam release candidate. Do not change project or build settings blindly; run the validator, audit values, then make intentional changes only when a listed decision is complete.

## Automated audit

- Run `Tools/Beavermania/Validate Steam Release Readiness` in the Unity editor.
- Save the console output with the release-candidate notes.
- Confirm the validator is report-only and did not modify `ProjectSettings/*`, scenes, prefabs, or assets.

## Display defaults

- Fullscreen/windowed default is intentional for Steam launch.
- Fullscreen switching is available if required by Steam Deck / desktop QA.
- Default resolution is sane for first launch.
- Native-resolution default is intentional.
- Window resize behavior is acceptable if windowed mode is supported.

## Frame pacing

- vSync decision is explicit:
  - `vSyncCount > 0`: Unity uses display sync; target frame rate may be ignored.
  - `vSyncCount == 0`: `Application.targetFrameRate` / platform default governs frame pacing.
- Target frame-rate behavior is tested on the target hardware class.
- No development-only FPS overlay appears in release builds.

## Quality / rendering

- Active quality level is the intended Steam release quality level.
- URP render pipeline asset is active through Graphics Settings or active Quality Settings.
- Shadow distance is manually validated in representative gameplay scenes.
- Shadow cascades are manually validated for visible pop-in and cost.
- Shadow quality/resolution matches target performance budget.
- Target hardware smoke test completes without camera/render-pipeline warnings.

## Build flags

- Development Build is off for the release candidate.
- Script Debugging is off for the release candidate.
- Autoconnect Profiler is off for the release candidate.
- Deep Profiling is off for the release candidate.
- Scripting define symbols do not force development diagnostics into release builds.

## Logging / diagnostics

- `BuildSafeLogger` calls are compiled out for non-development players.
- Non-development player build emits no development diagnostic UI or log spam.
- Logging configuration exists for editor/development-build diagnostics if those builds are being produced.
- Runtime diagnostic helpers under `Assets/Scripts/Debug` and `DevelopmentRuntimeDiagnostics` are unavailable in non-development players.

## Steam smoke test

- Steam overlay opens/closes in the release candidate.
- Overlay does not break pause/menu input focus.
- Fullscreen/window switching still works after opening the overlay.
- Resolution and aspect ratio remain correct after overlay use.
- Fresh install / first launch uses the intended display defaults.
