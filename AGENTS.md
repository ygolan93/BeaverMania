# BeaverMania — Agent Instructions

## Cursor Cloud specific instructions

### Environment Overview

This is a Unity 2021.3.3f1 C# game project (3D action-platformer). It has **no backend services, no databases, no Docker dependencies**. The entire product runs as a standalone Unity game.

### Available Tools

| Tool | Location | Purpose |
|------|----------|---------|
| Unity Editor | `/opt/unity/Editor/Unity` | Batch-mode builds, tests, scene loading (requires license) |
| .NET SDK 8.0 | `dotnet` | C# compilation checking via `.ci/BeaverMania.CI.csproj` |
| Cinemachine source | `/opt/unity-packages/cinemachine/` | Package source for compilation |

### Compilation / Lint Check

```bash
cd /workspace/BeaverMania/.ci && dotnet build BeaverMania.CI.csproj
```

This compiles all 88 game scripts (`Assets/Scripts/`) plus prefab scripts (`Assets/Prefabs/`) against Unity's managed DLLs and Cinemachine source. Zero warnings/errors means the code is valid C# that will compile in Unity.

**Suppressed warnings** (via `NoWarn` in the csproj): CS0649 (unassigned serialized fields — normal for Unity inspector-assigned fields), CS0414 (fields used by Unity serialization), CS0169, CS0108, CS0114, CS0162.

### Unity Editor Batch Mode (requires license)

If a Unity license file (`.ulf`) is placed at `~/.local/share/unity3d/Unity/Unity_lic.ulf`, the editor can be used:

```bash
/opt/unity/Editor/Unity -batchmode -nographics -projectPath /workspace -logFile - -quit
```

Without a license, the editor will exit with code 1 and a "Missing or bad username or password" message. This is expected.

### Tests

No automated unit/integration tests exist in this project. The `com.unity.test-framework` package is referenced but no test assemblies have been created.

### Key Caveats

- **No `Library/` folder**: This is gitignored. Unity resolves packages on first open. The `.ci/` compilation project uses pre-extracted DLLs as a workaround.
- **Scene files are binary YAML**: Do not hand-edit `.unity` or `.prefab` files unless you understand Unity YAML serialization.
- **GUID preservation**: See `MIGRATION.md` — never change `.meta` file GUIDs for frozen scripts.
- **Namespace conventions**: See `README.md` and user rules — use `Beavermania.*` namespaces (e.g., `Beavermania.Player`, `Beavermania.Core.GameFlow`).
- **C# version**: Unity 2021.3 supports C# 9. The CI project uses `LangVersion 9.0`.

---

## AI Development Workflow

### Tool / Agent Roles

| Tool | Use For | Avoid |
|------|---------|-------|
| **Cursor Plan** | Architecture, refactors, multi-system bug analysis, risky Unity changes | Direct implementation without review |
| **Cursor Agent** | Focused implementation of one clear task | Broad refactors or unrelated cleanup |
| **Cursor Multitask** | Independent parallel tasks that don't share files | Player/Input/Camera/Trader/Audio systems with shared dependencies |
| **Codex** | PR review, code review, regression analysis, compile-risk review, summarizing completed changes | Unity PlayMode validation without video/context |
| **Unity Editor** | Final source of truth for runtime behavior, serialized references, prefabs, scenes, UI bindings, PlayMode behavior | Not a replacement for code review |

### Mode Selection Rules

**Use Cursor Plan** when the task touches:
- Player movement, combat, or input
- Trader, dialogue, or shop integration
- Camera switching
- Pause, restart, checkpoint, win/lose flow
- Audio manager, music persistence, volume sliders
- Scene/prefab wiring
- Any refactor across multiple scripts

**Use Cursor Agent** when:
- The bug is isolated
- The target scripts are known
- The expected behavior is specific
- The change can be verified with `dotnet build` and one Unity PlayMode test

**Use Cursor Multitask** only when:
- Tasks are independent
- Files do not depend on each other
- No shared serialized references are involved
- No scene/prefab integration is required

**Use Codex** for:
- Reviewing a finished branch or PR
- Finding risky regressions
- Checking whether a proposed fix is safe
- Summarizing what changed
- Verifying whether additional commits are still relevant

---

## Unity Runtime Safety Rules

The `.ci/BeaverMania.CI.csproj` build is **only a C# sanity check**.

**It validates:**
- C# syntax
- Compile errors
- Missing `using` statements
- Broken type references
- Some namespace/class conflicts

**It does NOT validate:**
- Inspector-assigned references
- Missing scripts on prefabs
- Scene object references
- Animator controller parameters
- UI Button OnClick bindings
- ScriptableObject assignments
- Camera transitions
- Cursor lock/visibility state
- PlayMode-only behavior

**Rule:** Any change involving Unity serialization, prefabs, scenes, Animator, UI buttons, ScriptableObjects, or runtime object wiring **must** include a manual Unity verification checklist in the agent's response.

---

## System Ownership Map

### Input

**Primary files:** `PlayerInputReader`, BeaverInputSystem generated files, player/controller scripts subscribing to input events.

**Rules:**
- `PlayerInputReader` owns input events and action map switching.
- Gameplay scripts subscribe to `PlayerInputReader` events — do not read raw input directly.
- Do not add duplicate input pathways unless explicitly required.
- Do not hand-edit generated input system files unless the task explicitly asks for it.

### Game Flow

**Primary systems:** GameMaster, pause menu, restart/checkpoint flow, win/lose flow, cursor visibility and lock state.

**Rules:**
- Game flow code should not directly implement player movement or combat.
- Pause/resume must restore `Time.timeScale` and cursor state correctly.
- Restart/checkpoint changes must be verified in Unity PlayMode.

### Player

**Primary systems:** Movement, jump, sprint, combat, health, animation driver, inventory/pickups.

**Rules:**
- Avoid growing a single god script.
- Prefer small components with clear responsibilities when refactoring is explicitly requested.
- Do not broadly refactor player systems unless the task explicitly asks for it.
- Any player change must consider input, animation, camera, and inspector references.

### NPC / Trader

**Primary systems:** Trader interaction trigger, dialogue UI, shop UI, camera switch, ObjectiveText prompt.

**Rules:**
- Trader proximity should only show the interaction prompt.
- Dialogue/shop should open only after player input.
- Closing shop/dialogue must restore UI state, camera state, cursor state, and player control.
- Do not solve trader UI bugs by duplicating panels or creating parallel state flows.

### Audio

**Primary systems:** Music playlist, SFX, UI volume sliders, music/SFX balance.

**Rules:**
- Music should persist across scene transitions if the current design requires it.
- UI sliders must control the correct audio sources or mixers.
- Do not solve audio bugs by blindly duplicating AudioSources.
- Volume changes must be tested in PlayMode.

---

## Change Policy

### Before Changing Code

Agents must:
- Identify the exact files involved.
- State whether prefabs, scenes, or inspector references are affected.
- Avoid unrelated cleanup.
- Avoid broad rewrites unless explicitly requested.
- Prefer small, reviewable changes.

### After Changing Code

Run:

```bash
cd /workspace/BeaverMania/.ci && dotnet build BeaverMania.CI.csproj
```

Then report:
- Files changed
- What behavior changed
- What the CI build confirms
- What still requires Unity Editor verification
- Any assumptions made
- Any risks or limitations

### Definition of Done

A task is **not complete** until:
- C# compilation passes, or the failure is clearly explained.
- No unrelated systems were modified.
- Unity-specific manual checks are listed.
- Known limitations are stated.
- The agent clearly separates what was verified by CI from what still requires Unity PlayMode testing.

---

## Refactor Safety Rules

- Do not perform large architecture refactors during bug fixes.
- Do not rename scripts/classes without considering Unity serialization and `.meta` GUIDs.
- Do not move scripts unless necessary.
- Do not delete old scripts unless references have been checked.
- Prefer adding safe adapter code over breaking existing serialized references.
- If a refactor is necessary, first produce a plan and list affected systems.

---

## Prompting / Reporting Standard

Agents should structure responses as:

1. **Summary** — Short description of the task.
2. **Files touched** — List of files expected to change.
3. **Risk level:**
   - **Low** — pure C# isolated change
   - **Medium** — touches multiple scripts
   - **High** — touches prefabs/scenes/inspector/UI/Animator/player/trader/game flow
4. **Implementation summary** — What was done.
5. **Verification performed** — CI build result, tests run.
6. **Unity manual checks required** — What must be verified in the editor.
