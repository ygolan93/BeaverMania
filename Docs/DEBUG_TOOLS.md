# Debug Tools

## Ownership

| Tool/component | Owner | Lifetime | Gate |
| --- | --- | --- | --- |
| `DebugBootstrapper` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Persistent | `UNITY_EDITOR || DEVELOPMENT_BUILD`; `DebugQAConfig.enableDebugBootstrapper` |
| `FpsDisplay` | Spawned under `Debug QA Runtime` | Persistent debug runtime | `DebugQAConfig.enableFpsDisplay` |
| `DebugSceneResetShortcut` | Spawned under `Debug QA Runtime` | Persistent debug runtime | `DebugQAConfig.enableSceneResetShortcut` |
| `DebugCheckpointTeleport` | Spawned under `Debug QA Runtime` | Persistent debug runtime | `DebugQAConfig.enableCheckpointTeleport` |
| `DebugDamageTrigger` | Spawned under `Debug QA Runtime` | Persistent debug runtime | `DebugQAConfig.enableDamageTrigger` |
| `CombatDebugGate` | Player prefab | Player-local | `DebugQAConfig.disableCombatDamage` |
| `PrefabRuntimeHardening` | Owning prefab | Prefab-local | `DebugQAConfig.enableRuntimeReferenceValidation` |
| UI debug widgets | `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab` | Scene UI | UI object active state / scene wiring |

## Rules

- Persistent debug runtime belongs only to `GameMusic.prefab`.
- Do not add debug bootstrap components to player, enemy, hive, or canvas prefabs.
- Keep debug shortcuts config-driven; do not hard-code new keys outside `DebugQAConfig`.
- Debug validation may report missing refs but must not silently create gameplay dependencies.
- Player-local damage gating belongs on the player prefab, not on enemy prefabs.

## Optional refs

- `DebugBootstrapper.config` may be null only when all debug runtime features are intentionally disabled.
- Individual debug tool components must tolerate disabled config flags.
- UI debug refs may be null only for inactive/debug-only screens that are not used in the current scene.
