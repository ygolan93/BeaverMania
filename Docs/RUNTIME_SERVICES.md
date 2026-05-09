# Runtime Services

`RuntimeServices` registers singleton-style services by component type. Ownership is prefab-based, not scene-name-based.

## Service lifetime table

| Service | Lifetime | Owner prefab | Required refs | Optional refs | Notes |
| --- | --- | --- | --- | --- | --- |
| `SceneTransitionService` | `Persistent` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | `RuntimeBootstrapOwner.ownedServices` entry. | None. | Global scene loading and scene-service reset coordinator. |
| `CursorStateService` | `Persistent` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | `RuntimeBootstrapOwner.ownedServices` entry. | None. | Global cursor visibility/lock owner. |
| `GameFlowController` | `Persistent` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | `RuntimeBootstrapOwner.ownedServices` entry. | UI listeners may be scene-provided. | Global boot/play/pause/game-over state owner. |
| `GameInputReader` | `Persistent` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | `RuntimeBootstrapOwner.ownedServices` entry. | None. | Global input polling and input-mode owner. |
| `DebugBootstrapper` | `Persistent` | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | `RuntimeBootstrapOwner.ownedServices` entry; `DebugQAConfig` when debug tools are enabled. | Individual debug tools may be disabled by config. | Development/debug runtime feature bootstrapper. |
| `CheckpointService` | `Scene` | `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` | Player root/local checkpoint state. | Respawn offset callers. | Player/scene checkpoint state; destroyed on scene reset. |

## Rules

- Persistent services must live only on `Assets/Prefabs/Objects/UI/GameMusic.prefab`.
- `RuntimeBootstrapOwner.ownerId` must be stable and globally unique; current id is `runtime-bootstrap`.
- `RuntimeBootstrapOwner.ownedServices` must list every persistent service component owned by `GameMusic.prefab`.
- `DoNotDestroy` is legacy compatibility only; do not use it as the sole ownership marker for new runtime services.
- Do not add persistent `RuntimeServices.Register(..., ServiceLifetime.Persistent)` components to player, enemy, hive, or UI canvas prefabs.
- Keep `GameFlowController` and `GameInputReader` off both player prefabs.
- Keep player/scene-local systems, including `CheckpointService`, on the prefab that owns the state.
- Run `Tools/Runtime Services/Validate Prefab Registrations` before committing prefab ownership changes.
