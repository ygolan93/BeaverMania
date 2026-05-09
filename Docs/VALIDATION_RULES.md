# Validation Rules

## Required checks

- Run `Tools/Runtime Services/Validate Prefab Registrations` after adding, moving, removing, or variant-overriding any component that calls `RuntimeServices.Register`.
- Run prefab play-mode smoke tests after changing required refs, tags, layers, animation paths, hitboxes, or UI event wiring.
- Keep `PrefabRuntimeHardening.requiredComponents` and `requiredObjects` aligned with this documentation.

## Runtime service validation

- No duplicate `RuntimeServices.Register` component type may exist across project prefabs unless explicitly documented.
- No duplicate `RuntimeBootstrapOwner.ownerId` may exist across project prefabs.
- Persistent services must be registered from `GameMusic.prefab` only.
- Scene services must be safe to destroy through `RuntimeServices.ResetSceneServices()`.

## Reference validation

- Required refs: non-null serialized components/objects needed for startup, gameplay, damage, input, UI navigation, spawning, or scene transition.
- Optional refs: null-safe, feature-gated, scene-provided, or cosmetic refs.
- `PrefabRuntimeHardening` should cover required refs that can be validated on `Awake` without scene dependencies.
- Do not add validation for refs intentionally resolved from the active scene.

## Debug validation

- `DebugQAConfig.enableRuntimeReferenceValidation` may disable validation for debug sessions only; do not rely on that for production fixes.
- `DebugBootstrapper` must remain persistent and owned by `GameMusic.prefab`.
- Debug-only controls must compile out or stay gated by `UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Prefab variant validation

- After editing a base prefab, inspect variants for stale overrides, missing fileIDs, removed components, and duplicate services.
- Do not apply variant overrides that remove required components/objects without updating validation and ownership docs.
- Recheck animation clips, Timeline, VFX, button events, and scene serialized refs after hierarchy renames.

## Hierarchy validation

- Do not rename or regroup Level 1/Menu scene objects for cleanup-only changes.
- New hierarchy conventions are allowed only for future scenes or editable prefabs.
- Prefer explicit serialized refs over hard-coded names, child paths, tags, or global `Find` calls.
