# Codex Next Prompt

## Task summary
- Read `AGENTS.md`.
- Read `AI_WORKFLOW/README.md`.
- Read `AI_WORKFLOW/active-task.md`.
- Read `AI_WORKFLOW/routing/current-routing.md`.
- Execute only the Codex-owned part of the task.

## Codex ownership
- Own script/code changes, documentation, code-level review, diff analysis, and PR-ready summaries when routed to Codex.

## Allowed scope
- Prefer minimal script/code changes.
- Preserve Unity serialized references.
- Work only inside the scope assigned in `AI_WORKFLOW/active-task.md` and `AI_WORKFLOW/routing/current-routing.md`.

## Forbidden scope
- Do not execute Cursor-owned Unity Editor work.
- Avoid scene, prefab, animation, and UI YAML edits.
- Do not rename or remove serialized fields unless migration is explicitly in scope.
- Do not claim Unity runtime behavior is fixed without Play Mode verification.

## Required files to inspect
- `AGENTS.md`
- `AI_WORKFLOW/README.md`
- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/routing/current-routing.md`
- Any task-specific files listed in the active task or routing decision.

## Implementation / review instructions
- Keep changes focused on the routed task.
- Avoid unrelated cleanup.
- State assumptions, Unity serialization risks, and manual follow-up requirements.

## Code-level verification
- Run available code-level validation if applicable.
- For C# changes, prefer `cd /workspace/BeaverMania/.ci && dotnet build BeaverMania.CI.csproj` when the environment supports it.
- State when Unity Play Mode verification is still required.

## Handoff trigger
- If code changes require Inspector, prefab, scene, UI, animation, audio, or Play Mode follow-up, update `AI_WORKFLOW/handoffs/codex-to-cursor.md`.
- Include changed files, required Unity checks, expected runtime behavior, and risk notes.

## Expected final report
- Changed files.
- Behavior changed.
- Risk level.
- Code-level verification performed.
- Unity Play Mode verification status.
- Inspector or prefab assignments required.
- Assumptions, risks, and limitations.
- Next owner and next action.
