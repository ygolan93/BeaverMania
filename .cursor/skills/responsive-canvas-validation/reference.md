# responsive-canvas-validation — Detailed Validation Reference

Detailed validation categories for the `responsive-canvas-validation` skill. Read the relevant section when performing validation.

---

## Validation Categories

### 1. Canvas Inventory

Find and report:

- all active Canvases
- render mode for each Canvas
- Canvas Scaler mode
- reference resolution
- match width/height value
- pixel-perfect setting
- sorting order
- Graphic Raycaster presence
- whether the Canvas contains HUD, menu, overlay, or debug UI

Expected baseline for normal HUD/menu Canvas:

~~~text
Render Mode: Screen Space - Overlay
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1920x1080
Screen Match Mode: Match Width Or Height
Match: 0.5
~~~

Do not force this baseline if the existing project has a deliberate reason for another setup. Explain the reason before changing it.

---

### 2. EventSystem Validation

Check:

- exactly one active EventSystem exists
- correct input module is used
- no duplicate EventSystems exist in additive scenes or prefabs
- UI buttons/sliders remain interactable in menu state
- gameplay HUD does not block clicks unnecessarily

If using Unity's new Input System, prefer:

~~~text
InputSystemUIInputModule
~~~

Avoid creating duplicate EventSystems.

---

### 3. RectTransform Validation

For every important UI group, inspect:

- anchor min
- anchor max
- pivot
- anchored position
- size delta
- parent layout context
- whether manual positioning fights layout components

Important groups include:

- top-left HUD
- top-right counters
- bottom-left stamina
- health/lives group
- objective prompt
- tips panel
- interaction prompt
- pause menu panel
- settings menu panel
- confirmation dialogs
- overlays

Recommended anchors:

~~~text
Top-left HUD:
Anchor: top-left
Pivot: 0,1

Top-right resources:
Anchor: top-right
Pivot: 1,1

Bottom-left stamina:
Anchor: bottom-left
Pivot: 0,0

Center objective/prompt:
Anchor: center or top-center
Pivot: 0.5,0.5

Pause/settings panel:
Anchor: center
Pivot: 0.5,0.5

Fullscreen overlay:
Anchor: stretch
Pivot: 0.5,0.5
~~~

Flag anything that uses suspicious fixed positioning or mismatched anchors/pivots.

---

### 4. HUD Responsiveness Validation

Validate the HUD in gameplay state.

Check:

- health does not overlap stamina
- stamina does not overlap resource counters
- resource counters remain inside screen bounds
- objective text does not cover combat center too aggressively
- interaction prompts are readable
- tips do not block key gameplay view
- no permanent paragraph text clutters gameplay HUD
- icons and numbers align consistently
- no text clips at lower resolution
- no UI element is partially offscreen

For Beavermania-style HUD direction, prefer:

~~~text
Top Left:
Health / lives / critical status only

Bottom Left:
Stamina / sprint / movement-related meter

Top Right:
Resource counters as icon + number only

Center / Top Center:
Temporary objective updates and interaction prompts

Menu Center:
Pause/settings/restart/exit panels

Tips:
One consolidated tip card system
~~~

---

### 5. Menu Responsiveness Validation

Validate all menu states:

- pause menu
- settings menu
- confirmation dialogs
- restart/exit prompts
- any nested menu panels

Check:

- panel remains centered
- overlay covers full screen
- buttons remain visible
- labels do not clip
- sliders remain aligned
- footer buttons remain reachable
- menu does not overflow vertically at 1366x768 or 1280x720
- selected/default UI element is sensible for keyboard/controller
- mouse cursor is usable if the menu requires mouse interaction

Expected pause menu structure:

~~~text
PauseMenu
├── OverlayDim
└── Panel
    ├── Title
    ├── ResumeButton
    ├── SettingsButton
    ├── RestartButton
    └── ExitButton
~~~

Expected settings menu structure:

~~~text
SettingsMenu
└── Panel
    ├── Title
    ├── Rows
    │   ├── MusicVolumeRow
    │   ├── SfxVolumeRow
    │   ├── SensitivityRow
    │   └── DisplayModeRow
    └── FooterButtons
        ├── ApplyButton
        └── BackButton
~~~

---

### 6. Safe-Area Validation

If safe-area support exists or is needed, validate:

- UI does not sit under screen cutouts
- important HUD elements keep safe margins
- top corners are not too close to screen edge
- bottom-left stamina is not clipped
- menus remain centered inside safe area if applicable

Desktop default margins:

~~~text
HUD outer margin: 32–48 px
Small-screen margin: 20–32 px
Icon/text spacing: 8–16 px
Counter spacing: 12–24 px
Menu padding: 32–64 px
~~~

Only implement safe-area scripts if the target platforms require it.

---

### 7. Text Validation

Inspect all visible UI text for:

- clipping
- overflow
- unreadable size
- bad contrast
- paragraph-length HUD clutter
- inconsistent font sizes
- unnecessary labels next to obvious icons
- TextMeshPro auto-size causing unstable layout
- Hebrew/RTL issues if Hebrew UI exists
- localization overflow risk if multiple languages exist

Rules:

- HUD text must be short.
- Objective text must be readable at 1366x768.
- Resource counters should prefer icon + number.
- Permanent HUD should not contain long explanations.
- Tips should be short and controlled.

Flag text that will likely break with localization.

---

### 8. Layout Group Validation

Inspect all active:

- HorizontalLayoutGroup
- VerticalLayoutGroup
- GridLayoutGroup
- ContentSizeFitter
- LayoutElement
- AspectRatioFitter

Flag:

- layout groups nested too deeply
- ContentSizeFitter fighting parent LayoutGroup
- manually sized children inside strict LayoutGroups
- dynamic runtime content causing frequent rebuilds
- layout elements without clear preferred width/height
- settings rows with inconsistent label widths
- counters with inconsistent icon sizes

Preferred counter setup:

~~~text
Counter
├── Icon
└── ValueText
~~~

Recommended:

~~~text
Horizontal Layout Group
Child Alignment: Middle Center
Spacing: 8–12 px
Controlled child size: as needed
~~~

---

### 9. Raycast Validation

Check:

- non-interactive HUD images have Raycast Target disabled
- decorative TMP text has Raycast Target disabled
- overlay blockers intentionally block clicks
- menu buttons/sliders receive clicks
- hidden menus do not block gameplay interaction
- inactive overlays are actually inactive or non-raycasting

Flag any invisible UI object that blocks interaction.

---

### 10. Runtime State Validation

Validate UI in these states:

~~~text
Gameplay active
Pause menu open
Settings menu open
Confirmation dialog open
Objective/tip visible
Resource counter value changed
Stamina changing
Health/lives changing
Scene transition if relevant
~~~

Check:

- no layout jump
- no clipped text
- no offscreen elements
- no duplicate menu panels
- no broken cursor behavior
- no lost EventSystem selection
- no UI state remains stuck after closing a menu
- dynamic values do not cause layout overflow

---

## Cursor / Input Validation

When validating menu-related Canvas behavior, also check:

- cursor becomes visible when mouse-driven menu is opened
- cursor unlocks when mouse-driven menu is opened
- cursor hides again when returning to gameplay, if gameplay expects that
- cursor lock state returns to gameplay mode
- pause/resume flow still works
- UI action map does not permanently block gameplay input
- gameplay action map does not interfere with menu navigation

Expected behavior when applicable:

~~~text
Gameplay:
Cursor.visible = false
Cursor.lockState = Locked

Menu:
Cursor.visible = true
Cursor.lockState = None
~~~

Do not change cursor behavior unless the task requires it or validation proves it is broken.

---

## Performance Validation

Look for UI choices likely to cause runtime cost.

Flag:

- text updated every frame even when value did not change
- LayoutGroups rebuilding during gameplay
- ContentSizeFitter on frequently changing runtime HUD
- expensive scene searches in UI scripts
- unnecessary Update methods in static layout scripts
- enabling/disabling large UI hierarchies repeatedly
- many raycast targets on static HUD elements

Prefer event-driven UI updates.

Recommended counter update pattern:

~~~csharp
if (_lastValue == newValue)
    return;

_lastValue = newValue;
_text.SetText("{0}", newValue);
~~~

Recommended fill update pattern:

~~~csharp
if (Mathf.Approximately(_lastFillAmount, newFillAmount))
    return;

_lastFillAmount = newFillAmount;
_fillImage.fillAmount = newFillAmount;
~~~

---

## Automated Validation Helper

If appropriate, create or improve an editor-only validation script.

Preferred location:

~~~text
Assets/Editor/UI/ResponsiveCanvasValidator.cs
~~~

The validator may check:

- active Canvases
- Canvas Scaler settings
- duplicate EventSystems
- RectTransforms outside parent bounds
- suspicious anchors
- Graphic Raycaster presence
- excessive Raycast Targets
- missing TMP references
- inactive menu objects that still block raycasts
- layout groups with risky ContentSizeFitter combinations

Do not add editor tooling unless it clearly helps the task.

If added, keep it editor-only:

~~~csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ResponsiveCanvasValidator
{
    [MenuItem("Tools/UI/Validate Responsive Canvases")]
    public static void Validate()
    {
        Debug.Log("Responsive Canvas validation started.");
        // Implementation should inspect current scene Canvases, EventSystems, and key RectTransforms.
    }
}
#endif
~~~
