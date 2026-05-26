# Unity Safety Rules

- Do not casually edit scenes, prefabs, animation clips, Animator assets, or UI hierarchy assets.
- Avoid hand-editing Unity YAML unless a task explicitly scopes the asset edit and Unity validation is available.
- Preserve serialized fields and existing `.meta` GUIDs.
- Avoid renaming or moving `MonoBehaviour` classes and their files.
- Avoid changing or removing public fields used by the Inspector without checking every serialized use and planning migration.
- Prefer C# guards and local script fixes over prefab edits where possible.
- Do not assume Inspector references are assigned. Handle plausible missing references defensively.
- If an Inspector assignment is required, list it as manual Unity work rather than attempting to infer asset wiring.
- Any scene or prefab change requires Unity Play Mode verification.

See [../MIGRATION.md](../MIGRATION.md) for frozen script GUIDs and migration-sensitive bindings.
