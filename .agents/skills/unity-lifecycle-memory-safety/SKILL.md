---
name: unity-lifecycle-memory-safety
description: Use when auditing or generating Unity C# code for lifecycle ordering, event leak prevention, async cancellation, static-event cleanup, coroutine teardown, native allocation disposal, and Unity Test Framework lifecycle regression coverage.
---

# Unity Lifecycle Memory Safety

## Overview

Use this skill to audit or generate Unity C# scripts that manage initialization, subscriptions, async work, coroutines, native memory, and teardown explicitly. Default to BeaverProject safety rules: script-level only, no prefab or scene edits, no unsafe serialized-field renames, and no runtime-fix claims without proof.

When asked to audit code or generate a script, return exactly these four phases:

1. `The Lifecycle Flow`
2. `The Audited/Corrected Script`
3. `The Lifecycle Regression Test`
4. `Safety Audit Checklist`

## Hard Gates

Reject or explicitly warn on these patterns:

- Assuming Unity will implicitly clean up C# events, coroutines, `CancellationTokenSource` instances, or native containers.
- Editing prefabs, scenes, Animator assets, UI hierarchy assets, or Unity YAML to solve a script-lifecycle problem.
- Renaming serialized fields, public Inspector-facing fields, `MonoBehaviour` classes, or script filenames unless the task explicitly includes migration handling.
- Claiming a bug is fixed when only static review or compilation has run.
- Inventing Inspector wiring, prefab changes, or runtime object assignments that were not verified in Unity.

If the task requires serialized wiring or asset follow-up, say so directly and end the verification summary with `Needs Unity Play Mode verification.`

## Quick Reference

| Concern | Default |
| --- | --- |
| `Awake()` | Allocate private state only |
| `OnEnable()` or `Start()` | External linking, subscriptions, listeners, and stream opening |
| `OnDisable()` | Full structural teardown with exact unsubscribe symmetry and coroutine-state reset |
| `OnDestroy()` | Native or unmanaged cleanup, CTS cancel/dispose, static-reference release, and runtime asset cleanup |
| Async lifetime | Pass `CancellationToken` through every long-lived async path |
| Static cleanup | Reset static state explicitly for scene changes and disabled domain reload |
| Test posture | Unity Test Framework lifecycle tests with frame boundaries and teardown assertions |

## Workflow

1. Inspect the script before proposing changes. Identify event sources, async entry points, coroutine owners, native allocations, static state, and serialized fields.
2. Separate internal allocation from external linking:
   - `Awake()` may allocate local collections, caches, CTS placeholders, or non-shared helper objects.
   - Cross-object lookups, manager registration, event subscription, and stream binding belong in `OnEnable()` or `Start()`.
3. Enforce subscribe and unsubscribe symmetry:
   - Every `+=` added during enable or startup must have one explicit matching `-=` in `OnDisable()`.
   - If a subscription is conditional, the unsubscription path must be conditional in the same ownership scope.
4. Enforce explicit teardown:
   - Stop owned coroutines and reset any state they mutate when disabled.
   - Cancel and dispose owned `CancellationTokenSource` instances in `OnDestroy()`.
   - Dispose owned `NativeArray`, `NativeList`, and similar containers exactly once.
   - Release static references or static event handlers explicitly when the design creates them.
5. Preserve scope:
   - Keep fixes script-level.
   - Avoid unrelated cleanup or architectural expansion unless the user explicitly asks for it.
6. Report verification honestly:
   - Compilation proves syntax and type safety only.
   - Runtime cleanup, scene transitions, domain reload behavior, and Inspector wiring still require Unity verification.

## Lifecycle Contract

| Callback | Required action | Forbidden action |
| --- | --- | --- |
| `Awake()` | Allocate private state and initialize internal invariants | Cross-object lookups, manager registration, event binding, or stream opening |
| `OnEnable()` | Link external dependencies, subscribe handlers, start listeners, and begin owned coroutines | Leaving any owned subscription untracked |
| `Start()` | Finish external linking that requires other objects to be initialized | Using `Start()` to hide missing teardown symmetry |
| `OnDisable()` | Unsubscribe every handler added during enable or startup, stop owned coroutines, and reset coroutine-owned state | Assuming `OnDestroy()` alone is enough for structural teardown |
| `OnDestroy()` | Dispose native containers, cancel and dispose CTS instances, release runtime-created assets, and clear static references owned by the script | Leaving unmanaged or cross-scene state to implicit engine cleanup |

## Async and Cancellation Rules

- Every long-lived async method must accept a `CancellationToken` parameter and pass it through awaited child operations.
- In this Unity 2021.3 repo, default to a lifecycle-owned `CancellationTokenSource` pattern rather than assuming `destroyCancellationToken` exists.
- Only mention `destroyCancellationToken` as a newer Unity option when the target runtime already supports it and the codebase uses it intentionally.
- Never start fire-and-forget async work that mutates object state after disable or destroy without a cancellation path.
- If async work can be restarted on re-enable, the code must create a fresh CTS for the new active lifetime.

Preferred ownership pattern:

```csharp
private CancellationTokenSource _lifetimeCts;

private void OnEnable()
{
    _lifetimeCts = new CancellationTokenSource();
    _ = RunLoopAsync(_lifetimeCts.Token);
}

private void OnDestroy()
{
    _lifetimeCts?.Cancel();
    _lifetimeCts?.Dispose();
    _lifetimeCts = null;
}
```

## Static State and Domain Reload Rules

- Treat static fields and static events as cross-scene memory hazards.
- If the script owns static state, require an explicit reset path for play sessions and scene transitions.
- When static cleanup matters under disabled domain reload, use `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` to reset state before the first scene loads.
- If a static event is subscribed during runtime, ensure duplicate registration cannot accumulate across repeated play sessions.

Preferred reset pattern:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    SomeStaticEvent = null;
    _sharedInstance = null;
}
```

## Coroutine Ownership Rules

- Coroutines must have a clear owner and shutdown path.
- If a coroutine mutates flags, UI text, timers, or cached references, `OnDisable()` must restore those fields to a safe idle state when the coroutine stops early.
- Prefer storing the started `Coroutine` handle when only one owned routine should run at a time.
- Do not assume a stopped or interrupted coroutine will leave the object in a clean state.

## Memory Hot Spot Audit Targets

Always scan for:

- Native containers allocated without a matching dispose path.
- `new` allocations inside `Update()`, `FixedUpdate()`, or `LateUpdate()` that can be cached or avoided.
- Repeated `GetComponent` or cross-object lookups in hot paths when a stable reference can be cached safely.
- `Awake()` logic that depends on other objects being initialized.
- Conditional event registration without a mirrored conditional unregistration path.
- Static caches that survive scene reloads without a reset hook.

## Test Requirements

Default test guidance must use Unity Test Framework lifecycle coverage:

- Use `[SetUp]` and `[TearDown]` to reset global state, static fields, and managers between tests unless the chosen UTF base type has its own required lifecycle conventions.
- Use `[UnityTest]` and explicit frame boundaries such as `yield return null;` to verify enable, disable, and destroy behavior across frames.
- Cycle components through `Enable -> Disable -> Destroy`.
- Assert that event handlers are detached, native containers are disposed, CTS instances are canceled and disposed, coroutine-owned state is reset, and static state is reinitialized when applicable.
- Do not present compile-only checks as proof of runtime cleanup.

## Required Output Details

Phase requirements:

1. `The Lifecycle Flow`
   - Show the execution order from allocation through teardown.
   - Call out where subscriptions begin and end, where coroutines start and stop, and where async cancellation occurs.
2. `The Audited/Corrected Script`
   - Return a complete, compilation-ready C# script.
   - Add brief comments only where lifecycle ownership is non-obvious.
3. `The Lifecycle Regression Test`
   - Return a complete UTF integration-style test that exercises enable, disable, destroy, frame boundaries, and cleanup assertions.
4. `Safety Audit Checklist`
   - Use a compact Markdown table with these rows:
     - Event symmetry
     - Async cancellation
     - Coroutine reset
     - Native-container disposal
     - Static or domain-reload safety
     - Verification status
     - Unity Play Mode follow-up

After the four phases, include:

- Changed files
- Behavior changed
- Risk level
- Verification performed and what it proves
- Manual Unity verification steps
- Whether Inspector or prefab assignments are required
- Assumptions, risks, and limitations

If runtime behavior remains unverified, include the exact line:

`Needs Unity Play Mode verification.`

## Reject These Patterns Unless Compatibility Work Is Explicitly Requested

- Event subscriptions in `Awake()`
- Fire-and-forget async methods without cancellation
- `OnDisable()` paths that skip `-=`
- Native containers disposed only by hope or finalization
- Coroutines that leave stale flags or references on early exit
- Static events that accumulate handlers across play sessions
- Runtime fixes that depend on editing scene or prefab YAML

If the repo already uses one of these patterns, call it out, explain the risk, and keep compatibility work scoped to the user request instead of silently normalizing it.

## Forward-Test Prompts

Use these prompt classes to check whether the skill is behaving correctly:

1. Audit an existing MonoBehaviour that subscribes in `Awake()` and never unsubscribes in `OnDisable()`.
2. Generate a memory-safe MonoBehaviour that uses async work, coroutines, and a `NativeArray<T>`.
3. Review a static-event leak that appears after scene reloads or when domain reload is disabled in the Unity Editor.

Each forward-test response should preserve the four exact phase headings, stay C# 9 and Unity 2021.3 compatible, remain script-level only, and avoid claiming runtime verification from compilation alone.

## Common Mistakes

- Moving all initialization into `Awake()` and calling it "deterministic."
- Canceling async work without disposing the CTS.
- Unsubscribing in `OnDestroy()` but leaving the object subscribed while disabled.
- Stopping a coroutine without restoring the state it was mutating.
- Disposing native containers without checking ownership or `IsCreated`.
- Forgetting that static handlers can survive repeated play sessions when domain reload is disabled.
- Returning a code snippet instead of a complete script or complete UTF test.
