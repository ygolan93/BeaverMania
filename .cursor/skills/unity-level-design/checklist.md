# Level Design Checklist

## Terrain-Aware Placement Workflow

Follow this process:

1. Inspect the existing terrain.
2. Identify playable areas and non-playable areas.
3. Identify the main route and side routes.
4. Mark flat zones suitable for gameplay.
5. Mark slopes suitable only for decoration.
6. Place major gameplay objects first.
7. Place landmarks second.
8. Place large environmental assets third.
9. Add medium props.
10. Add small detail props.
11. Add paths, decals, grass, stones, and transitions.
12. Test player traversal.
13. Test camera readability.
14. Test combat and interaction spacing.
15. Optimize hierarchy, colliders, static flags, and prefab usage.

---

## Validation Checklist

After changes, verify:

### Terrain Fit

- [ ] No important asset is floating.
- [ ] No important asset is buried.
- [ ] Props align believably with terrain.
- [ ] Slopes are used correctly.
- [ ] Terrain textures support placed assets.

### Gameplay

- [ ] Player can move through the area.
- [ ] Main route is readable.
- [ ] Interactions work.
- [ ] NPCs are approachable.
- [ ] Enemies have enough space.
- [ ] Collectibles are reachable.
- [ ] No accidental soft-locks.
- [ ] No blocked objectives.

### Camera

- [ ] Camera does not clip through major props.
- [ ] Important objects are visible.
- [ ] Dense foliage does not hide gameplay.
- [ ] Landmarks are readable from useful angles.

### Scene Structure

- [ ] Objects are under correct parent objects.
- [ ] Names are clear.
- [ ] Repeated assets use prefabs.
- [ ] No unnecessary duplicates.
- [ ] No random loose GameObjects.
- [ ] No test objects left behind.

### Performance

- [ ] Static objects are marked static where appropriate.
- [ ] Repeated props use prefab instances.
- [ ] Colliders are simple.
- [ ] No unnecessary Rigidbody components.
- [ ] No excessive real-time lights.
- [ ] No excessive particle systems.
- [ ] Foliage density is reasonable.
