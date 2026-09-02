# Graphite

A small code-first 2D engine and game starter built on **MonoGame DesktopGL + Gum**.

Pinned dependencies:

- MonoGame.Framework.DesktopGL **3.8.5.1**
- Gum.MonoGame **2026.9.2.1**
- .NET **8** target

## Instant setup

### Windows

Double-click `setup.cmd`, or from PowerShell:

```powershell
.\Scripts\setup.ps1
```

### macOS / Linux

```bash
./setup.sh
```

The setup script:

1. Uses the project-local .NET 8 SDK or a compatible system installation.
2. Otherwise installs .NET 8 **locally into `.dotnet/`** (no sudo/admin).
3. Restores MonoGame and Gum from NuGet.
4. Builds the game.
5. Launches it.

After the first setup, use `run.cmd` or `./run.sh`.

> The first setup needs internet access to fetch .NET (if missing) and NuGet packages.

## Demo

The initial `CounterScene` opens a Gum UI screen containing:

- current counter value
- `+ Increment` button
- `- Decrement` button

Press **Escape** to quit.

## Project settings

Edit `settings.json` to configure the game without changing engine code:

```json
{
  "game": {
    "name": "My Game",
    "icon": "Content/icon.png",
    "startupScene": "Scenes/BootstrapScene"
  },
  "window": {
    "width": 1280,
    "height": 720,
    "fullscreen": false,
    "borderless": false,
    "resizable": true
  },
  "graphics": {
    "clearColor": "#16181F",
    "vsync": true,
    "preferMultiSampling": false
  },
  "runtime": {
    "mouseVisible": true,
    "fixedTimeStep": true,
    "targetFramesPerSecond": 60
  },
  "content": {
    "rootDirectory": "Content"
  },
  "ui": {
    "project": "GumProject/Graphite.gumx"
  }
}
```

`game.icon` accepts an image path relative to `settings.json`; PNG is recommended. Set it to `null` to use the platform default. `game.startupScene` is a class path relative to the game's namespace, so `Scenes/BootstrapScene` resolves to `Graphite.Game.Scenes.BootstrapScene`. Forward slashes, backslashes, and dots are accepted. Invalid or missing settings fail at startup with a specific error instead of silently falling back. Run the project again after changing settings so the file is copied beside the executable.

## Architecture

```text
Engine/
  Configuration/
    GraphiteSettings.cs
  Core/
    GameHost.cs
    Thing.cs
    Component.cs
    Behaviour.cs
    Transform.cs
    Time.cs
    Input.cs
  Scenes/
    Scene.cs
    SceneManager.cs
    SceneLoadMode.cs
  UI/
    Controls/
      Button.cs
      Text.cs
    UIElementAttribute.cs
    UIElementBinder.cs
    UI.cs
    UIScope.cs
    UIScreen.cs
    Gum/
      GumScreenInstance.cs
  Platform/
    WindowIcon.cs

Game/
  Scenes/
    BootstrapScene.cs
    MainMenuScene.cs
    CounterScene.cs
  UI/
    MainMenuScreen.cs
    CounterScreen.cs

Content/
  GumProject/
    Graphite.gumx
    Screens/MainMenuScreen.gusx
    Screens/CounterScreen.gusx
```

The intended vocabulary is:

```text
Scene -> Thing -> Component -> Behaviour
SceneManager.Load<T>()
Scene.UI.Open<TScreen>()
```

Scene transitions are queued and committed after update, so loading a scene from inside a Behaviour doesn't invalidate the current update traversal.

## Scene ergonomics

```csharp
SceneManager.Load<CounterScene>();
SceneManager.Load<PauseScene>(SceneLoadMode.Additive);
SceneManager.Reload();
SceneManager.Unload<PauseScene>();
```

A `Thing` is Graphite's equivalent of Unity's `GameObject`. A scene creates one like this:

```csharp
var player = Create("Player");
player.Transform.Position = new(200, 100);
player.Add<PlayerController>();
```

A behaviour looks like:

```csharp
public sealed class PlayerController : Behaviour
{
    protected internal override void Update(float dt)
    {
        Transform.Position += Vector2.UnitX * 100 * dt;
    }
}
```

## Gum visual editor

The official Gum editor is installed locally under `.tools/Gum` and is intentionally excluded from Git. Open the Graphite UI project by double-clicking `gum.cmd` or running:

```powershell
.\Scripts\open-gum.ps1
```

If the local tool is missing on another machine, install the pinned editor release with:

```powershell
.\Scripts\install-gum.ps1
```

`Content/GumProject/Graphite.gumx` includes Gum's Standard Forms components and the editable `CounterScreen`. Save changes in Gum and run Graphite again to copy the updated visual files beside the executable.

Game screens use Graphite's Unity-like binding API rather than Gum types:

```csharp
public sealed class CounterScreen : UIScreen
{
    [UIElement] private Text _counterText = null!;
    [UIElement] private Button _incrementButton = null!;
    [UIElement] private Button _decrementButton = null!;

    protected override void Awake()
    {
        _incrementButton.Clicked += Increment;
        _decrementButton.Clicked += Decrement;
    }
}
```

The binder converts `_incrementButton` to the Gum instance name `IncrementButton`. Use `[UIElement("OtherName")]` when the field and visual names differ. Missing screens, missing controls, and incompatible control types fail immediately with a specific binding error. Gum remains confined to `Engine/UI/Gum` and the Graphite control adapters.

## Code quality

The repository uses [pre-commit](https://pre-commit.com/) to reject malformed configuration, invalid XML, formatting violations, compiler warnings, build errors, and C# control-flow statements without braces.

Install the pinned development dependency and Git hook on a new checkout:

```powershell
python -m pip install --user -r requirements-dev.txt
python -m pre_commit install --install-hooks
```

Run the complete suite manually with:

```powershell
python -m pre_commit run --all-files
```

If `dotnet format` reports a violation, apply safe automatic fixes with:

```powershell
dotnet format Graphite.csproj --no-restore --severity warn
```

## Where shaders go

The next rendering layer can live under:

```text
Engine/Rendering/
  Material.cs
  Shader.cs
  SpriteRenderer.cs
  Camera.cs

Content/Shaders/
```

For UI shaders, keep Gum as the layout/input system and add a material/effect-aware Gum rendering adapter. That avoids leaking Gum internals into gameplay code.

## Notes

This starter deliberately avoids the MonoGame Content Pipeline because it contains no compiled assets yet. When you add `.fx` shaders, fonts, spritesheets, etc., add `MonoGame.Content.Builder.Task` and an `.mgcb` file or use your preferred raw-content pipeline.
