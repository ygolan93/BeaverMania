# Beavermania script migration (Steam-safe)

This document tracks **frozen script GUIDs** and **post-Steam** moves. Do not hand-edit existing `.meta` GUIDs.

## Frozen MonoScript GUIDs (do not orphan)

| Script | Path | Asset GUID |
|--------|------|------------|
| Behaviour | `Assets/Scripts/Player/Behaviour.cs` | `8c489014c99e2174688db9b08f27a14e` |
| AudioScript | `Assets/Scripts/Player/SoundScripts/AudioScript.cs` | `8cb792c87e0d29343bcc404e81e585a7` |
| GameMaster | `Assets/Scripts/SceneScripts/GameMaster.cs` | `e29b6dacd5af9c045a26df095e303a9f` |
| UIMenu | `Assets/Scripts/UI/UIMenu.cs` | `7b8c5813f15eb9b4d9fc1ba5751d178e` |

## Other scene- or prefab-bound scripts (validate before move/rename)

| Script | Path | GUID |
|--------|------|------|
| RestartGameLooseMenu | `Assets/Scripts/SceneScripts/RestartGameLooseMenu.cs` | `dd7a1efbf477eb54d9c503b2b56cac04` |
| MainMenu | `Assets/Scripts/SceneScripts/MainMenu.cs` | `d43912f31b2470043b1cf53da9bddeb4` |
| LoadingScreenController | `Assets/Scripts/SceneScripts/LoadingScreenController.cs` | `ff7f81ca5ce4b6f4a8004bf23566e7cd` |
| DoNotDestroy | `Assets/Scripts/SceneScripts/DoNotDestroy.cs` | `a2ed497359b0e9e4e97cc49c75b18dbc` |
| MusicPlaylist | `Assets/Scripts/UI/MusicPlaylist.cs` | `7724bd1507218364691562fd02f6ed08` |
| PauseController | `Assets/Scripts/Core/GameFlow/PauseController.cs` | `99bd4973b27a4ed18af19e86fc68c6ea` |
| PlayerInputReader | `Assets/Scripts/Core/Input/PlayerInputReader.cs` | `85da84c7191c4b60ab0add9089244848` |
| SceneRestartController | `Assets/Scripts/Core/GameFlow/SceneRestartController.cs` | `75a115eb099b4aeb92c17c4f6144175c` |
| CheckpointState | `Assets/Scripts/Core/GameFlow/CheckpointState.cs` | `0217cbccfd30409cbd436c3920640b45` |

After any migration step: open primary prefabs (player, `PlayerCanvas`, `GameMusic`, NPCs, bridges), load Level 1 / Menu / Tutorial, confirm **no missing script** components, and diff YAML for unintended `m_Script` GUID changes.

## Post-Steam (meta-preserving moves)

1. **Physical moves** of `Behaviour.cs`, `UIMenu.cs`, `AudioScript.cs`, `MusicPlaylist.cs` — move folder + `.meta` together in Unity or Git; run full prefab/scene pass.
2. **Rename / namespace** `Behaviour` — updates all `global::Behaviour` references (`PauseController`, `PlayerCheckpointRespawn`, etc.).
3. **Split `Behaviour`** into player components — animation event audit + regression matrix.
4. **Relocate** `Assets/Scripts/SkySeries Freebie/` out of `Scripts/` — large non-code tree.
5. **Folder spelling** `Newbridge` vs `NewBridge` — settle on one; watch case on Windows.
6. **Merge** `SceneScripts/` into `Core/GameFlow/` on disk — move `.meta` with files; recheck Build Settings and addresses.
