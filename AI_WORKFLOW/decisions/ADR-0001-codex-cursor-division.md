# ADR-0001: Codex/Cursor Cooperation via Repository Files

- Status: Accepted

## Context

Beavermania is a Unity project with fragile serialized references and high coupling between script behavior and Unity Editor wiring (prefabs/scenes/UI/Animator/Inspector). Prior guidance defines tool strengths, but lacked a strict operational protocol for cross-agent lifecycle ownership and handoffs.

## Decision

Codex and Cursor cooperate through repository files under `AI_WORKFLOW/` instead of direct freeform conversation.

## Consequences

- Task state becomes auditable in git history.
- Ownership boundaries are explicit before changes start.
- Cross-boundary tasks require formal handoffs.
- Merge readiness depends on both code-level and Unity verification records.

## Codex responsibilities

- Script correctness, architecture, focused refactors, code reviews, risk notes, PR summaries.
- Maintain/update workflow documents and handoff artifacts when Cursor follow-up is required.
- Avoid unverified Unity runtime claims; request Play Mode confirmation when needed.

## Cursor responsibilities

- Unity Editor integration: prefab/scene/UI/Animator/Inspector wiring.
- Play Mode verification and visual/system behavior validation.
- Generate Cursor→Codex handoffs when Unity investigation indicates script-level defects.

## User responsibilities

- Define priorities and acceptance criteria.
- Approve ownership strategy and risk tradeoffs.
- Provide final merge approval.

## Explicit anti-patterns

- Both tools editing the same Unity asset concurrently.
- Codex claiming Unity behavior is fixed without Play Mode verification.
- Cursor performing broad C# refactors without Codex review.
- Large multi-system changes without `AI_WORKFLOW/active-task.md`.
- Unclear ownership for next action.

## Why this matters for Beavermania

Serialized references can silently break when ownership is ambiguous or when script/editor changes are not coordinated. A file-backed workflow enforces traceable handoffs and verification checkpoints, reducing regression risk for Steam release stability.
