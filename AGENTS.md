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

# UI theme

- Use the bundled Abel font for all game text through the shared Myra theme.
- Use Lucide icons for UI controls; bundle the icon assets and their license.
- Use `GameThemes.DeepDrive.Spacing` (`Xs`, `Sm`, `Md`, `Lg`, `Xl`) for gaps, padding, and margins.
- Use `GameThemes.DeepDrive.BorderRadius` (`Zero`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Full`) for rounded surfaces; `Full` produces pills or circles.
- Use `GameThemes.DeepDrive` for UI colors; do not hardcode colors in widgets.
- Keep ordinary UI monochrome. Orange is only for selection, focus, important action, or immediate attention. Green is only for successful, completed, purchased, valid, or confirmed state.
- Never use color as the only indication of state; pair orange and green with text, icons, borders, shape, or position.
- Keep shared Myra styling in `Game/UI/Theming`; initialize it before constructing screens.

# Full-game scope

- [Content Scope Lock](<DEEP DRIVE — Game Design Document.md#41-content-scope-lock>) and [Things Explicitly Out of Scope](<DEEP DRIVE — Game Design Document.md#42-things-explicitly-out-of-scope>) are the hard scope for the full game.
- Do not add features beyond that scope, exceed its content caps, or implement excluded systems.
- Only an explicit user instruction revising the scope can change these limits.
