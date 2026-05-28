# BeaverMania prompt constraints

When the workspace is **BeaverProject** / BeaverMania, include these in the prompt **Constraints** section (adjust if the user narrows scope).

## Scope

- Default code edits: `Assets/Scripts/**` only.
- Scenes/prefabs: only when the task is wiring/serialization or the user explicitly authorizes.
- Never modify: `Assets/External Packages/**`, `Assets/Eden/**` sample/demo scripts, vendor code.

## Architecture (reuse, do not duplicate)

| System | Rule |
|--------|------|
| **Input** | `Beavermania.Core.Input.PlayerInputReader` — no scattered `Input.GetKey` in feature code |
| **Pause / time** | `GameTimeScaleGate.SetFreeze(token, true/false)` — never set `Time.timeScale` directly for pause/lose |
| **Player** | Prefer `Beavermania.Player.BeaverPlayerBehaviour` and existing combat/HUD APIs |
| **Data** | Tunables in `ScriptableObject` under `Assets/Scripts/Data/` with `Beavermania/<Category>/<Name>` menu |
| **Namespaces** | `Beavermania.<Area>` — Core, Data, Player, NPC, Objects, UI, Audio, Display |

## Style

- Small `MonoBehaviour` components; avoid growing god scripts.
- Match surrounding files; explicit `using`; `[SerializeField]` for inspector wiring.
- Meaningful null checks; `Debug.LogWarning(..., this)` when wiring missing; no silent empty `catch`.
- Ask before splitting `BeaverPlayerBehaviour` or cross-cutting input/time-scale changes.

## Migration

- Check `MIGRATION.md` before moving/renaming scripts.
- Do not hand-edit `.meta` GUIDs.

## Mode hints for BeaverMania

- **Plan**: prefab/scene YAML, animation parameters, Input Actions assets, trader/shop/dialogue flow, multi-system bugs.
- **Agent**: single-script fix with known file and clear repro.
- **Multitask**: rare — only when areas are file/prefab disjoint.

## Cross-agent workflow prompts

When a prompt involves multi-step feature work, bugfixes crossing code + Unity scene/prefab/UI/animation boundaries, Codex/Cursor cooperation, or PR/review/merge readiness, include:

- `AI_WORKFLOW/README.md` as protocol context.
- `AI_WORKFLOW/active-task.md` update requirement (yes/no + owner).
- Whether a handoff file must be created:
  - `AI_WORKFLOW/handoffs/cursor-to-codex.md`
  - `AI_WORKFLOW/handoffs/codex-to-cursor.md`
- Which tool owns the next step (Codex, Cursor, or User).
