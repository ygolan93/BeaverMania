# Active Task

## Task title

- Shared NPC Prompt Presenter + repo-local skills

## Branch

- `fix/npc-dialogue-ui-alignment`

## Ownership

- **Codex**: C# presenter flow, source interfaces/adapters, compatibility shims, workflow notes
- **Cursor + Unity Editor**: `PlayerCanvas` hierarchy, `NPC_DialoguePanel` child structure, prefab/scene assignments, button wiring, Play Mode proof

## Current phase

- Code implementation is in place.
- Unity hierarchy wiring and Play Mode verification are still pending in Cursor.

## Code changes made by Codex

| File | Change |
| --- | --- |
| `.agents/skills/unity-ui-controller/SKILL.md` | Repo-local skill for shared Unity presenter/controller work where Cursor owns hierarchy and Codex owns code flow |
| `.agents/skills/csharp-event-driven-ui/SKILL.md` | Repo-local skill for source-to-presenter interaction flow and single-owner UI state |
| `.agents/skills/safe-refactor/SKILL.md` | Repo-local skill for serialized-safe refactors with compatibility shims |
| `Assets/Scripts/UI/INpcDialogueInteractionSource.cs` | New interaction-source contract plus explicit session close reasons |
| `Assets/Scripts/UI/NpcDialogueSessionContext.cs` | New runtime session payload carrying `TraderDialogueData` or legacy fallback dialogue content |
| `Assets/Scripts/UI/NpcDialoguePresenter.cs` | Reworked into the single owner for shared prompt, dialogue, and shop session state on `PlayerCanvas` |
| `Assets/Scripts/UI/Dialogue.cs` | Added runtime session binding, presenter callbacks for open/close/shop, and shared `ShopButton` handling |
| `Assets/Scripts/NPC/Beaver/Trader.cs` | Converted trader flow to `INpcDialogueInteractionSource` while preserving legacy serialized fields and button-entry shims |
| `Assets/Scripts/UI/ActivateDialogue.cs` | Converted legacy distance-based dialogue toggler into a non-shop interaction-source adapter |
| `Assets/Scripts/Player/BossDialogueInteractionSource.cs` | New boss adapter so boss dialogue uses the shared presenter contract |
| `Assets/Scripts/Player/BossHandler.cs` | Narrow boss chat flow to use the shared source/presenter path before combat starts |
| `Assets/Scripts/Player/BeaverPlayerBehaviour.cs` | Removed trigger-time trader presentation re-entry so presenter-driven sessions are the source of truth |
| `AI_WORKFLOW/active-task.md` | Updated mixed-ownership status and Unity follow-up requirements |
| `AI_WORKFLOW/handoffs/codex-to-cursor.md` | Added explicit Codex-to-Cursor handoff for hierarchy wiring and Play Mode proof |

## Event flow now implemented in code

1. A dialogue-capable source (`Trader`, `ActivateDialogue`, boss adapter) reports availability to `NpcDialoguePresenter`.
2. The presenter keeps a registered source list, chooses the nearest available source, and shows exactly one prompt.
3. If a dedicated prompt widget is not wired, the presenter falls back to `PlayerHudState.ObjectiveTextOverride`.
4. `PlayerInputReader.WasWorldInteractPressed()` is consumed centrally by the presenter.
5. On interact, the presenter requests a `NpcDialogueSessionContext`, binds it into the shared `Dialogue`, shows the shared dialogue root, and starts the session.
6. Source callbacks own NPC-specific side effects such as trader camera/cursor presentation or boss pre-fight setup.
7. `Dialogue` routes shop open, shop close, and conversation end back through the presenter instead of directly driving scene panels.
8. The presenter swaps between shared dialogue content and the source's shop content without allowing overlapping prompts or multiple active sessions.
9. Leaving range, disabling the source, dialogue completion, skip, or external close all route through presenter cleanup plus a source close callback.

## Cursor follow-up required in Unity

- Add a dedicated prompt widget under `PlayerCanvas` and assign it to `NpcDialoguePresenter.promptRoot` and `NpcDialoguePresenter.promptText`.
- Keep `NPC_DialoguePanel` as the shared session root and ensure dialogue content plus shop content are sibling branches so shop close returns to the same session without restarting from a legacy panel.
- Wire shared `Continue`, `Skip`, `Shop`, and shop-close buttons to the shared `Dialogue` and presenter path only.
- Repoint trader and NPC prefab/scene references so content/data feeds the shared presenter instead of each NPC toggling a scene-owned panel.
- Keep legacy panels under `Dialogues` inactive until each NPC family is migrated and verified; do not delete them in the same pass.

## Verification status

- `git diff --check` passed. Git reported line-ending normalization warnings only.
- `dotnet build .ci/BeaverMania.CI.csproj` could not complete in this Windows environment because the Unity-managed assembly references expected by the CI project are not present here.
- Code path review completed for the new presenter flow and the first-open shared dialogue lifecycle path was patched in `Dialogue.cs`.

## Pending Play Mode checklist

- Market trader prompt, dialogue, shop open/close, and skip path
- Arms dealer prompt, dialogue, and correct shop content
- One legacy non-shop NPC through `ActivateDialogue`
- Boss prompt, dialogue, skip-to-combat path
- Overlap case with two prompt sources competing
- External close path such as `DoorShut`/guard disable
- Prompt-widget-missing fallback through objective override
- No legacy panel flicker and no `NullReferenceException`

## Risks / limitations

- `PlayerCanvas.prefab`, shared dialogue buttons, and per-NPC prefab assignments remain high-risk serialized follow-up and were intentionally left to Cursor.
- Shared prompt selection and source arbitration are code-complete, but final correctness depends on Unity wiring to the shared `PlayerCanvas` hierarchy.
- Needs Unity Play Mode verification.
