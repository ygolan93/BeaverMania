---
name: gameplay-audio-throttling
description: Use this skill when a Unity gameplay sound effect repeats too frequently, overlaps aggressively, clips, restarts, stacks, or becomes too loud during repeated player/enemy actions such as jump, slash, sword attacks, footsteps, magic attacks, pickup sounds, damage sounds, or ability effects. Also use it when implementing cooldown-based SFX playback, one-shot audio guards, per-action audio rate limiting, or safe AudioSource.PlayOneShot behavior in gameplay scripts.
---

# Gameplay Audio Throttling

Use this skill for Unity C# gameplay audio fixes where sound effects are triggered too frequently or overlap in a way that hurts game feel.

## Scope

This skill is for script-level audio throttling only.

Allowed:
- C# gameplay scripts.
- Audio playback guards.
- Cooldown timers.
- `AudioSource.PlayOneShot(...)` safety.
- Preventing repeated SFX spam.
- Per-sound or per-action cooldown logic.
- Lightweight helper methods/classes.
- Localized changes with low regression risk.

Not allowed unless explicitly requested:
- Editing scenes.
- Editing prefabs.
- Editing Audio Mixer assets.
- Editing animation clips.
- Editing generated files.
- Editing `.meta` files.
- Replacing existing audio architecture with a large new system.
- Changing music playlist logic unless the user explicitly asks.

## Core Rule

Never let a gameplay action trigger the same sound every frame or every animation/event tick without a cooldown, state check, or one-shot guard.

Bad patterns:
- Calling audio playback directly inside `Update()` without a state transition.
- Calling jump/slash/footstep sounds every frame while a boolean is true.
- Re-triggering the same ability SFX every animation frame.
- Creating new `AudioSource` objects repeatedly at runtime.
- Starting the same looping sound repeatedly without checking if it is already playing.
- Using `Play()` for short repeated SFX when `PlayOneShot()` is more appropriate.

Preferred patterns:
- Track the last play time per sound/action.
- Use a small cooldown per gameplay action.
- Trigger sounds only on input/action transitions.
- Use `PlayOneShot()` for short SFX.
- Use `isPlaying` checks only for looping or long sounds.
- Keep cooldown values serialized when tuning is expected.
- Keep defaults conservative and easy to tune in Inspector.

## Implementation Pattern

Prefer a minimal helper method inside the relevant script first:

```csharp
[SerializeField] private AudioSource sfxSource;
[SerializeField] private AudioClip jumpClip;
[SerializeField] private float jumpSfxCooldown = 0.12f;

private float _lastJumpSfxTime = -999f;

private void PlayJumpSfx()
{
    if (sfxSource == null || jumpClip == null)
        return;

    if (Time.time - _lastJumpSfxTime < jumpSfxCooldown)
        return;

    _lastJumpSfxTime = Time.time;
    sfxSource.PlayOneShot(jumpClip);
}
```

For multiple related sounds, use named cooldown fields rather than a generic over-engineered manager unless the codebase already has an audio manager.

Example:

```csharp
[SerializeField] private float attackSfxCooldown = 0.18f;
[SerializeField] private float footstepSfxCooldown = 0.1f;
[SerializeField] private float damageSfxCooldown = 0.25f;

private float _lastAttackSfxTime = -999f;
private float _lastFootstepSfxTime = -999f;
private float _lastDamageSfxTime = -999f;
```

## When Fixing Existing Bugs

Before editing:

1. Identify where the sound is triggered.
2. Check if it is called from `Update`, animation events, input callbacks, collision callbacks, or coroutines.
3. Check whether the trigger can fire repeatedly.
4. Check whether the current implementation uses `Play`, `PlayOneShot`, `loop`, `isPlaying`, or instantiated audio objects.
5. Determine whether the correct fix is:
   - cooldown,
   - state transition guard,
   - input phase guard,
   - animation event cleanup,
   - loop start/stop guard,
   - or null/reference safety.

## Unity Input / Action Audio Rules

For input-driven sounds:

- Trigger sound on `Performed`, not continuously.
- Do not play sound on every input callback if the callback receives `Started`, `Performed`, and `Canceled`.
- For held actions like sprint, shield, charge, or ability hold:
  - play start sound once,
  - optionally play loop sound once,
  - stop loop sound on cancel,
  - do not replay every frame.

## Animation Event Audio Rules

For animation-event sounds:

- Do not assume animation events fire only once.
- Add cooldown if the animation can blend, replay, interrupt, or loop.
- Keep event methods small and safe.
- Never throw if an AudioSource or AudioClip is missing.
- Prefer warning only when useful; avoid logging every frame.

## Footstep Audio Rules

For footsteps:

- Use distance, grounded state, movement state, or animation event timing.
- Never play footsteps directly every `Update()` while moving.
- Use cooldown or stride interval.
- Do not play footsteps while airborne.
- Do not stack multiple footstep systems unless the user explicitly asks for layered audio.

## Combat / Ability Audio Rules

For attack sounds:

- Separate input trigger from hit detection.
- The swing/slash sound should usually play once per attack start.
- Hit impact sounds should play when a valid hit is detected, with a separate cooldown if multiple colliders can report the same hit.
- Ability sounds such as fire, hurricane, electric, wind, slash, or sword glare should not restart every frame while active.
- If an ability is blocked due to insufficient HP/stamina/resources, stop or skip its SFX cleanly.

## Beavermania-Specific Guidance

In Beavermania:

- Prefer localized script changes.
- Preserve existing namespaces such as:
  - `Beavermania.Audio`
  - `Beavermania.Player`
  - `Beavermania.Player.Combat`
  - `Beavermania.Core.Input`
- Do not edit `BeaverInputSystem.cs`; it is generated by Unity Input System.
- Do not edit `.meta` files.
- Do not change prefabs or scene references unless explicitly requested.
- If serialized fields are added, list them clearly in the final response so they can be assigned in Unity Inspector.
- If the fix requires Inspector wiring, say exactly which GameObject/Component/Field needs assignment.

## Validation Checklist

After implementing a throttling fix, report:

1. Which sound/action was throttled.
2. Which script changed.
3. Which cooldown/state guard was added.
4. Whether any serialized fields were added.
5. Whether Inspector assignment is required.
6. How to test manually in Unity Play Mode.

Manual test examples:

- Press jump repeatedly and confirm jump SFX does not stack or clip.
- Hold attack/ability input and confirm sound does not restart every frame.
- Spam slash/hurricane/fire/electric and confirm SFX remains controlled.
- Walk/run and confirm footsteps match movement without rapid spam.
- Trigger damage/hit collisions and confirm impact sounds do not multiply from duplicate colliders.

## Final Response Format

When done, summarize briefly:

- Files changed.
- Audio issue fixed.
- Throttle/cooldown values added.
- Inspector fields to assign.
- Manual Play Mode checks.

Do not claim Unity Play Mode was tested unless it was actually run.
Do not claim audio balance is final unless it was tested in the Unity Editor.
