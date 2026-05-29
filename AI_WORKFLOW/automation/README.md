# AI Workflow Automation

This repository uses a semi-automatic routing pipeline:

- ChatGPT is the router/orchestrator.
- GitHub plus `AI_WORKFLOW/` acts as the command bus and shared memory.
- Codex and Cursor execute from prepared repo files instead of reinterpreting the original task.
- The user still manually launches Codex or Cursor when needed, but does not need to rethink or rewrite the task.
- Full remote execution of Cursor from ChatGPT is not assumed.
- Unity Play Mode remains the source of truth for runtime and editor verification.

## Practical flow

1. User describes task to ChatGPT.
2. ChatGPT fills or proposes `AI_WORKFLOW/inbox/task-intake.md`.
3. ChatGPT writes `AI_WORKFLOW/routing/current-routing.md`.
4. ChatGPT prepares `AI_WORKFLOW/prompts/cursor-next.md` and/or `AI_WORKFLOW/prompts/codex-next.md`.
5. User launches the selected tool and tells it to execute the matching prompt file.
6. Tool updates the handoff file or final report.
7. ChatGPT reviews the PR/output and routes the next step.
