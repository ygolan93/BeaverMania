# Cursor Next Prompt

## Task summary
- Read `AGENTS.md`.
- Read `AI_WORKFLOW/README.md`.
- Read `AI_WORKFLOW/active-task.md`.
- Read `AI_WORKFLOW/routing/current-routing.md`.
- Execute only the Cursor-owned part of the task.

## Cursor ownership
- Own Unity Editor integration, scene/prefab/UI/animation/input/audio wiring, Inspector assignments, and visual/runtime verification when explicitly routed to Cursor.

## Allowed scope
- Work only inside the scope assigned in `AI_WORKFLOW/active-task.md` and `AI_WORKFLOW/routing/current-routing.md`.
- Prefer Plan mode for Unity scene, prefab, UI, animation, input, audio, or multi-system tasks.
- Use Agent only for narrow, clearly scoped implementation.

## Forbidden scope
- Do not execute Codex-owned work.
- Do not make broad C# refactors.
- Do not edit Unity assets outside explicit scope.
- Do not claim runtime behavior is fixed without Play Mode verification.

## Required files to inspect
- `AGENTS.md`
- `AI_WORKFLOW/README.md`
- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/routing/current-routing.md`
- Any task-specific files listed in the active task or routing decision.

## Unity Editor work
- Record every scene, prefab, UI hierarchy, Animator, animation, audio, or Inspector asset touched.
- Preserve serialized references unless the active task explicitly requires a change.

## Play Mode verification
- Record Unity verification status.
- Document tested scene(s), scenario(s), expected result, actual result, and remaining gaps.
- State clearly when Play Mode was not run or was blocked.

## Handoff trigger
- If a script-level issue is discovered outside safe local scope, stop and update `AI_WORKFLOW/handoffs/cursor-to-codex.md`.
- Include reproduction details, suspected files, Unity evidence, and the exact next Codex action requested.

## Expected final report
- Changed files/assets.
- Behavior changed.
- Verification performed.
- Play Mode status.
- Inspector/prefab/scene assignments required.
- Risks and limitations.
- Next owner and next action.
