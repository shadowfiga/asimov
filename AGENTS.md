# UI and text

- Never add unnecessary text unless the user explicitly requests it.
- Never add UI components solely to display unnecessary text.
- Do not add unsolicited taglines, promotional copy, descriptions, explanatory labels, or redundant instructions.
- Keep UI text limited to what is necessary to identify the screen, operate controls, or communicate essential state.
- Keep responses concise; do not add filler.

# Collections

- Arrays, lists, dictionaries, sets, and other collection-like fields and properties must default to an empty collection of the appropriate type, never `null` or `null!`.
- Never use `null` or `null!` as a fallback when initializing or assigning a collection; use an empty collection instead.

# UI theme

- Use the bundled Abel font for all game text through the shared Myra theme.
- Use Lucide icons for UI controls; bundle the icon assets and their license.
- Use `GameThemes.Aftergreen.Spacing` (`Xs`, `Sm`, `Md`, `Lg`, `Xl`) for gaps, padding, and margins.
- Use `GameThemes.Aftergreen.BorderRadius` (`Zero`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Full`) for rounded surfaces; `Full` produces pills or circles.
- Use `GameThemes.Aftergreen` for UI colors; do not hardcode colors in widgets.
- Use the neutral grays for ordinary controls, `Color1`–`Color6` for game information, and status colors only for meaningful state.
- Keep shared Myra styling in `Game/UI/Theming`; initialize it before constructing screens.

# Full-game scope

- [Appendix A: v1.0 Scope Lock](<AFTERGREEN — Full Game Design Document.md#appendix-a-v10-scope-lock>) is the hard scope for the full game and overrides conflicting earlier design content, including the vertical slice GDD.
- Do not add features beyond that scope, exceed its content caps, or implement excluded systems. Removing other content does not authorize an exception.
- Only an explicit user instruction revising the scope can change these limits.
