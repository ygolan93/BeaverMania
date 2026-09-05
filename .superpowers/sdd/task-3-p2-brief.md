# Task 3: Scorpion health phases, charge variants, recovery, and post-stun pressure

## Context

Priority 1 already added an opt-in advanced Scorpion AI with weighted Attack/Charge/Reverse/Hold decisions, anti-repeat, readable windup, committed Charge, bounded Attack/Reverse, and pure EditMode-tested helpers. This task adds Priority 2 personality/pacing only. It must reuse the same actions, Animator parameters, Rigidbody movement, hit colliders, model, and clips.

## Requirements

1. Follow TDD. Add focused tests before production changes and record honest RED/GREEN evidence when executable.
2. Add three health behavior profiles selected from normalized HP:
   - Controlled: above 65%.
   - Aggressive: 65% down to and including 30%.
   - Frenzy: below 30%.
   - Threshold behavior must be explicit and tested.
3. Each profile supplies Attack/Charge/Reverse/Hold decision weights and pacing. Keep the data model compact:
   - Controlled: more Reverse/Hold, Normal Charge favored, longest recovery.
   - Aggressive: more Charge/Attack, less Hold, Short Charge more common, some Committed Charge, shorter recovery.
   - Frenzy: highest Charge/Attack pressure, least Hold/Reverse, Committed Charge more common, shortest recovery.
4. Add three variants of the same existing Charge:
   - Short: shorter maximum duration/distance; normal readable windup.
   - Normal: current configured advanced charge values.
   - Committed: longer maximum duration/distance, earlier direction lock (shorter tracking), more punishable on a lateral dodge.
   - Do not add animations, states, hitboxes, root motion, or speed/damage inflation.
   - Select a variant once at ChargeWindup entry using weighted profile values, not every FixedUpdate.
5. Add short bounded recovery before the next decision:
   - Charge miss/end: configurable profile-based recovery, with a meaningful punish window.
   - Attack sequence end: configurable profile-based recovery.
   - Later phases shorten recovery but do not remove readable boundaries.
   - Reuse Look's bounded timer rather than adding state explosion if clean.
6. Add temporary post-stun aggression after `Stunned -> Recovered`:
   - configurable duration;
   - reduce Hold weight;
   - slightly increase Charge weight;
   - slightly reduce recovery duration;
   - no invulnerability, new attack, animation, or phase transition.
7. Preserve anti-repeat across phases and action variants. Charge variants are still the same macro action for the max-two rule.
8. Keep windup at or above 0.35 seconds in all phases/variants.
9. Keep existing `chargeDuration` serialized/public contract, all Task 1/2 behavior, Parry=2 combo transaction, victory/death/drop/stun/projectile/boost/bridge integrations, Animator bool names, and legacy behavior when advanced AI is disabled.
10. Tune only `Assets/Data/NPC/Scorpion/ScorpionBossStatsData.asset` to enable the advanced AI and populate final values. Do not increase existing maxHealth, attackDamage, stingDamage, chargeSpeed, or projectile multiplier.
11. Do not edit the normal `ScorpionStatsData.asset` or normal `Scorpion.prefab`; missing/new serialized fields must leave advanced AI off by default.
12. Priority 3 Fake Charge and Animator.speed changes are explicitly skipped because static Animator inspection shows only bool-driven Idle/Walk/Backwards/Attack/Stunned states and no dedicated fake-cancel presentation. Record this as an intentional stability decision.
13. Do not edit scenes, prefabs, Animator controller, animation clips, `.meta` files manually, vendor code, or the attached plan file.
14. Do not commit, stage, push, merge, or modify git configuration.

## Suggested compact implementation

- Add pure enums/value types/helpers for health profile and Charge variant selection to `ScorpionCombatDecision.cs`.
- Preserve current phase-one flat fields if required for serialized compatibility; add only coherent phase-two/phase-three/variant/recovery/post-stun fields.
- Resolve the current profile at explicit decision/Charge entry points.
- Store only the active Charge variant and resolved runtime duration/distance/tracking values on `ScorpionScript`.

## Files in scope

- `Assets/Scripts/NPC/Scorpion/ScorpionCombatDecision.cs`
- `Assets/Scripts/NPC/Scorpion/ScorpionScript.cs`
- `Assets/Scripts/Data/NPC/Scorpion/ScorpionStatsData.cs`
- `Assets/Data/NPC/Scorpion/ScorpionBossStatsData.asset`
- focused tests under `Assets/Tests/EditMode/NPC/Scorpion/`
- append concise Task 3 status to `AI_WORKFLOW/active-task.md` only if current entry needs validation status adjustment

## Validation

- Tests for exact HP threshold selection.
- Tests for phase-specific action weights being selected/consumed correctly.
- Tests that Charge variant selection is weighted and anti-repeat remains macro-level.
- Tests for Short/Normal/Committed runtime multipliers and earlier committed lock.
- Tests for profile recovery ordering and post-stun modifiers.
- Static check that boss health/damage/speed/projectile values are unchanged and advanced AI is enabled only in boss asset.
- Focused Unity tests and CI build if available; report unavailable execution honestly.

## Report

Write the detailed report to `.superpowers/sdd/task-3-p2-report.md`, including TDD evidence, exact tuning values, files changed, self-review, and concerns. Do not commit.
