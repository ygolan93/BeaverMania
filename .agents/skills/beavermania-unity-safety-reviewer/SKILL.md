---
name: beavermania-unity-safety-reviewer
description: Use this skill to review Beavermania Unity changes for prefab, scene, serialized reference, Inspector, animation, and gameplay regression risks. Prefer this before committing Unity-heavy changes.
---

# Beavermania Unity Safety Reviewer

Review the current diff as a Unity safety reviewer.

## Check For

- Accidental prefab changes.
- Accidental scene changes.
- Broken serialized references.
- Renamed serialized fields.
- Renamed `MonoBehaviour` classes.
- Deleted public or serialized fields.
- Changes to PlayerCanvas, Trader, Dialogue, Shop, Player, or Level scenes.
- Logic that depends on missing Inspector references.
- Risky changes in `Update`, `FixedUpdate`, or `LateUpdate`.

## Required Output

1. Safe to commit? Yes/No.
2. High-risk files changed.
3. Potential Unity serialization issues.
4. Potential gameplay regressions.
5. Required manual Play Mode checks.
6. Recommended rollback or split if needed.

## Behavior

- Do not implement changes unless explicitly asked.
- Focus on review and risk detection.
