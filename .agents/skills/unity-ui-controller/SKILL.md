---
name: unity-ui-controller
description: Use this skill for Beavermania Unity UI presenter/controller work where Cursor owns prefab, scene, and hierarchy wiring while Codex owns C# flow, serialized-safe bindings, and null-safe runtime behavior.
---

# Unity UI Controller

Keep UI visibility and session state owned by a single presenter or controller.

## Rules

- Let NPC, gameplay, or trigger scripts report state to the presenter instead of toggling random UI panels directly.
- Preserve serialized fields and public entry points unless there is a scoped migration plan.
- Prefer additive runtime bindings and null-safe fallbacks over risky prefab or scene edits.
- Treat `PlayerCanvas`, dialogue panels, trader/shop UI, and button wiring as Cursor-owned integration surfaces.
- When a prompt widget or panel reference is missing, fail closed and log once instead of guessing another panel.

## Codex Deliverables

- Define the controller-facing interface or event flow.
- Keep visibility rules in one place.
- Document exact prefab or Inspector wiring Cursor must apply.
- Write a Codex-to-Cursor handoff whenever script changes require UI hierarchy follow-up or Play Mode proof.

## Do Not Do

- Do not edit prefab YAML casually.
- Do not scatter `SetActive` calls across gameplay scripts.
- Do not let multiple controllers compete for the same prompt or panel.
