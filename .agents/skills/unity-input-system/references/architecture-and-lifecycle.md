# Architecture and Lifecycle

Use `GameInputActions` below as the narrative example type. Replace it with the generated wrapper type that exists in the target repo.

## Data Flow

```text
Hardware or OS signal
-> Input System bindings and action maps
-> Generated C# wrapper instance
-> Reader or adapter MonoBehaviour
-> Plain state and events
-> Gameplay consumer MonoBehaviours
```

## Ownership Split

| Layer | Owns | Must avoid |
| --- | --- | --- |
| Generated wrapper | Action maps, bindings, action instances | Gameplay decisions |
| Reader or adapter | Wrapper lifetime, event wiring, cached state | Moving characters, opening menus, combat rules |
| Gameplay consumer | Interpreting state and events for game logic | Touching bindings or instantiating wrappers |

Keep the reader layer thin. It translates hardware intent into stable code-facing state, not into gameplay behavior.

## Exact Lifecycle Order

| Callback | Required work | Notes |
| --- | --- | --- |
| `Awake()` | `_actions = new GameInputActions();` | Instantiate only. Do not enable maps or bind to other objects here. |
| `OnEnable()` | Subscribe callbacks, then enable the owning map | Build the full subscription list before the map starts producing callbacks. |
| `Update()` / `FixedUpdate()` | Poll value controls with `ReadValue<T>()` | Use this for movement, look, aim, or other continuously sampled values. |
| `OnDisable()` | Disable the owning map, then unsubscribe every handler added in `OnEnable()` | Keep the pair exact and symmetrical to avoid leaks and stale callbacks. |
| `OnDestroy()` | `_actions?.Dispose();` | Dispose runtime-created or generated wrapper instances explicitly. |

## Reader Skeleton

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInputReader : MonoBehaviour
{
    private GameInputActions _actions;

    public Vector2 Move { get; private set; }
    public bool JumpHeld { get; private set; }
    public event Action JumpPressed;

    private void Awake()
    {
        _actions = new GameInputActions();
    }

    private void OnEnable()
    {
        _actions.Player.Jump.performed += OnJumpPerformed;
        _actions.Player.Jump.canceled += OnJumpCanceled;
        _actions.Player.Enable();
    }

    private void Update()
    {
        Move = _actions.Player.Move.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Jump.performed -= OnJumpPerformed;
        _actions.Player.Jump.canceled -= OnJumpCanceled;
        Move = Vector2.zero;
        JumpHeld = false;
    }

    private void OnDestroy()
    {
        _actions?.Dispose();
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
}
```

## Domain Reload and Teardown

- Treat `OnDisable()` and `OnDestroy()` as mandatory cleanup boundaries. Domain reload, scene unload, and prefab teardown are not excuses to skip explicit cleanup.
- Do not keep generated wrapper instances in static fields unless the user explicitly wants a persistent input service and the lifetime rules are fully specified.
- Do not share one wrapper instance across unrelated gameplay consumers unless you are deliberately building a central input reader or service.
- If a task also touches prefabs, scenes, or serialized references, separate the code change from the Unity wiring work and state what still needs Play Mode verification.
