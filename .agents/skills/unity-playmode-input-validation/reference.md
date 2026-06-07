# PlayMode Input Validation Reference

## Manifest

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

## PlayMode Test Assembly (.asmdef)

```json
{
  "name": "Beavermania.PlayMode.InputTests",
  "rootNamespace": "Beavermania.Tests.PlayMode.Input",
  "references": [
    "Unity.InputSystem",
    "Unity.InputSystem.TestFramework",
    "UnityEngine.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

Place under a folder such as `Assets/Tests/PlayMode/Input/` with **Test Assemblies** enabled for PlayMode only in the Test Runner window.

---

## Input-to-Logic Flow Map Example

```
[Keyboard.spaceKey] --Press-->
  GameInputActions.Player.Jump (performed) -->
    PlayerInputReader.OnJumpPerformed -->
      JumpController.TryJump() -->
        Assert: _isGrounded == false && _velocity.y > 0
```

---

## Production Component Skeleton

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beavermania.Core.Input
{
    public sealed class ExampleInputReader : MonoBehaviour
    {
        private GameInputActions _actions;

        public bool JumpPressed { get; private set; }
        public Vector2 Move { get; private set; }

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

        private void OnDisable()
        {
            _actions.Player.Disable();
            _actions.Player.Jump.performed -= OnJumpPerformed;
            _actions.Player.Jump.canceled -= OnJumpCanceled;
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        private void Update()
        {
            Move = _actions.Player.Move.ReadValue<Vector2>();
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            JumpPressed = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            JumpPressed = false;
        }
    }
}
```

Replace `GameInputActions` with the project's generated wrapper type.

---

## PlayMode Validation Suite Skeleton

```csharp
using System.Collections;
using System.Collections.Generic;
using Beavermania.Core.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.Input
{
    public sealed class ExampleInputReaderTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private readonly List<GameObject> _ownedObjects = new();

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            for (int i = _ownedObjects.Count - 1; i >= 0; i--)
            {
                if (_ownedObjects[i] != null)
                {
                    Object.Destroy(_ownedObjects[i]);
                }
            }

            _ownedObjects.Clear();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator JumpPressed_True_AfterSpacePress()
        {
            // Arrange
            var go = new GameObject("Reader");
            _ownedObjects.Add(go);
            var reader = go.AddComponent<ExampleInputReader>();
            yield return null;

            // Act
            Press(_keyboard.spaceKey);
            yield return null;

            // Assert
            Assert.IsTrue(reader.JumpPressed);

            // Act
            Release(_keyboard.spaceKey);
            yield return null;

            // Assert
            Assert.IsFalse(reader.JumpPressed);
        }

        [UnityTest]
        public IEnumerator Move_Updates_AfterAxisSet()
        {
            // Arrange
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var go = new GameObject("Reader");
            _ownedObjects.Add(go);
            var reader = go.AddComponent<ExampleInputReader>();
            yield return null;

            // Act
            Set(gamepad.leftStick, new Vector2(1f, 0f));
            yield return null;

            // Assert
            Assert.AreEqual(new Vector2(1f, 0f), reader.Move);
        }
    }
}
```

---

## Frame Yield Points

Yield at least one frame after:

- `Press(...)` / `Release(...)`
- `Set(...)` on axes or sticks
- Enabling or disabling the component under test
- Destroying the tested object
- Any assertion that depends on `Update()` / `FixedUpdate()` / callback timing

For multi-frame behavior, use a bounded loop with a declared frame budget and fail if exceeded.

---

## What InputTestFixture Proves vs Does Not Prove

**Proves:**

- Virtual device → action → reader/consumer state transitions
- Callback enable/disable symmetry
- Teardown leaves no stale input state

**Does not prove:**

- Inspector wiring or prefab references
- Scene integration or UI focus
- Real hardware latency or platform-specific bindings
