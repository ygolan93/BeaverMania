# Test Assembly And Layout

## Default Layout

Use these folders by default:

- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`

Keep runtime production code outside the test folders. Keep helper fakes local to the smallest test assembly that needs them.

## Assembly Definition Rules

Create a dedicated `.asmdef` per test mode:

- one EditMode test assembly under `Assets/Tests/EditMode/`
- one PlayMode test assembly under `Assets/Tests/PlayMode/`

Unity 2021.3 guidance:

- Mark the assembly as a test assembly using the canonical Unity test-assembly metadata.
- Prefer the normal Unity reference flow with `overrideReferences: false`.
- If `overrideReferences` is turned on, explicitly verify that `UnityEngine.TestRunner` and `UnityEditor.TestRunner` still resolve in the Inspector or generated `.csproj`.
- Keep `autoReferenced` conservative when isolating helper test assemblies.

Common naming patterns:

- `BeaverProject.Foundation.EditMode.Tests`
- `BeaverProject.Foundation.PlayMode.Tests`
- `BeaverProject.<Feature>.EditMode.Tests`
- `BeaverProject.<Feature>.PlayMode.Tests`

## Mode Split

Choose EditMode when the behavior can run without the real player loop:

- state machines
- validation logic
- null and boundary handling
- fake clocks or fake tickers
- editor-safe wrappers around Unity services

Choose PlayMode when behavior depends on runtime frame progression:

- coroutines
- delayed callbacks that require `yield return null;`
- enable or disable ordering across frames
- animation or physics-adjacent gameplay code
- runtime object lifetime that must flow through Unity callbacks

Do not put heavy scene loading or hardware-dependent input paths into EditMode tests.

## Input System Note

If the suite uses `InputTestFixture`, confirm the target project actually uses the Input System package. When package tests need access to Input System test helpers, expose the package through `testables` in `Packages/manifest.json` and keep the fixture lifecycle overrides in the PlayMode test assembly unless the target behavior is proven editor-safe.
