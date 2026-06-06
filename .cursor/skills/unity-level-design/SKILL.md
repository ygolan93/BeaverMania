---
name: unity-level-design
description: >-
  Integrates assets into existing Unity scenes while preserving terrain shape,
  gameplay flow, scale, performance, and visual consistency. Use when expanding
  or improving levels, adding props/trees/rocks/buildings/bridges/NPCs/enemies/
  collectibles, organizing scene hierarchy, improving visual density, replacing
  placeholders, or reviewing asset placement in Unity scenes.
---

# Unity Level Design Scalability & Asset Integration

## Purpose

Use this skill when working on Unity level design tasks that involve expanding, improving, or reorganizing an existing scene while preserving the current terrain structure, gameplay flow, scale, performance, and visual consistency.

This skill focuses on:

- Integrating new assets into an existing Unity scene.
- Respecting existing Terrain shape, height, slopes, paths, and gameplay areas.
- Maintaining scalable level-design structure.
- Avoiding messy scene hierarchy, random asset placement, and unoptimized prefabs.
- Improving composition, navigation, readability, and player guidance.

## When To Use This Skill

Use this skill for tasks such as:

- Adding props, trees, rocks, buildings, bridges, paths, platforms, enemies, collectibles, NPCs, or interactable objects into an existing Unity scene.
- Improving the visual density of an existing level.
- Turning a rough terrain blockout into a playable level.
- Replacing placeholder assets with final or semi-final assets.
- Expanding a level without breaking scale.
- Designing areas around existing terrain elevation.
- Creating readable paths, landmarks, zones, and progression flow.
- Reviewing whether assets are placed correctly inside a scene.
- Organizing a scene so future level expansion remains clean and maintainable.

## Core Principle

Do not treat the scene as an empty canvas.

Always analyze the existing scene first:

1. Terrain shape
2. Existing paths
3. Player scale
4. Camera perspective
5. Gameplay mechanics
6. Current scene hierarchy
7. Existing lighting and atmosphere
8. Existing prefab conventions
9. Performance impact
10. Navigation and traversal constraints

Only then modify or add assets.

## Agent Workflow

When asked to modify a level:

1. **Inspect** the existing scene (Unity MCP `manage_scene`, hierarchy, terrain, spawn/objectives if available).
2. **Summarize** current level structure using [Initial analysis](reference.md#required-initial-analysis).
3. **Identify** safest areas for modification; do not move critical gameplay objects without understanding scripts/references.
4. **Propose** a short level-design plan before implementing.
5. **Implement** using rules in [reference.md](reference.md) and [checklist.md](checklist.md).
6. Keep changes modular and reversible; prefer parent containers and prefab variants.
7. Preserve existing gameplay references and scene hierarchy conventions.
8. **Report** using [Output format](#output-format-for-cursor-agent) below.

### Terrain-Aware Placement Order

Follow this process (details in [checklist.md](checklist.md)):

1. Inspect existing terrain → 2. Playable vs non-playable areas → 3. Main and side routes → 4. Flat gameplay zones → 5. Decoration-only slopes → 6. Major gameplay objects → 7. Landmarks → 8. Large environment → 9. Medium props → 10. Small detail → 11. Paths/transitions → 12–15. Traversal, camera, combat, optimization checks.

### BeaverMania Project

When working in `BeaverProject`, also read [beavermania.md](beavermania.md) for scene/prefab safety, high-risk assets, and scope limits.

## Final Rule

Level design is not decoration.

Every asset placed in the scene must support at least one of these:

- Gameplay clarity
- Navigation
- Visual composition
- Environmental storytelling
- Performance-safe scene density
- Future scalability

## Output Format For Cursor Agent

When completing a task, provide:

### Summary

Briefly explain what was changed.

### Scene Structure

List the parent objects or zones created/modified.

### Terrain Considerations

Explain how placement respected terrain height, slope, paths, and playable zones.

### Gameplay Considerations

Explain whether player traversal, interaction, combat, or objectives were affected.

### Performance Notes

Mention static flags, prefab usage, colliders, lights, particles, or density concerns.

### Manual Review Needed

List anything the developer should visually inspect inside Unity.

## Additional Resources

- [reference.md](reference.md) — asset integration, scalability, gameplay placement, composition, collision, lighting, anti-patterns
- [checklist.md](checklist.md) — validation checklist and full placement workflow
- [examples.md](examples.md) — correct vs incorrect task interpretation
- [beavermania.md](beavermania.md) — BeaverMania-specific constraints
