# Hierarchy and Reference Checklist

## Scope

- This checklist is for future hierarchy/reference cleanup passes only.
- Do **not** rename, regroup, or otherwise reorganize existing Level 1/Menu scene objects.
- Apply naming/reference cleanup only inside editable prefabs.
- Do not edit third-party, imported, generated, or package-owned prefab contents unless a local editable variant is created first.

## Future Scene Groups

Use these root-level groups only for future scenes or newly-authored scene layouts:

- `_Runtime` — runtime bootstrap objects, scene services, managers, spawners, and generated runtime containers.
- `_Player` — player root, player controller dependencies, player-only colliders, player VFX/SFX emitters, and player spawn points.
- `_Camera` — active cameras, camera rigs, Cinemachine objects, camera bounds, and camera-only helpers.
- `_UI` — canvases, UI event systems, HUD/menu roots, UI-only animation/audio hooks, and screen-space helpers.
- `_Enemies` — enemy roots, enemy spawn points, enemy encounter containers, and enemy-only patrol/waypoint helpers.
- `_Hazards` — traps, damage volumes, environmental hazards, temporary danger zones, and hazard VFX/SFX emitters.
- `_Interactables` — pickups, doors, switches, bridges, NPC interaction anchors, objectives, and non-hazard trigger objects.
- `_Audio` — music emitters, ambient loops, mixer-routing helpers, one-shot audio pools, and scene-local audio triggers.

## Level 1/Menu Freeze

- Treat current Level 1/Menu scene object names and grouping as compatibility-sensitive.
- Do not move Level 1/Menu objects under the future groups above.
- Do not rename Level 1/Menu objects to satisfy naming conventions.
- If a Level 1/Menu reference must be fixed, prefer prefab-local fixes or explicit serialized references over scene hierarchy edits.

## Editable Prefab Cleanup Rules

- Cleanup is allowed only inside project-owned editable prefabs.
- Prefer prefab variants for imported/third-party content.
- Keep prefab public API stable: child names, serialized fields, tags, layers, and animation paths may be externally referenced.
- Before renaming prefab children, check animation clips, Timeline bindings, VFX bindings, audio/event hooks, and serialized scene references.
- Prefer explicit serialized references on components over `Transform.Find`, hard-coded child paths, or scene-wide name lookups.

## Tag Dependencies

Preserve these tag dependencies until the corresponding systems are migrated to typed references/components:

- `Player` — built-in Unity tag expected by player detection, targeting, camera, and damage/interaction code paths.
- `NPC` — NPC detection/interaction routing.
- `Scorpion` — scorpion enemy identification.
- `Boss` — boss encounter identification and boss-specific logic.
- `Hive` — hive/objective/enemy-spawn identification.
- `Strike` — player/enemy strike hitbox identification.
- `Damage` — generic damage volume/projectile identification.
- `ScorpionDamage` — scorpion-specific damage volume/projectile identification.
- `ScorpionSting` — scorpion sting hitbox/effect identification.
- `Isle` — island/terrain region trigger identification.
- `Bridge` — bridge/interactable traversal object identification.
- `Arena` — arena/encounter boundary identification.

## Layer Dependencies

Current project layers that may affect collision matrices, raycasts, cameras, post-processing, and filtering:

- `Default`
- `TransparentFX`
- `Ignore Raycast`
- `Character`
- `Water`
- `UI`
- `Post Process`
- `Pieces`
- `Feet`
- `ClimbLayer`
- `Enemy`
- `BeaverNPC`
- `IgnoreOtherBeavers`

Before changing any layer assignment or layer name, verify:

- Project collision matrix.
- Physics/raycast masks in scripts and serialized fields.
- Camera culling masks.
- Post-processing/renderer feature masks.
- Animator, VFX, and gameplay trigger assumptions.

## New Code Rules

- Prefer typed components, interfaces, and serialized references over tag strings in new code.
- Use tags only as compatibility shims for existing systems.
- When replacing a tag dependency, add a component/interface marker and migrate callers incrementally.
- Avoid new hard-coded names, child paths, root-group names, and global `Find` calls.
- Prefer dependency injection, inspector references, `TryGetComponent`, and small marker interfaces/components.

## Review Checklist

- [ ] No Level 1/Menu scene object was renamed.
- [ ] No Level 1/Menu scene object was regrouped.
- [ ] Cleanup changes are limited to editable prefabs.
- [ ] Tag dependencies above are preserved or explicitly migrated.
- [ ] Layer dependencies above are preserved or explicitly migrated.
- [ ] New code uses typed references/components/interfaces instead of tag strings.
- [ ] Serialized references, animation paths, Timeline bindings, and prefab variants remain valid.
