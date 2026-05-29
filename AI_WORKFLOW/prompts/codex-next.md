# Codex Next Prompt

Use this file as the execution prompt for the Codex-owned portion of the current routed task.

## Required startup reads

Before acting, read:

1. [AGENTS.md](../../AGENTS.md)
2. [AI_WORKFLOW/README.md](../README.md)
3. `AI_WORKFLOW/active-task.md`
4. [AI_WORKFLOW/routing/current-routing.md](../routing/current-routing.md)

## Task summary

- _Paste or summarize the routed task here._

## Codex ownership

- Execute only the Codex-owned part of the task.
- Prefer minimal script/code changes.
- Preserve Unity serialized references.
- Do not take over Cursor-owned scene, prefab, UI, animation, Inspector, or Play Mode work unless the routing file explicitly assigns it to Codex review only.

## Allowed scope

- _List allowed files, systems, or review targets._

## Forbidden scope

- Avoid scene, prefab, animation, Animator, UI YAML, texture, model, audio, `.meta` GUID, or unrelated asset edits.
- Avoid broad refactors and unrelated cleanup.
- Do not rename or remove serialized fields unless the routing file explicitly covers migration and Unity follow-up.
- Do not claim runtime behavior is fixed without Unity Play Mode verification.

## Required files to inspect

- [AI_WORKFLOW/routing/current-routing.md](../routing/current-routing.md)
- _Add task-specific files here._

## Implementation / review instructions

- Make the smallest safe code or documentation change that satisfies the routed task.
- Keep changes compatible with Unity 2021.3 and C# 9 when code changes are in scope.
- Preserve existing gameplay feel unless the routed task explicitly changes it.
- If reviewing only, do not edit files unless the routing file allows implementation.

## Code-level verification

- Run available code-level validation when applicable.
- For C# changes, prefer:

```bash
dotnet build .ci/BeaverMania.CI.csproj
```

- For Markdown-only changes, confirm the diff is documentation-only and links are valid.

## Handoff trigger

Stop and update `AI_WORKFLOW/handoffs/codex-to-cursor.md` if:

- Code changes require Inspector, prefab, scene, UI, animation, input, audio, or Play Mode follow-up.
- A serialized reference or Unity asset risk must be resolved in the Editor.
- Runtime behavior cannot be verified without Cursor + Unity Editor.

## Expected final report

Include:

- Branch used
- Files changed
- Codex-owned work completed
- Behavior changed
- Risk level
- Code-level verification performed and what it proves
- Unity Play Mode verification required or not required
- Handoff created or not created
- Inspector/prefab/scene assignments required
- Assumptions, risks, and limitations
