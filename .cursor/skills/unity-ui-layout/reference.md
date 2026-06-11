# unity-ui-layout — Detailed Layout Reference

Detailed implementation rules for the `unity-ui-layout` skill. Read the relevant section when implementing layout changes.

---

## Canvas Rules

### Canvas Scaler

Prefer:

~~~text
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1920 x 1080
Screen Match Mode: Match Width Or Height
Match: 0.5
~~~

Only change this if the existing project already uses a deliberate different setup.

### Canvas Render Mode

Prefer:

- `Screen Space - Overlay` for standard HUD and menus.
- `Screen Space - Camera` only if the project needs camera-specific UI effects.
- `World Space` only for diegetic UI, NPC prompts, interaction markers, or floating labels.

Do not casually change render mode without checking dependencies.

### Graphic Raycaster

Rules:

- Menu Canvas should have raycasting when clickable.
- HUD-only Canvas can disable raycast targets on non-interactive graphics.
- Disable `Raycast Target` on decorative images and static TMP text where interaction is not needed.

### EventSystem

Rules:

- Ensure exactly one active EventSystem exists in the scene.
- If using the new Input System, verify the correct Input System UI module is present.
- Do not create duplicate EventSystems.

---

## RectTransform Layout Rules

### Anchors

Use anchors intentionally:

- Top-left HUD: anchor top-left
- Top-right resources: anchor top-right
- Bottom-left stamina: anchor bottom-left
- Center prompts: anchor center or top-center
- Pause/settings menu: anchor center
- Fullscreen overlay background: stretch anchors

Avoid:

- random absolute pixel offsets
- manually stretched elements without reason
- anchors that fight the pivot
- nested objects with contradictory layout behavior
- layout groups inside layout groups unless justified

### Pivots

Recommended pivots:

- Top-left groups: `(0, 1)`
- Top-right groups: `(1, 1)`
- Bottom-left groups: `(0, 0)`
- Center menu panels: `(0.5, 0.5)`
- Fullscreen overlays: `(0.5, 0.5)`

### Safe Margins

Use consistent padding:

~~~text
Desktop HUD margin: 32–48 px
Small screen margin: 20–32 px
Menu panel padding: 32–64 px
Icon/text spacing: 8–16 px
Counter group spacing: 12–24 px
~~~

---

## HUD Layout Rules

### Top Right Corner

For resource counters:

- Prefer icon + number only.
- Remove redundant labels such as:
  - `Coins:`
  - `Logs:`
  - `Ammo:`
  - `Keys:`
- Use clear icons.
- Keep numbers aligned.
- Keep spacing consistent between counters.

Good:

~~~text
[coin icon] 24
[log icon] 6
[arrow icon] 12
~~~

Bad:

~~~text
Coins: 24
Collected Logs Amount: 6
Current Arrow Ammunition: 12
~~~

### Top Left Corner

Recommended use:

- Health
- lives
- critical player state
- compact status effects

Avoid placing too much in this corner.

If stamina currently lives near health and creates clutter, move stamina to the bottom-left.

### Bottom Left Corner

Recommended use:

- stamina bar
- sprint meter
- ability energy
- compact movement-related resource

Stamina should be visually separate from health.

Preferred stamina styles:

- horizontal bar
- compact radial bar
- segmented bar

Avoid:

- overly long labels
- large explanatory text
- crowding with unrelated counters

### Center / Top Center

Recommended use:

- current objective
- short interaction prompts
- temporary mission hints

Rules:

- Keep text short.
- Avoid permanent paragraph-length messages.
- Use fade or timed display when possible.
- Make sure prompts do not cover enemies, crosshair, or player aim.

---

## Tips Subsystem Rules

Tips should not be random scattered text.

Prefer a consolidated tips subsystem:

- one root container
- one reusable tip card prefab
- priority-based display
- max one visible tip at a time unless explicitly designed otherwise
- optional fade in/out
- optional queue
- optional cooldown
- no combat visibility blockage

Recommended structure:

~~~text
Tips_Root
└── TipCard
    ├── Icon
    ├── TitleText
    └── BodyText
~~~

Recommended behavior:

- Tips are triggered by gameplay events.
- Tips should not rely on arbitrary scene booleans spread across unrelated scripts.
- Tips should have IDs so they can avoid repeating unnecessarily.
- Critical tips can override low-priority tips.
- Optional tips should disappear after a short duration.

---

## Menu Layout Rules

### Pause Menu

Pause menu should:

- appear centered
- use a clean panel
- optionally dim the background
- clearly expose:
  - Resume
  - Settings
  - Restart
  - Exit / Main Menu
- support keyboard/controller navigation if the project uses Input System UI navigation
- not permanently break cursor state after closing

Recommended structure:

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

### Settings Menu

Settings menu should:

- use aligned rows
- use consistent label width
- keep sliders aligned
- expose current value when helpful
- route controls to actual game systems, not dummy UI sliders

Recommended structure:

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

### Confirmation Dialogs

Use confirmation dialogs for destructive actions:

- Restart
- Quit to menu
- Delete save
- Reset settings

Recommended structure:

~~~text
ConfirmationDialog
├── OverlayBlocker
└── Panel
    ├── Message
    ├── ConfirmButton
    └── CancelButton
~~~

---

## Performance Rules

Avoid:

- `GetComponent` in `Update`
- layout rebuilds every frame
- constantly changing LayoutGroup children
- frequently rebuilding TMP text unnecessarily
- unnecessary nested Canvases
- toggling many child objects every frame
- expensive scene searches like `FindObjectOfType` during gameplay

Prefer:

- cached references
- serialized references
- event-driven UI updates
- separate Canvas for dynamic HUD counters if needed
- only updating text when values change

For counters, prefer this pattern:

~~~csharp
if (_lastValue == newValue)
    return;

_lastValue = newValue;
_text.SetText("{0}", newValue);
~~~

For bars, prefer this pattern:

~~~csharp
if (Mathf.Approximately(_lastFillAmount, newFillAmount))
    return;

_lastFillAmount = newFillAmount;
_fillImage.fillAmount = newFillAmount;
~~~

---

## Text Rules

Prefer TextMeshPro for readable UI text.

Rules:

- Use consistent font sizes.
- Avoid tiny text below practical gameplay readability.
- Avoid long permanent HUD sentences.
- Use short verbs in prompts:
  - `Press E`
  - `Pick Up`
  - `Talk`
  - `Build`
  - `Return to Beavus`
- Use larger text for important objective changes.
- Use smaller text for secondary details.

Avoid:

- all-caps paragraphs
- multiple unrelated font sizes in the same panel
- low contrast text
- text over busy backgrounds without a panel/shadow/outline

---

## Icon Rules

For icon + number UI:

- icons should be visually clear at gameplay distance
- icons should be the same visual style
- icon size should be consistent across counters
- numbers should align vertically
- do not mix large labels with compact counters

Recommended counter structure:

~~~text
Counter
├── Icon
└── ValueText
~~~

Recommended layout:

- Horizontal Layout Group
- child alignment: middle center
- spacing: 8–12 px
- padding: minimal

---

## Layout Components Guidance

### Horizontal Layout Group

Use for:

- icon + number counters
- button rows
- settings row value sections

### Vertical Layout Group

Use for:

- menu button stacks
- settings rows
- objective/tip text stack

### Content Size Fitter

Use carefully.

Acceptable:

- static menu panels
- tip cards with controlled text length

Avoid:

- frequently changing runtime HUD elements
- deeply nested dynamic layout groups
- scrollable content that updates often

### Layout Element

Use for:

- fixed preferred widths in settings labels
- enforcing consistent button sizes
- stabilizing mixed icon/text rows
