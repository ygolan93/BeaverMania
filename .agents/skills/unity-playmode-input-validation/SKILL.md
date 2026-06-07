---
name: unity-playmode-input-validation
description: Generates and audits deterministic PlayMode integration tests for com.unity.inputsystem using InputTestFixture and Unity Test Framework. Simulates virtual hardware, validates gameplay logic responses, and enforces lifecycle and memory safety. Use when creating or debugging PlayMode input tests, InputTestFixture setup, virtual device injection, frame-boundary assertions, or input regression coverage.
---

# Unity PlayMode Input Validation

Expert Codex skill for Cursor specializing in **Unity PlayMode Input Validation** using the New Input System (`com.unity.inputsystem`) and the Unity Test Framework (UTF).

**Sole mission:** Generate and audit deterministic PlayMode integration tests that simulate hardware inputs, validate gameplay logic responses, and ensure clean execution without leaking memory or state across test runs.

## Related Skills

- [unity-input-system](../unity-input-system/SKILL.md) — action maps, generated wrappers, reader lifecycle
- [unity-deterministic-regression-testing](../unity-deterministic-regression-testing/SKILL.md) — broader UTF hermetic testing rules
- [unity-lifecycle-memory-safety](../unity-lifecycle-memory-safety/SKILL.md) — event leak and teardown patterns

For manifest, asmdef, and fixture details, see [reference.md](reference.md).

---

## 1. The PlayMode Testing Paradigm

- **Isolate via InputTestFixture:** Every test class validating input must inherit from `InputTestFixture`.
- **Strict Setup/TearDown Overrides:** Explicitly forbid NUnit `[SetUp]` and `[TearDown]` attributes. They break the fixture's internal state. You MUST use:

```csharp
public override void Setup() { base.Setup(); /* Custom setup */ }
public override void TearDown() { /* Custom cleanup */ base.TearDown(); }
```

- **Virtual Device Injection:** Always use `InputSystem.AddDevice<T>()` (e.g., `Keyboard`, `Gamepad`, `Mouse`) to create virtual hardware inside `Setup()`. Never poll real physical hardware.

---

## 2. Deterministic Input Simulation

- **The Frame Boundary Rule:** Input signals require time to propagate through Unity's event loop. Always use `[UnityTest]` returning an `IEnumerator`, and yield at least one frame (`yield return null;`) after an input action to let `Update()` or `FixedUpdate()` process the state change before asserting.
- **Simulating Actions:** Use the built-in fixture methods for input delivery:
  - Buttons: `Press(_keyboard.spaceKey);` followed by `Release(_keyboard.spaceKey);`
  - Values/Axes: `Set(_gamepad.leftStick, new Vector2(1f, 0f));`

---

## 3. Lifecycle & Memory Safety Regulation

- **Scene Cleanup:** Track every GameObject instantiated during `Setup` or the test body (e.g., player prefabs, cameras, UI canvases). Explicitly invoke `Object.Destroy(go);` inside `TearDown()`.
- **Action Map Safety:** Ensure that the components being tested enable their Action Maps inside `OnEnable()` and cleanly disable/unsubscribe from them inside `OnDisable()` so they don't capture events outside the active test scope.

---

## 4. Compilation & Configuration Rules

- **Assembly Validation:** Remind the user that these tests must live in a folder with an `.asmdef` file restricted to the `PlayMode` platform, with references to:
  - `Unity.InputSystem`
  - `Unity.InputSystem.TestFramework`
  - `UnityEngine.TestRunner`
- **Package Manifest:** Ensure `"com.unity.inputsystem"` is declared inside the `"testables"` array of `Packages/manifest.json`.

---

## 5. Required Output Structure

When asked to create an input validation test or debug a script, always respond with these **4 scannable sections**:

1. **Input-to-Logic Flow Map** — A quick text diagram tracking the virtual hardware event → action map → component state response.
2. **The Production Component** — A highly-testable C# script implementing correct Input System lifecycle patterns.
3. **The PlayMode Validation Suite** — A complete, compilation-ready C# test file inheriting from `InputTestFixture` using the AAA (Arrange, Act, Assert) format with proper frame-yielding.
4. **Determinism Checklist** — A small markdown table confirming device cleanup, frame-wait coverage, and scene memory safety.

If the production component needs no change, section 2 must contain exactly:

`No production script change required.`

---

## Workflow

1. Inspect the target component, generated `.inputactions` wrapper, action maps, and existing test footprint.
2. Map virtual device controls → bound actions → reader/consumer state → asserted outcome.
3. Verify production code follows `OnEnable()` subscribe-then-enable and `OnDisable()` disable-then-unsubscribe symmetry.
4. Generate or repair the PlayMode suite with virtual devices, explicit ownership tracking, and frame yields after every simulated input.
5. Confirm asmdef and manifest configuration; report honestly what compilation vs UTF execution proves.

---

## Reject These Patterns

- NUnit `[SetUp]` / `[TearDown]` on `InputTestFixture` subclasses
- Reading real physical hardware in tests
- Asserting immediately after `Press`/`Set` without `yield return null`
- Spawning GameObjects without explicit `Object.Destroy` in `TearDown`
- Action maps enabled in `Awake()` or left enabled after `OnDisable()`
- Claiming input behavior is verified when only compilation ran

---

## Determinism Checklist Template

Use this table in section 4 of every response:

| Check | Status |
| --- | --- |
| Inherits `InputTestFixture` with `Setup()` / `TearDown()` overrides | |
| Virtual devices added in `Setup()` via `InputSystem.AddDevice<T>()` | |
| `yield return null` after each simulated input change | |
| All spawned GameObjects destroyed in `TearDown()` before `base.TearDown()` | |
| Action maps enabled in `OnEnable()`, disabled in `OnDisable()` | |
| PlayMode `.asmdef` references Input System + TestFramework | |
| `com.unity.inputsystem` in manifest `testables` | |
