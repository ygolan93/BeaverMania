# Cursor Next Prompt

Use this file as the execution prompt for the Cursor-owned portion of the current routed task.

## Required startup reads

Before acting, read:

1. [AGENTS.md](../../AGENTS.md)
2. [AI_WORKFLOW/README.md](../README.md)
3. `AI_WORKFLOW/active-task.md`
4. [AI_WORKFLOW/routing/current-routing.md](../routing/current-routing.md)

## Task summary

- _Paste or summarize the routed task here._

## Cursor ownership

- Execute only the Cursor-owned part of the task.
- Prefer Plan mode for Unity scene, prefab, UI, animation, input, audio, or multi-system tasks.
- Use Agent only for narrow, clearly scoped implementation.
- Do not take over Codex-owned script-only work unless the routing file explicitly assigns it to Cursor.

## Allowed scope

- _List allowed files, systems, scenes, prefabs, UI panels, Inspector assignments, or verification steps._

## Forbidden scope

- Avoid broad C# refactors.
- Avoid editing Unity assets outside the explicit routed scope.
- Do not modify unrelated scenes, prefabs, animation clips, Animator assets, textures, models, audio assets, `.meta` GUIDs, or gameplay scripts.
- Do not claim runtime behavior is fixed without Play Mode verification.

## Required files to inspect

- [AI_WORKFLOW/routing/current-routing.md](../routing/current-routing.md)
- _Add task-specific files here._

## Unity Editor work

- _Describe required Editor, Inspector, prefab, scene, UI, animation, input, or audio work._
- Record exactly what was changed and which assets were touched.
- If no Editor work is required, state `Not required`.

## Play Mode verification

- _Describe required Play Mode scenario(s), expected result, and actual result._
- If Play Mode was not run, state: `Needs Unity Play Mode verification.`

## Handoff trigger

Stop and update `AI_WORKFLOW/handoffs/cursor-to-codex.md` if:

- A script-level issue is discovered outside safe local Cursor scope.
- The required fix becomes a code-only Codex task.
- The task needs independent code review before more Unity asset work.

## Expected final report

Include:

- Branch used
- Files changed
- Cursor-owned work completed
- Behavior changed
- Risk level
- Unity Editor verification status
- Play Mode verification status
- Handoff created or not created
- Inspector/prefab/scene assignments required
- Assumptions, risks, and limitations
