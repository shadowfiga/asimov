# DEEP // DRIVE

Mining-defense roguelite foundation using Graphite / MonoGame DesktopGL. Startup runs `BootstrapScene` → `MainMenuScene`. Gameplay is being rebuilt around the prototype scope, so operation buttons remain disabled while Settings is functional. The authoritative product scope is [DEEP DRIVE — Game Design Document.md](<DEEP DRIVE — Game Design Document.md>).

## Run

On macOS, double-click `Play DEEP DRIVE.command`. Alternatively, use `./run.sh` (macOS/Linux) or `run.cmd` (Windows). For initial setup, use `./setup.sh` or `setup.cmd`.

## Settings

`Game/Configuration/StagingSettings.cs` and `ProductionSettings.cs` inherit the shared `GameSettings` contract in `RuntimeSettings.cs`. Read values through `GameSettings.Instance`. Menu settings belong to the game layer; window, graphics, and runtime settings belong to the engine.

Debug defaults to staging; Release defaults to production. Override with `DEEP_DRIVE_ENVIRONMENT=staging` or `production`. Rebuild after editing settings. The main menu shows the STAGING badge only in staging.

## Theme

`Game/UI/Theming/GameThemes.cs` defines the monochrome industrial-terminal palette. `Startup.Initialize` applies `MyraTheme` before the first scene is constructed. Ordinary UI uses black, charcoal, gray, and off-white. Orange is reserved for selection, focus, important action, and immediate attention. Green is reserved for successful, completed, purchased, valid, or confirmed state. Use the `secondary` label style for muted text and the `nested` panel style for nested surfaces.

All UI text uses the bundled [Abel font](https://github.com/google/fonts/tree/main/ofl/abel). Its SIL Open Font License is included in `Content/Fonts/Abel/OFL.txt`.

`GameThemes.DeepDrive.Spacing` supplies `Xs`, `Sm`, `Md`, `Lg`, and `Xl` for gaps, padding, and margins. `BorderRadius` supplies `Zero`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, and `Full`; full rounding is limited to half the shorter side. Global defaults live in `Engine/UI/Theming/UITokens.cs`; each theme can override them. `RoundedRectangleBrush` renders these values for fills and borders.

The main menu uses [Lucide](https://lucide.dev/) icons, bundled as SVG sources and 96 px PNGs in `Content/Icons/Lucide` with their license and source revision. Normal builds use the PNGs. Menu spacing and type scale with the window. Continue, New Operation, and Load Operation remain disabled while gameplay is being rebuilt.

Settings expose only one fullscreen CRT progress-slider from `NO` (0%) to a more pronounced `FULL` (100%). Zero disables processing and every non-zero value enables it; no separate checkbox or boolean is stored. The only treatment is the Aged profile with horizontal scanlines. There is no direction selector or screen curvature. Menu buttons use ordinary Myra hover/focus states rather than per-button shader materials.

## Persistence

Use the static engine APIs from any scene. `GameHost` initializes storage and loads preferences before calling `Startup.Initialize` and constructing the first scene. The game startup callback installs its theme, interaction styles, and audio preferences; bootstrap then opens the main menu.

```csharp
using Graphite.Engine.Persistence;

Storage.Save(session);
Session restored = Storage.Load<Session>();

// Multiple saves use explicit slot IDs; omitting one uses the default slot.
var slotId = Guid.NewGuid();
Storage.Save(session, slotId, name: "Coast");
Session expedition = Storage.Load<Session>(slotId);
var slots = Storage.ListSlots<Session>();
Storage.Delete(slotId);

Preferences.Set("audio.masterVolume", .7f);
float volume = Preferences.Get("audio.masterVolume", 1f);
Preferences.Remove<float>("audio.masterVolume");

// Reusable typed keys add a shared default and optional validation.
Preferences.Set(PlayerPreferences.MasterVolume, .7f);
volume = Preferences.Get(PlayerPreferences.MasterVolume);
Preferences.Remove(PlayerPreferences.MasterVolume);
```

Save takes one data object and serializes it internally. Load returns a new, fully typed object. No game persistence manager or session coordinator is needed. Call these synchronous APIs on the game thread; file operations finish before returning. They have no background queue, gates, or cross-process locks. Set/Remove persist immediately, and a failed write leaves both the cached preferences and the current file unchanged. Large saves can block a frame; concurrent writers are outside this API's contract.

`Load<T>` throws for missing, invalid, incompatible, or unreadable files. Use `Storage.TryLoad<T>(slotId)` when you need a `SaveResult<T>` with a distinct status (`Success`, `NotFound`, `Corrupt`, `Incompatible`, `IoError`) and backup-recovery information. Save/Delete throw on failure. Slot listing includes metadata and compatibility status, validates each payload, and throws on directory I/O errors. Contract/programming errors always throw. Persistence remains available during scene teardown and is cleared when the host is disposed.

Mark each data class/struct with `[SaveContract("stable.id", Version = 1)]` and each persisted field/property with `[SaveMember("stableName")]`. Public/private instance members are supported; properties need getters and setters, and fields must be writable. Unmarked members are ignored. Classes need a public or private parameterless constructor. Keep saved names stable when renaming C# symbols. `ISaveValidatable.Validate()` runs before serialization and after loading; it should only inspect data and throw `InvalidDataException` for invalid state.

`SaveSerializer` supports primitives, enums, strings, GUIDs, date/time values, nested contracts, one-dimensional arrays, lists, sets, and string-keyed dictionaries (including common list/dictionary interfaces). Collections initialize empty; missing fields keep their defaults and explicit null collections load as empty. Polymorphic objects, reference identity, cycles, and unsupported member types fail explicitly. Standalone serializers accept custom `JsonConverter` instances. Session data lives in `Game/Sessions`; runtime scenes, graphics resources, services, and events stay outside saved state.

Storage uses `Environment.SpecialFolder.LocalApplicationData/<Game.Id>/{staging|production}`. Slots live under `saves/<guid>.json` or `saves/default.json`; preferences live in `preferences.json`. Both C# settings files define the stable game ID. Existing GUID slot files and preference files remain compatible.

Each document includes format and schema versions, timestamps, and a data checksum. Writes flush a temporary file, preserve the previous valid file as `.bak`, then replace the primary on the same filesystem. Load recovers a missing/corrupt primary from its backup, and never downgrades a newer-format save. Files are limited to 64 MiB; stale temporary files left by a process crash are ignored.

Register game migrations in `Startup.Initialize` before any scene loads:

```csharp
Storage.RegisterMigration(new SaveMigration("deep-drive.campaign", 1, data =>
{
    data["newName"] = data["oldName"]?.DeepClone();
    data.Remove("oldName");
    return data;
}));
```

Each migration advances one schema version. Loading runs the chain in memory before deserialization and validation; it never rewrites the file. Missing migrations/newer versions are incompatible. Version 1 currently has no migrations.

Preferences support `bool`, `int`, `long`, `float`, `double`, and `string`. `Get<T>(key)` uses the type's default (`""` for strings); pass a fallback or a `PreferenceKey<T>` for another default. Stored types must match Get/Set/Remove; mismatches throw rather than convert or overwrite values. Game keys live in `Game/Configuration/PlayerPreferences.cs`. Player preferences are shared across slots. No gameplay autosave triggers or save-slot UI have been added yet.

## Checks

```sh
dotnet build --warnaserror
dotnet format Graphite.csproj --verify-no-changes --severity warn
dotnet run --project Tests/Graphite.Persistence.Tests/Graphite.Persistence.Tests.csproj -p:TreatWarningsAsErrors=true
```

Dependencies: MonoGame DesktopGL 3.8.5.1, Myra 1.6.5, and .NET 8 with newer-runtime roll-forward support.

## UI materials and animation

Wrap a Myra control before adding it to a layout. The host owns its outer position, size, alignment, margin, and visibility; keep the original control for text, events, enabled state, and focus. Material passes include the control's background, border, and children, and run in list order.

Material captures preserve the back buffer while compositing, then restore its original usage policy. When drawing UI into a custom `RenderTarget2D`, create that destination with `RenderTargetUsage.PreserveContents` so nested material passes retain previously drawn content.

```csharp
var screenRoot = new Panel(styleName: "root");
var host = new UIMaterialHost(screenRoot, [MenuPresentation.Crt], MenuPresentation.FadeStyle);
CrtMaterial.Configure(host.Animation.BaseParameters, intensity: 1f);
```

The menu has no assigned sound assets. `UI.Audio` supports volume and mute; configure an alternative service with `UI.SetAudioService` before constructing hosts.

Apply CRT materials to fullscreen screen roots, not individual controls. Buttons use standard background, border, text, and icon states for hover, focus, press, and disabled feedback.

Register type defaults through `UI.Interactions.Set<T>()` in `Startup.Initialize`. More specific types and individual bindings override inherited bindings. `UIInteractionBinding.Empty` disables an inherited binding. Custom trigger names use `host.Trigger(name)`; explicit playback uses `host.Play(animation)` and its cancellation/completion handle. Hover runs once per entry and settles on exit; set `Repeat = 0` for continuous playback. Sounds run once per playback, with their delay relative to the animation start; looping voices stop when cancelled.

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
