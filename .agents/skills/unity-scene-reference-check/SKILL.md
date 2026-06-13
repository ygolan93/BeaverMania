---
name: unity-scene-reference-check
description: Inspect Unity scene and prefab serialized references for Beavermania before changing wiring. Use when a task mentions scene instances, prefab references, Inspector assignments, GameObject links, missing serialized references, or Level scene validation.
---

# Unity Scene Reference Check

Use this before editing Unity scenes, prefabs, or Inspector wiring. Treat scene YAML as fragile serialized runtime data; prefer Unity Editor/MCP inspection over direct text edits.

## Workflow

1. Read the active handoff or task file first.
2. Identify the exact target scene, prefab, component, and serialized fields.
3. Inspect the prefab definition and every required scene instance separately.
4. Record whether references are prefab defaults, scene overrides, missing links, or inactive UI objects.
5. Change only the required fields, using Unity Editor tooling where available.
6. Reinspect after saving to confirm the serialized links point to the intended objects.

## Beavermania Rules

- Use `GameMaster`, not `GameManager`, when searching this repo.
- Do not rename `MonoBehaviour` classes, files, public fields, or serialized fields during a wiring pass.
- Do not hand-edit `.unity` or `.prefab` YAML unless Unity tooling is unavailable and the target fileID/GUID relationship has been verified.
- For Level 1 boss work, explicitly inspect `Assets/Scenes/Level 1 - Remastered - Steam.unity`; do not infer it is wired because another Level 1 scene is wired.

## Report

Return:

- inspected scenes/prefabs
- components and fields checked
- fields changed
- unresolved missing references
- Unity Editor or Play Mode verification status
- manual follow-up required
