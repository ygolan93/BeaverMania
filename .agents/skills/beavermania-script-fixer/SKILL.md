---
name: beavermania-script-fixer
description: Use this skill for small, isolated Beavermania Unity C# bug fixes, null checks, guard clauses, state logic fixes, and minimal script-level changes. Do not use for prefab, scene, animation, or UI hierarchy edits.
---

# Beavermania Script Fixer

## Allowed

- C# script changes.
- Small state logic corrections.
- Null guards and defensive checks.
- Minimal method edits.
- Local helper methods when useful.

## Not Allowed

- Prefab edits.
- Scene edits.
- Animation clip edits.
- UI hierarchy edits.
- Broad refactors.
- Unrelated cleanup.
- Renaming serialized fields.
- Changing public APIs unless required by the scoped fix.

## Process

1. Identify the smallest set of files involved.
2. Explain the likely cause.
3. Make the minimal code change.
4. Avoid changing unrelated behavior.
5. Summarize changed files.
6. Provide Unity Play Mode verification steps.

## Unity Risk Rules

- Preserve serialized fields.
- Do not remove public fields.
- Do not rename `MonoBehaviour` classes.
- Do not rename files containing `MonoBehaviour` classes.
- Do not assume Inspector references are assigned.
- Add clear warnings or null guards when references may be missing.

## Output Required

- Files changed.
- Behavior changed.
- Risk level.
- Manual Unity verification steps.
- Any Inspector assignment required.
