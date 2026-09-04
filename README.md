# Black Company!

A barebones playable prototype of the corporate incremental game from the design document, built on the Graphite Unity-like, code-first 2D engine with MonoGame DesktopGL and Myra.

The current prototype contains one 60-second contract-board quarter:

- Evaluate and accept opportunities across three Contract Slots.
- Allocate 12 workers between active contracts, HR, and Legal.
- Complete contracts to reach a $50,000 Profit Target.
- Toggle BLACK! independently on each contract for 2.5x work speed at escalating risk.
- Select one of four Management Cards and play it onto a compatible active contract.
- Continue earning excess Profit after reaching the target, or lose immediately at 100% Bankruptcy.
- Retry after the fiscal timer expires or the company goes Bankrupt.

`GameManager.CurrentSession` is the in-memory data for the prototype slot. There is intentionally no saving or loading implementation yet; closing the game discards the session.

Immutable rules, contracts, cards, and semantic input bindings are authored in the `.chisel` project and exported by Chisel's MonoGame target. Mutable contracts, hands, worker assignments, timers, Profit, and Bankruptcy remain runtime session state.

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
    "name": "Black Company!",
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
    "clearColor": "#111111",
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
    Singleton.cs
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
  Content/
    PrototypeRules.cs
    ContractDefinition.cs
    ManagementCardDefinition.cs
  Scenes/
    BootstrapScene.cs
    MainMenuScene.cs
    ContractBoardScene.cs
  Scripts/
    GameManager.cs
    Session.cs
    ContractSlot.cs
    QuarterController.cs
  UI/
    MainMenuScreen.cs
    ContractBoardScreen.cs

.chisel/
  chisel.json
  tables/user/
  tables/system/input_bindings.json

GameData/Generated/
  ChiselManifest.g.cs
  ChiselContracts.g.cs
  ChiselManagementCards.g.cs
  ChiselGameRules.g.cs
  ChiselInput.g.cs
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

public sealed class StatusScreen : UIScreen
{
    private Label _status = null!;

    protected override Widget Build()
    {
        _status = new Label
        {
            Text = "Quarter ready"
        };
        return _status;
    }
}
```

Myra supplies stack panels, grids, windows, dialogs, lists, inputs, menus, tabs, sliders, a property grid, stylesheets, and optional MML markup. Graphite deliberately uses the direct C# widget API so UI stays code-first.

## Chisel-authored game data

Open the Graphite project directory in Chisel. Edit the `Game Rules`, `Contracts`, `Management Cards`, or `Input Bindings` tables, commit the source state inside Chisel, then choose the MonoGame export target. The generated C# under `GameData/Generated` compiles automatically and must not be edited by hand.

The runtime validates required Chisel rows and supported enum values as they are loaded. Missing or unsupported authored data fails immediately instead of silently falling back to hardcoded content.

## Scene ergonomics

```csharp
SceneManager.Load<ContractBoardScene>();
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
