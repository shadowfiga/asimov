# Graphite

A small code-first 2D engine and game starter built on **MonoGame DesktopGL + Gum**.

Pinned dependencies:

- MonoGame.Framework.DesktopGL **3.8.5.1**
- Gum.MonoGame **2026.8.3.1**
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
    "icon": "Content/icon.png"
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

`game.icon` accepts an image path relative to `settings.json`; PNG is recommended. Set it to `null` to use the platform default. Invalid or missing settings fail at startup with a specific error instead of silently falling back. Run the project again after changing settings so the file is copied beside the executable.

## Architecture

```text
Engine/
  Configuration/
    GraphiteSettings.cs
  Core/
    GameHost.cs
    Entity.cs
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
    Gum/
      GumScreen.cs
  Platform/
    WindowIcon.cs

Game/
  Scenes/
    CounterScene.cs
  UI/
    CounterScreen.cs

Content/
  GumProject/        <- put the Gum visual-editor project here
```

The intended vocabulary is:

```text
Scene -> Entity -> Component -> Behaviour
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

A scene creates entities like this:

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

The starter UI is intentionally created through **Gum Forms in code**, because that makes the zip immediately runnable without separately installing the editor.

Gum itself supports a WYSIWYG project workflow. To switch the demo to visual authoring:

1. Install/open the Gum UI editor.
2. Create a project under `Content/GumProject/`, e.g. `GameUI.gumx`.
3. In Gum choose **Content -> Add Forms Components**.
4. Create your screens/components visually.
5. Change `GameHost` initialization from:

```csharp
GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
```

to:

```csharp
GumService.Default.Initialize(this, "GumProject/GameUI.gumx");
```

6. Load the visual screen from the engine's Gum adapter. Gum also supports code generation for strongly-typed screen/component classes, which is the eventual recommended direction for this scaffold.

Because the rest of the game only sees `UIScreen`, `UIScope`, scenes, and game classes, the Gum-specific details stay confined to `Engine/UI/Gum`.

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
