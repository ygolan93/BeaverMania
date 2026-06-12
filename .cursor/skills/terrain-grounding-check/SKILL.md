---
name: terrain-grounding-check
description: Detect and fix floating or buried objects on Unity terrain by measuring object root Y against sampled terrain height and raycast ground hits, snapping via the correct transform override, and validating in Play Mode. Use when NPCs, props, or gameplay objects float above or sink into the ground, when placing objects on terrain, or when deciding whether a runtime ground-snap component is justified.
---

# Terrain Grounding Check

Measure-first workflow for finding and fixing objects that float above or sink into the ground in Unity scenes.

## Measurement (never eyeball)

For each suspect object, gather three numbers via Unity MCP `execute_code`:

1. **Root world Y**: `transform.position.y`
2. **Terrain height**: on multi-tile terrains, find the tile whose XZ bounds contain the position, then `tile.SampleHeight(pos) + tile.transform.position.y`. `Terrain.activeTerrain` alone is wrong on tiled scenes.
3. **Actual ground Y**: raycast down from slightly above the feet (`pos + Vector3.up * 0.3f`), `QueryTriggerInteraction.Ignore`, sort hits by distance, skip hits whose collider `IsChildOf` the object itself. The first remaining hit is the real standing surface - it may be a carpet, platform, or floor above the terrain.

The gap is `rootY - groundY`. Objects standing on props (market carpets, decks, bridges) must snap to the raycast ground, not the terrain sample.

## Character-specific check

For animated characters, also sample foot/toe bone world Y in **Play Mode** (Edit Mode shows the serialized pose, which can be misleading if bone overrides were saved):

- Correctly rigged: toe-end bones sit at root Y, so grounding the root grounds the feet
- If Edit Mode hips sit at the root while the prefab default is above it, a saved bone override is masking the true runtime height - revert it before placing

## Fix policy (in order of preference)

1. **Adjust the existing override level**: if the instance already has a position override (scene or outer prefab), correct its Y there. Apply through the Editor with `Undo.RecordObject`, set `transform.position.y = groundY`, `MarkSceneDirty`, save scene. Never hand-edit YAML.
2. **Adjust the prefab asset** only if the object floats identically in every instance and the offset is internal to the prefab.
3. **Runtime ground-snap component** only as a last resort, when placement cannot stay reliable (terrain still being remastered, procedurally moved objects). A minimal snap component raycasts down once in `Start()` against a ground `LayerMask` and clamps Y - no Update loop, no added physics components.

Static NPCs/props without Rigidbody, CharacterController, or NavMeshAgent never self-correct: their placement is exactly as serialized, so a one-time correct snap is a permanent fix until the terrain changes again.

## Slope and placement sanity

- Keep interactable NPCs on ground flatter than ~10 degrees; check by sampling terrain height at small offsets around the feet
- After snapping, confirm nothing clips through the surface (renderer bounds min Y should be at or slightly above ground)

## Validation checklist (Play Mode)

- [ ] Root Y equals raycast ground Y (gap under 0.02) while animation plays
- [ ] Toe/foot bones rest on the surface, not inside or above it
- [ ] Object visible from the player's actual approach angles (orbit screenshots)
- [ ] Interaction ranges still trigger (distance checks measure from the object position that just moved)
- [ ] No new console errors; scene saved
