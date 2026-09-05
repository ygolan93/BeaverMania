# Task 2: Make Parry combo reward intentional

## Context

`ScorpionScript.ReceiveDamage` currently increments `combo` once for every accepted damage event. `ScorpionDamage.ApplyHit` handles a successful player Parry by calling `ReceiveDamage(...)` and then directly incrementing `Scorpion.combo` again. The approved design preserves this gameplay reward—normal hit = 1 combo, Parry/counter = 2 combo—but must express it intentionally in one code path instead of relying on accidental duplicate increments.

## Requirements

1. Follow TDD. Add a focused regression test first and capture honest RED/GREEN evidence when executable.
2. Change the smallest possible area.
3. Preserve existing public `ReceiveDamage(int, EnemyDamageType, Transform)` behavior and all existing callers.
4. Add a clear method/API on `ScorpionScript` for a counter/Parry hit that applies the existing counter damage and exactly 2 combo progress in one transaction, or add an optional/internal combo-award parameter without breaking public contracts.
5. Update `ScorpionDamage` to call that intentional API and remove the direct `Scorpion.combo++`.
6. Ensure rejected/dead/non-positive damage does not award combo.
7. Preserve Parry chip damage (6), counter damage (10), Light damage type, boost charge handling, boss health updates, hit VFX/audio, stun threshold behavior, jaw/sting normal-hit behavior, and normal Scorpion compatibility.
8. Do not change combo limits, stun duration, boss damage/health, prefabs, scenes, Animator, stats assets, `.meta` files, vendor code, or the attached plan file.
9. Do not commit, stage, push, merge, or modify git configuration.

## Files in scope

- `Assets/Scripts/NPC/Scorpion/ScorpionScript.cs`
- `Assets/Scripts/NPC/Scorpion/ScorpionDamage.cs`
- focused Scorpion EditMode/PlayMode test under `Assets/Tests/`

## Validation

- Test that normal `ReceiveDamage` awards exactly 1 combo.
- Test that intentional counter/Parry API awards exactly 2 combo.
- Test rejected damage awards no combo.
- Static/lint/diff checks if Unity execution is blocked.
- Do not alter unrelated Priority 1 logic.

## Report

Write the detailed report to `.superpowers/sdd/task-2-parry-report.md`. Include RED/GREEN evidence, commands/results, files changed, self-review, and concerns. Do not commit.
