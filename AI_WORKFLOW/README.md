# AI Workflow Layer (Codex <-> Cursor)

## Purpose

- Provide a file-based operating protocol for mixed AI delivery in this repository.
- Make task ownership, handoffs, verification status, and merge readiness explicit.
- Reduce regressions from Unity serialized-reference fragility by preventing ambiguous ownership.

## Communication Model

- Codex and Cursor do **not** chat directly.
- They communicate through committed repository files in `AI_WORKFLOW/`.
- Use:
  - `AI_WORKFLOW/active-task.md`
  - `AI_WORKFLOW/handoffs/cursor-to-codex.md`
  - `AI_WORKFLOW/handoffs/codex-to-cursor.md`
  - `AI_WORKFLOW/task-templates/*`

## Role Model

- **Codex** is the senior architect and codebase guardian.
- **Cursor** is the hands-on implementation engineer.
- **User / Product Owner** owns prioritization, final acceptance, merge approval, and conflict resolution.

## Ownership Model

- **Codex owns:** architecture decisions, system boundaries, refactor strategy, code review, PR summaries, and risk control.
- **Cursor owns:** focused implementation, local editing, Unity Editor integration (scenes, prefabs, UI wiring, Animator/animation, Inspector references), and Play Mode / visual verification.
- **Shared / mixed work:** any task that crosses code plus Unity Editor boundaries, or requires Cursor execution after Codex review/design.

## Ownership classes

Classify each task as exactly one:

- `Cursor-owned`
- `Codex-owned`
- `Mixed`
- `User decision required`

If a task stops fitting its current classification, record the handoff instead of improvising a larger rewrite.

## Task Lifecycle

1. Start from `AI_WORKFLOW/active-task.md` or a task template.
2. Define owner, exact scope, protected areas, and required validation before implementation.
3. Inspect the relevant files/assets before editing.
4. Implement only within assigned ownership.
5. Record verification evidence, remaining risks, and any required manual Unity follow-up.
6. End with one of:
   - Final report
   - Formal handoff to the other tool

## Coordination Rules

- Do not let Codex and Cursor edit the same scene/prefab/animation/UI asset concurrently.
- For cross-boundary work (scripts + Unity Editor), lock next owner explicitly in `active-task.md`.
- Runtime Unity behavior claims require Play Mode verification.
- Script-level validation alone is not sufficient for serialized wiring outcomes.

## Cursor report standard

Cursor reports should include:

- `Summary`
- `Files Changed`
- `Validation`
- `Notes / Risks`

The report must separate:

- verified by command/test
- verified manually in Unity
- still unverified / risky
