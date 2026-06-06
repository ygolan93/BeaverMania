# Level Design Examples

## Example Task Interpretation

**User request:**

"Add forest assets around the existing island terrain without blocking the main path."

### Correct behavior

- Inspect terrain and player route.
- Identify path and clear gameplay zones.
- Add trees mostly to borders and background.
- Add smaller foliage near path edges.
- Keep the center path readable.
- Use clusters, not random scatter.
- Align assets to terrain height.
- Avoid steep slopes for interactable objects.
- Organize under Environment/Trees and Environment/Grass.
- Mark static where appropriate.
- Report areas changed.

### Incorrect behavior

- Scatter trees everywhere.
- Block the player route.
- Place trees floating above terrain.
- Ignore scene hierarchy.
- Add too many high-poly assets.
- Hide enemies, NPCs, or objectives.
- Modify terrain destructively.
