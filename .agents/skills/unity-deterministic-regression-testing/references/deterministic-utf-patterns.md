# Deterministic UTF Patterns

## Bounded Frame Loops

Use bounded loops for coroutine or async validation:

```csharp
const int FrameBudget = 5;
var completed = false;

for (var frame = 0; frame < FrameBudget && !completed; frame++)
{
    yield return null;
    completed = sut.IsComplete;
}

Assert.That(completed, Is.True, "Operation exceeded the declared frame budget.");
```

Do not use:

- `WaitForSeconds`
- wall-clock stopwatches
- `while (true)`
- loops that rely on machine speed

## State Machine Coverage

Test both success and rejection paths:

- expected transition reaches the exact target state
- invalid trigger leaves the previous state unchanged
- repeated trigger does not double-apply side effects
- null or missing dependency paths fail safely

Prefer explicit state assertions over log-only validation.

## Boundary Coverage

Always consider these edges when they apply:

- `null` references
- empty collections
- zero values
- negative values
- maximum and minimum integers
- float epsilon edges
- already-destroyed or disabled objects

If the code accepts vectors or floats, use a comparer instead of exact equality:

```csharp
private readonly Vector2EqualityComparer _vectorComparer = new Vector2EqualityComparer(0.0001f);
```

## InputTestFixture Rules

When inheriting from `InputTestFixture`:

- override `public override void Setup()`
- override `public override void TearDown()`
- call `base.Setup()` before adding devices
- destroy owned objects before `base.TearDown()`
- yield a frame after `Press(...)`, `Release(...)`, `Set(...)`, enable or disable transitions, and destruction paths that depend on callback timing

Do not use NUnit `[SetUp]` or `[TearDown]` on the same class.

## Async And Coroutine Proof

Use `[UnityTest]` with `IEnumerator` for multi-frame behavior. Prove:

- the operation starts
- the operation reaches completion or the expected stall state within a frame budget
- teardown stops further progress after disable or destroy
- the completion flag, callback, or state is correct after the final frame boundary

If async work is wrapped behind a fake scheduler or fake clock, keep the test in EditMode unless a real player-loop boundary is required.
