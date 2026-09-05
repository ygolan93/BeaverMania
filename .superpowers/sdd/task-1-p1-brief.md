# Task 1: Essential Scorpion combat decision and action boundaries

## Context

Implement Priority 1 of the approved Scorpion Boss AI Rework in the existing BeaverMania repository. The shared `ScorpionScript` currently has a deterministic Look chain, one-physics-tick Charge windup, continuously homing/unbounded Charge, and Attack that can remain active indefinitely. The generic and boss prefabs share the same script and differ through their `ScorpionStatsData` assets.

## Requirements

1. Follow TDD. Add focused EditMode tests first and record RED and GREEN evidence.
2. Add a small pure decision helper under `Assets/Scripts/NPC/Scorpion/` with:
   - Macro actions: Attack, Charge, Reverse, Hold.
   - Weighted selection only when called at explicit decision points.
   - Distance eligibility using attack/charge/look distances.
   - Hard anti-repeat rule: the same macro action can be selected twice, never a third time.
   - Deterministic injected roll input (or equivalent) so tests assert constraints rather than Unity RNG sequences.
3. Extend `ScorpionStatsData` with the smallest coherent set of serialized advanced-AI fields required by Priority 1:
   - explicit enable flag defaulting false so ordinary Scorpions retain legacy behavior;
   - charge windup min/max with an enforced readable minimum of 0.35 seconds;
   - charge tracking duration;
   - charge maximum duration and distance;
   - attack window duration;
   - decision hold min/max;
   - phase-one weights sufficient for Attack/Charge/Reverse/Hold.
     Keep existing public fields and `chargeDuration` serialized contract intact.
4. For advanced AI only, make `Look` the decision/hold hub:
   - select once on entry or when the hold completes, never each FixedUpdate;
   - Attack is illegal outside attack range;
   - Reverse gains eligibility at very close range;
   - Charge is eligible in combat range and favored by configured weights;
   - Hold has bounded duration and cannot stall forever;
   - after Attack, Charge, and Reverse complete, return to an explicit Look decision.
5. Charge behavior:
   - Windup lasts a random configured duration clamped to at least 0.35 seconds.
   - During early Charge, track/rotate toward the player for the configured tracking window.
   - Then lock a normalized horizontal direction and stop re-aiming.
   - Move using the existing Rigidbody velocity approach.
   - End reliably on configured maximum time or distance, loss of combat target, passing the player, obstruction/collision, or a valid contact opportunity in attack range.
   - Charge must not automatically force Attack every time; end at Look so the next weighted decision chooses.
6. Attack behavior:
   - End after configured attack window even if the player remains close.
   - Return to Look for a new decision.
   - The shared anti-repeat rule limits Attack to at most two consecutive selections.
7. Preserve the legacy path exactly when advanced AI is disabled.
8. Preserve existing public/runtime contracts, serialized fields, Animator bool names, Rigidbody movement, victory/death/drop/projectile/boost/stun systems, and normal Scorpion behavior.
9. Update `AI_WORKFLOW/active-task.md` with a concise current mixed-task entry and owner/validation status. Do not rewrite unrelated historical entries.
10. Do not edit prefabs, scenes, Animator controller, animation clips, `.meta` files, vendor code, or the attached plan file in this task.
11. Do not commit, push, merge, or modify git configuration.

## Files in scope

- `Assets/Scripts/NPC/Scorpion/ScorpionScript.cs`
- `Assets/Scripts/NPC/Scorpion/ScorpionState.cs` only if truly necessary
- new small helper(s) in `Assets/Scripts/NPC/Scorpion/`
- `Assets/Scripts/Data/NPC/Scorpion/ScorpionStatsData.cs`
- new EditMode tests under `Assets/Tests/EditMode/NPC/Scorpion/`
- `AI_WORKFLOW/active-task.md`

## Validation

- Focused EditMode tests for anti-repeat, distance eligibility, bounded selection, phase selection if introduced, and direction-lock helper behavior.
- `dotnet build .ci/BeaverMania.CI.csproj` (known baseline may fail from missing Unity assembly references; identify whether any new Scorpion-specific compiler error appears).
- Inspect final diff for allocations in `FixedUpdate`, serialized-contract breaks, and unintended legacy behavior changes.

## Report

Write the detailed report to `.superpowers/sdd/task-1-p1-report.md`, including RED/GREEN commands and output, files changed, self-review, and concerns. Return no more than 15 lines. Do not commit.
