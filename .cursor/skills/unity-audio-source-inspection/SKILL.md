---
name: unity-audio-source-inspection
description: Use this skill when inspecting or debugging Unity AudioSource components, AudioClip assignments, Audio Mixer routing, SFX playback, music/SFX separation, spatial blend, Play On Awake, looping, volume, priority, duplicated AudioSources, missing references, or script-driven audio behavior in scenes, prefabs, and gameplay objects.
---

# Unity AudioSource Inspection

Use this skill to inspect Unity AudioSource setup safely.

The goal is to find and fix audio problems caused by incorrect AudioSource configuration, missing references, bad mixer routing, duplicate playback, incorrect 2D/3D settings, bad loop/play settings, or unsafe script-triggered audio behavior.

## Scope

Allowed:
- Inspect AudioSource components.
- Inspect AudioClip assignments.
- Inspect Audio Mixer group routing.
- Inspect prefabs and scene objects for audio configuration issues.
- Inspect scripts that trigger AudioSource playback.
- Detect duplicate or competing AudioSources.
- Detect missing or null audio references.
- Recommend Inspector changes clearly.
- Make localized script fixes when requested.

Allowed only when explicitly requested:
- Editing prefabs.
- Editing scene objects.
- Changing AudioSource serialized values.
- Changing Audio Mixer assets.
- Changing AudioClip import settings.
- Replacing audio architecture.
- Creating new audio managers.
- Reassigning clips or mixer groups.

Not allowed:
- Editing `.meta` files.
- Replacing existing prefabs.
- Creating broad audio refactors without approval.
- Changing unrelated UI, gameplay, animation, or VFX logic.
- Claiming Unity Play Mode audio was tested unless it actually was.

## Inspection Checklist

When investigating an AudioSource issue, check:

1. Which GameObject owns the AudioSource.
2. Whether the AudioSource is on a scene object, prefab, child object, or runtime-instantiated object.
3. Whether the AudioSource has an assigned AudioClip.
4. Whether the script uses `Play`, `PlayOneShot`, `Stop`, `Pause`, `UnPause`, or `clip = ...`.
5. Whether `Play On Awake` is enabled correctly.
6. Whether `Loop` is enabled correctly.
7. Whether the AudioSource is routed to the correct Audio Mixer Group.
8. Whether the source is meant to be Music, SFX, UI, Ambience, Voice, or Ability audio.
9. Whether `Spatial Blend` should be 2D or 3D.
10. Whether volume/pitch/priority settings are sane.
11. Whether min/max distance and rolloff are correct for 3D audio.
12. Whether multiple AudioSources play the same sound at once.
13. Whether audio playback is triggered every frame or too frequently.
14. Whether missing serialized references can cause NullReferenceException.
15. Whether the object is disabled/destroyed before the clip can finish.
16. Whether pooling affects playback lifecycle.
17. Whether an AudioListener exists and is not duplicated.

## Common Problems To Detect

Look specifically for:

- Missing AudioClip reference.
- Missing AudioSource reference in script.
- AudioSource exists but is disabled.
- AudioSource GameObject is inactive.
- `Play On Awake` enabled on SFX that should only play on action.
- `Play On Awake` disabled on ambience/music that should start automatically.
- `Loop` enabled on one-shot SFX.
- `Loop` disabled on music/ambience/continuous ability audio.
- SFX routed to Music mixer group.
- Music routed to SFX mixer group.
- UI sound routed to world SFX group.
- 3D sound configured as 2D.
- UI/global sound configured as 3D.
- Very high volume causing clipping.
- Multiple AudioSources on parent/child playing the same effect.
- Duplicate AudioListeners.
- `Play()` called repeatedly and restarting the clip.
- `PlayOneShot()` called too frequently and stacking.
- Runtime-created AudioSources not cleaned up.
- AudioSource destroyed before clip finishes.
- Same clip assigned to several gameplay systems unintentionally.
- Animation Events and scripts both trigger the same sound.

## AudioSource Configuration Rules

Use these defaults unless the project clearly requires otherwise.

### One-shot gameplay SFX

Recommended:
- `Play On Awake`: false
- `Loop`: false
- `Spatial Blend`: usually 0 for UI/global, 1 for world-positioned SFX
- Playback method: `PlayOneShot`
- Add cooldown/throttle if the action can repeat quickly
- Route to SFX mixer group

### Music

Recommended:
- Dedicated AudioSource
- `Loop`: depends on playlist system
- `Spatial Blend`: 0
- Route to Music mixer group
- Do not restart music unnecessarily between scenes unless intended
- Do not use gameplay SFX sources for music

### UI sounds

Recommended:
- `Spatial Blend`: 0
- `Play On Awake`: false
- `Loop`: false
- Route to UI or SFX mixer group
- Trigger from UI events only

### Ambience

Recommended:
- Usually looped
- Usually 2D for global ambience or 3D for positional ambience
- Route to Ambience or SFX mixer group
- Avoid many overlapping ambience sources in the same location

### Ability / combat looping sounds

Recommended:
- Start once when ability begins
- Stop once when ability ends/cancels
- Do not restart every frame
- Guard with `isPlaying` or explicit state flag
- Route to SFX or Ability mixer group

## Script Inspection Rules

When inspecting scripts, look for these patterns:

Bad:
```csharp
private void Update()
{
    audioSource.Play();
}
```

Bad:
```csharp
private void Update()
{
    if (isAttacking)
        audioSource.PlayOneShot(attackClip);
}
```

Better:
```csharp
[SerializeField] private AudioSource sfxSource;
[SerializeField] private AudioClip attackClip;
[SerializeField] private float attackSfxCooldown = 0.18f;

private float _lastAttackSfxTime = -999f;

private void PlayAttackSfx()
{
    if (sfxSource == null || attackClip == null)
        return;

    if (Time.time - _lastAttackSfxTime < attackSfxCooldown)
        return;

    _lastAttackSfxTime = Time.time;
    sfxSource.PlayOneShot(attackClip);
}
```

For looping audio:
```csharp
private void StartAbilityLoop()
{
    if (abilitySource == null)
        return;

    if (abilitySource.isPlaying)
        return;

    abilitySource.loop = true;
    abilitySource.Play();
}

private void StopAbilityLoop()
{
    if (abilitySource == null)
        return;

    if (!abilitySource.isPlaying)
        return;

    abilitySource.Stop();
}
```

## Mixer Routing Rules

When inspecting mixer routing:

- Music should route to Music.
- Player SFX should route to SFX or PlayerSFX.
- Enemy SFX should route to SFX or EnemySFX.
- UI sounds should route to UI or SFX.
- Ambience should route to Ambience or SFX.
- Do not leave important runtime AudioSources unrouted if the project uses Audio Mixer volume sliders.
- If a UI slider controls SFX but a sound ignores it, check whether the AudioSource is routed to the correct mixer group.

If the correct Audio Mixer Group cannot be confirmed:
- Do not guess silently.
- Report the source and current routing.
- Recommend the expected group based on sound type.

## Duplicate AudioSource Audit

Check for duplicate playback sources:

- Same sound triggered from both script and Animation Event.
- Same clip assigned to parent and child objects.
- Multiple AudioSources on the same GameObject with overlapping responsibility.
- Prefab variant adds an AudioSource while base prefab already has one.
- Runtime instantiation creates a new AudioSource every time an action is used.
- Object pooling reuses sources without stopping previous playback.

When duplicates are found, report:
```text
Duplicate AudioSource Risk:
- GameObject:
- Components:
- Clip:
- Trigger path:
- Why it may duplicate:
- Safe fix:
```

## 2D / 3D Spatial Audio Rules

Use 2D audio for:
- Music
- UI sounds
- Global feedback sounds
- Non-positional player feedback
- Menu sounds

Use 3D audio for:
- Enemy sounds
- World pickups
- Environmental objects
- Positional combat impacts
- Localized ambience
- Trader/NPC voice or interaction sounds if positional

For 3D audio, inspect:
- Spatial Blend
- Min Distance
- Max Distance
- Rolloff Mode
- Doppler Level
- Spread
- Priority

Avoid extreme Doppler unless specifically desired.

## Beavermania-Specific Guidance

In Beavermania:
- Prefer localized fixes.
- Preserve existing namespaces:
  - `Beavermania.Audio`
  - `Beavermania.Player`
  - `Beavermania.Player.Combat`
  - `Beavermania.Player.Movement`
  - `Beavermania.UI`
  - `Beavermania.Core.Input`
- Do not edit `BeaverInputSystem.cs`; it is generated by Unity Input System.
- Do not edit `.meta` files.
- Do not replace prefabs or scene objects unless explicitly requested.
- Be careful with:
  - player jump SFX
  - slash SFX
  - Hurricane / wind sounds
  - FireBreath sounds
  - electric ability sounds
  - footsteps
  - pickup sounds
  - hurt/damage sounds
  - music continuing from Menu into gameplay
  - SFX volume slider behavior
- If serialized fields are added, list them clearly so they can be assigned in Unity Inspector.
- If mixer routing requires Inspector changes, report exact GameObject, AudioSource, and expected Output Mixer Group.

## Required Investigation Output

Before fixing, produce a short inspection summary:

```text
AudioSource Inspection:
- Suspected GameObject:
- AudioSource/component:
- Clip:
- Current symptom:
- Current trigger path:
- Current mixer routing:
- 2D/3D setup:
- Likely cause:
- Safe fix:
- Requires Unity Editor change: yes/no
```

## Fix Strategy

Prefer this order:

1. Fix missing/null-safe script references.
2. Add cooldown/throttle if playback repeats too frequently.
3. Fix `Play`, `PlayOneShot`, `Stop`, or loop lifecycle logic.
4. Recommend AudioSource Inspector changes.
5. Recommend Audio Mixer routing changes.
6. Only then suggest broader audio architecture changes.

Avoid large refactors unless the user explicitly asks.

## Validation Checklist

After changes, report:

1. Files changed.
2. AudioSources inspected.
3. Audio issue found.
4. Script changes made.
5. Inspector changes required.
6. Mixer routing changes required.
7. Manual Play Mode checks.

Manual Play Mode test examples:
- Trigger the sound once and confirm it plays once.
- Spam the action and confirm audio does not stack or clip.
- Confirm SFX slider affects the sound if expected.
- Confirm music slider does not affect SFX unless designed.
- Confirm 3D sound attenuates correctly with distance.
- Confirm UI/global sounds are not affected by player position.
- Confirm no NullReferenceException appears in Console.
- Confirm no duplicate AudioListener warnings appear.

## Final Response Format

When done, summarize:

- Files changed.
- GameObjects/AudioSources inspected.
- AudioSource issues found.
- Script fixes applied.
- Inspector assignments required.
- Mixer routing changes required.
- Manual Unity Play Mode checks.

Do not claim Unity Play Mode was tested unless it was actually run.
Do not claim Inspector or Audio Mixer changes were made unless they were actually edited.
