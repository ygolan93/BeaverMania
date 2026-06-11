---
name: responsive-canvas-validation
description: Validates Unity Canvas/UI layouts across resolutions, aspect ratios, Canvas Scaler settings, safe-area constraints, and runtime UI states - detecting overlap, clipped text, broken anchors, EventSystem issues, raycast blockers, and fragile fixed-pixel positioning. Use when validating UI responsiveness, testing HUD layout at multiple resolutions or aspect ratios, verifying pause/settings menus after layout changes, checking Canvas Scaler or anchor setups, or validating UI before a PR or merge. Also use after HUD movement, counter, stamina/health positioning, menu, or Canvas hierarchy changes.
---

# Skill: responsive-canvas-validation

## Purpose

Use this Cursor skill to validate Unity Canvas/UI layouts across multiple resolutions, aspect ratios, Canvas Scaler settings, safe-area constraints, and runtime UI states.

This skill is intended for Unity UI work where the goal is not to redesign the UI, but to prove that the current layout behaves correctly and does not break under common display conditions.

The skill focuses on:

- Canvas Scaler validation
- RectTransform anchors and pivots
- HUD responsiveness
- pause/menu/settings layout validation
- safe-area inspection
- text clipping detection
- icon/counter alignment
- layout group stability
- EventSystem sanity
- UI behavior across gameplay/menu states
- identifying fragile fixed-pixel positioning

---

## Scope Recommendation

Use this as a repo-scoped Cursor skill for the current Unity project.

Reason:

Responsive Canvas validation is highly dependent on the existing scene hierarchy, prefab setup, Canvas configuration, HUD structure, menu flow, and project-specific UI expectations.

This skill should work alongside:

- `unity-ui-layout`
- UI/UX cleanup tasks
- pause/settings menu validation
- HUD readability checks
- scene/prefab QA before merging UI changes

---

## When to Use

Use this skill when the user asks to:

- validate Unity UI responsiveness
- check if Canvas layout works across resolutions
- test HUD layout at multiple aspect ratios
- verify Canvas Scaler settings
- inspect anchors and pivots
- detect UI overlap
- detect clipped text
- verify pause/settings menus after layout changes
- confirm top-left/top-right/bottom-left HUD placement
- validate UI before a PR or merge
- check if UI is safe for 1080p, 1440p, laptop, and ultrawide screens

Also use this skill after any task involving:

- HUD movement
- resource counters
- stamina/health positioning
- tips/objective layout
- menu redesign
- settings screen changes
- Canvas hierarchy cleanup
- safe-area adjustments

Do not use this skill for:

- redesigning the UI from scratch
- implementing new gameplay systems
- changing generated Input System C# files
- editing art assets unless the UI layout depends on them
- optimizing non-UI rendering
- fixing audio sliders unless the issue is only visual layout

---

## Primary Goal

The goal is to answer this:

~~~text
Does the current Unity UI layout remain readable, correctly anchored, unclipped, and usable across the target display resolutions and UI states?
~~~

If not, identify exactly what breaks, where it breaks, and what the smallest safe fix should be.

---

## Target Resolutions

Validate at minimum:

~~~text
1920x1080    16:9 baseline desktop
2560x1440    16:9 high resolution
1366x768     small laptop / low resolution
1600x900     mid-size 16:9
1920x1200    16:10
2560x1080    ultrawide
3440x1440    ultrawide
1280x720     minimum HD fallback
~~~

If the project supports windowed mode, also check:

~~~text
Windowed 1280x720
Windowed 1600x900
Windowed 1920x1080
~~~

Optional mobile/safe-area validation only if the project targets mobile or handheld devices.

---

## Required Workflow

Before changing anything:

1. Inspect the current UI hierarchy.
2. Identify all active Canvases.
3. Inspect each Canvas Scaler.
4. Inspect EventSystem setup.
5. Inspect active HUD roots.
6. Inspect pause/menu/settings roots.
7. Inspect dynamic UI components.
8. Inspect prefabs used by the current scene.
9. Validate anchors and pivots.
10. Validate layout behavior across target resolutions.
11. Report issues before applying fixes unless the user explicitly asked for direct fixing.

---

## Detailed Validation Reference

The full validation categories live in [reference.md](reference.md). Read it when actually performing validation. It covers:

- Validation Categories 1–10 (Canvas Inventory, EventSystem, RectTransform, HUD Responsiveness, Menu Responsiveness, Safe-Area, Text, Layout Group, Raycast, Runtime State)
- Cursor / Input Validation
- Performance Validation (with counter/fill update code patterns)
- Automated Validation Helper (editor-only validator script guidance)

---

## Fix Strategy

If validation finds problems, prioritize fixes in this order:

1. Canvas Scaler misconfiguration
2. duplicate or missing EventSystem
3. broken anchors/pivots
4. elements offscreen at target resolutions
5. clipped text
6. HUD clutter
7. menu overflow
8. invisible raycast blockers
9. layout group conflicts
10. performance risks

Do not redesign visual style unless the user explicitly asks.

Apply the smallest safe fix.

---

## Beavermania-Specific Validation Bias

For Beavermania, validate against this intended layout direction:

~~~text
Top Left:
Health / lives / critical player state only

Bottom Left:
Stamina / sprint meter

Top Right:
Icon + number resource counters only

Center / Top Center:
Temporary objective updates and interaction prompts

Menus:
Centered pause/settings panels

Tips:
One consolidated card, controlled by a tips subsystem
~~~

Flag any UI that violates this direction unless the scene clearly needs an exception.

---

## Anti-Patterns to Report

Actively report:

- Canvas set to Constant Pixel Size without reason
- UI only looking correct at one resolution
- fixed pixel positioning on responsive HUD elements
- top-right counters with redundant labels
- stamina mixed into cluttered health corner
- tips scattered across unrelated scene objects
- giant permanent tutorial text
- pause menu not centered
- settings panel overflowing at 1366x768
- invisible UI blocking clicks
- duplicate EventSystems
- generated Input System code edited manually
- menu opens while cursor remains locked
- menu closes while cursor remains visible in gameplay
- TextMeshPro clipping at lower resolution
- layout group and ContentSizeFitter fighting each other
- resource counters causing layout jumps when numbers change

---

## Validation Output Format

When finished, respond using this format:

~~~text
Summary:
- Overall responsive Canvas status

Canvas Inventory:
- Canvas name
- render mode
- Canvas Scaler settings
- sorting order
- raycaster status

Validated States:
- Gameplay
- Pause
- Settings
- Dialogs
- Dynamic HUD updates

Resolution Results:
- 1920x1080:
- 2560x1440:
- 1366x768:
- 1600x900:
- 1920x1200:
- 2560x1080:
- 3440x1440:
- 1280x720:

Issues Found:
- Issue
- Severity
- Location
- Why it matters

Fixes Applied:
- Files/objects changed
- Exact layout changes

Remaining Risks:
- Manual Unity Editor checks still needed
- Prefabs/scenes requiring visual review

Recommendation:
- Merge-safe / needs follow-up / blocked
~~~

---

## Severity Levels

Use these severity levels:

~~~text
Critical:
UI is unusable, offscreen, blocks input, or breaks pause/menu/gameplay flow.

High:
Important HUD/menu information overlaps, clips, or becomes unreadable at target resolutions.

Medium:
Layout works but is fragile, cluttered, inconsistent, or likely to break with localization/value changes.

Low:
Cosmetic spacing/alignment issue that does not harm usability.
~~~

---

## Default Behavior

If the user gives a vague request such as:

~~~text
check the UI
validate the canvas
make sure the layout is responsive
test the HUD
~~~

Then perform a validation audit first.

Do not immediately redesign the UI.

Default flow:

~~~text
Inspect → validate states → test resolutions → report issues → apply smallest safe fixes only if requested
~~~

---

## Hard Rules

- Do not edit generated Input System C# files.
- Do not create duplicate EventSystems.
- Do not redesign the entire HUD unless explicitly requested.
- Do not change gameplay logic unless required for UI state validation.
- Do not leave cursor/menu behavior broken.
- Do not ignore 1366x768 and 1280x720.
- Do not rely on one resolution screenshot as proof.
- Do not leave invisible raycast blockers active.
- Do not add runtime-heavy validation scripts.
- Do not make broad scene changes without reporting exact affected objects.
