# Input Handling Patterns

## Choose the Read Pattern by Signal Type

| Control shape | Default pattern | Why |
| --- | --- | --- |
| `Vector2` or `Vector3` value | Poll with `ReadValue<T>()` every frame | Continuous devices need the current value, not just edge transitions. |
| One-shot button | `.performed` callback | The action matters at the instant it fires. |
| Held or resettable state | `.performed` plus `.canceled` | This gives a deterministic enter and exit path. |

Do not drive movement purely from `.performed`. A joystick, trackpad, or held keyboard direction needs continuous sampling.

## Recommended Reader Surface

Expose stable, gameplay-friendly state:

```csharp
public Vector2 Move { get; private set; }
public Vector2 Look { get; private set; }
public bool JumpHeld { get; private set; }
public event Action JumpPressed;
public event Action PausePressed;
```

This lets the consumer layer read values or subscribe to one-shot intent without needing to understand bindings or the generated wrapper.

## Reader to Consumer Boundary

| Reader owns | Consumer owns |
| --- | --- |
| Wrapper instance | Gameplay-side references such as motor, weapon, menu controller |
| Action callbacks | Converting input state into movement, combat, or UI behavior |
| Cached input state | Queueing or consuming gameplay actions |
| Resetting input state on cancel or disable | Resetting gameplay-only state on disable |

## Polling Pattern

Use polling for values even if the action also exposes callbacks:

```csharp
private void Update()
{
    Move = _actions.Player.Move.ReadValue<Vector2>();
    Look = _actions.Player.Look.ReadValue<Vector2>();
}
```

If gameplay logic runs in `FixedUpdate()`, keep the read in `Update()` and consume the cached value in `FixedUpdate()` unless the repo already has a different, intentional timing model.

## One-Shot Pattern

Use callbacks for actions that represent an instant:

```csharp
private void OnEnable()
{
    _actions.Player.Jump.performed += OnJumpPerformed;
    _actions.Player.Jump.canceled += OnJumpCanceled;
    _actions.Player.Enable();
}

private void OnJumpPerformed(InputAction.CallbackContext context)
{
    JumpHeld = true;
    JumpPressed?.Invoke();
}

private void OnJumpCanceled(InputAction.CallbackContext context)
{
    JumpHeld = false;
}
```

The event communicates intent to gameplay code. The held flag supports systems that care about button release or buffered state.

## Compatibility Work

If the repo already uses `PlayerInput` or string lookups:

- Flag the pattern as compatibility mode.
- Keep changes scoped to the user request.
- Avoid introducing more string-based access paths if the repo is already migrating toward generated wrappers.
- Explain the migration target clearly instead of silently rewriting architecture the user did not request.
