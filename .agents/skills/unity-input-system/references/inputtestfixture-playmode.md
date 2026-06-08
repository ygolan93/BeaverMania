# InputTestFixture PlayMode

## Manifest Setup

Add `com.unity.inputsystem` to the `testables` array in `Packages/manifest.json` if the project does not already expose the package to tests:

```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.x.x"
  },
  "testables": [
    "com.unity.inputsystem"
  ]
}
```

## Fixture Rules

- Inherit from `InputTestFixture`.
- Do not use NUnit `[SetUp]` or `[TearDown]` on that class. Override `public override void Setup()` and `public override void TearDown()` instead.
- Call `base.Setup()` before adding virtual devices.
- Destroy spawned `GameObject` instances before `base.TearDown()`.
- Add hardware with `InputSystem.AddDevice<T>()`.

## Deterministic Frame Boundaries

After every simulated input change, yield a frame:

- `Press(keyboard.spaceKey);`
- `yield return null;`
- assert

Do the same after:

- `Release(...)`
- enabling or disabling the component under test
- destroying the tested object
- any operation that relies on Unity callback timing

## Typical Test Shape

```csharp
public sealed class ReaderTests : InputTestFixture
{
    private Keyboard _keyboard;

    public override void Setup()
    {
        base.Setup();
        _keyboard = InputSystem.AddDevice<Keyboard>();
    }

    public override void TearDown()
    {
        // Destroy test objects here.
        base.TearDown();
    }

    [UnityTest]
    public IEnumerator Jump_Fires_OnPerformed()
    {
        Press(_keyboard.spaceKey);
        yield return null;
        // Assert here.
    }
}
```

## What to Prove

At minimum, cover:

- value polling updates after device input
- one-shot callbacks fire on `.performed`
- reset state clears on `.canceled`
- disabled components stop receiving callbacks
- teardown paths do not leave stale state behind

`InputTestFixture` proves code-level behavior. It does not prove Inspector wiring, scene integration, UI focus behavior, or other serialized Unity setup.
