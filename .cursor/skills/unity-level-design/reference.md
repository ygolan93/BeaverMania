# Level Design Reference

## Required Initial Analysis

Before making changes, inspect the existing scene and identify:

### Terrain

- Terrain size and scale.
- Main elevation changes.
- Slopes that are walkable vs. decorative.
- Existing flat areas.
- Natural paths or possible paths.
- Cliffs, valleys, rivers, caves, bridges, or chokepoints.
- Areas where assets would float, clip, or look disconnected.

### Player Movement

- Approximate player height.
- Movement speed.
- Jump height.
- Combat range.
- Interaction range.
- Camera distance and angle.
- Whether the player can clearly see objectives and paths.

### Existing Level Structure

- Spawn point.
- Main route.
- Side areas.
- Objective locations.
- Enemy zones.
- NPC/trader zones.
- Collectible routes.
- Bridges or traversal gates.
- Current boundaries.

### Visual Style

- Cartoon / stylized / realistic / low-poly / fantasy / natural.
- Existing material palette.
- Existing foliage density.
- Existing asset scale.
- Existing lighting mood.
- Existing terrain textures.

### Scene Organization

- Existing parent objects.
- Naming conventions.
- Prefab usage.
- Existing managers.
- Existing ScriptableObjects.
- Existing object layers and tags.
- Existing colliders and nav-related components.

---

## Asset Integration Rules

### 1. Respect Terrain Height

Every placed asset must be aligned to the terrain surface unless intentionally floating.

For each asset:

- Raycast downward to terrain if needed.
- Match the Y position to the terrain height.
- Avoid visible floating.
- Avoid deep clipping.
- Rotate slightly to match slopes when appropriate.
- Keep major gameplay objects level if player interaction requires precision.

### 2. Respect Terrain Slope

Do not place important gameplay objects on aggressive slopes unless that is intentional.

Avoid placing these on steep terrain:

- Traders
- Shops
- Dialogue triggers
- Combat arenas
- Quest objects
- Bridges
- Collectibles requiring precise pickup
- Interactive props
- Spawn points

Use flatter ground or create/identify a plateau.

### 3. Preserve Player Readability

The player must understand where to go.

Use:

- Paths
- Clearings
- Landmarks
- Lighting contrast
- Repeated visual language
- Asset framing
- Bridges
- Gates
- Natural corridors
- Silhouettes

Avoid:

- Dense clutter near the main path
- Random objects blocking visibility
- Repeated assets without variation
- Important objects hidden behind noise
- Excessive vertical scale without purpose

### 4. Maintain Scale Consistency

Before placing assets, compare them to:

- Player height
- Trees already in scene
- Rocks already in scene
- Existing buildings
- Terrain size
- Doorways / bridges / platforms
- Camera view

Do not randomly scale assets unless necessary.

Recommended:

- Use small variation for natural props: 0.85–1.25 scale.
- Use larger variation only for background decoration.
- Keep interactable objects consistent and predictable.
- Avoid scaling colliders into broken proportions.

### 5. Integrate Assets Into the Scene, Not Onto the Scene

New assets should feel embedded in the environment.

Use supporting details:

- Rocks near cliffs.
- Grass around tree bases.
- Dirt paths near travel routes.
- Small props near traders or camps.
- Foliage transitions between terrain textures.
- Broken logs near forests.
- Stones around bridges or cave entrances.
- Decorative clusters instead of isolated random props.

### 6. Avoid Gameplay Blockage

Do not block:

- Main player route.
- Combat movement.
- Camera line of sight.
- Bridge paths.
- Interaction zones.
- Enemy navigation.
- Collectible access.
- Restart/checkpoint routes.

If blocking is intentional, make it visually obvious.

### 7. Use Asset Clustering

Prefer believable clusters over uniform distribution.

Good examples:

- 3–7 rocks near a cliff edge.
- Tree clusters with open gaps.
- Grass patches near soft terrain transitions.
- Props grouped around camps/NPCs.
- Fallen logs near wooded zones.
- Small stones along path borders.

Bad examples:

- Evenly spaced trees like a grid.
- One random rock in the middle of a path.
- Repeating the same prefab with identical rotation.
- Dense clutter everywhere.

---

## Scalability Rules

### Scene Hierarchy

Organize new level content under clear parent objects:

```
Level
├── Terrain
├── Gameplay
│   ├── PlayerSpawn
│   ├── Checkpoints
│   ├── NPCs
│   ├── Enemies
│   ├── Collectibles
│   └── Interactables
├── Environment
│   ├── Rocks
│   ├── Trees
│   ├── Grass
│   ├── Props
│   ├── Paths
│   └── Structures
├── Lighting
├── VFX
├── Audio
└── Boundaries
```

If the scene already has a structure, follow it instead of creating a competing one.

### Naming

Use clear names:

Good:

- ENV_RockCluster_Cliff_01
- ENV_TreeCluster_ForestEntrance_01
- GP_TraderCamp_01
- GP_BridgeBuildZone_01
- COL_CoinTrail_RiverPath_01
- BND_LevelEdge_North_01

Bad:

- Cube (18)
- TreeNew
- test object
- rock final final
- GameObject

### Prefab Usage

Whenever possible:

- Use prefabs for repeated objects.
- Do not unpack prefabs unless necessary.
- Create prefab variants for repeated design variations.
- Keep scene overrides intentional.
- Avoid editing imported package prefabs directly.
- Prefer local project prefab variants.

### Modular Level Design

Design level areas as reusable zones:

Examples:

- Forest path segment
- Trader camp
- Bridge construction area
- Enemy encounter pocket
- Collectible trail
- Cliff boundary
- River crossing
- Resource gathering zone
- Tutorial objective area

Each zone should be easy to move, duplicate, disable, or replace.

### Performance

When adding environment assets:

- Use LOD Groups when available.
- Use static batching for static props.
- Mark non-moving environment objects as Static.
- Avoid unnecessary MeshColliders.
- Use simple colliders for gameplay.
- Avoid excessive real-time lights.
- Avoid too many particle systems in one area.
- Avoid overusing transparent materials.
- Keep foliage density reasonable.
- Do not place high-poly assets everywhere.

---

## Gameplay Placement Rules

### Traders / NPCs

Place NPCs only where:

- Ground is mostly flat.
- Player has enough space to approach.
- Dialogue camera has room.
- UI interaction prompt is visible.
- NPC is not hidden by foliage.
- NPC does not stand inside uneven terrain.
- Nearby props support the character's role.

Recommended support assets:

- Small camp table
- Crates
- Rug
- Lantern
- Signpost
- Fence
- Path leading toward the NPC

### Enemies

Enemy zones should have:

- Enough space for movement.
- Clear entry/exit points.
- No excessive terrain bumps.
- No props that break enemy navigation.
- Visual warning before encounter.
- Optional cover or obstacles, but not clutter.

### Collectibles

Collectibles should:

- Lead the player subtly.
- Reward exploration.
- Not be hidden in visual noise.
- Follow terrain contours.
- Avoid steep slopes unless intentional.
- Use spacing that matches player movement speed.

### Bridges / Build Zones

Bridge areas should:

- Clearly show a gap or obstacle.
- Have enough room on both sides.
- Align with terrain edges.
- Avoid impossible angles.
- Have visible start/end anchors.
- Use support rocks/logs/planks to make placement believable.

### Resource Gathering

Trees, logs, stones, or resources should:

- Be visually distinct from decoration.
- Have enough interaction space.
- Avoid being buried in dense foliage.
- Be placed where harvesting makes environmental sense.
- Use consistent prefab variants for interactable vs. decorative versions.

---

## Visual Composition Rules

Every area should have:

### 1. A Purpose

Examples:

- Combat
- Exploration
- Resource gathering
- Trading
- Navigation
- Tutorial
- Reward
- Story moment
- Environmental storytelling

### 2. A Landmark

Examples:

- Huge tree
- Broken bridge
- Trader camp
- Cave entrance
- Statue
- Big rock formation
- Waterfall
- Glowing object
- Unique building

### 3. A Path

The player should see a logical way forward.

Paths can be created using:

- Dirt texture
- Grass clearing
- Stones
- Fences
- Torches
- Repeated props
- Terrain shape
- Lighting
- Coin trail
- Enemy placement

### 4. Boundaries

Boundaries should feel natural.

Use:

- Cliffs
- Dense trees
- Rocks
- Water
- Fences
- Fallen logs
- Height differences

Avoid invisible walls unless hidden behind believable physical limits.

---

## Terrain Texture Integration

When placing assets, terrain textures should support the scene:

- Dirt under paths.
- Grass in open natural zones.
- Mud near water or low valleys.
- Moss near rocks, trees, shaded zones.
- Sand or dry dirt in exposed areas.
- Stones near cliffs and slopes.

Do not place trees, rocks, or structures without checking whether the terrain texture underneath makes sense.

---

## Collision Rules

For each placed asset:

### Decorative Props

- Use simple colliders or no colliders.
- Avoid blocking the player unnecessarily.
- Mark static if not moving.

### Gameplay Props

- Use accurate but simple colliders.
- Ensure interaction triggers are reachable.
- Avoid collider mismatch after scaling.
- Test from multiple approach directions.

### Terrain/Cliff Boundaries

- Use clear physical blockers when needed.
- Avoid tiny collider gaps.
- Avoid invisible walls in open areas unless unavoidable.
- Make boundaries visually readable.

---

## Lighting & Atmosphere Rules

When adding assets:

- Do not break the existing lighting mood.
- Avoid placing important objects in unreadable darkness.
- Use subtle lights only when they guide the player.
- Avoid too many real-time lights.
- Use baked or static lighting where possible.
- Keep color temperature consistent with the scene.

For stylized/cartoon scenes:

- Prefer readable silhouettes.
- Use clean contrast.
- Avoid noisy realism.
- Keep materials simple and coherent.
- Use exaggerated shapes carefully.

---

## Anti-Patterns To Avoid

Never do the following:

- Do not randomly scatter assets across the terrain.
- Do not place gameplay objects on steep slopes unless intentional.
- Do not ignore player scale.
- Do not hide objectives behind dense decoration.
- Do not create a messy hierarchy.
- Do not unpack prefabs without a reason.
- Do not use MeshCollider for everything.
- Do not place trees or rocks in perfect grids.
- Do not overfill the scene just to make it look richer.
- Do not create beautiful areas that break gameplay.
- Do not modify terrain destructively without checking existing gameplay routes.
- Do not move existing critical objects without understanding their scripts and references.
- Do not replace scene structure with a new one unless explicitly requested.
