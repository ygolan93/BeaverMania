---
name: unity-deterministic-regression-testing
description: Use when auditing, generating, or repairing Unity Test Framework regression coverage for Unity 2021.3 projects, including EditMode and PlayMode tests, hermetic setup and teardown, deterministic frame stepping, InputTestFixture lifecycle overrides, state-machine transition coverage, coroutine or async flow coverage, boundary-condition stress tests, test assembly (.asmdef) layout, and CI-safe regression hardening.
---

# Unity Deterministic Regression Testing

Use this skill to generate or repair Unity Test Framework coverage that is fast, hermetic, and repeatable in BeaverProject-style Unity repos. Default to script-level changes only, minimal testability seams, and honest verification language.

## Output Contract

When asked to generate, refactor, or repair regression coverage, return exactly these four phase headings:

1. `The Component Map`
2. `The Production Script`
3. `The Regression Test Suite`
4. `Test Stability Checklist`

If the production code does not need to change, phase 2 must contain the exact sentence:

`No production script change required.`

## Hard Gates

Reject or explicitly warn on these patterns:

- Solving testability by editing prefabs, scenes, Animator assets, ScriptableObject YAML, or UI hierarchy assets.
- Tests without explicit `// Arrange`, `// Act`, and `// Assert` blocks.
- NUnit `[SetUp]` or `[TearDown]` on classes that inherit from `InputTestFixture` or another fixture with its own lifecycle contract. Use `public override void Setup()` and `public override void TearDown()` instead.
- Spawning `GameObject` instances, prefab instances, temporary assets, or static state without explicit ownership and teardown.
- `WaitForSeconds`, wall-clock timing, unbounded polling loops, or assertions that depend on machine speed.
- Putting runtime physics, input hardware dependency, or heavy scene integration into EditMode tests.
- Claiming runtime behavior is verified when only compilation or static review ran.

Cleanup defaults:

- EditMode: destroy owned objects with `Object.DestroyImmediate()`.
- PlayMode: destroy owned objects with `Object.Destroy()` and yield a frame before final assertions when callback timing matters.

Timing defaults:

- Replace time-based waiting with mocked clocks, fake tick sources, explicit counters, or bounded frame loops.
- After simulated input, enable or disable transitions, or destruction that depends on callback timing, use `yield return null;`.
- Every multi-frame assertion must declare a frame budget and fail if the budget is exceeded.

## Workflow

1. Inspect the target script, dependencies, serialized fields, and existing test footprint before proposing code.
2. Choose the narrowest valid test boundary:
   - EditMode for pure logic, isolated state transitions, deterministic helpers, and editor-safe seams.
   - PlayMode for frame-driven behavior, coroutine progress, runtime callbacks, or gameplay integration that must tick through Unity's player loop.
3. If the current code is not hermetically testable, introduce the smallest possible seam:
   - constructor or setter injection for plain C# collaborators
   - adapter interfaces around Unity-only services
   - fake clocks, fake tickers, or bounded coroutine drivers
   - explicit reset methods for static state
4. Design regression coverage that proves:
   - expected state-machine transitions
   - invalid transitions do not mutate state
   - coroutine or async flows finish within a declared frame budget
   - null, empty, max, min, and negative-value inputs stay stable
5. Track every owned runtime object and every temporary global mutation so teardown is symmetric.
6. Report verification honestly. Compilation proves syntax and type safety only. UTF execution proves only the scenarios that ran.

## Phase Requirements

### 1. The Component Map

- Name the unit under test.
- List owned dependencies, mocked dependencies, and runtime boundaries.
- State whether the suite belongs in EditMode or PlayMode and why.
- Call out what is being isolated from Unity serialization or scene wiring.

### 2. The Production Script

- Return a complete, compilation-ready script when a code change is required.
- Keep the seam minimal and local to the requested regression need.
- Do not broaden the architecture or normalize unrelated code.
- If no change is needed, return only:

`No production script change required.`

### 3. The Regression Test Suite

- Return a complete UTF test class.
- Every test must contain labeled `// Arrange`, `// Act`, and `// Assert` blocks.
- Use deterministic comparers for floats or vectors when exact equality is not stable.
- Use `InputTestFixture` lifecycle overrides when the base fixture owns setup or teardown.
- Destroy all owned objects in teardown and reset every touched static or global value.
- Keep all loops bounded. Prefer constants such as `const int FrameBudget = 5;`.

### 4. Test Stability Checklist

Use a compact Markdown table that covers at least:

- isolation safety
- deterministic timing
- owned object cleanup
- static or global reset
- correct EditMode or PlayMode placement
- compile verification
- runtime proof limits
- Unity follow-up status

If runtime behavior remains unverified, end the response with the exact line:

`Needs Unity Play Mode verification.`

## Resources

- Read [test-assembly-and-layout.md](references/test-assembly-and-layout.md) for folder layout, `.asmdef` rules, and EditMode versus PlayMode placement.
- Read [hermetic-isolation-and-cleanup.md](references/hermetic-isolation-and-cleanup.md) for ownership tracking, static reset, and teardown ordering.
- Read [deterministic-utf-patterns.md](references/deterministic-utf-patterns.md) for bounded frame loops, `InputTestFixture` rules, comparer guidance, and boundary-condition coverage.
- Read [ci-and-verification.md](references/ci-and-verification.md) for `.ci` compilation checks, Unity batch-mode test commands, and verification wording.
- Copy from [EditModeRegressionTemplate.cs.txt](assets/templates/EditModeRegressionTemplate.cs.txt), [PlayModeRegressionTemplate.cs.txt](assets/templates/PlayModeRegressionTemplate.cs.txt), and [TestAssemblyDefinitionTemplate.asmdef.txt](assets/templates/TestAssemblyDefinitionTemplate.asmdef.txt) when the repo needs a concrete starting point.

## Common Mistakes

- Writing one integration-style PlayMode test when the risky logic could have been locked down with smaller EditMode tests.
- Asserting after a simulated input change without a frame boundary.
- Using `WaitForSeconds` in a regression suite and calling it deterministic.
- Forgetting to restore `Time.timeScale`, static events, or singleton caches between tests.
- Leaving teardown in comments instead of code.
- Returning code snippets instead of complete scripts or complete test classes.
