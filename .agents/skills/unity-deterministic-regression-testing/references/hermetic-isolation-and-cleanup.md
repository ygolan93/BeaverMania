# Hermetic Isolation And Cleanup

## Fixture Setup

Every test must build its own world:

- instantiate its own `GameObject` graph
- inject its own fakes or stubs
- seed any data locally inside the test or fixture
- avoid hidden dependencies on scene state, singleton carryover, or manual Inspector setup

Prefer local builders over shared mutable fixtures when the shared fixture can leak state between tests.

## Owned Object Registries

Track every object the test creates:

- `List<GameObject>` for runtime objects
- `List<ScriptableObject>` for temporary assets
- explicit fields for event buses, fake clocks, or temporary managers

Destroy in reverse ownership order when the dependency graph requires it.

Cleanup defaults:

- EditMode: `Object.DestroyImmediate(...)`
- PlayMode: `Object.Destroy(...)` and yield a frame before assertions that depend on Unity callbacks

## Static And Global Reset

Reset every touched global:

- static fields
- static events
- `Time.timeScale`
- custom service locators
- cached random seeds or deterministic generators
- global log or event hooks

If the production code owns static state, ask for a minimal reset hook instead of allowing tests to rely on undefined editor carryover.

## Mock And Fake Patterns

Prefer the narrowest seam:

- plain C# fake classes for domain logic
- interface adapters for Unity-only services
- deterministic clocks or counters instead of real time
- scripted fake callbacks instead of scene-driven integration

Avoid heavyweight mocks when a hand-written fake communicates the behavior more clearly.

## Cleanup Ordering

Use setup and teardown symmetry:

1. unregister event handlers
2. stop coroutines or async drivers owned by the test
3. destroy runtime objects
4. restore global or static state
5. clear registries and references

If a base fixture owns lifecycle work, use its documented override path and call the base method in the correct order.
