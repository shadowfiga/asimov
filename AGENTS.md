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

- Use `GameThemes.Aftergreen` for UI colors; do not hardcode colors in widgets.
- Use the neutral grays for ordinary controls, `Color1`–`Color6` for game information, and status colors only for meaningful state.
- Keep shared Myra styling in `Game/UI/Theming`; initialize it before constructing screens.
