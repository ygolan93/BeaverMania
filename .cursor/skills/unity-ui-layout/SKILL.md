---
name: unity-ui-layout
description: Improves, audits, or rebuilds Unity UI layouts - Canvas hierarchy, RectTransform anchors and pivots, responsive HUD placement, pause/settings menus, safe-area support, Canvas Scaler configuration, cursor lock behavior, and layout-rebuild performance. Use when fixing overlapping, stretched, or misanchored UI, cleaning up HUD layout, simplifying icon and counter UI, consolidating tips/objective panels, making UI readable across resolutions and aspect ratios, or reorganizing UI prefab hierarchies.
---

# Skill: unity-ui-layout

## Purpose

Use this skill when improving, auditing, or rebuilding Unity UI layouts inside the current repository.

The skill focuses on:

- Canvas hierarchy structure
- RectTransform anchors and pivots
- responsive HUD placement
- pause/menu/settings UI
- safe-area support
- readable scaling across resolutions
- reducing visual clutter
- avoiding layout rebuild / GC performance issues
- preserving existing gameplay/UI input flow

This skill is especially relevant for Beavermania-style third-person / FPS Unity gameplay where HUD clarity and game feel matter more than decorative UI complexity.

---

## Scope Recommendation

Use this as a **repo-scoped Cursor skill**.

Reason:

Unity UI layout work depends heavily on the current scene hierarchy, prefab structure, Canvas setup, input flow, pause menu setup, HUD design, and project-specific gameplay needs. This should not be treated as a fully generic Unity skill unless later converted into a broader template.

---

## When to Use

Use this skill for tasks involving:

- HUD layout cleanup
- pause menu layout
- settings menu layout
- stamina / health / resource counters
- tips/tutorial popups
- objective panels
- icon + number UI simplification
- mobile/desktop safe-area support
- Canvas Scaler configuration
- prefab UI hierarchy cleanup
- fixing overlapping, stretched, or misanchored UI
- making UI readable at:
  - 16:9
  - 16:10
  - ultrawide
  - smaller laptop resolutions

Do not use this skill for:

- gameplay systems unrelated to UI
- input-system refactors unless required for UI navigation
- audio slider logic unless the task explicitly includes settings UI wiring
- art redesign beyond layout and readability
- generated Input System C# files unless explicitly required

---

## Project Context

Assume this repository is a Unity game project.

Important current context:

- Pause UI may be controlled through a `pauseMenu` GameObject reference.
- Gameplay and UI input modes may be switched through an `InputReader`.
- Do not break existing pause/resume behavior.
- Do not edit generated Input System code unless explicitly required.
- Prefer clean integration with existing MonoBehaviours and ScriptableObject input flow.
- Cursor lock/unlock behavior must be treated carefully when menus open or close.
- UI work should improve gameplay readability, not create decorative clutter.

---

## Core Goals

When working on Unity UI layout, prioritize the following.

### 1. Player Clarity

Important gameplay information must be visible immediately.

Rules:

- Remove unnecessary labels if icons already explain the resource.
- Do not overload screen corners with text.
- Keep counters readable during movement/combat.
- Make objectives and tips noticeable without covering combat visibility.
- Keep visual hierarchy simple.

### 2. Resolution Safety

UI must hold up at common resolutions:

- 1920x1080
- 2560x1440
- 1366x768
- 3440x1440

Use:

- proper anchors
- proper pivots
- Layout Groups where appropriate
- Content Size Fitters only when safe
- safe-area handling when needed
- Canvas Scaler with stable reference resolution

### 3. Game Feel

HUD should guide the player, not distract them.

Rules:

- Tips/objectives should be short and clear.
- Permanent HUD should stay minimal.
- Menu layout should feel intentional and centered.
- Avoid huge text blocks in gameplay HUD.
- Avoid scattering tutorial messages across unrelated objects.

### 4. Maintainability

Prefer:

- small reusable prefabs
- named UI groups
- event-driven UI updates
- serialized references over scene-wide searches
- clear root separation between HUD, menus, overlays, and debug UI

Avoid:

- one giant manually positioned Canvas
- duplicate HUD widgets
- hidden legacy objects left active in the scene
- unnamed RectTransform groups
- fragile absolute positioning

### 5. Performance

Avoid:

- unnecessary LayoutGroups on constantly changing runtime elements
- frequent layout rebuilds
- `GetComponent` in `Update`
- rebuilding TMP text every frame
- deeply nested dynamic Canvases
- unnecessary `SetActive` spam on rapidly changing UI elements

Prefer:

- cached references
- event-driven value changes
- updating text only when value changes
- separate Canvas roots for frequently changing HUD elements when needed

---

## Required Workflow

Before modifying anything:

1. Inspect existing UI hierarchy:
   - Canvases
   - EventSystem
   - Canvas Scaler
   - Graphic Raycaster
   - HUD roots
   - Pause menu
   - Settings menu
   - Objective/tips panels
   - prefab variants

2. Identify layout problems:
   - bad anchors
   - fixed pixel positioning
   - overlapping text
   - inconsistent scale
   - unsafe corner placement
   - duplicated UI logic
   - too many labels
   - unclear visual hierarchy
   - broken navigation or focus
   - unnecessary layout rebuild risk

3. Propose the layout plan before large changes:
   - what moves
   - what remains
   - what becomes prefab
   - what needs script support
   - what should not be touched

4. Make the smallest safe implementation.

5. Validate in Unity across several aspect ratios.

6. Report exact changed files and scene/prefab objects.

---

## Preferred UI Architecture

Use this hierarchy pattern when applicable:

~~~text
Canvas_Main
├── HUD_Root
│   ├── TopLeft_Status
│   │   ├── HealthGroup
│   │   └── OptionalStatusGroup
│   ├── BottomLeft_Stamina
│   │   └── StaminaBar
│   ├── TopRight_Resources
│   │   ├── CoinsCounter
│   │   ├── LogsCounter
│   │   └── AmmoCounter
│   ├── Center_Objective
│   │   └── ObjectivePrompt
│   └── Tips_Root
│       └── TipCard
│
├── Menu_Root
│   ├── PauseMenu
│   ├── SettingsMenu
│   └── ConfirmationDialog
│
└── Debug_Root
    └── OptionalDebugUI
~~~

Use separate roots for:

- HUD
- menus
- overlays
- debug-only UI

Do not mix pause menu objects inside active HUD groups unless the current project architecture already requires it.

---

## Detailed Layout Reference

Detailed implementation rules live in [reference.md](reference.md). Read it when actually implementing layout changes. It covers:

- Canvas Rules (Canvas Scaler, render mode, Graphic Raycaster, EventSystem)
- RectTransform Layout Rules (anchors, pivots, safe margins)
- HUD Layout Rules (per-corner placement and counter formats)
- Tips Subsystem Rules
- Menu Layout Rules (pause, settings, confirmation dialogs)
- Performance Rules (with counter/bar update code patterns)
- Text Rules
- Icon Rules
- Layout Components Guidance (Layout Groups, Content Size Fitter, Layout Element)

---

## Input Safety

Do not break:

- `PauseEvent`
- `ResumeEvent`
- Gameplay/UI action map switching
- cursor lock/unlock behavior
- existing menu GameObject references
- existing serialized references in scene objects

If UI navigation requires changes:

- inspect current `InputReader` flow first
- avoid editing generated Input System C# files
- prefer updating `.inputactions` or wrapper scripts only if needed
- explain why the change is required
- test pause/resume after changes

Generated Input System C# files should be treated as generated artifacts. Do not manually edit them unless the user explicitly asks.

---

## Cursor Lock Rules

When menus open:

- unlock cursor if the menu requires mouse interaction
- show cursor if the menu requires mouse interaction

When menus close:

- restore gameplay cursor lock state
- hide cursor if gameplay expects it
- avoid leaving cursor visible during gameplay unless intentionally required

Preferred behavior:

~~~text
Gameplay:
Cursor.visible = false
Cursor.lockState = Locked

Menu:
Cursor.visible = true
Cursor.lockState = None
~~~

Only apply this if it matches the existing project design.

---

## Responsive Testing Checklist

Test UI at:

~~~text
1920x1080
2560x1440
1366x768
3440x1440
~~~

Check:

- HUD does not overlap
- top-right counters stay inside screen
- bottom-left stamina does not clip
- health remains readable
- objective prompt remains centered
- pause menu remains centered
- settings rows do not overflow
- text is not clipped
- icons remain readable
- no layout groups collapse unexpectedly

---

## Visual Quality Checklist

Before completing the task, verify:

- [ ] HUD is readable at 1080p
- [ ] HUD does not overlap at 1366x768
- [ ] top-right counters are icon + number only when applicable
- [ ] stamina is separated from health if requested
- [ ] pause menu remains centered
- [ ] settings menu rows are aligned
- [ ] no text is clipped
- [ ] no UI element depends on fragile absolute positioning
- [ ] Canvas Scaler is configured correctly
- [ ] EventSystem still exists
- [ ] pause/resume input still works
- [ ] cursor behavior is preserved
- [ ] changed prefabs/scenes are saved
- [ ] no generated Input System C# file was manually edited

---

## Beavermania-Specific UI Direction

For Beavermania, bias toward:

- cartoon-readable UI
- chunky but clean icons
- minimal HUD text
- fast gameplay readability
- objective clarity
- reduced clutter
- strong separation between health, stamina, resources, and tips

Recommended layout direction:

~~~text
Top Left:
- Health / lives only

Bottom Left:
- Stamina / sprint bar

Top Right:
- Resource counters as icon + number only

Top Center / Center:
- temporary objective updates
- interaction prompts

Menu Center:
- pause/settings/restart/exit panels

Tips:
- consolidated tip card system
~~~

---

## Anti-Patterns to Fix

Actively look for and fix these:

- resource counters with redundant text labels
- stamina and health crammed into the same corner
- tutorial text permanently occupying gameplay space
- pause menu positioned by raw pixel offsets
- UI built only for one resolution
- huge HUD labels that explain obvious icons
- inactive duplicate UI objects left in scene
- layout groups fighting manual RectTransform sizes
- multiple EventSystems
- generated Input System code edited manually
- menu opens but cursor remains locked
- menu closes but cursor remains visible during gameplay
- settings sliders that visually move but do not affect anything

---

## Safe Implementation Strategy

When asked to improve layout:

1. Start with a UI audit.
2. Identify the most damaging layout issues.
3. Fix hierarchy and anchoring first.
4. Simplify HUD text.
5. Move stamina if needed.
6. Consolidate tips if needed.
7. Validate menu behavior.
8. Validate input/cursor behavior.
9. Report remaining manual Unity checks.

Do not blindly redesign the entire UI unless explicitly requested.

The default approach is:

~~~text
Audit → small structural cleanup → anchor/pivot correction → clutter reduction → validation
~~~

---

## Expected Output Format

When finished, respond with:

~~~text
Summary:
- What was changed

Files/Objects Changed:
- Scene/prefab/script paths

Layout Decisions:
- Why each major UI element was moved or restructured

Validation:
- Resolutions/aspect ratios checked
- Pause/menu/input behavior checked

Risks / Follow-ups:
- Anything still requiring manual Unity Editor validation
~~~

---

## Default Behavior

If the task is vague, perform a layout audit first.

Do not blindly redesign the UI.

First determine what is broken:

- clutter
- readability
- anchoring
- scaling
- hierarchy
- interaction
- runtime performance

Then apply the smallest clean fix that improves player experience.
