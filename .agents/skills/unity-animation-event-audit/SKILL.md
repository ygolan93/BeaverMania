---
name: unity-animation-event-audit
description: Use this skill when auditing, debugging, or fixing Unity Animation Events connected to gameplay actions, combat, movement, VFX, SFX, hitboxes, weapon activation, jump/slash timing, ability triggers, or animation-driven method calls. Use it when sounds, effects, colliders, attack windows, or gameplay actions trigger too early, too late, too often, not at all, or continue after an animation is interrupted.
---

# Unity Animation Event Audit

Use this skill to inspect and fix Unity Animation Events safely.

The goal is to verify that animation events call the correct methods, at the correct timing, exactly as intended, without creating duplicated gameplay triggers, audio spam, VFX leaks, stuck hitboxes, or broken prefab/scene references.

## Scope

Allowed:
- Inspect animation clips.
- Inspect Animator Controllers.
- Inspect scripts referenced by animation events.
- Verify method names and signatures.
- Fix C# methods called by animation events.
- Add safe guards to event receiver methods.
- Add cooldown/state protection when animation events can fire repeatedly.
- Report required Unity Editor changes clearly.

Allowed only when explicitly requested:
- Editing animation clips.
- Moving animation event timestamps.
- Adding/removing animation events.
- Editing prefabs.
- Editing scene object hierarchy.
- Replacing Animator Controllers.
- Reworking the animation state machine.

Not allowed:
- Editing generated files.
- Editing `.meta` files.
- Replacing clips/controllers instead of fixing them.
- Creating a new animation system.
- Performing broad animation refactors unless explicitly requested.

## Audit Checklist

When investigating an Animation Event issue, check:

1. Which animation clip contains the event.
2. Which frame/time the event fires on.
3. Which method name is called.
4. Which GameObject receives the event.
5. Which component contains the target method.
6. Whether the method exists.
7. Whether the method signature is valid for Unity Animation Events.
8. Whether the method can safely be called more than once.
9. Whether the animation can loop, blend, interrupt, transition, or replay.
10. Whether the event triggers gameplay, SFX, VFX, hitboxes, particles, damage, input state, or object activation.
11. Whether the event depends on serialized fields that may be missing.
12. Whether the event causes side effects that must be reset later.

## Valid Unity Animation Event Method Forms

Animation Event methods should usually be simple public or private instance methods on a MonoBehaviour attached to the animated GameObject or an appropriate parent/child receiver.

Valid common forms:

```csharp
public void MethodName()
public void MethodName(float value)
public void MethodName(int value)
public void MethodName(string value)
public void MethodName(UnityEngine.Object value)
public void MethodName(AnimationEvent animationEvent)
```

Avoid:

```csharp
static methods
async animation event handlers
methods requiring multiple parameters
methods with complex gameplay branching
methods that assume references are always assigned
methods that instantiate repeatedly without limits
```

## Event Receiver Method Rules

Animation Event receiver methods must be safe.

Preferred pattern:

```csharp
public void OnAttackHitboxEnabled()
{
    if (!isActiveAndEnabled)
        return;

    if (attackHitbox == null)
        return;

    attackHitbox.SetActive(true);
}
```

For paired enable/disable events:

```csharp
public void EnableAttackWindow()
{
    if (attackHitbox == null)
        return;

    attackHitbox.SetActive(true);
}

public void DisableAttackWindow()
{
    if (attackHitbox == null)
        return;

    attackHitbox.SetActive(false);
}
```

For audio events:

```csharp
[SerializeField] private AudioSource sfxSource;
[SerializeField] private AudioClip slashClip;
[SerializeField] private float slashSfxCooldown = 0.15f;

private float _lastSlashSfxTime = -999f;

public void PlaySlashSfxFromAnimation()
{
    if (sfxSource == null || slashClip == null)
        return;

    if (Time.time - _lastSlashSfxTime < slashSfxCooldown)
        return;

    _lastSlashSfxTime = Time.time;
    sfxSource.PlayOneShot(slashClip);
}
```

For VFX events:

```csharp
public void EnableSlashVfx()
{
    if (slashVfx == null)
        return;

    if (!slashVfx.activeSelf)
        slashVfx.SetActive(true);
}

public void DisableSlashVfx()
{
    if (slashVfx == null)
        return;

    if (slashVfx.activeSelf)
        slashVfx.SetActive(false);
}
```

## Common Problems To Detect

Look specifically for:

- Event method name typo.
- Event method exists on the wrong GameObject.
- Method was renamed but animation clip still calls the old name.
- Event fires on both entry and looping frames.
- Event fires during blend/transition more than expected.
- Event triggers SFX repeatedly and causes audio spam.
- Event enables VFX but no matching disable/reset exists.
- Event enables hitbox but hitbox remains active after interruption.
- Event spawns objects repeatedly.
- Event applies damage multiple times per swing.
- Event assumes references are assigned and throws NullReferenceException.
- Event fires while the player has insufficient HP/stamina/resources.
- Event calls gameplay logic that should be input-driven instead.
- Event exists on duplicate animation clips but only one was updated.

## Combat Animation Event Rules

For combat:
- Use animation events for timing windows, not high-level decision logic.
- Attack start should usually be input/state driven.
- Hitbox enable/disable can be animation-event driven.
- Damage should be guarded against duplicate collider hits.
- Swing SFX should usually fire once per swing.
- Impact SFX should fire only on valid hit.
- VFX should have clear enable/disable/reset behavior.
- Interrupted attacks must clean up hitboxes and VFX.

## Movement Animation Event Rules

For movement:
- Footstep events must not fire while airborne.
- Footstep events should be guarded by grounded/movement checks.
- Landing sounds should not stack with jump sounds.
- Jump SFX should trigger from the jump action or a clearly timed animation event, not both unless intentionally layered.
- Repeated run/idle transitions should not spam SFX.

## Ability Animation Event Rules

For abilities:
- Resource checks must happen before starting the ability.
- Animation events should not bypass HP/stamina/mana gates.
- If an ability is cancelled, its VFX/SFX/hitboxes must be reset.
- Looping ability animation events must not restart audio/VFX every loop unless designed that way.
- Fire, wind, electric, hurricane, slash, sword glare, and similar effects must have clear lifecycle ownership.

## Beavermania-Specific Guidance

In Beavermania:
- Prefer localized fixes.
- Preserve existing namespaces:
  - `Beavermania.Player`
  - `Beavermania.Player.Combat`
  - `Beavermania.Player.Movement`
  - `Beavermania.Audio`
  - `Beavermania.Display`
  - `Beavermania.Core.Input`
- Do not edit `BeaverInputSystem.cs`; it is generated by Unity Input System.
- Do not edit `.meta` files.
- Do not replace animation clips, prefabs, or Animator Controllers unless explicitly requested.
- Be careful with player combat animations, sword VFX, FireBreath, Hurricane, slash effects, jump events, footsteps, and enemy hit reactions.
- If serialized fields are added, list them clearly so they can be assigned in Unity Inspector.
- If Unity Editor-only changes are required, do not pretend they were done in code. Report the exact clip/event/timestamp/object to update.

## Required Investigation Output

Before fixing, produce a short audit summary:

```text
Animation Event Audit:
- Suspected clip:
- Suspected event method:
- Receiver GameObject/component:
- Current symptom:
- Likely cause:
- Safe fix:
- Requires Unity Editor change: yes/no
```

## Fix Strategy

Prefer this order:

1. Fix missing/null-safe C# receiver methods.
2. Add state guards/cooldowns where repeated firing is possible.
3. Add cleanup/reset methods for VFX/hitboxes.
4. Only then recommend moving/adding/removing animation events in Unity Editor.
5. Avoid large animation/controller refactors.

## Validation Checklist

After changes, report:

1. Files changed.
2. Animation event methods changed or added.
3. Serialized fields added.
4. Whether animation clips need manual Editor changes.
5. Which gameplay action should be tested.
6. Expected Play Mode behavior.

Manual Play Mode test examples:
- Trigger the animation once and confirm the event fires once.
- Spam the action and confirm SFX/VFX/hitboxes do not stack.
- Interrupt the animation and confirm hitboxes/VFX reset correctly.
- Test transitions/blends and confirm no duplicate gameplay effects.
- Confirm no NullReferenceException appears in Console.

## Final Response Format

When done, summarize:

- Files changed.
- Event receiver methods fixed.
- Animation clip changes required, if any.
- Inspector assignments required, if any.
- Manual Unity Play Mode checks.

Do not claim Unity Play Mode was tested unless it was actually run.
Do not claim animation events were moved/added/removed unless the animation clip was actually edited.
