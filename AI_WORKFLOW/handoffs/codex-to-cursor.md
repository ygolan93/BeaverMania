# Codex → Cursor Handoff

## Task
- AudioListener ownership: move active listener from root player prefab to PlayerPack/Main Camera.

## Files changed
- `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab`
- `Assets/Prefabs/OtterPlayer/PlayerPack.prefab`
- `Assets/Scenes/ShadowRevenantTestArena.unity`
- `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`

## Prefab changes made
- Disabled the root `Player` `AudioListener` in `Player.prefab`.
- Added/enabled one `AudioListener` on `PlayerPack/Main Camera` in `PlayerPack.prefab`.
- Kept direct-`Player` scenes covered by overriding the prefab listener enabled in `ShadowRevenantTestArena.unity` and `Level 1 - Remastered - Steam.unity`.

## Inspector assignments required
- None expected.

## What Cursor should test in Play Mode
- Open the target test scene using `PlayerPack`.
- Open `ShadowRevenantTestArena.unity` and `Level 1 - Remastered - Steam.unity`.
- Enter Play Mode and confirm exactly one active `AudioListener` is present in each scene.
- Confirm the Console has no duplicate `AudioListener` warning.
- Confirm camera audio still follows `PlayerPack/Main Camera`.

## Known limitations
- Unity Editor/Play Mode was unavailable in this environment. Needs Unity Play Mode verification.

## Risk notes
- Player/audio prefab risk: Medium; serialized prefab/scene YAML changed in high-risk player assets/scenes, but only `AudioListener` enable/component state changed.

---


## Task
- Player camera anchors: replace animated-bone/root Cinemachine targets with stable yaw-only anchor transforms.

## Code changes made
- Added `PlayerCameraAnchors` runtime component with `rootReference`, `faceReference`, `cameraFollowAnchor`, `gameplayLookAnchor`, and `closePovLookAnchor`.
- `BeaverPlayerBehaviour` now binds gameplay `CinemachineFreeLook.m_Follow` to `CameraFollowAnchor` and `m_LookAt` to `GameplayLookAnchor` when anchors are available.
- Close POV/crouch camera retargets to `ClosePovLookAnchor` instead of `Face`.
- Boss dialogue camera restore retargets gameplay camera to `GameplayLookAnchor` instead of `Root`.

## Files changed
- `Assets/Scripts/Player/PlayerCameraAnchors.cs`
- `Assets/Scripts/Player/PlayerCameraAnchors.cs.meta`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/BossHandler.cs`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`

## New or changed serialized fields
- New serialized `PlayerCameraAnchors cameraAnchors` reference on `BeaverPlayerBehaviour`.
- New public `Transform` fields on `PlayerCameraAnchors`: `rootReference`, `faceReference`, `cameraFollowAnchor`, `gameplayLookAnchor`, `closePovLookAnchor`.
- No existing serialized fields were renamed or removed.

## Inspector assignments required
- On the player root/prefab, add `PlayerCameraAnchors` if not already present.
- Assign `rootReference` to the stable player root reference used for camera follow.
- Optional: assign `faceReference` only to a stable non-animated reference; if unset, look anchors use their local offsets from `rootReference`.
- Create/verify child hierarchy under player root:
  - `CameraAnchors`
  - `CameraAnchors/CameraFollowAnchor`
  - `CameraAnchors/GameplayLookAnchor`
  - `CameraAnchors/ClosePovLookAnchor`
- Assign the three anchor child transforms to the matching `PlayerCameraAnchors` fields.
- Verify gameplay `CinemachineFreeLook` uses `CameraFollowAnchor` as Follow and `GameplayLookAnchor` as LookAt in Play Mode.

## Prefab/scene/UI/Animator follow-up required
- Apply the above player prefab/scene hierarchy changes in Unity Editor; Codex did not edit prefab YAML.
- Do not assign `mixamorig:HeadTop_End`, `mixamorig:Head`, `Face`, `RootMovement`, spine bones, or other animated bones directly to active Cinemachine Follow/LookAt fields.

## What Cursor should test in Play Mode
- Enter gameplay scene, confirm FreeLook follow/look targets are the anchor children, not bones.
- Move, sprint, crouch/close POV, and roll; confirm camera position/orientation remains stable and yaw-only/world-up.
- Enter and exit boss/trader/dialogue camera states; confirm gameplay camera restores to anchor targets.
- Check console for missing-reference or runtime-created-anchor warnings/errors.

## Known limitations
- Unity Editor/Play Mode was unavailable in this environment. Needs Unity Play Mode verification.
- Runtime fallback creates missing anchor children and no longer defaults `faceReference` to `Face`; serialized prefab hierarchy still needs Editor setup.

## Risk notes
- Player camera risk: Medium; gameplay camera target binding changed.
- Serialization risk: Low-medium; new fields only, no renamed/removed fields.

---


## Task
- UI/UX and game-feel improvements — Phase 1 HUD cleanup, Phase 2 tips, Phase 3 objective tracker, Phase 4 footstep feedback, Phase 5 carried-log weight

## Code changes made
- Compact collectible/ammo/log HUD text values to numbers only so existing icons carry the label meaning.
- Move `StaminaBar` from the top-left HUD cluster to bottom-left anchored screen space in `PlayerCanvas.prefab`.
- Add a runtime pause-menu collectibles summary for coins, nuts, apples, goblets, arrows, and logs while preserving compact HUD counters.
- Add code-only tips subsystem with `TipDefinition`, settings persistence, cooldown/display-count/priority gating, idle/active gating, trigger-zone hooks, and an animated runtime tip card.
- Add a runtime Tips On/Off toggle to the existing pause menu through `UIMenu` startup without editing the pause menu prefab hierarchy.
- Add contextual bridge construction tips through `NewConstructor` so bridge/log guidance appears only near intended bridge construction areas.
- Add an objective tracker presenter that formats current objective text as a compact bullet list and keeps trader/objective override text flowing through the existing `DebugReference` path.
- Add pooled footstep contact particles triggered from the existing `AudioScript.Step()` animation-event method, with surface resolution by profile, tag, physics material, material name, or fallback.
- Centralize carried-log walk/run/jump/animation penalties in `Carry` with threshold, max weight penalty, per-log penalties, animation multiplier, and caps.
- Rebase carried-log penalties when other temporary effects change player walk/run/jump/animation values, so the log penalty remains active and restores cleanly.
- Expose `Carry.CurrentWeightPenalty01` for future HUD/combat hooks and add warning guards for missing carry references.
- Record mixed ownership and verification requirements in `AI_WORKFLOW/active-task.md`.

## Files changed
- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`
- `Assets/Scripts/Player/Carry.cs`
- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Scripts/Data/Tips/TipDefinition.cs`
- `Assets/Scripts/Data/Tips/TipRequest.cs`
- `Assets/Scripts/UI/Tips/*`
- `Assets/Scripts/UI/UIMenu.cs`
- `Assets/Scripts/UI/Menus/PauseCollectiblesSummary.cs`
- `Assets/Scripts/Objects/Newbridge/NewConstructor.cs`
- `Assets/Scripts/UI/DebugReference.cs`
- `Assets/Scripts/UI/Objectives/ObjectiveTrackerPresenter.cs`
- `Assets/Scripts/Data/Display/FootstepSurfaceEffectProfile.cs`
- `Assets/Scripts/Display/FootstepVfxEmitter.cs`
- `Assets/Scripts/Player/SoundScripts/AudioScript.cs`

## New or changed serialized fields
- New serialized tuning fields on `TipDefinition`, `TipsService`, `PlayerIdleStateTracker`, `TipCardPresenter`, and `TipTriggerZone`.
- New serialized tuning fields on `FootstepSurfaceEffectProfile`, `FootstepVfxEmitter`, and `AudioScript.footstepVfxEmitter`.
- New serialized tuning fields on `Carry`: log threshold, max weight penalty, max walk/run/jump/animation penalties, animation speed multiplier, and minimum animation speed.
- No existing serialized fields were renamed or removed.

## Inspector assignments required
- None expected from script changes.
- Verify existing `DebugReference` text references and `Health_Bar_Script` slider references remain assigned in the active scenes.
- Phase 2 scene authoring requires creating `TipDefinition` assets and assigning them to `TipTriggerZone` components in intended areas.
- Optional: assign a `FootstepSurfaceEffectProfile` on `FootstepVfxEmitter` for tuned per-surface colors/prefabs; fallback procedural dust works without assignment.
- Optional: tune `Carry` weight fields on the player prefab after Play Mode checks; defaults preserve the previous 9-log total penalties.
- Inspect any `Carry` missing-reference warnings before tuning movement penalties.

## Prefab/scene/UI/Animator follow-up required
- Inspect `PlayerCanvas.prefab` in Unity Editor and confirm `StaminaBar` is readable at bottom-left across common resolutions.
- Confirm top-right collectible/ammo/log counters still align with their icons after label removal.
- Confirm pause-menu collectibles summary appears, refreshes when pause opens, and does not overlap volume/restart/controls UI.
- Decide in Phase 2/3 whether collectibles should move fully into pause/diary UI or remain compact HUD counters.
- Confirm the runtime `Tips: On/Off` toggle appears in the pause menu and does not overlap volume/restart controls.
- Confirm bridge tips appear near `NewConstructor` trigger areas and do not appear globally when the player is away from bridge constructors.
- Confirm the objective tracker appears middle-left, is readable, and formats current objectives as a compact list.
- Confirm footstep particles appear subtly on walk/run animation events and do not appear while idle/airborne animations are not stepping.
- Confirm carrying logs gradually slows walk/run/jump/animation, then restores those stats after dropping logs or building a bridge.
- Confirm goblet boost and other temporary movement changes still preserve carried-log penalties while active and restore correctly afterward.
- Confirm no `Carry` missing-reference warnings appear on player prefab instances.
- Add `TipTriggerZone` components near intended bridge/log/tutorial areas after validating the runtime card and toggle.

## What Cursor should test in Play Mode
- Reproduction path: open Level 1 Remastered, enter Play Mode, move/jump/sprint, pick up coins/nuts/apples/goblets/arrows/logs, open/close pause, toggle Tips off/on.
- Functional checks: health stays top-left, stamina appears bottom-left and updates, counter text shows icon + number only, pause menu shows collectibles summary with current counts, objective tracker appears middle-left, log count shows `N/9`, 9-log carry state shows `LCtrl+RMB to build`, carrying logs slows movement/jump/animation and restores on drop/build, goblet boost preserves carried-log penalties, pause menu still opens/closes, Tips toggle persists across pause reopen, bridge tips appear only near bridge construction triggers, footstep particles trigger from existing walk/run step events.
- Negative/regression checks: no `NullReferenceException`, trader/lose UI still locks input/cursor, objective text still updates, tips do not show while disabled, paused, in trader UI, or away from bridge construction areas.

## What Cursor should not change
- Do not edit scenes or additional prefabs for later phases until Phase 1 layout is accepted.
- Do not rewrite input, pause, movement, combat, or objective systems as part of this handoff.
- Do not scatter bridge/log tips globally; place `TipTriggerZone` only at intended bridge-building/tutorial spaces.

## Implementation complete (code)
- All planned phases (1–5) and pause collectibles summary are committed on `cursor/feature-ui-ux-gamefeel-improvements-75e4`.
- Remote tracking branch was deleted; re-push before opening or refreshing a PR.

## Known limitations
- Unity Play Mode was not available in Cloud, so layout and serialized-reference behavior need Editor verification.
- A compact bridge-build instruction remains in the 9/9 log HUD as a fallback while contextual bridge-area tips are verified.
- Additional authored `TipDefinition` assets or scene `TipTriggerZone` placements have not been added yet.
- The objective tracker runtime container is created from code; visual padding/background must be checked in the Editor.
- Footstep VFX uses procedural fallback particles until a tuned `FootstepSurfaceEffectProfile` or particle prefabs are authored in Unity.
- Carried-log penalties still apply through existing `Carry` stat adjustments; deeper combat attack-speed integration is deferred to avoid broad combat rewrites.

## Risk notes
- Enemy/NPC behavior risk: Low for Phase 1.
- Interaction/dialogue/shop flow risk: Medium because `PlayerCanvas.prefab` and pause menu runtime UI are shared with dialogue/shop/pause flows.
- Player combat/input risk: Low; input is read for gating only, not rebound or intercepted.
- Pooling/performance risk: Medium-low; footstep particles are pooled and throttled, but visual/performance impact needs Play Mode observation.
