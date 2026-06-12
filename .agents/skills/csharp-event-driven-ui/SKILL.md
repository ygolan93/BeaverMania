---
name: csharp-event-driven-ui
description: Use this skill when replacing polling-heavy or direct UI coupling with C# source-to-presenter interaction flow, especially for shared prompts, dialogue sessions, shop panels, or other single-owner UI state in Unity.
---

# CSharp Event Driven UI

Model UI as a single owner reacting to source state.

## Rules

- Give each UI surface one owner.
- Let sources expose availability, context, and callbacks; let the presenter choose what is visible.
- Use explicit open, shop-open, shop-close, and close reasons instead of boolean state soup.
- Pass the active source or session context directly; do not re-discover it with scene-wide searches once it is known.
- Keep compatibility shims for prefab-bound public methods during the first migration pass.

## Preferred Shape

- Source interface or adapter
- Runtime session/context object
- Presenter/controller that arbitrates overlap
- Thin source callbacks for camera, cursor, or NPC-specific side effects

## Do Not Do

- Do not let buttons call back into unrelated scene panels.
- Do not duplicate interact-key handling across every NPC script when a shared presenter can own it.
- Do not hide state transitions inside `Update` branches that other systems cannot observe.
