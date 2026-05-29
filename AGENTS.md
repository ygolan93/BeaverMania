# Beavermania — Codex Agent Instructions

## 1. Project Identity

Beavermania is a Unity 2021.3.3f1 3D cartoon action/platformer targeting a Steam-ready Windows PC release. The game includes player movement and combat, NPC and trader dialogue/shop interactions, audio systems, checkpoints and game-flow, menus, enemies, pickups, and performance-sensitive levels.

This is a Unity project with fragile serialized references. Treat scene, prefab, UI, Animator, and Inspector wiring as runtime assets rather than ordinary editable source files.

Useful project constraints:

- The product is a standalone Unity game with no backend service, database, or Docker dependency.
- Use `Beavermania.*` namespaces for migrated or new game code, following [README.md](README.md) and [MIGRATION.md](MIGRATION.md).
- Unity 2021.3 supports C# 9; `.ci/BeaverMania.CI.csproj` uses `LangVersion 9.0`.
- Do not change `.meta` GUIDs for frozen or prefab-bound scripts. See [MIGRATION.md](MIGRATION.md).

## 2. Default Codex Role

Codex should default to cautious script-level assistance and review.

Codex may:

- Inspect code and explain systems.
- Fix small, isolated C# bugs.
- Add defensive guards and null-reference protection.
- Improve local state logic with minimal behavior impact.
- Review diffs and identify regression, compilation, and Unity serialization risks.
- Prepare commit summaries and PR summaries.
- Perform code-level performance and audio routing review.
- Use worktrees for isolated experiments.

Codex should avoid unless explicitly requested:

- Broad refactors or architecture changes.
- Gameplay behavior changes outside the stated scope.
- Scene, prefab, animation clip, Animator, or UI hierarchy edits.
- Serialized Inspector reference changes.
- Unrelated cleanup.
- Unity integration claims that have not been verified in the Editor.

## 3. Tool Division

| Tool | Use For | Do Not Rely On It For |
| --- | --- | --- |
| Codex Desktop | Isolated C# fixes, null guards, local state fixes, diff review, summaries, worktree experiments, code-level performance/audio review | Prefab/scene/UI hierarchy/animation wiring or visual validation |
| Cursor + Unity Editor | Prefab and scene hierarchy editing, UI wiring, animation work, serialized Inspector assignments, Play Mode and visual debugging, complex Unity integration | Replacing independent code review |
| Codex Web / GitHub | PR review, second-opinion regression review, pre-merge risk checks | Editing or validating local Unity runtime wiring |

Existing Cursor workflow guidance remains relevant: use planning/review for multi-system work, and only use focused implementation for a small, known script bug with a specific expected outcome.

## 4. High-Risk Files and Areas

Treat these as high risk:

- `PlayerCanvas.prefab`
- `Trader.prefab`
- `TraderPanel.prefab`
- Level 1 scenes, including Level 1 Remastered scenes
- Player prefabs
- Animation clips and Animator-bound changes
- Dialogue and shop UI panels
- Checkpoint and game-flow scripts
- Audio routing scripts
- Player combat scripts

Examples present in this repository include:

- `Assets/Prefabs/Objects/UI/PlayerCanvas.prefab`
- `Assets/Prefabs/BeaverNPC/Trader.prefab`
- `Assets/Prefabs/Objects/UI/TraderPanel.prefab`
- `Assets/Scenes/Level 1.unity`
- `Assets/Scenes/Level 1 - Remastered - Steam.unity`
- `Assets/Prefabs/OtterPlayer/Player Updated.prefab`
- `Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab`

Do not edit Unity YAML scene or prefab files casually. Never touch multiple prefabs or scenes in one pass unless explicitly requested.

## 5. Unity Safety Rules

- Prefer script-level fixes before prefab-level changes.
- Do not casually edit `.unity`, `.prefab`, animation, or other serialized asset files.
- Do not rename serialized fields unless migration is handled.
- Do not remove public or serialized fields without checking prefab and scene usage.
- Do not rename or move `MonoBehaviour` files/classes without checking GUID and serialized binding consequences.
- Do not assume Inspector references exist; add null checks where missing references are plausible.
- If an Inspector assignment is required, list it clearly as manual work instead of guessing or changing YAML.
- Preserve `.meta` files and GUIDs, especially the frozen and prefab-bound scripts listed in [MIGRATION.md](MIGRATION.md).
- Any requested prefab, scene, Animator, UI binding, ScriptableObject, or runtime object-wiring change requires Unity Play Mode verification.

System-specific constraints retained from existing project guidance:

- `PlayerInputReader` owns input events and action-map switching. Gameplay scripts subscribe to it; do not add raw duplicate input paths or hand-edit generated input files unless specifically required.
- Game-flow code must not implement player movement or combat. Pause/resume must restore `Time.timeScale` and cursor state correctly; restart/checkpoint work requires Play Mode checks.
- Player work must account for input, animation, camera, and Inspector references; avoid growing a god script.
- Trader proximity should only show interaction prompts. Dialogue/shop opening and closing must correctly restore UI, camera, cursor, and player control.
- Audio work must verify correct music/SFX routing and volume behavior in Play Mode; do not duplicate `AudioSource` components as a shortcut.

## 6. Coding Style

- Make minimal, focused changes.
- Avoid unrelated cleanup.
- Preserve existing gameplay feel unless the request explicitly changes it.
- Prefer small components or helpers only when they solve the stated issue without broadening scope.
- Avoid allocations in `Update`, `FixedUpdate`, `LateUpdate`, and other hot loops.
- Avoid repeated `GetComponent` calls in hot paths when a stable reference can be cached safely.
- Use `Beavermania.*` namespaces where the current migration conventions require them.
- Keep changes compatible with Unity 2021.3 and C# 9.

## 7. Verification Expectations

For C# changes, run the compilation check when the environment supports it:

```bash
dotnet build .ci/BeaverMania.CI.csproj
```

In the Cursor Cloud environment, the equivalent documented command is:

```bash
cd /workspace/BeaverMania/.ci && dotnet build BeaverMania.CI.csproj
```

Environment notes retained from existing project guidance:

- `Library/` is gitignored; Unity resolves packages when the project first opens.
- The `.ci/` project is a compilation workaround that references Unity managed assemblies and Cinemachine source; its normal Unity serialization warnings are suppressed in the project file.
- In the documented Cursor Cloud environment, Unity Editor is at `/opt/unity/Editor/Unity` and can only perform batch-mode checks when a valid Unity license is present.

This build checks syntax, type references, namespaces, and other C# compilation issues. It does not verify Inspector assignments, missing prefab scripts, scene references, Animator parameters, UI Button bindings, camera transitions, cursor state, audio behavior, or runtime gameplay.

No automated Unity unit/integration tests are currently documented for this project. Unity Editor/Play Mode remains the source of truth for serialized and runtime behavior.

After any change, report:

- Changed files.
- Behavior changed.
- Risk level.
- Verification performed and what it proves.
- Manual Unity verification steps.
- Whether Inspector or prefab assignments are required.
- Assumptions, risks, and limitations.

Do not claim a bug is fully fixed without an available build/test/run verification that demonstrates it. When runtime Unity behavior remains untested, state: "Needs Unity Play Mode verification."

## 8. Branch and Worktree Policy

- Risky tasks should use a separate branch or worktree.
- Never work directly on `main` for agent-driven changes.
- Keep a branch focused on one bug or one review objective.
- Do not allow Codex and Cursor/Unity to edit the same prefab, scene, animation, or UI hierarchy files concurrently.

Recommended branch names:

- `fix/trader-shop-close`
- `fix/audio-slider-sfx`
- `fix/lookrotation-zero-vector`
- `review/performance-gc`
- `refactor/player-combat-safe`

## 9. Current Project Priorities

- Steam release candidate stability.
- Level 1 Remastered integration.
- Trader, dialogue, and shop stability.
- Player combat polish.
- Audio balance and routing.
- FPS and rendering stability.
- Avoid large architecture work unless it directly supports Steam release stability.

For intake and routing between ChatGPT, Cursor, and Codex, use the lightweight routing inbox layer in [AI_WORKFLOW/README.md](AI_WORKFLOW/README.md).

For repo-local workflow detail, read:

- [AI_CONTEXT/PROJECT_OVERVIEW.md](AI_CONTEXT/PROJECT_OVERVIEW.md)
- [AI_CONTEXT/CURRENT_STATUS.md](AI_CONTEXT/CURRENT_STATUS.md)
- [AI_CONTEXT/UNITY_SAFETY_RULES.md](AI_CONTEXT/UNITY_SAFETY_RULES.md)
- [AI_CONTEXT/CODEX_WORKFLOW.md](AI_CONTEXT/CODEX_WORKFLOW.md)
- [AI_CONTEXT/REVIEW_CHECKLIST.md](AI_CONTEXT/REVIEW_CHECKLIST.md)

## Cursor Cloud specific instructions

### Environment summary

- **.NET SDK 8.0** is pre-installed. The update script runs `dotnet restore .ci/BeaverMania.CI.csproj` on startup.
- **Unity Editor 2021.3.3f1** is at `/opt/unity/Editor/Unity`. It can report its version and run batch-mode commands, but Play Mode / GUI testing requires a valid Unity license (not present by default in the Cloud VM).
- **Unity managed DLLs** are at `/opt/unity/Editor/Data/Managed/UnityEngine/`.
- **Cinemachine source** is at `/opt/unity-packages/cinemachine/`.

### Compilation check (primary verification)

```bash
cd /workspace/BeaverMania/.ci && dotnet build BeaverMania.CI.csproj
```

This validates C# syntax, type references, and namespaces across all scripts in `Assets/Scripts/` and `Assets/Prefabs/`. It does **not** verify Inspector assignments, runtime gameplay, or serialized references.

### What you cannot do in Cloud without a Unity license

- Open the Unity Editor GUI or enter Play Mode.
- Run Unity Test Runner (EditMode/PlayMode tests).
- Build the game executable.

These require a user-activated Unity license on the VM.

### No other services or dependencies

This is a standalone Unity game project. There are no backend services, databases, Docker containers, npm/pip packages, or web servers to start.
