# Active Task

## Task title
- Player jump audio pitch fix (strafe/roll → jump transitions)

## Type
- Mixed (script hardening + prefab verification + Play Mode audio QA)

## Owner
- **Codex/Cursor** — `AudioScript.cs` jump API unification, static prefab audit
- **User** — Play Mode ear verification in Level 1 Remastered - Steam

## Current phase
- Jump pitch transition fix implemented. Play Mode QA pending user ear check.

## Goal
- Ensure jump SFX (`Leap.mp3`) plays once per jump input with no duplicate triggers from animation events or dual throttle channels.

## Audit findings (static)
- No player animation clips/events call `Jump`, `PlayJumpSound`, or similar.
- Single live trigger: `BeaverPlayerBehaviour.Update` → `Sound.PlayPlayerJumpSfx()`.
- `Jump.fbx` (Midair) has zero animation events.
- `NewSwordJump.anim` has combat events only (misleading name).

## Changes made

### Scripts
- `Assets/Scripts/Player/SoundScripts/AudioScript.cs` — unified jump API; jump plays 2D with doppler disabled during one-shot; suppresses `Step`/`Roll` SFX same frame and 0.12s after jump.
- `Assets/Scripts/SceneScripts/GameplayAudio.cs` — `WasChannelPlayedWithin()` for movement/jump overlap guards.
- `Assets/Editor/GameplayAudioThrottleTests.cs` — test for `WasChannelPlayedWithin`.

### Prefabs (verified, no edits required)
- `Assets/Prefabs/Player/Otter_Shapekeys/Player.prefab`
  - `AudioScript.audioClip[0]` = Leap.mp3
  - `audioSource` → OTTER_ShapekeysIdle (Player mixer, clip empty, Play On Awake off)
  - `audioEffects` → AudioEffects child (Effects mixer, clip empty, Play On Awake off)
  - `HammerLeft` AudioSource disabled (orphan)

## Manual Play Mode jump audio checklist (Level 1 Remastered - Steam)
- [ ] Single jump (Space once): exactly one Leap sound, no Console errors
- [ ] Double jump: two sounds spaced by ~0.16s cooldown (expected)
- [ ] Spam Space on ground: throttle prevents stack/clipping
- [ ] Jump then air slash (`NewSwordJump`): jump once; sword/wind separate
- [ ] SFX slider affects jump; music slider does not
- [ ] Pause/resume: jump still single per press
- [ ] Carry logs / goblet boost: jump still single per press
- [ ] Console: no `AnimationEvent 'Jump' ... has no receiver` warnings
- [ ] Console: no duplicate AudioListener warnings

## Verification performed
- Static scan of player `.anim` / FBX `.meta` animation events
- Serialized prefab AudioSource + AudioScript wiring review
- IDE linter: no issues on `AudioScript.cs`
- `dotnet build .ci/BeaverMania.CI.csproj`: failed locally (UnityEngine refs missing in this shell — environment limitation)
- Unity MCP: no Editor instance connected; Play Mode not run

## Risk
- Low. Behavior change: `NewConstructor.Construction.Jump()` now shares `player.jump` throttle with player (prevents latent duplicate if both fired near-simultaneously on same AudioScript instance).
