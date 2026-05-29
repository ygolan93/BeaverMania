# AI Workflow Layer (Codex ↔ Cursor)

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

## Ownership Model

- **Codex owns:** script correctness, architecture decisions, refactors, code review, PR summaries, and workflow/documentation maintenance.
- **Cursor owns:** Unity Editor integration (scenes, prefabs, UI wiring, Animator/animation, Inspector references), plus visual/runtime Play Mode verification.
- **User / Product Owner owns:** prioritization, final acceptance, merge approval, and conflict resolution.

## Task Lifecycle

1. Start from `AI_WORKFLOW/active-task.md` or a task template.
2. Define owner and scope boundaries before implementation.
3. Implement only within assigned ownership.
4. Record verification evidence and remaining risks.
5. End with one of:
   - Final report, or
   - Formal handoff to the other tool.

## Coordination Rules

- Do not let Codex and Cursor edit the same scene/prefab/animation/UI asset concurrently.
- For cross-boundary work (scripts + Unity Editor), lock next owner explicitly in `active-task.md`.
- Runtime Unity behavior claims require Play Mode verification.
- Script-level validation alone is not sufficient for serialized wiring outcomes.

## Routing Inbox Layer

Use the routing inbox layer when a task needs intake, ownership classification, and prepared execution prompts before work starts:

- `AI_WORKFLOW/inbox/task-intake.md` records the incoming task, evidence, constraints, and first owner guess.
- `AI_WORKFLOW/routing/current-routing.md` records ChatGPT's routing decision, execution order, mode, stop condition, and verification needs.
- `AI_WORKFLOW/prompts/cursor-next.md` is the reusable Cursor execution prompt for Cursor-owned work.
- `AI_WORKFLOW/prompts/codex-next.md` is the reusable Codex execution prompt for Codex-owned work.
- `AI_WORKFLOW/automation/README.md` explains the semi-automatic ChatGPT → repo command bus → Cursor/Codex pipeline.
