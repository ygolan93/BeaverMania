# Codex to Cursor Handoff

## Task

- Shared NPC prompt presenter and dialogue-session refactor for trader, legacy NPC dialogue, and boss dialogue flows

## Codex scope completed

- Added repo-local embedded skills:
  - `.agents/skills/unity-ui-controller/SKILL.md`
  - `.agents/skills/csharp-event-driven-ui/SKILL.md`
  - `.agents/skills/safe-refactor/SKILL.md`
- Added `INpcDialogueInteractionSource` and `NpcDialogueSessionContext` so NPCs report availability and session payloads to a shared presenter.
- Reworked `NpcDialoguePresenter` into the single runtime owner for prompt selection, interact handling, dialogue binding, shop swapping, and cleanup.
- Updated `Dialogue` so shared buttons route through the presenter and legacy `Trader` lookup is only a fallback path.
- Migrated `Trader` to the shared presenter contract while preserving public/serialized compatibility shims such as `DialoguePanel`, `Shop`, and `activateSkip()`.
- Converted `ActivateDialogue` into a legacy-content adapter that no longer toggles scene UI directly.
- Added `BossDialogueInteractionSource` and narrowed `BossHandler` so boss dialogue enters the same shared presenter flow before combat starts.
- Narrowed trader camera/presentation re-entry in `BeaverPlayerBehaviour` so the presenter path is authoritative.

## Files changed by Codex

- `AI_WORKFLOW/active-task.md`
- `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- `.agents/skills/unity-ui-controller/SKILL.md`
- `.agents/skills/csharp-event-driven-ui/SKILL.md`
- `.agents/skills/safe-refactor/SKILL.md`
- `Assets/Scripts/UI/INpcDialogueInteractionSource.cs`
- `Assets/Scripts/UI/NpcDialogueSessionContext.cs`
- `Assets/Scripts/UI/NpcDialoguePresenter.cs`
- `Assets/Scripts/UI/Dialogue.cs`
- `Assets/Scripts/NPC/Beaver/Trader.cs`
- `Assets/Scripts/UI/ActivateDialogue.cs`
- `Assets/Scripts/Player/BossDialogueInteractionSource.cs`
- `Assets/Scripts/Player/BossHandler.cs`
- `Assets/Scripts/Player/BeaverPlayerBehaviour.cs`

## New or changed serialized fields

- `NpcDialoguePresenter`
  - `dialogue`
  - `promptRoot`
  - `promptText`
  - `dialogueRoot`
- `Dialogue`
  - `ShopButton`
- `Trader`
  - existing fields preserved
  - additive `dialogueData`
  - additive `interactionPromptMessage`
- `ActivateDialogue`
  - additive `interactionPromptMessage`
  - additive `interactionRange`
- `BossDialogueInteractionSource`
  - `bossHandler`
  - `legacyBossPanel`
  - `promptMessage`

No serialized or public fields were removed in this pass.

## Unity wiring Cursor owns

- `PlayerCanvas.prefab`
  - Add a dedicated prompt widget under the HUD.
  - Assign the prompt widget root to `NpcDialoguePresenter.promptRoot`.
  - Assign the prompt text label to `NpcDialoguePresenter.promptText`.
  - Keep `NPC_DialoguePanel` as the shared session root.
  - Inside `NPC_DialoguePanel`, keep dialogue content and shop content as sibling branches so closing shop returns to the same shared session path.
- Shared dialogue buttons
  - `Continue` -> shared `Dialogue.Continue`
  - `Skip` -> shared `Dialogue.EndConversation` or the existing shared close path
  - `Shop` -> shared `Dialogue.OpenShop`
  - shop close button -> shared `Dialogue.CloseShop`
  - Remove per-NPC panel/button routing where the shared panel is now authoritative.
- NPC prefab and scene references
  - Trader-family NPCs should point `DialoguePanel` and optional `Shop` references at shared `PlayerCanvas` children, not scene-owned inline bars.
  - Legacy non-shop NPCs using `ActivateDialogue` can keep their old dialogue object only as a content source until fully migrated.
  - Boss flow should keep the old boss dialogue content as source data only; do not leave direct panel-open behavior active in parallel.

## Expected runtime flow after wiring

1. Source enters range and registers with `NpcDialoguePresenter`.
2. Presenter selects the nearest available source and shows a single prompt.
3. Interact input is consumed centrally by the presenter.
4. Presenter binds a `NpcDialogueSessionContext` into the shared `Dialogue`.
5. Source callback applies NPC-specific camera/cursor presentation.
6. Shared buttons drive continue, shop open, shop close, and end/skip through the presenter only.
7. Leaving range, disabling the source, skip, dialogue completion, or external close all go through presenter cleanup and source callbacks.

## What Cursor should test in Play Mode

- Market trader:
  - one prompt only
  - interact opens shared panel
  - shop opens and closes without switching back to a legacy inline panel
  - skip restores cursor and camera cleanly
- Arms dealer:
  - same shared prompt path
  - correct dialogue content
  - correct shop content
- One legacy non-shop NPC using `ActivateDialogue`:
  - shared prompt
  - shared panel opens with legacy lines
  - no shop button shown
- Boss dialogue:
  - shared prompt/session opens
  - skip or completion still transitions into boss combat correctly
- Overlap case:
  - two NPC sources in range still show exactly one prompt, with nearest source winning
- External close case:
  - guard disable or `DoorShut` path closes the session cleanly with no orphaned UI
- Fallback case:
  - missing prompt widget still uses objective override
  - missing dialogue data logs once and fails closed with no null refs

## Verification completed by Codex

- `git diff --check` passed aside from line-ending normalization warnings from Git.
- Manual code-path review completed for presenter registration, session binding, shop swap, compatibility shims, and first-open shared dialogue initialization.

## Verification blocked in this environment

- `dotnet build .ci/BeaverMania.CI.csproj` is blocked on this Windows machine because the Unity-managed assembly references expected by the CI project are not available here.
- No Unity Editor or Play Mode proof was performed by Codex.

## Constraints Cursor should preserve

- Do not remove legacy panels or serialized fields in the same pass as wiring.
- Do not rename `MonoBehaviour` classes or move prefab-bound scripts.
- Keep `activateSkip()` and other legacy button-entry shims alive until each migrated NPC family is verified in Play Mode.
- Keep Cursor as owner of prefab, scene, button, and serialized-reference changes.

## Known limitations

- Prompt and shared panel behavior are code-complete but not Unity-verified yet.
- Legacy traders and NPCs still depend on prefab/scene references being repointed to the shared `PlayerCanvas` structure.
- Needs Unity Play Mode verification.
