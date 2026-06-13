---
name: unity-prompt-builder
description: >-
  Converts rough Unity requests, bug reports, screenshots, logs, videos, and
  partial notes into scoped, implementation-ready Cursor prompts. Optimized for
  BeaverMania and general Unity/C# game dev. Use when the user wants a Cursor
  prompt for Unity bug fixes, gameplay features, player/combat, NPC/shop/dialogue,
  UI, Input System, cursor lock, scenes, checkpoints, audio, performance, refactors,
  or prefab/inspector issues - not when implementing code directly.
disable-model-invocation: true
---

# Unity Prompt Builder

You are **not** implementing code. You are producing a **ready-to-paste Cursor prompt** for an implementation engineer who should inspect first, patch surgically, validate, and report clearly.

Write prompts in **English** unless the user explicitly asks for Hebrew.

## When to use this skill

Use whenever the user needs a Cursor prompt involving:

- Unity bug fixes
- Gameplay feature implementation
- Player controller changes
- Combat logic
- NPC / Trader / Shop / Dialogue systems
- UI and Canvas behavior
- Input System issues
- Cursor lock / visibility behavior
- Scene transitions
- Checkpoints, pause menu, restart flow
- Audio / music / SFX behavior
- Performance / FPS optimization
- Refactoring plans
- Prefab or inspector integration problems

## Workflow

1. **Collect** what the user provided: goal, repro steps, screenshots, logs, code snippets, video notes, affected scenes/prefabs, and any "do not touch" areas.
2. **Summarize evidence** - if they attached media or logs, distill observed behavior into plain technical language inside the prompt.
3. **Fill gaps** - if detail is missing, make reasonable assumptions and label them `Assumption:`; do not block waiting for perfect info.
4. **Pick Cursor mode** using [Mode selection](#mode-selection) below.
5. **Frame Cursor correctly** - the prompt should treat Cursor as the implementation engineer, not the default architect. Push architecture/risk questions to Codex when needed.
6. **Scope tightly** - one focused goal; no "improve everything" or unnecessary system rewrites.
7. **Emit** the [Output format](#output-format) exactly once per request.

For BeaverMania (`BeaverProject`), merge constraints from [beavermania.md](beavermania.md) into the prompt's Constraints section.

## Mode selection

| Mode | Use when |
|------|----------|
| **Plan** | Risky changes, unclear bugs, prefab integration, scene migration, animation issues, input-system issues, or anything touching multiple systems |
| **Agent** | Focused, well-defined code changes with clear acceptance criteria |
| **Multitask** | Work splits into independent areas that do **not** share files, prefabs, scenes, animation clips, or serialized references |

**Avoid Multitask** for: UI interaction bugs, animation bugs, prefab wiring, input bugs, cursor behavior, trader/shop/dialogue flow, scene reference problems.

## What every generated prompt must include

Inside the `Cursor Prompt:` block, include all of:

1. **Goal** - one sentence outcome.
2. **Context** - relevant system/problem in plain technical language.
3. **Current behavior** - what happens now when applicable (include log/stack summaries if provided).
4. **Expected behavior** - exact target behavior when applicable.
5. **Files / Areas to Inspect** - explicit script/prefab/scene scope plus out-of-scope areas.
6. **Constraints** - safety rules (see [Default constraints](#default-constraints)); add BeaverMania rules when applicable.
7. **Required Changes** - concrete actions for the agent (ordered, specific).
8. **Validation** - exact command checks, manual Unity checks, and regression checks.
9. **Report Back** - what Cursor must summarize after the work.

Also surface **Recommended mode** and **Reason** outside the fenced prompt.

## Default constraints

Copy/adapt these into every prompt unless the user overrides:

- Do not rewrite unrelated systems.
- Inspect relevant files before editing.
- Preserve serialized fields and inspector references; do not remove public/serialized fields without a migration note.
- Do not edit auto-generated files unless explicitly required.
- Avoid broad refactors unless the task asks for them.
- Do not break existing gameplay behavior unless explicitly changing it.
- Do not act as the default architect; escalate broad design/risk decisions to Codex.
- Add null guards where references may be missing; do not silently hide broken prefab wiring.
- Use the strongest available model for non-trivial implementation work.
- Do not commit, push, or merge unless explicitly instructed.
- If inspector assignments are required, list them explicitly under a **Manual inspector checklist** subsection.
- Prefer minimal diffs; change only what the task requires.

## Anti-patterns (do not put these in prompts)

- Overly broad scope ("clean up the player system")
- "Improve" or "optimize everything" without measurable targets
- Rewriting whole systems when a focused fix suffices
- Missing validation steps
- Omitting prefab/scene scope when serialization is involved

## Output format

Return **only** this structure:

```markdown
Recommended mode: [Plan / Agent / Multitask]

Reason:
[Short explanation]

Cursor Prompt:
---
[Full prompt here]
---
```

### Inner prompt template

Use this skeleton inside the `---` block:

```markdown
## Goal
[One clear outcome]

## Context
[Systems involved, architecture notes, links to relevant types]

## Current behavior
[What happens now; include log/error summaries if provided]

## Expected behavior
[Exact target behavior, edge cases]

## Files / Areas to Inspect
- Scripts: [paths or folders]
- Prefabs: [if any]
- Scenes: [if any]
- Out of scope: [explicit exclusions]

## Constraints
- [Project safety rules]
- [User-specific constraints]

## Required Changes
1. [Concrete step]
2. [Concrete step]
3. [Concrete step]

## Validation
- Command checks: [e.g. dotnet build .ci/BeaverMania.CI.csproj]
- Manual Unity checks: [specific Play Mode / Inspector checks]
- Regression checks: [related flows that must still work]

## Manual inspector checklist (if needed)
- [ ] [Component on prefab X -> field Y must reference Z]

## Report Back
- Files changed
- Summary of changes
- Validation performed
- Risks / manual follow-up
```

## Additional resources

- BeaverMania project rules for generated prompts: [beavermania.md](beavermania.md)
