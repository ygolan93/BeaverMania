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
cd /workspace/.ci && dotnet build BeaverMania.CI.csproj
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
