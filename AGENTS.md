# UI and text

- Never add unnecessary text unless the user explicitly requests it.
- Never add UI components solely to display unnecessary text.
- Do not add unsolicited taglines, promotional copy, descriptions, explanatory labels, or redundant instructions.
- Keep UI text limited to what is necessary to identify the screen, operate controls, or communicate essential state.
- Keep responses concise; do not add filler.

# C# formatting

- Put opening and closing braces on their own lines for code blocks (Allman style).
- Never inline `if`, `else`, loops, `try`, `catch`, `finally`, or method bodies inside `{ ... }`, even for one statement.
- Follow `.editorconfig`; do not preserve single-line blocks or put multiple statements on one line.

# Collections

- Arrays, lists, dictionaries, sets, and other collection-like fields and properties must default to an empty collection of the appropriate type, never `null` or `null!`.
- Never use `null` or `null!` as a fallback when initializing or assigning a collection; use an empty collection instead.

# Fail-fast game code

- Outside `Engine/`, trust internal callers, generated Chisel definitions, and contracts already enforced by the engine. Do not add blanket null/range checks, repeated `Validate()` calls, or defensive try/catch cleanup wrappers around ordinary game code.
- Let programming errors propagate and crash with their original exception. Do not swallow them, log-and-continue, silently return, substitute defaults, clamp invalid state, or otherwise make broken game code appear to work.
- Prefer direct access over redundant existence/bounds checks when the runtime or engine already throws. Keep a focused fail-fast assertion only when invalid state would otherwise be silently accepted, corrupt behavior, or hang (for example, an invalid firing interval).
- Keep intentional behavior: optional UI content, cache misses, input debouncing, pause/focus handling, normal resource disposal, and actual gameplay limits are not error recovery. Validate external settings/data once at their owning boundary, not throughout their consumers.
- This cleanup policy does NOT apply to `Engine/`. Preserve the engine's existing level of validation, defensive handling, and resource/lifecycle safety; do not weaken it to simplify game code.

# Engine bug regression tests

- Every discovered engine-level bug must have a focused automated regression test reproducing it. Write the test before or alongside the fix, verify that it fails against the broken behavior and passes with the fix, and run the relevant existing tests.
- Do not fix engine bugs without regression coverage or weaken/remove existing tests to make a failure disappear.

# UI theme

- Use the bundled Abel font for all game text through the shared Myra theme.
- Use Lucide icons for UI controls; bundle the icon assets and their license.
- Use `GameThemes.DeepDrive.Spacing` (`Xs`, `Sm`, `Md`, `Lg`, `Xl`) for gaps, padding, and margins.
- Use `GameThemes.DeepDrive.BorderRadius` (`Zero`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Full`) for rounded surfaces; `Full` produces pills or circles.
- Use `GameThemes.DeepDrive` for UI colors; do not hardcode colors in widgets.
- Keep ordinary UI monochrome. Orange is only for selection, focus, important action, or immediate attention. Green is only for successful, completed, purchased, valid, or confirmed state.
- Never use color as the only indication of state; pair orange and green with text, icons, borders, shape, or position.
- Keep shared Myra styling in `Game/UI/Theming`; initialize it before constructing screens.

# UI layout

- Screens and dialogs declare components, preferred logical sizes, alignment, padding, and scrolling; the UI backend owns responsive fitting and global scale.
- Use theme-defined component size presets. Do not add local scale multipliers, per-screen resize loops, or viewport-based width/height calculations for ordinary layout.
- Use shared layout components for reflow rules. Reserve `UIScreen.OnLayoutChanged()` overrides for genuinely specialized behavior, not normal component sizing.
- Keep preference synchronization independent of layout; do not use resize callbacks as settings bindings.

# Full-game scope

- [Content Scope Lock](<GIRLS & MINING — Game Design Document.md#41-content-scope-lock>) and [Things Explicitly Out of Scope](<GIRLS & MINING — Game Design Document.md#42-things-explicitly-out-of-scope>) are the hard scope for the full game.
- Do not add features beyond that scope, exceed its content caps, or implement excluded systems.
- Only an explicit user instruction revising the scope can change these limits.
