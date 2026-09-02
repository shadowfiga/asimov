# Gum visual project goes here

The starter demo intentionally builds its first screen through Gum Forms code so `run` works before you install/open the editor.

When you want visual authoring:

1. Open the Gum editor.
2. Create/save a Gum project in this folder, for example `Content/GumProject/GameUI.gumx`.
3. Add Forms Components from Gum's Content menu.
4. Create a screen such as `CounterScreen` visually.
5. Change the engine's Gum initialization to load `"GumProject/GameUI.gumx"`.
6. Either bind the screen by name at runtime or turn on Gum code generation for strongly typed partial classes.

The rest of the engine (`Scene`, `SceneManager`, game logic) does not need to change.
