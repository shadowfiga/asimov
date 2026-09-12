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

The main menu uses [Lucide](https://lucide.dev/) icons, bundled as SVG sources and 96 px PNGs in `Content/Icons/Lucide` with their license and source revision. Normal builds use the PNGs. Menu spacing and type scale with the window; the background fills it without stretching. Continue, New Game, and Load Game remain disabled while gameplay is being rebuilt.

## Persistence

`Engine/Persistence` provides `SaveSerializer`, `SaveStore`, and typed `Preferences`. Game state belongs in `Game/Sessions` (`Graphite.Game.Sessions`); `SessionManager` replaces the old `GameManager`. A session contains data and stable entity/asset IDs. Runtime scenes, graphics resources, services, and events stay outside saved state.

Mark each data class/struct with `[SaveContract("stable.id", Version = 1)]` and each persisted field/property with `[SaveMember("stableName")]`. Public/private instance members are supported; properties need getters and setters, and fields must be writable. Unmarked members are ignored. Classes need a public or private parameterless constructor. Keep saved names stable when renaming C# symbols. `ISaveValidatable.Validate()` runs before serialization and after loading; it should only inspect data and throw `InvalidDataException` for invalid state.

The serializer supports primitives, enums, strings, GUIDs, date/time values, nested contracts, one-dimensional arrays, lists, sets, and string-keyed dictionaries (including common list/dictionary interfaces). Collections initialize empty; missing fields keep their defaults and explicit null collections load as empty. Polymorphic objects, reference identity, cycles, and unsupported member types fail explicitly. Supply `JsonConverter` instances to `SaveSerializer` for additional value types, such as game-specific coordinates. The current implementation uses cached reflection contracts, not an AOT source generator.

```csharp
var sessions = SessionManager.Instance;
var session = sessions.StartNew("Coast");
session.AddPlayTime(30);
var saved = await sessions.SaveAsync();
if (saved.IsSuccess)
{
    var loaded = await sessions.LoadAsync(saved.Value!.Id);
}

// The engine also accepts any contracted data object.
var result = await GamePersistence.Saves.LoadAsync<Session>(slotId);
var slots = await GamePersistence.Saves.ListSlotsAsync<Session>();
```

Mutate session data and initiate saves at the game update boundary. `SaveAsync` serializes immediately before returning its task, so queued file writes cannot observe later changes to the live session. A validated loaded session replaces the active data reference; failures leave the current session intact. Starting/ending a session or requesting another load invalidates an older pending load. Apply scene changes after awaiting a successful load on the game thread.

Storage uses `Environment.SpecialFolder.LocalApplicationData/aftergreen/{staging|production}`. Slots live under `saves/<guid>.json`; preferences live in `preferences.json`. The game ID and environment determine the location, independently of C# namespaces and the install directory. `SaveSlotInfo` holds metadata, compatibility status, and backup-recovery state. Slot listing currently validates each payload; filesystem errors while listing throw. Save/load/delete return success, missing, corrupt, incompatible, or I/O error results. Cancellation throws `OperationCanceledException`; contract/programming errors throw directly.

Each document includes its format version, contract/schema version, timestamps, and a data checksum. Writes use a unique temporary file, flush it, preserve the previous valid file as `.bak`, then replace the primary on the same filesystem. Locks serialize competing writers; failures clean temporary files and preserve the current save. Load can recover a missing/corrupt primary from its backup, but never silently downgrades a newer-format save. Cancellation is honored before commit; a committed write returns success. Files are limited to 64 MiB. Locks leave small `.lock` files; stale `.tmp` files left by a process crash are ignored.

Add game-owned migrations to `Game/Persistence/SessionMigrations.cs` whenever the session version increases:

```csharp
new SaveMigration("aftergreen.session", 1, data =>
{
    data["newName"] = data["oldName"]?.DeepClone();
    data.Remove("oldName");
    return data;
})
```

A migration converts its `FromVersion` to the next version. Loading runs the chain in memory, then deserializes and validates; it never rewrites the file. Missing migrations/newer versions are incompatible, and failed migrations leave files unchanged. Version 1 currently has no migrations.

Preferences support `bool`, `int`, `long`, `float`, `double`, and `string`, with typed defaults and optional validation. Declare game keys in `Game/Preferences/PlayerPreferences.cs`. Unknown or invalid stored values use the key's default; conflicting key types within an instance are rejected. Updates remain in memory until flushed, merge with unrelated on-disk keys, and preserve changes made during an in-flight write.

```csharp
GamePersistence.Preferences.Set(PlayerPreferences.MasterVolume, .7f);
PlayerPreferences.ApplyAudio();
await GamePersistence.Preferences.FlushAsync();
// Remove(key) restores its default; the removal persists on the next flush.
```

Bootstrap loads preferences and discovers slots before opening the menu, then applies audio preferences. Player preferences are shared across slots; the staging/production C# settings continue to define application configuration. No gameplay autosave triggers or save-slot UI have been added yet.

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
