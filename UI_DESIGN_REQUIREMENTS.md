# DEEP // DRIVE UI Design Requirements

This document defines the designer handoff required by DEEP // DRIVE and the current Graphite/Myra implementation boundary. Designers provide approved tokens, component states, screen layouts, assets, and behavior specifications; engineering implements those decisions in the game.

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
- Baseline resolution is 1280x720. The window is resizable.
- Mouse and keyboard are first priority; controller is second priority.
- Controls must not require pixel-perfect pointing.
- Copy must be concise and functional.

## 3. Theme tokens

### Color

| Token | Value | Required use |
| --- | --- | --- |
| `DeepBlack` | `#050505` | Deepest terminal/background value |
| `Background` | `#090A09` | Root screen background |
| `RaisedSurface` | `#111211` | Panels, dialogs, raised regions |
| `ControlSurface` | `#181918` | Buttons, fields, selectable controls |
| `Border` | `#393B39` | Ordinary boundaries, dividers, grids |
| `Disabled` | `#70736F` | Disabled text and icons |
| `SecondaryText` | `#B7BAB6` | Secondary labels and supporting data |
| `PrimaryText` | `#E8EAE7` | Primary text and high-emphasis data |
| `Selection` | `#E9943A` | Selection, focus, action, immediate attention |
| `SelectionHighlight` | `#FFA143` | Bright edge/glow for focused interactive state |
| `Success` | `#79C98B` | Success, completion, purchase, validity, confirmation |

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
| `Xs` | 2 px |
| `Sm` | 4 px |
| `Md` | 8 px |
| `Lg` | 12 px |
| `Xl` | 16 px |
| `Full` | Half of the shorter side |

The severe terminal direction should normally use `Zero`, `Xs`, or `Sm`. Larger radii need a specific reason.

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

CRT is a fullscreen screen treatment, never a per-control hover effect.

The implementation exposes only one progress-meter slider labelled from `NO` to a deliberately pronounced `FULL`. At 0%, processing is disabled; every non-zero intensity enables it. No separate checkbox or boolean is stored. Scanlines are horizontal only; there is no direction selector, alternative profile, or screen curvature.

The Aged treatment combines scanlines, faint noise, vignette, and bloom. Its 50% point matches the earlier full-strength Aged look; the upper half becomes visibly more weathered. White text must remain sharp. Orange may bloom more visibly than green, but neither should become blurry.

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
- custom menu buttons,
- tintable PNG icons,
- rounded fills and borders,
- responsive sizing,
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

- Keep future content and terminology aligned with the DEEP // DRIVE GDD.
- Add semantic checkbox, radio, slider, and scroll-view styling before the full Settings screen expands.
- Add centralized typography, border-width, component-height, and icon-size tokens.
- Implement controller navigation and controller glyph switching.
- Implement full input remapping.
- Implement UI scale, text-size, reduced-bloom/flashing, and damage-number settings.
- Define complete aspect-ratio reflow rules; the current layout primarily scales by window height.
- Build a component-gallery screen for review before producing the Command Center and technology trees.

## 10. Screen delivery checklist

For every screen or overlay, provide:

- 1280x720 reference layout,
- narrow and ultrawide behavior,
- maximum expected text/data lengths,
- controller focus order and initial focus,
- every applicable interaction and data state,
- UI-scale and larger-text behavior,
- reduced-effect behavior,
- motion and sound annotations,
- asset names and export dimensions.
