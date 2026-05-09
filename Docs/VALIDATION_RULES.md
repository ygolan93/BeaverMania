# Validation Rules

## Editor prefab integration validation

Run `Tools/BeaverMania/Validation/Run Prefab Integration Validation` after prefab, ownership, runtime service, or serialized-reference changes.

Menu items:

- `Tools/BeaverMania/Validation/Run Prefab Integration Validation`: full prefab scan.
- `Tools/BeaverMania/Validation/Validate References`: missing `MonoBehaviour` scripts and detectable missing serialized object references.
- `Tools/BeaverMania/Validation/Validate Service Ownership`: runtime service duplication, ownership markers, and unapproved service locations.

The validator scans all project prefabs with `AssetDatabase.FindAssets("t:Prefab")` and prints one deterministic Unity console report sorted by rule, prefab path, and detail.

### Rules

- Missing `MonoBehaviour` scripts are errors.
- Serialized object references are errors when a referenced asset GUID is missing, or when a non-prefab-instance-backed local `fileID` does not exist in the prefab YAML.
- A component type whose script calls `RuntimeServices.Register` may appear in only one prefab unless this document and the validator allow-list are updated.
- Runtime service components are allowed only in approved service owner prefabs:
  - `Assets/Prefabs/Objects/UI/GameMusic.prefab`
  - `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab`
- Focus prefabs must keep ownership markers:
  - `Assets/Prefabs/Objects/UI/GameMusic.prefab`: `RuntimeBootstrapOwner.ownerId=runtime-bootstrap`
  - `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab`: `PrefabRuntimeHardening`
  - `Assets/Prefabs/Wasp/LVL1 Wasp.prefab`: `PrefabRuntimeHardening`
  - `Assets/Prefabs/Scorpion/ScorpionBoss.prefab`: `PrefabRuntimeHardening`
- Nested prefab instances containing runtime service components outside approved owner prefabs are errors.

## Required checks

- Run `Tools/BeaverMania/Validation/Run Prefab Integration Validation` before committing prefab integration changes.
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
