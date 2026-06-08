---
name: unity-input-system
description: Use when working on Unity projects that use com.unity.inputsystem, generated .inputactions C# wrappers, action maps, input readers or adapters, MonoBehaviour input consumers, InputTestFixture regression coverage, or migration away from PlayerInput and string-based action lookups.
---

# Unity Input System

## Overview

Use this skill to generate or audit a Unity New Input System layer that keeps bindings, generated wrapper access, gameplay logic, and regression tests cleanly separated. Default to the generated `.inputactions` C# wrapper, strict `MonoBehaviour` lifecycle ordering, and deterministic PlayMode coverage with `InputTestFixture`.

## Quick Reference

| Concern | Default |
| --- | --- |
| Architecture | Generated wrapper -> reader or adapter -> plain state and events -> gameplay consumer |
| Wrapper choice | Generated C# class from `.inputactions`, not `PlayerInput` |
| `Awake()` | Instantiate only |
| `OnEnable()` | Subscribe first, then enable the map |
| `OnDisable()` | Disable the map first, then unsubscribe every handler |
| `OnDestroy()` | Dispose generated or runtime-created wrapper instances |
| Value controls | Poll with `ReadValue<T>()` in `Update()` or `FixedUpdate()` |
| One-shot controls | `.performed` callbacks |
| State reset | `.canceled` callbacks |
| Test posture | PlayMode `InputTestFixture` with virtual devices and `yield return null` |

## Workflow

1. Inspect the existing `.inputactions` asset, generated wrapper class, and current consumer scripts before proposing code.
2. If no generated wrapper exists, instruct the user to enable C# class generation on the `.inputactions` asset. Do not fall back to string-based lookups or `PlayerInput` unless the task is explicitly compatibility work.
3. Split the system into two layers:
   - Input generation: owns the generated wrapper instance, action-map lifetime, event handlers, and cached state.
   - Input consumption: reads plain properties or events and never touches bindings directly.
4. Match the read pattern to the control shape:
   - `Vector2` or `Vector3` controls: poll in `Update()` or `FixedUpdate()`.
   - One-shot buttons: use `.performed`.
   - Resettable state: use `.canceled`.
5. When asked to generate code, return exactly four phases:
   - `1. The Architecture Map`
   - `2. The Audited Script`
   - `3. The Regression Test`
   - `4. Safety Verification Checklist`

## Lifecycle Contract

| Callback | Required action | Forbidden action |
| --- | --- | --- |
| `Awake()` | Instantiate the generated wrapper only | Enabling action maps or binding to other objects |
| `OnEnable()` | Subscribe every callback, then enable the owning map | Enabling before the subscription list is complete |
| `OnDisable()` | Disable the owning map, then unsubscribe every handler added in `OnEnable()` | Leaving any subscription behind or reversing the symmetry |
| `OnDestroy()` | Dispose generated or runtime-created wrapper instances with `_actions?.Dispose();` | Assuming domain reload or scene unload will clean up for you |

## Naming and Placeholders

- Use `GameInputActions` only as a narrative example. Replace it with the repo's real generated wrapper type.
- In reusable templates, replace `<GeneratedActionsClass>` with the actual generated wrapper type from the target project.
- Rename example map and action members such as `Player`, `Move`, `Look`, or `Jump` to match the repo's real wrapper API.

## Reject These Patterns Unless Compatibility Work Is Explicitly Requested

- Enabling action maps in `Awake()`
- Asymmetric subscribe and unsubscribe pairs
- Movement driven only from `.performed`
- `PlayerInput` as the primary abstraction
- Direct string-based action lookup as the default path
- NUnit `[SetUp]` or `[TearDown]` on tests that inherit from `InputTestFixture`

If the repo already uses one of these patterns, call it out, explain the tradeoff, and keep compatibility work scoped to the user's request instead of silently standardizing on it.

## Resources

- Read [architecture-and-lifecycle.md](references/architecture-and-lifecycle.md) for the decoupled data flow, exact lifecycle ordering, and domain-reload cleanup rules.
- Read [input-handling-patterns.md](references/input-handling-patterns.md) for value polling, button callbacks, canceled resets, and reader-to-consumer API shapes.
- Read [inputtestfixture-playmode.md](references/inputtestfixture-playmode.md) for `InputTestFixture` setup, virtual devices, manifest setup, and frame-boundary rules.
- Copy from [InputReaderTemplate.cs.txt](assets/templates/InputReaderTemplate.cs.txt), [InputConsumerTemplate.cs.txt](assets/templates/InputConsumerTemplate.cs.txt), and [InputRegressionTestsTemplate.cs.txt](assets/templates/InputRegressionTestsTemplate.cs.txt) when the repo needs a concrete starting point.

## Common Mistakes

- Binding gameplay code directly to `InputAction` instances instead of exposing stable state or events.
- Forgetting the `.canceled` path, which leaves stale held-state or movement values behind.
- Writing tests that press or release inputs without a frame boundary, which makes assertions flaky.
- Assuming generated wrapper names or map names from an example without checking the real asset first.
- Claiming runtime verification is complete when only `InputTestFixture` or compilation checks have run.
