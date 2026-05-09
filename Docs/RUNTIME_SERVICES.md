# Runtime Services

`RuntimeServices` owns singleton-style registrations by component type. Persistent services must live only on `Assets/Prefabs/Objects/UI/GameMusic.prefab`, the runtime bootstrap owner prefab. Player prefabs must keep only player-local or scene-local systems.

| Service | Lifetime | Owner prefab | Notes |
| --- | --- | --- | --- |
| `SceneTransitionService` | Persistent (`RuntimeBootstrapOwner`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global scene loading and scene-service reset coordinator. |
| `CursorStateService` | Persistent (`RuntimeBootstrapOwner`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global cursor visibility/lock owner. |
| `GameFlowController` | Persistent (`RuntimeBootstrapOwner`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global boot/play/pause/game-over state owner. |
| `GameInputReader` | Persistent (`RuntimeBootstrapOwner`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global input polling and input-mode owner. |
| `DebugBootstrapper` | Persistent (`RuntimeBootstrapOwner`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Development/debug runtime feature bootstrapper. |
| `CheckpointService` | Scene-local | `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` | Player/scene checkpoint state; destroyed on scene reset. |

## Prefab rules

- Runtime bootstrap prefabs must include exactly one `RuntimeBootstrapOwner` with a stable, unique `ownerId` and `ownedServices` references to the services it owns.
- `RuntimeBootstrapOwner.ownerId` values must be unique across prefabs; `Tools/Runtime Services/Validate Prefab Registrations` reports duplicate owner ids.
- `DoNotDestroy` is legacy compatibility only. Do not use it as the sole ownership marker for new runtime services; attach `RuntimeBootstrapOwner` instead.
- Do not add persistent `RuntimeServices.Register(..., ServiceLifetime.Persistent)` components to player prefabs.
- Keep `GameFlowController` and `GameInputReader` off `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` and `Assets/Prefabs/OtterPlayer/Player Updated.prefab`.
- Keep player-specific systems, including scene-local `CheckpointService`, on the player prefab that owns that state.
- Use `Tools/Runtime Services/Validate Prefab Registrations` before committing prefab ownership changes.
