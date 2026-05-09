# Runtime Services

`RuntimeServices` owns singleton-style registrations by component type. Persistent services must live only on `Assets/Prefabs/Objects/UI/GameMusic.prefab`, the runtime-service owner prefab. Player prefabs must keep only player-local or scene-local systems.

| Service | Lifetime | Owner prefab | Notes |
| --- | --- | --- | --- |
| `SceneTransitionService` | Persistent (`DontDestroyOnLoad`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global scene loading and scene-service reset coordinator. |
| `CursorStateService` | Persistent (`DontDestroyOnLoad`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global cursor visibility/lock owner. |
| `GameFlowController` | Persistent (`DontDestroyOnLoad`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global boot/play/pause/game-over state owner. |
| `GameInputReader` | Persistent (`DontDestroyOnLoad`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Global input polling and input-mode owner. |
| `DebugBootstrapper` | Persistent (`DontDestroyOnLoad`) | `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Development/debug runtime feature bootstrapper. |
| `CheckpointService` | Scene-local | `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` | Player/scene checkpoint state; destroyed on scene reset. |

## Prefab rules

- Do not add persistent `RuntimeServices.Register(..., ServiceLifetime.Persistent)` components to player prefabs.
- Keep `GameFlowController` and `GameInputReader` off `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` and `Assets/Prefabs/OtterPlayer/Player Updated.prefab`.
- Keep player-specific systems, including scene-local `CheckpointService`, on the player prefab that owns that state.
- Use `Tools/Runtime Services/Validate Prefab Registrations` before committing prefab ownership changes.
