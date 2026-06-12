---
name: unity-prefab-debugging
description: Debug Unity prefab instance issues such as floating, misplaced, invisible, or disappearing objects by tracing GUID references, inspecting nested prefab overrides, verifying pivots/colliders/renderer bounds, and confirming behavior live via Unity MCP. Use when a prefab instance looks wrong in a scene, when diagnosing NPC/prop placement or visibility bugs, or when deciding at which override level (prefab asset, nested prefab, or scene instance) a fix belongs.
---

# Unity Prefab Debugging

Systematic workflow for diagnosing prefab instance bugs (floating, buried, invisible, disappearing, misplaced objects) without guessing, and for choosing the correct level to fix them.

## Core principle

A prefab instance bug has exactly one of these origins. Identify which before changing anything:

1. Prefab asset internals (bad pivot, offset model child, wrong collider center)
2. Nested prefab override (an outer prefab repositions an inner one)
3. Scene instance override (scene-level transform or property modification)
4. Runtime behavior (animation, physics, scripts toggling state)
5. Rendering (culling mask, renderer bounds, occlusion, LOD)

## Step 1 - Trace where instances come from (file level)

```powershell
# GUID of the prefab/script
Get-Content "Assets/Path/Thing.prefab.meta" | Select-String guid
```

Then search `Assets/` for that GUID to find every prefab and scene referencing it. Repeat upward through nesting (prefab A inside prefab B inside scene). Record the full chain - the bug usually lives in one specific link.

Key YAML markers when reading prefab/scene files:

- `m_Father: {fileID: 0}` - root transform of the prefab
- `PrefabInstance` + `m_Modifications` - overrides applied by the outer asset/scene
- `propertyPath: m_LocalPosition.y` entries - position overrides
- `stripped` Transform/MonoBehaviour - handles into nested prefabs

## Step 2 - Verify prefab internals before blaming the scene

On the prefab asset itself check:

- Root pivot: does collider bottom (center.y - height/2, after scale) sit at local Y=0?
- Model child offsets (skeleton root / mesh child local positions)
- Collider type, center, size; trigger vs solid
- Animator: `m_ApplyRootMotion`, `m_CullingMode`
- SkinnedMeshRenderer: `m_UpdateWhenOffscreen`, `m_AABB`
- Layer and tag of the root

If these are clean, the bug is in an override level or at runtime.

## Step 3 - Measure live via Unity MCP (do not eyeball)

Use `execute_code` to get hard numbers. Patterns that matter:

- World position of the instance root vs ground: `Physics.RaycastAll` downward with `QueryTriggerInteraction.Ignore`, skipping hits that are children of the instance (self-hits)
- Terrain height: pick the terrain tile containing the position (multi-tile scenes make `Terrain.activeTerrain.SampleHeight` wrong), then `tile.SampleHeight(pos) + tile.transform.position.y`
- Renderer culling bounds vs real mesh: compare `smr.bounds` against a `BakeMesh` + `RecalculateBounds` result; culling is only a real suspect if the baked mesh escapes the culling bounds
- Visibility: `renderer.isVisible`, `Animator.cullingMode`, camera `cullingMask`, baked occlusion data (`m_OcclusionCullingData` in the scene file)
- Pending overrides: `PrefabUtility.GetPropertyModifications(go)` to list serialized modifications, including bone transforms

Take positioned screenshots (`manage_camera` with `view_position`/`view_target`, or `batch="orbit"`) to document before/after and to find angles where an object visually disappears behind geometry.

## Step 4 - Watch for the edit-mode pose trap

In Edit Mode the Animator does not run, so bones show their **serialized** pose. If someone saved a scene with bone overrides (e.g. hips moved to the root), the character can look grounded in Edit Mode but jump up by the hips offset the moment Play Mode animation normalizes the pose. Symptoms:

- Hips/root bone local position overridden to (0,0,0) on the instance while the prefab default is non-zero
- Object placed "correctly" in Edit Mode but floating in Play Mode by exactly the hips offset times root scale

Fix: revert the bone override (`PrefabUtility.RevertPropertyOverride` on the bone's `m_LocalPosition`) so Edit Mode matches runtime, then place the root using Play Mode-true numbers.

## Step 5 - Fix at the right level, minimally

| Bug origin                          | Fix location                      |
| ----------------------------------- | --------------------------------- |
| Wrong everywhere the prefab is used | Prefab asset                      |
| Wrong only inside one outer prefab  | Override inside that outer prefab |
| Wrong only in one scene             | Scene instance override           |
| Runtime-only (animation/script)     | Script or animation, never YAML   |

Never hand-edit `.unity`/`.prefab` YAML for transform fixes - apply changes through the Editor (MCP `execute_code` with `Undo.RecordObject` + `MarkSceneDirty`, then save) so Unity keeps serialization consistent.

## Step 6 - Verify in Play Mode

Enter Play Mode and re-measure the same numbers (root vs ground, bone heights, `isVisible`). A fix is not done until the Play Mode numbers confirm it. Check `read_console` for new errors/warnings afterwards.

## Rule-out checklist for "object disappears"

- [ ] Distance/`SetActive` scripts actually referenced in the scene (GUID search, not assumption)
- [ ] Renderer culling bounds smaller than animated mesh (`BakeMesh` comparison)
- [ ] Animator culling mode (`CullCompletely` stops animation off-screen)
- [ ] Camera culling mask vs object layer
- [ ] Baked occlusion culling data with changed geometry
- [ ] LOD group / max render distance
- [ ] Plain line-of-sight occlusion by opaque geometry (orbit screenshots reveal this)
