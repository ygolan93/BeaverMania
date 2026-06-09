# Active Task

## Task title
- Audio Mixer prefab/scene wiring

## Type
- Mixed (scripts + prefab wiring + Play Mode verification)

## Owner
- **Cursor** — prefab wiring, EditMode audit tests, Unity MCP verification
- **User** — final Play Mode slider QA confirmation

## Current phase
- Implementation complete; merged `origin/main` (objective flow). EditMode + partial Play Mode verification done.

## Goal
- Ensure all priority AudioSources and AudioVolumeSettings catalog references are assigned to NewAudioMixer groups; validate with EditMode tests and Play Mode slider QA in Level 1 - Remastered - Steam.

## Changes made

### Scripts
- `Assets/Editor/AudioRoutingPrefabAudit.cs` — allowlist prefab scan for null mixer groups
- `Assets/Editor/AudioRoutingEditModeTests.cs` — `AllowlistedPrefabs_HaveRoutedEnabledAudioSources` test

### Prefabs
- `Assets/Prefabs/Objects/UI/GameMusic.prefab` — full AudioVolumeSettings group catalog (Music/SFX/Enemies/UI)
- `Assets/Prefabs/Player/Otter_Shapekeys/Player.prefab` — disabled orphan AudioSource → Effects

### Verified unchanged (already wired)
- ShadowRevenant → Boss (×2)
- ShadowRevenantShadeMinion → Enemies
- ScorpionBoss → Boss (×2)
- PlayerCanvas → UI (×5)
- FirstLift → Elevator/Button
- Electric Effect → Effects

## Manual Play Mode validation checklist (Level 1 Remastered - Steam)
- [x] EditMode prefab audit passes (`AllowlistedPrefabs_HaveRoutedEnabledAudioSources`)
- [x] EditMode routing tests pass (Music/Sfx/Enemy/UI EnsureRoute + prefab audit)
- [x] SFX slider updates SfxVolume + EnemiesVolume at runtime (console evidence during Play Mode)
- [x] MusicVolume unchanged while SFX slider moved (console evidence)
- [ ] Music slider audible-only QA (human ear check)
- [ ] Master slider affects all categories (human ear check)
- [ ] Volume settings persist after scene reload
- [ ] No `[AudioSourceRouting]` warnings during combat

---

## Merged from main (objective flow — complete, no code conflicts)

Objective flow Remastered wiring landed via PR #175 and merged cleanly with audio routing (`BeaverPlayerBehaviour`, `Static_Hive` auto-merged). Status on main: complete with PlayMode tests passing.
