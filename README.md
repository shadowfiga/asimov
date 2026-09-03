# Graphite

A small Unity-like, code-first 2D engine and game starter built on MonoGame DesktopGL and Myra.

Pinned dependencies:

- MonoGame.Framework.DesktopGL **3.8.5.1**
- Myra **1.6.5**
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

The setup script uses the project-local .NET 8 SDK or a compatible system installation, restores MonoGame and Myra, builds the game, and launches it. If necessary, it installs .NET 8 locally under `.dotnet` without requiring administrator access.

After the first setup, use `run.cmd` or `./run.sh`.

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
  }
}
```

`game.icon` accepts an image path relative to `settings.json`; PNG is recommended. Set it to `null` to use the platform default. `game.startupScene` is a class path relative to the game's namespace, so `Scenes/BootstrapScene` resolves to `Graphite.Game.Scenes.BootstrapScene`. Forward slashes, backslashes, and dots are accepted. Invalid or missing settings fail at startup with a specific error.

## Architecture

```text
Engine/
  Configuration/GraphiteSettings.cs
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
    UI.cs
    UIScope.cs
    UIScreen.cs
  Platform/WindowIcon.cs

Game/
  Scenes/
    BootstrapScene.cs
    MainMenuScene.cs
    CounterScene.cs
  UI/
    MainMenuScreen.cs
    CounterScreen.cs
```

The intended vocabulary is:

```text
Scene -> Thing -> Component -> Behaviour
SceneManager.Load<T>()
Scene.UI.Open<TScreen>()
```

A `Thing` is Graphite's equivalent of Unity's `GameObject`:

```csharp
var player = Create("Player");
player.Transform.Position = new(200, 100);
player.Add<PlayerController>();
```

Scene transitions are queued and committed after update, so loading a scene from inside a behaviour does not invalidate the current update traversal.

## Code-first Myra UI

Graphite owns one Myra `Desktop`. Each `UIScreen` builds a Myra widget tree and Graphite attaches it when the screen opens. Scene unload closes every screen in its `UIScope`, detaches its widget tree, calls `OnDestroy`, and releases the screen automatically.

There is no XML layout, reflection binder, generated UI code, or `[UIElement]` field. Use Myra controls directly:

```csharp
using Graphite.Engine.UI;
using Myra.Events;
using Myra.Graphics2D.UI;

public sealed class CounterScreen : UIScreen
{
    private Label _counterText = null!;
    private Button _incrementButton = null!;

    protected override Widget Build()
    {
        _counterText = new Label { Text = "Count: 0" };
        _incrementButton = Button.CreateTextButton("+ Increment");

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };
        content.Widgets.Add(_counterText);
        content.Widgets.Add(_incrementButton);
        return content;
    }

    protected override void Awake()
    {
        _incrementButton.Click += Increment;
    }

    protected override void OnDestroy()
    {
        _incrementButton.Click -= Increment;
    }

    private void Increment(object sender, MyraEventArgs args)
    {
        _counterText.Text = "Clicked";
    }
}
```

Myra supplies stack panels, grids, windows, dialogs, lists, inputs, menus, tabs, sliders, a property grid, stylesheets, and optional MML markup. Graphite deliberately uses the direct C# widget API so UI stays code-first.

The former Gum project and helper scripts are retained under `Legacy/` only as recoverable reference material. They are outside `Content`, are not packaged, and have no runtime dependency.

## Scene ergonomics

```csharp
SceneManager.Load<CounterScene>();
SceneManager.Load<PauseScene>(SceneLoadMode.Additive);
SceneManager.Reload();
SceneManager.Unload<PauseScene>();
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

## Code quality

The repository uses pre-commit to reject malformed configuration, formatting violations, compiler warnings, build errors, and C# control-flow statements without braces.

```powershell
python -m pip install --user -r requirements-dev.txt
python -m pre_commit install --install-hooks
python -m pre_commit run --all-files
```

If `dotnet format` reports a violation, apply safe automatic fixes with:

```powershell
dotnet format Graphite.csproj --no-restore --severity warn
```

## Rendering and content

The next rendering layer can live under:

```text
Engine/Rendering/
  Material.cs
  Shader.cs
  SpriteRenderer.cs
  Camera.cs

Content/Shaders/
```

This starter does not yet use the MonoGame Content Pipeline for authored game assets. When adding `.fx` shaders, fonts, spritesheets, or other processed assets, add `MonoGame.Content.Builder.Task` and an `.mgcb` file or use the preferred raw-content pipeline.
