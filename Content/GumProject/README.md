# Graphite Gum project

Open `Graphite.gumx` by double-clicking `gum.cmd` at the project root. The project includes Gum's official Standard Forms components and an editable `CounterScreen`.

Graphite loads this project from the `ui.project` entry in `settings.json`. A `UIScreen` binds fields marked with `[UIElement]` to same-named Gum instances. For example, `_incrementButton` binds to `IncrementButton`.

Keep control instance names synchronized with the corresponding code fields, or pass an explicit name such as `[UIElement("ConfirmButton")]`.
