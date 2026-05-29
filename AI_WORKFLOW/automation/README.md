# AI Workflow Automation

This folder documents the semi-automatic routing pipeline for Beavermania AI work.

## Roles

- ChatGPT is the router and orchestrator.
- GitHub plus `AI_WORKFLOW/` acts as the command bus and shared memory.
- Codex and Cursor execute from prepared repo files instead of ad hoc rewritten instructions.
- The user still manually launches Codex or Cursor when needed.
- Full remote execution of Cursor from ChatGPT is not assumed.
- Unity Play Mode remains the source of truth for runtime and Editor verification.

## Practical flow

1. User describes the task to ChatGPT.
2. ChatGPT fills or proposes [AI_WORKFLOW/inbox/task-intake.md](../inbox/task-intake.md).
3. ChatGPT writes the routing decision in [AI_WORKFLOW/routing/current-routing.md](../routing/current-routing.md).
4. ChatGPT prepares [AI_WORKFLOW/prompts/cursor-next.md](../prompts/cursor-next.md) and/or [AI_WORKFLOW/prompts/codex-next.md](../prompts/codex-next.md).
5. User launches the selected tool and tells it to execute the matching prompt file.
6. The tool updates a handoff file or final report with changed files, verification, risks, and next needs.
7. ChatGPT reviews the PR/output and routes the next step.

## Operating notes

- Use the routing file as the single source of truth for ownership, mode, branch, stop condition, and verification.
- Keep prompt files task-specific enough to execute without the user rewriting instructions.
- Use handoff files when ownership changes between Codex and Cursor.
- Do not treat code-level checks as Unity runtime verification.
