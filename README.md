# AFTERGREEN

Project foundation using Graphite / MonoGame DesktopGL. Startup runs `BootstrapScene` → `MainMenuScene`. Gameplay has been removed for rebuilding; the play button is disabled. The design documents remain as reference.

## Run

On macOS, double-click `Play AFTERGREEN.command`. Alternatively, use `./run.sh` (macOS/Linux) or `run.cmd` (Windows). For initial setup, use `./setup.sh` or `setup.cmd`.

## Settings

`Game/Configuration/StagingSettings.cs` and `ProductionSettings.cs` inherit the shared `GameSettings` contract in `RuntimeSettings.cs`. Read values through `GameSettings.Instance`. Menu settings belong to the game layer; window, graphics, and runtime settings belong to the engine.

Debug defaults to staging; Release defaults to production. Override with `AFTERGREEN_ENVIRONMENT=staging` or `production`. Rebuild after editing settings. The main menu shows the DEMO banner only in staging.

## Theme

`Game/UI/Theming/GameThemes.cs` defines the palette. Bootstrap applies `MyraTheme` before opening the menu. Ordinary controls use the neutral grays; game colors identify game information, and status colors communicate state. Use the `secondary` label style for muted text and the `nested` panel style for nested surfaces.

All UI text uses the bundled [Abel font](https://github.com/google/fonts/tree/main/ofl/abel). Its SIL Open Font License is included in `Content/Fonts/Abel/OFL.txt`.

`GameThemes.Aftergreen.Spacing` supplies `Xs`, `Sm`, `Md`, `Lg`, and `Xl` for gaps, padding, and margins. `BorderRadius` supplies `Zero`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, and `Full`; full rounding is limited to half the shorter side. Global defaults live in `Engine/UI/Theming/UITokens.cs`; each theme can override them. `RoundedRectangleBrush` renders these values for fills and borders.

The main menu uses [Lucide](https://lucide.dev/) icons, bundled as SVG sources and 96 px PNGs in `Content/Icons/Lucide` with their license and source revision. Normal builds use the PNGs. Menu spacing and type scale with the window; the background fills it without stretching. Continue, New Game, and Load Game remain disabled until gameplay and saves are implemented.

## Checks

```sh
dotnet build --warnaserror
dotnet format Graphite.csproj --verify-no-changes --severity warn
```

Dependencies: MonoGame DesktopGL 3.8.5.1, Myra 1.6.5, and .NET 8 with newer-runtime roll-forward support.

## UI materials and animation

Wrap a Myra control before adding it to a layout. The host owns its outer position, size, alignment, margin, and visibility; keep the original control for text, events, enabled state, and focus. Material passes include the control's background, border, and children, and run in list order.

```csharp
var button = Button.CreateTextButton("BACK");
var host = new UIMaterialHost(button, [MenuPresentation.Crt], new UIInteractionStyle
{
    Bindings = new Dictionary<string, UIInteractionBinding>
    {
        [UITrigger.Hover] = new() { Animation = MenuPresentation.CrtHover, SettleSeconds = 0 },
        [UITrigger.Click] = new() { Sounds = [new UISoundCue { Asset = "Content/Audio/click.wav" }] }
    }
});
panel.Widgets.Add(host);
```

The audio path above is an authoring example; the menu has no assigned sound assets. `UI.Audio` supports volume and mute; configure an alternative service with `UI.SetAudioService` before constructing hosts.

Menu buttons apply CRT while an enabled button is hovered. Leaving or disabling the button stops it immediately. The menu background and keyboard focus alone do not activate CRT.

Register type defaults through `UI.Interactions.Set<T>()` during bootstrap. More specific types and individual bindings override inherited bindings. `UIInteractionBinding.Empty` disables an inherited binding. Custom trigger names use `host.Trigger(name)`; explicit playback uses `host.Play(animation)` and its cancellation/completion handle. Hover runs once per entry and settles on exit; set `Repeat = 0` for continuous playback. Sounds run once per playback, with their delay relative to the animation start; looping voices stop when cancelled.

`UIAnimationTrack` subclasses write additive translation/rotation, multiplicative scale/opacity, or typed material parameters through `UIAnimationFrame`. Tracks must keep playback state out of shared definitions. Material parameters start from `host.Animation.BaseParameters`; the newest active parameter track wins, and completion restores the underlying value. `UIShaderMaterial` subclasses load `.mgfxo` assets and bind typed parameters in `Configure`. Custom `UIMaterialInstance` implementations can draw textures or implement other passes. Set `OverflowPadding` on material hosts where decorations extend beyond the content; ancestor clipping still applies.

Use `host.Show()`, `host.Hide()`, and `UI.Close(screen)` for transitions. Hide animations must be finite. Direct visibility changes are immediate and cancel playback; ancestor visibility does not alter child visibility. Disposal and scene teardown are immediate. Use `UI.Post` when a task continuation needs to change widgets on the game thread. Myra input processing is isolated in `MyraInput` because version 1.6.5 exposes visual rendering separately but keeps event dispatch internal; verify that adapter when upgrading Myra.

### Rebuilding shaders

Shader source and compiled DesktopGL bytecode live together in `Content/Shaders`. Normal builds use committed bytecode and need no Wine installation. After changing a shader, run `./Scripts/build-shaders.sh` or `./Scripts/build-shaders.ps1`, and commit both files. The tool manifest pins MGFXC to 3.8.5.1.

On macOS/Linux, first configure Wine and `MGFXC_WINE_PATH` using [MonoGame's shader compiler setup](https://docs.monogame.net/errors/mgfx0001/index.html). Windows does not require Wine. The rebuild command restores the local compiler and uses the OpenGL profile.

### UI verification

```sh
dotnet run --project Tests/Graphite.UI.Tests/Graphite.UI.Tests.csproj
dotnet run --project Tests/Graphite.UI.Tests/Graphite.UI.Tests.csproj -- --graphics
```

The first command checks animation composition, cancellation, defaults, and audio timing without a window. The second also exercises the native UI, shaders, focus, transitions, resizing, and resource cleanup; it writes captures under `.artifacts/ui-checks`.
