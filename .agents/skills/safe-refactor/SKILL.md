---
name: safe-refactor
description: Use this skill for serialized-safe Unity refactors in Beavermania, especially when changing prefab-bound C# flow, introducing adapters, or migrating shared UI behavior without renaming files, classes, or serialized fields in the first pass.
---

# Safe Refactor

Refactor in compatibility-preserving stages.

## Rules

- Do not rename `MonoBehaviour` files or classes in the first pass.
- Do not remove serialized or public fields until prefab and scene migration is complete.
- Add shims for old button hooks, trigger paths, and public methods when shared flow is moving elsewhere.
- Prefer runtime adapters and explicit handoff notes over speculative prefab rewiring.
- Keep the diff focused on one migration seam at a time.

## Verification

- Run the compilation/build check available in the environment.
- Call out what was verified structurally versus what still needs Unity Play Mode.
- List required Cursor follow-up for prefab, scene, button, or Inspector work.

## Do Not Do

- Do not mix broad cleanup into a serialized-risk refactor.
- Do not claim the Unity behavior is fixed without Play Mode proof.
