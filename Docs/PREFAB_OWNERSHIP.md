# Prefab Ownership

## Rules

- A prefab owns only the serialized references, child hierarchy, runtime services, and debug hooks listed for that prefab.
- Required refs must stay assigned; missing refs are runtime defects.
- Optional refs may be null only when the feature is disabled or scene-provided.
- Persistent services must live on `Assets/Prefabs/Objects/UI/GameMusic.prefab` only.
- Scene/player state must stay on the prefab that owns the state.
- Do not move persistent services into player, enemy, hive, or UI canvas prefabs.
- Do not add `RuntimeServices.Register(..., ServiceLifetime.Persistent)` to prefab variants unless this document is updated first.

## Ownership matrix

| Prefab | Owns | Required refs | Optional refs | Service lifetime | Debug ownership | Variant risk |
| --- | --- | --- | --- | --- | --- | --- |
| `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab` | Canonical shapekey player runtime: controller stack, combat, inventory, health, camera helpers, HUD anchors, local audio/VFX, checkpoint state. | Core player components referenced by `PrefabRuntimeHardening.requiredComponents`; player root, movement/controller, health/combat/inventory/interaction dependencies, `CheckpointService`. | UI text/debug text, waypoint/objective UI hooks, camera/audio/VFX helpers that can be scene-provided. | `CheckpointService` is `Scene`; no persistent services allowed. | Owns `CombatDebugGate` and player-local runtime-reference validation only. | High: child names, animator bindings, blend-shape/skinned-mesh paths, tags/layers, and scene references may be variant-overridden. |
| `Assets/Prefabs/OtterPlayer/Player Updated.prefab` | Legacy/alternate player presentation and player-local scripts. | Existing player-local controller/health/combat/UI/audio refs serialized on the prefab. | Debug text, presentation-only UI/VFX/audio links. | No persistent services; do not add `GameFlowController`, `GameInputReader`, or other global services. | No global debug ownership; debug refs are player-local only. | High: likely diverges from canonical player; do not assume overrides propagate between player prefabs. |
| `Assets/Prefabs/Wasp/LVL1 Wasp.prefab` | Level 1 wasp enemy root, AI, health, combat feedback, audio, floating UI/VFX. | `PrefabRuntimeHardening.requiredComponents`; required child objects for hit/visual/UI/audio anchors. | Cosmetic VFX/audio emitters and floating debug/UI text. | None. | Owns enemy-local runtime-reference validation only. | Medium: imported/model child paths and animation/event refs can break on rename. |
| `Assets/Prefabs/Scorpion/ScorpionBoss.prefab` | Boss scorpion enemy root, AI, health, boss hitboxes, combat feedback, audio/VFX. | `PrefabRuntimeHardening.requiredComponents`; required child objects for boss hitbox/visual/audio anchors. | Presentation VFX/audio and combat-feedback emitters. | None. | Owns boss-local runtime-reference validation only. | High: boss hitboxes and animation/event bindings are sensitive to hierarchy edits. |
| `Assets/Prefabs/Scorpion/Scorpion.prefab` | Non-boss scorpion enemy root, AI, health, hitboxes, combat feedback, audio/VFX. | Existing serialized AI/health/hitbox/audio/VFX refs. | Presentation VFX/audio and combat-feedback emitters. | None. | No global debug ownership. | High: many nested/remains child scripts; prefab variant overrides are easy to orphan. |
| `Assets/Prefabs/Hive/NewHive.prefab` | Hive objective/spawner root, hive health, wasp spawning, combat feedback, UI marker. | `Static_Hive`, `WaspSpawner`, `NPC_Health`, spawn/feedback refs serialized on the prefab. | UI marker, floating text, cosmetic feedback/audio. | None. | No global debug ownership. | Medium: spawn-point and objective references are gameplay-critical. |
| `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab` | Player HUD/menu/dialogue/shop/lose-menu UI hierarchy and presenters. | Canvas hierarchy, HUD presenter refs, dialogue/shop/menu controller refs, event/navigation links. | Debug reference widgets, inactive screens, scene-provided player refs. | Scene UI only; no persistent runtime services. | Owns UI debug widgets only; not the debug runtime bootstrapper. | High: UI object names, animation paths, navigation, and serialized button events are brittle. |
| `Assets/Prefabs/Objects/UI/GameMusic.prefab` | Runtime bootstrap owner, owner of music playback (`MusicPlaylist` + local `AudioSource`), scene transitions, cursor, game flow, input, debug bootstrapper. | `RuntimeBootstrapOwner.ownerId=runtime-bootstrap`; `ownedServices` refs to global services; `MusicPlaylist.MusicSource` assigned to the local `AudioSource`; `DoNotDestroy` legacy shim. | `DebugBootstrapper.config` may disable individual debug tools. | `SceneTransitionService`, `CursorStateService`, `GameFlowController`, `GameInputReader`, and `DebugBootstrapper` are `Persistent`. | Sole owner of persistent debug bootstrap/runtime debug tools. | Medium: duplicated variants create duplicate singleton services and duplicate `ownerId`s. |

## Required vs optional refs

- Required refs are dependencies without fallback, or refs listed in `PrefabRuntimeHardening.requiredComponents` / `requiredObjects`.
- Optional refs are refs with feature-gated usage, null-safe code paths, scene lookup fallback, or cosmetic-only behavior.
- When promoting an optional ref to required, add it to `PrefabRuntimeHardening` where available and update this file.
- When removing a required ref, migrate callers to typed optional access first.

## Service lifetime

- `Persistent`: survives scene transitions; only `GameMusic.prefab` may own these services.
- `Scene`: reset/destroyed on scene reload; player/scene-owned state only.
- `None`: normal prefab component; no `RuntimeServices.Register` ownership.

## Debug ownership

- Persistent debug runtime: `GameMusic.prefab` via `DebugBootstrapper`.
- Player combat/debug gates: player prefab only.
- Reference validation: each prefab may own its own `PrefabRuntimeHardening` component.
- UI debug widgets: `PlayerCanvas.prefab` only.

## Prefab-variant risks

- Variants can override component arrays with stale fileIDs; revalidate required refs after base-prefab edits.
- Avoid renaming child objects used by animation clips, Timeline, VFX, UI events, or scene serialized refs.
- Avoid changing tags/layers unless all consumers and physics/camera masks are audited.
- Keep singleton/runtime-service components out of variants unless the base prefab owns that service.

## Allowed future hierarchy conventions

- New editable prefabs may group children by `_Runtime`, `_Visuals`, `_Colliders`, `_Hitboxes`, `_Audio`, `_VFX`, `_UI`, `_Sockets`, and `_Debug`.
- New scenes may use root groups from `Docs/HIERARCHY_GUIDE.md`.
- Existing Level 1/Menu scene hierarchies remain frozen; prefer prefab-local cleanup and explicit serialized refs.
