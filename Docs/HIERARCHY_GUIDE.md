# Hierarchy Guide

See also: [Hierarchy and Reference Checklist](HierarchyAndReferenceChecklist.md).

## Scope

- Current Level 1/Menu scene hierarchies are frozen for cleanup-only changes.
- New conventions apply to future scenes and editable prefab internals only.
- Imported/third-party prefab contents should not be edited directly; create a local editable variant first.

## Future scene root groups

- `_Runtime` — runtime bootstrap objects, scene services, managers, spawners, generated runtime containers.
- `_Player` — player root, player controller dependencies, player colliders, player VFX/SFX, spawn points.
- `_Camera` — active cameras, camera rigs, Cinemachine objects, bounds, camera helpers.
- `_UI` — canvases, event systems, HUD/menu roots, UI animation/audio hooks.
- `_Enemies` — enemy roots, spawn points, encounter containers, patrol/waypoint helpers.
- `_Hazards` — traps, damage volumes, environmental hazards, temporary danger zones, hazard VFX/SFX.
- `_Interactables` — pickups, doors, switches, bridges, NPC interaction anchors, objectives, non-hazard triggers.
- `_Audio` — music emitters, ambient loops, mixer-routing helpers, one-shot pools, scene-local audio triggers.

## Future prefab groups

- `_Runtime` — prefab-local service/state helpers only; no persistent global services unless this prefab is the documented owner.
- `_Visuals` — meshes, skinned meshes, renderers, model roots.
- `_Colliders` — movement, trigger, detection, and physics colliders.
- `_Hitboxes` — damage/parry/strike volumes and combat ownership markers.
- `_Audio` — emitters and prefab-local audio routing.
- `_VFX` — particles, trails, decals, effect anchors.
- `_UI` — world-space UI, floating text, HUD anchors.
- `_Sockets` — stable attachment/spawn/IK/weapon/camera anchors.
- `_Debug` — debug-only helpers and validation markers.

## Reference rules

- Prefer serialized refs, typed components, interfaces, and `TryGetComponent` over tags or hierarchy names.
- Avoid new `Transform.Find`, global `Find`, hard-coded child paths, and string tag dependencies.
- Before renaming children, check animation clips, Timeline, VFX bindings, UI events, audio hooks, and scene refs.
- Preserve existing tags/layers unless all consumers, physics masks, raycasts, and camera masks are audited.
