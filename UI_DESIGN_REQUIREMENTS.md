# GIRLS & MINING UI Design Requirements

This document defines the designer handoff required by GIRLS & MINING and the current Graphite/Myra implementation boundary. Designers provide approved tokens, component states, screen layouts, assets, and behavior specifications; engineering implements those decisions in the game.

## 1. Required designer handoff

| Area | Required output |
| --- | --- |
| Color | Approved monochrome palette, orange/green semantic rules, foreground pairings, and non-color cues |
| Typography | Abel-based type scale for titles, body, controls, terminal data, HUD values, captions, and tooltips |
| Layout | 1280x720 reference screens plus narrow, tall, and ultrawide behavior |
| Components | Default, hover, focus, pressed, disabled, selected, error, and busy states |
| Input | Mouse, keyboard, and controller focus/navigation behavior |
| Accessibility | UI scale, text-size, reduced bloom/flashing, readable contrast, and color-independent status designs |
| Motion | Timings for screen transitions and terminal responses |
| Audio | Terminal click, confirmation, warning, and navigation cue assignments |
| Assets | Screen backgrounds, enemy portraits, structure/technology icons, and non-Lucide interface imagery |

Designers do not need to provide C#, Myra `.xmms` files, or compiled shaders. A single monochrome icon can be tinted for normal, hover, focus, pressed, and disabled states.

## 2. Product constraints

- The interface is an industrial monochrome terminal, not green phosphor, cyberpunk, or colorful space fantasy.
- Orange means current selection, focus, important action, current upgrade, or immediate attention.
- Green means successful, completed, purchased, valid, or operationally confirmed.
- Enemy and threat information stays white/gray with hostile iconography until immediate attention is required.
- No blue resource language and no rainbow-coded research branches.
- No interaction or status may rely on color alone.
- All game text uses the bundled Abel Regular font. Do not depend on multiple weights or italics.
- Text keeps its logical theme size while glyph rasterization adapts to effective screen scale. Inspect high-scale sharpness with CRT at 0%; labels, text inputs, and dropdown rows update globally on resize without requiring dialogs to reopen.
- Validate layouts at 1280x720 and 2560x1440. Staging defaults to 2560x1440 borderless fullscreen; production defaults to 1280x720 windowed. Windowed mode is resizable; borderless follows the native desktop resolution. The logical UI authoring reference remains 1600x900.
- Mouse and keyboard are first priority; controller is second priority.
- Controls must not require pixel-perfect pointing.
- Copy must be concise and functional.

## 3. Theme tokens

### Color

| Token | Value | Required use |
| --- | --- | --- |
| `DeepBlack` | `#050505` | Deepest terminal/background value |
| `Background` | `#090A09` | Root screen background |
| `DialogScrim` | `#050505B8` | Modal fullscreen overlay behind centered dialog content |
| `RaisedSurface` | `#111211` | Panels, dialogs, raised regions |
| `ControlSurface` | `#181918` | Buttons, fields, selectable controls |
| `Border` | `#393B39` | Ordinary boundaries, dividers, grids |
| `Disabled` | `#70736F` | Disabled text and icons |
| `SecondaryText` | `#B7BAB6` | Secondary labels and supporting data |
| `PrimaryText` | `#E8EAE7` | Primary text and high-emphasis data |
| `Selection` | `#E9943A` | Selection, focus, action, immediate attention |
| `SelectionHighlight` | `#FFA143` | Bright edge/glow for focused interactive state |
| `Success` | `#79C98B` | Success, completion, purchase, validity, confirmation |
| `Danger` | `#D96A66` | Explicit cancel/destructive action text and border |
| `DangerHighlight` | `#FF8B85` | Hover/focus on those red actions |

Every use of orange or green requires an accompanying label, icon, border treatment, shape, or stable position. Ordinary text should target at least 4.5:1 contrast; large text and essential control boundaries should target at least 3:1.

### Spacing

| Token | Value |
| --- | ---: |
| `Xs` | 4 px |
| `Sm` | 8 px |
| `Md` | 16 px |
| `Lg` | 24 px |
| `Xl` | 32 px |

Use the scale for gaps, padding, and margins. Any revision must replace the token values instead of introducing isolated measurements.

### Corner radii

| Token | Value |
| --- | ---: |
| `Zero` | 0 px |
| `Xs` | 0 px |
| `Sm` | 0 px |
| `Md` | 0 px |
| `Lg` | 0 px |
| `Xl` | 0 px |
| `Full` | 0 px |

All game UI corners are square. Use the shared `UIBorderRadii.Square` theme profile; do not reintroduce rounding for hover, focus, selected states, dialogs, or dropdowns.

### Typography

The family is fixed to Abel Regular, but the size scale still requires approval. Define:

- game/display title,
- screen title,
- section/terminal heading,
- large numeric HUD value,
- body,
- control label,
- compact data label,
- caption/secondary text,
- tooltip,
- minimum size after responsive scaling,
- casing and tracking for each role.

The current menu uses sizes from approximately 14-48 px. These are working implementation values, not a complete type system.

### Menu button

Menu buttons have complete size presets under `GameThemes.DeepDrive.MenuButton`. Dimensions are logical UI units, configured once; the backend fits them to their parent and applies global scale. The designer must approve these presets:

| Size preset | Preferred size | Default font | Leading / trailing icon | Horizontal padding / icon gaps |
| --- | --- | --- | --- | --- |
| `Standard` | 520 × 68 | 30 | 34 / 20 | 24 |
| `Menu` | 416 × 54 | 24 | 27 / 16 | 19 |
| `Dialog` | 200 × 48 | 24 | 28 / 20 | 24 |
| `Confirmation` | 200 × 44 | 24 | 28 / 20 | 24 |

Each preset exposes these independently configurable tokens (standard values shown):

| Token | Current value | Controls |
| --- | ---: | --- |
| `HorizontalPadding` | 24 px | Space inside the button's left and right edges |
| `VerticalPadding` | 0 px | Space inside the button's top and bottom edges |
| `IconTextSpacing` | 24 px | Gap between the leading icon and label |
| `TextTrailingIconSpacing` | 24 px | Gap between the label and optional trailing icon |
| `CompactFontSize` | 24 px | Compact control-label size |
| `DefaultFontSize` | 30 px | Standard control-label size |
| `ProminentFontSize` | 34 px | High-emphasis control-label size |
| `IconSize` / `MinimumIconSize` | 34 / 22 px | Leading-icon size and minimum explicit override |
| `TrailingIconSize` / `MinimumTrailingIconSize` | 20 / 14 px | Trailing-icon size and minimum explicit override |

Every variant remains Abel Regular. `Compact`, `Default`, and `Prominent` are text-variant choices within a size preset, not font weights. A constructor selects `size` and may override `textVariant`, `iconSize`, or `trailingIconSize`. Icon overrides must meet the matching theme minimums. These logical sizes stay stable during reflow; only the shared desktop scale changes their physical size. Settings and Credits BACK buttons select the `Dialog` preset; graphics confirmation buttons select `Confirmation`. Main-menu buttons select `Menu`, with no local multiplier.

Icons are optional. Text-only buttons center their label and reserve no icon space; a trailing arrow can be requested independently. The graphics confirmation uses text-only KEEP and CANCEL buttons. CANCEL uses the red `Danger` tone (including hover/focus) and still reverts the preview; ordinary buttons retain their existing palette.

## 4. Required interaction states

Every interactive component requires:

1. default,
2. mouse hover,
3. keyboard/controller focus,
4. pressed,
5. disabled,
6. selected/on and deselected/off where applicable,
7. error/invalid where applicable,
8. loading/busy where applicable.

Focus must be visibly different from hover. The current menu uses ordinary surface, border, text, and icon changes for button feedback; buttons do not own shader materials.

## 5. Complete screen scope

The designer must cover exactly these production screens:

1. Main Menu
2. Settings
3. Save / Load with three campaign slots
4. Command Center — Mission
5. Command Center — Research
6. Gameplay HUD
7. In-Run Upgrade Tree
8. Results / Operation Report

Lightweight supporting UI:

- pause overlay,
- confirmation dialog,
- tooltip,
- autosave message.

### Settings implementation

Use the reference's wide modal, left sidebar, active-page content, separators, and fixed Back footer. Keep the current theme; do not copy decorative logos, HUD data, background scenes, angled frames, or extra copy. Only one submenu is visible at a time.

- Video: display mode and resolution, with the existing 15-second Keep/Cancel confirmation.
- Audio: master, music, and FX volume sliders (0–100%), applied live and saved immediately. Master multiplies both channel levels; 0% silences them without a separate Mute control. Music uses streamed song playback; FX includes game effects and UI cues.
- Accessibility: UI scale (0.75×, 1.00×, 1.25×, 1.50×, 1.75×) and CRT strength (0–100%).

Do not add empty Gameplay/Controls pages or inert settings. Input remapping, text size, reduced shake, reduced bloom/flashing, and damage-number visibility remain required future work once their backing systems exist; this pass does not implement them or the reference's unrelated options. Current controls save live, so there is no redundant Apply button. The header/sidebar/Back remain usable while longer page content scrolls.

Main-menu composition and Credits live in separate files under `Game/Scenes/MainMenu`. The reusable Settings dialog and its page/control files live in `Game/UI/Settings`; keep preference and display logic out of the main-menu composition.

## 6. Required gameplay components

- Ore balance and Ore change feedback
- Threat indicator with non-color escalation cues
- wave number, wave progress, and construction interval
- Outpost Core health
- Miner-01 disabled/reconstruction state
- selected hardpoint and valid structure choices
- valid/invalid build state
- structure health, upgrade, repair, online, and destroyed states
- Field Technology Tree node states: locked, available, affordable, unaffordable, purchased, and max rank
- permanent Research node states using the same clear hierarchy
- sector rows: locked, available, selected, cleared, and record-bearing
- mission modifiers and known-enemy presentation
- BEST ORE and other record treatment
- Operation Report metrics and new-record indicators
- Research Core award/balance treatment
- extraction versus Continue Deeper decision
- autosave in-progress, success, and failure states

## 7. CRT presentation

CRT is a game-wide final-frame treatment, never a widget, screen-root, or per-control effect. The renderer captures the complete scene followed by the UI at backbuffer resolution, applies CRT once, and presents that result. The same setting therefore affects menus and gameplay.

The implementation exposes only one progress-meter slider labelled from `NO` to `FULL`. At 0%, processing is disabled; every non-zero intensity enables it. No separate checkbox or boolean is stored. Scanlines are horizontal only; there is no direction selector, alternative profile, or screen curvature.

The original Aged treatment combines subtle horizontal scanlines, faint two-dimensional screen-space noise, mild radial falloff, and bloom. Scanline phase comes from the full-screen vertical coordinate, so every line spans the entire display horizontally; noise varies across both dimensions but must not resolve into directional bands. The slider scales the complete recipe proportionally, reaching the original full Aged strengths at 100%. White text must remain sharp. Orange may bloom more visibly than green, but neither should become blurry.

Avoid aggressive distortion, constant flicker, excessive chromatic aberration, or effects that obscure terminal data. Reduced-bloom/flashing accessibility behavior still needs a final product setting.

## 8. What Graphite/Myra supports

Substantially themed now:

- root, ordinary, and nested panels,
- labels and secondary labels,
- tooltips,
- text boxes,
- windows and close buttons,
- separators and progress bars,
- list boxes,
- combo-box dropdowns,
- tab controls,
- custom menu buttons with theme-driven spacing, semantic text variants, and icon sizing,
- modal fullscreen dialogs with a themed translucent scrim and centered child,
- tintable PNG icons,
- square fills and borders in all states; thin outlines remain at least one physical screen pixel at fractional UI scales, including dropdown popup lists,
- borderless progress/volume tracks: `DeepBlack` empty track, `Selection` orange fill, no inset or outline on either the progress bar or its overlaid slider in any state,
- global reactive viewport/UI scaling, including open dialogs and pointer hitboxes,
- fullscreen CRT processing,
- show/hide animation,
- UI sound cues.

Available motion parameters:

- translation, rotation, scale, opacity,
- custom shader parameters,
- duration, delay, repeat, alternate direction,
- linear, smooth, and cubic-out easing,
- cancellation with optional settle time.

Available sound parameters:

- asset, delay, volume, pitch, stereo pan, and looping.

## 9. Engineering prerequisites for later screens

- Keep future content and terminology aligned with the GIRLS & MINING GDD.
- Add semantic checkbox, radio, slider, and scroll-view styling before the full Settings screen expands.
- Extend centralized typography and icon-size tokens beyond the menu-button component; add border-width and component-height tokens.
- Implement controller navigation and controller glyph switching.
- Implement full input remapping.
- The UI-scale dropdown uses `RuntimePreferences.UiScaleOptions` (0.75×–1.75× in 0.25 increments, default 1.00×) and `RuntimePreferences.UiScale`, with live global resizing and immediate persistence. Older saved values clamp to the new limits and snap to the nearest preset before layout, including outside Settings. Implement the remaining text-size, reduced-bloom/flashing, and damage-number settings.
- Display mode and resolution dropdowns are implemented globally through `DisplaySettings` and the confirmed `RuntimePreferences.Display` preference. Modes are Windowed, Borderless Fullscreen, and Fullscreen. Borderless locks the resolution selector to the native desktop size; exclusive fullscreen offers adapter-supported resolutions. A centered Keep/Cancel dialog automatically reverts unconfirmed changes after 15 seconds. Saved settings override environment defaults and survive scene changes/restarts.
- Define preferred sizes in `GameThemes.DeepDrive.Layout`, alignment, padding, and scrolling declaratively. The UI backend fits components to their parent; screen classes must not duplicate resize calculations. `AdaptiveGrid.ColumnRules` handles declarative parent-width breakpoints, and the shared dialog owns safe-area fitting and centering. The engine scales from a 1600x900 authoring reference using the smaller viewport ratio, multiplied by the UI-scale preference. Do not scale widgets a second time. Only exceptional layouts should override the lifecycle-managed `UIScreen.OnLayoutChanged()` hook.
- Build a component-gallery screen for review before producing the Command Center and technology trees.

## 10. Screen delivery checklist

For every screen or overlay, provide:

- 1280x720 and 2560x1440 layouts,
- narrow and ultrawide behavior,
- maximum expected text/data lengths,
- controller focus order and initial focus,
- every applicable interaction and data state,
- UI-scale and larger-text behavior,
- reduced-effect behavior,
- motion and sound annotations,
- asset names and export dimensions.
