# GIRLS & MINING

Mining-defense roguelite foundation using Graphite / MonoGame DesktopGL. Startup runs `BootstrapScene` → `MainMenuScene`. Play prepares a fresh in-memory session through `SessionLoadingScene`, then enters the top-down mech prototype in `SandboxScene`. Escape returns to the menu. Settings is functional. The authoritative product scope is [GIRLS & MINING — Game Design Document.md](<GIRLS & MINING — Game Design Document.md>).

## Run

On macOS, double-click `Play DEEP DRIVE.command` (legacy launcher filename). Alternatively, use `./run.sh` (macOS/Linux) or `run.cmd` (Windows). For initial setup, use `./setup.sh` or `setup.cmd`.

Selected licensed audio is managed by Chisel and stored through Git LFS. After cloning, ensure the LFS objects are present with `git lfs pull`. To replace the selections from the owned vendor archives, place both ZIPs in one directory and run:

```sh
python Scripts/import_audio.py "/path/to/archive-directory"
```

The importer adds or replaces the eight `AUDIO` assets through Chisel, commits a source snapshot, and runs the MonoGame export. See [audio asset selections and provenance](Content/Audio/README.md).

## Mech prototype

Player mechs are one feature domain in `Game/Domain/Player`: loadout, input/controller, mech prefab, mech-part rendering, and reticle rendering live together. Only players use mechs; there is no separate `Mechs` domain or controller layer. Shared aiming, weapons, and projectiles remain in `Game/Domain/Combat`.

`Session` owns its `CurrentRun`. After preload succeeds, `SessionLoadingScene` calls `StartRun()` before publishing the session and entering `SandboxScene`. Direct gameplay entry without a run fails fast; Sandbox never creates one. `StartRun()` explicitly replaces the run with zeroed Ore, XP and kills, `TimeSpan.Zero` duration, and an empty modifiers array, without resetting the session's loadout or cleared sectors. Scenes receive float `dt` in seconds; Sandbox calls `Run.AddTime(TimeSpan.FromSeconds(dt))` while active, before object-simulation clamping. The run owns its duration, with no scene timing state. `Run.Duration` saves directly under `duration` using the serializer's built-in TimeSpan support, preserving the accumulated ticks without a custom converter or fractional accumulator. Enemy deaths and mining are not implemented yet.

`Run.Ore` is a plain public integer, saved directly as `ore` and initialized to zero. Use `Run.AddOre(amount)` and `Run.TrySpendOre(amount)` for gameplay changes; they notify the existing Ore-only `ResourceHud` through `Run.OreChanged`. Unaffordable purchases return false without changing the balance. `SandboxUI` owns the HUD and scene teardown unsubscribes it. Layout, fonts, and icon sizing come from the shared theme and global UI scaling. There is no resource container, Gold/Credits balance or simulated income. `Run.RecordKill(ChiselEnemiesId enemyId)` reads `ChiselEnemies.Experience[enemyId.ToInt()]`, increments kills and awards run XP without granting Ore. Invalid IDs throw at the lookup before counters change; there is no raw-XP overload or copied enemy definition. Enemy-death integration will call it once per kill when enemies and damage are implemented.

The GDD now assigns run XP to survivor-style upgrade card choices and Ore to ordinary research/incremental progression, alongside its construction role. Level-ups, cards, permanent Ore banking and research purchases are still planned. A single rare resource (Research Cores, Xenonite or Animerium) for prestige and/or better mechs is a design candidate, not an implemented currency or a finalized reset/mech system.

`SandboxUI` lays out the compact Ore readout at top-right, XP and a ten-minute countdown at top-center, the named minimap at bottom-left, and pilot/HP at bottom-right. Map geometry, the pilot portrait icon and full health are presentation stubs. The XP total reads the run, but its progress bar stays empty until level thresholds exist. The countdown stops at `00:00` without ending the sandbox; it pauses when the game loses focus. There are no waves, objectives, fuel or ammo widgets. HUD dimensions come from existing theme tokens, with no new classes or per-screen resize handling. The graphics tests capture `.artifacts/ui-checks/sandbox-hud-final.png` at 1440p, including the actual screen filter; its Ore and XP amounts are test fixtures, not starting balances.

WASD or arrow keys move the mech; the legs face the movement direction. Their placeholder pose is static; frame-based animation will be added later. The torso faces the mouse and each arm gun aims independently at the reticle, with a mirrored 15° maximum inward rotation relative to the torso. Close targets clamp the barrels and projectile directions at that limit; distant targets still converge normally. Outward aiming and torso rotation remain unrestricted. Configure `MechPrefab.MaxInwardAngle` in radians when constructing the prefab; the existing `AimController` enforces its parent-relative joint limits on attachment and each update. Hold left mouse to fire both guns. Diagonal movement is normalized. The camera follows the mech, and aiming uses world coordinates independently of the UI scale. Losing window focus pauses the simulation and requires releasing the fire button before shooting again.

`Objects.Spawn(new MechPrefab(SessionManager.ActiveSession.CurrentLoadout), position)` assembles a real object hierarchy: Mech → Bottom and Top → LeftWeapon/RightWeapon → Muzzle. `MechPrefab` takes a `Loadout`, captures its chassis/pilot/left-weapon/right-weapon IDs, and reads Chisel columns directly. `PlayerController` retains the selected pilot ID and supplies movement, aim targets and trigger intent; pilots are presentation-only, not a second source of combat stats. `AimController` turns parts using world-space targets; each `WeaponComponent` owns independent fire cadence and reads weapon data directly through its Chisel weapon ID. Bullets use `ProjectilePrefab` to create independent scene roots, so they outlive their shooter and never follow its transform. `MechPartRenderer` supplies the static placeholder art; no procedural walking or muzzle flash is enabled.

Every `Scene` owns `Objects` (`GameWorld`). New roots enter through `Spawn(Prefab<T>, position, rotation)`; raw root creation is private to the engine. Prefabs receive a positioned root, build its children/components, and return a typed instance handle. The engine automatically updates, renders and destroys its objects/components, including children, and rolls back new objects if construction fails. `SandboxScene` spawns mech, player camera and presentation prefabs, reads input, and updates its audio listener/HUD. `PlayerCameraPrefab` creates an independent root with the engine's `CameraComponent`, following player position without inheriting mech rotation or scale. The component and scene backend own following and reactive viewport sizing, preserving 1600×900 reference framing independently of UI scale. `Scene.Camera` resolves the active camera object's projection, with no scene-owned fallback; UI-only scenes need no camera. The reusable `Prefab<T>`, `GameObject`, `Component`, `Transform2D`, `CameraComponent`, `SpriteRenderer` and `FrameAnimator` live in the engine; mech/combat behavior remains in `Game`. See [prefab, object and component usage](Engine/Objects/README.md). Sprite-sheet playback is available, but the mech keeps its static geometry until frame art is provided.

Chisel owns the input bindings and the **Chassis**, **Weapons**, **Pilot**, and **Enemies** definitions under `.chisel`. Chassis no longer select a weapon; the runtime loadout selects each arm independently. Tune the starter mech's speed and dimensions, or its autocannon's fire rate, projectile speed/lifetime and barrel length there. Commit the data in Chisel, then export MonoGame to this project. `GameData/Generated` is generated source, not a place to hand-edit settings. Import `Graphite.Game.Data` and access the unchanged generated arrays with `ChiselChassis.MoveSpeed[id.ToInt()]`. The shared game-side `ChiselIdExtensions` converts any generated ID enum directly; invalid indexes fail at column access through normal array bounds checking, without repeated enum validation or fallback IDs. It requires no Chisel exporter changes. The committed content snapshot is included. The `Pilot` table (`.chisel/tables/user/pilots.json`) exports `ChiselPilotId`/`ChiselPilot`; its starter row has `display_name` and an optional `UI` portrait reference, ready for additional presentation fields. The `Enemies` table (`.chisel/tables/user/enemies.json`) exports `ChiselEnemiesId`/`ChiselEnemies`; its initial `SWARMER` row has an `experience` reward of **10 XP** as provisional tuning, ready for more enemy definitions. With the sibling Chisel checkout and its dependencies installed, the same exporter can also be run from this folder:

```sh
bun Scripts/export-chisel.ts
bun Scripts/export-chisel.ts --check
```

Normal builds use the checked-in C# exports and do not require Bun or Chisel. Mech state is currently scene-local: there are no enemies, hit detection, damage, terrain collision, weapon sounds or saved mech positions yet.

## Settings

`Game/Configuration/StagingSettings.cs` and `ProductionSettings.cs` inherit the shared `GameSettings` contract in `RuntimeSettings.cs`. Read values through `GameSettings.Instance`. Menu settings belong to the game layer; window, graphics, and runtime settings belong to the engine.

`GameSettings.DisplayName` defines the shared window/menu title, **GIRLS & MINING**. The internal `deep-drive` storage ID, `deep-drive.campaign` save contract, `DEEP_DRIVE_ENVIRONMENT` override, and `GameThemes.DeepDrive` theme identifier remain stable; the name change does not relocate existing settings or saves.

Debug defaults to staging; Release defaults to production. Override with `DEEP_DRIVE_ENVIRONMENT=staging` or `production`. Rebuild after editing settings. A host-owned `EnvironmentOverlay` prints the active environment (STAGING or PRODUCTION) in the bottom-left corner across all scenes and dialogs. It is drawn directly after the scene/UI and CRT pass, not as a UI widget: its 18px Abel text and 16px margin stay fixed when the window or UI scale changes.

Staging defaults to **2560×1440 borderless fullscreen**; production retains **1280×720 windowed**. Borderless fills the current desktop at its native resolution (1440p on a 1440p desktop); it does not change the monitor's mode. Confirmed player settings override these environment defaults on subsequent launches.

Settings uses a wide modal with sidebar navigation and one active page: **Video** (display mode and resolution), **Audio** (master, music, FX, ambience and UI volume sliders), and **Accessibility** (UI scale and CRT intensity). There is no Mute setting; zero master volume silences every channel. There are no placeholder Gameplay/Controls pages or unsupported switches. Settings apply and persist live; the footer only needs Back. The page area scrolls when necessary while the header and Back button stay visible.

Video offers Windowed, Borderless Fullscreen, and Fullscreen modes plus a resolution dropdown. Windowed resolutions fit the desktop; exclusive fullscreen lists the graphics adapter's supported modes. Borderless displays its desktop resolution with the selector disabled. Mode/resolution changes are previewed immediately, then require **Keep** within 15 seconds or automatically revert; the red **Cancel** button restores the prior configuration immediately. Both confirmation buttons are text-only. Escape/Back first reverts an open display confirmation, otherwise it closes Settings.

`Game/Scenes/MainMenu/MainMenuScene.cs` owns `MainMenuUI.cs`; the menu only builds its own content and opens independently scoped dialogs. `CreditsDialog.cs` is alongside the menu. The reusable `Game/UI/Settings/SettingsDialog.cs` lives outside the scene, with separate Video, Audio, Accessibility, and display-confirmation files. Any scene can open it with its `UI.Open<SettingsDialog>()` scope. Dialogs own their preference bindings and unsubscribe/release their resources on close; the menu closes any dialogs it opened when its scene is destroyed.

Display configuration is engine-wide, independent of scenes:

```csharp
DisplaySettings.Preview(WindowMode.Windowed, new Point(1920, 1080));
DisplaySettings.KeepChanges(); // After the preview is applied and the player confirms
DisplaySettings.Revert();      // Cancel a preview
Preferences.Remove(RuntimePreferences.Display); // Restore environment defaults live
```

`DisplaySettings` lives in `Graphite.Engine.Graphics`. The host applies queued previews before UI input, exposes `Current`, `Resolutions(mode)`, `NeedsConfirmation`, `SecondsRemaining`, and `Changed`, and restores confirmed settings before the first scene. `RuntimePreferences.Display` stores mode and resolution together as one validated preference. UI layout and whole-game CRT targets follow the actual backbuffer size.

UI scaling is an engine-wide runtime preference, not a main-menu setting:

```csharp
Preferences.Set(RuntimePreferences.UiScale, 1.25f); // 125%, saved immediately
Preferences.Remove(RuntimePreferences.UiScale);  // Restore 100%
```

`RuntimePreferences` lives in `Graphite.Engine.Persistence`. `UiScale` accepts exactly 0.75, 1.00, 1.25, 1.50, and 1.75, and defaults to 1. `UiScaleOptions` defines the shared presets for validation and the settings dropdown. The shared Myra desktop automatically scales every screen, dialog, font, icon, control, and pointer coordinate. Its effective scale is `min(windowWidth / 1600, windowHeight / 900) * UiScale`. Changes apply before the next UI layout/input/render pass, including already-open dialogs; new scenes inherit the preference, and startup restores it before constructing the first scene. Fullscreen/window resizing uses the same path. CRT remains an independent physical-screen filter.

Declare widget dimensions, alignment, padding, and scrolling in logical units once. The UI backend's parent-relative measure/arrange pass constrains those dimensions automatically; screens must not calculate widths from `UI.LayoutSize`, multiply by `UI.Scale`, or subscribe to resize events for ordinary layout. Shared dialogs center their child within themed safe padding, including material-wrapped content. `AdaptiveGrid.ColumnRules` declares parent-width breakpoints for layouts such as the Settings sidebar. `UIScreen.OnLayoutChanged()` is an optional, lifecycle-managed escape hatch for genuinely specialized reflow; none of the current screens needs it. `UI.Scale` remains the physical-pixels-per-unit conversion for screen-space integrations.

The menu scrolls when magnification makes it taller than the viewport. Settings includes a dropdown from 0.75× to 1.75× in 0.25 increments; choosing a preset applies and saves the global scale immediately. Older saved values clamp to the new limits and snap to the nearest preset globally before layout. Settings-page bindings use `UI.PreferencesChanged` to refresh values independently of resizing.

## Theme

`Game/UI/Theming/GameThemes.cs` defines the monochrome industrial-terminal palette. `Startup.Initialize` applies `MyraTheme` before the first scene is constructed. Ordinary UI uses black, charcoal, gray, and off-white. Orange is reserved for selection, focus, important action, and immediate attention. Green is reserved for successful, completed, purchased, valid, or confirmed state. Use the `secondary` label style for muted text and the `nested` panel style for nested surfaces.

All UI text uses the bundled [Abel font](https://github.com/google/fonts/tree/main/ofl/abel). Its SIL Open Font License is included in `Content/Fonts/Abel/OFL.txt`.

Font rasterization follows the effective screen scale, independently of logical font sizes. `ThemeAssets` caches whole-resolution glyph atlases (1× for small text, 3× at 1440p/175%, 5× at 4K/175%) using [FontStashSharp's resolution-factor support](https://github.com/FontStashSharp/FontStashSharp/wiki/Making-Fonts-Sharper-And-Better-At-Scaling). The shared UI pass refreshes Label/TextBox fonts before layout, including existing dialogs and newly opened dropdowns. This avoids magnifying low-resolution glyph images at large scales and undersampling oversized atlases at small scales. `DistributedLabel`, used by the menu title, automatically distributes characters across its arranged width and inherits global font updates; screens never call a title-fitting method. Font systems are reused across resizes and released with the game; no CRT or font-family change is involved.

`GameThemes.DeepDrive.Spacing` supplies `Xs`, `Sm`, `Md`, `Lg`, and `Xl` for gaps, padding, and margins. The game uses `UIBorderRadii.Square`: every `BorderRadius` token (`Zero` through `Full`) resolves to zero. All buttons, dialogs, dropdowns, fields, and interaction states have square corners. Shared tokens live in `Engine/UI/Theming/UITokens.cs`. Axis-aligned zero-radius surfaces snap their borders to physical pixels with a one-screen-pixel minimum, using the current drawing transform (including nested widgets and popup lists). This prevents thin edges disappearing at fractional UI scales; fills and borders remain non-overlapping so transparency is preserved.

Progress controls are borderless: `DeepBlack` supplies the empty track and `Selection` supplies the orange fill. Their overlaid drag slider has no background or outline in any state; the flat handle retains themed hover/focus/pressed feedback. CRT intensity and all volume controls share this styling.

`GameThemes.DeepDrive.MenuButton` defines complete `Standard`, `Menu`, `Dialog`, and `Confirmation` size presets. Each preset owns preferred width/height, padding, icon gaps, `Compact`/`Default`/`Prominent` font sizes, and leading/trailing icon metrics. `MenuButton` resolves the selected preset once; parent layout and the global desktop transform handle subsequent resizing. There is no component scale multiplier or `Resize` call. Individual constructors can still select `textVariant`, `iconSize`, and `trailingIconSize`, or explicitly override normal widget dimensions when needed. All text variants use Abel Regular.

```csharp
new MenuButton(assets, "SETTINGS", "settings", size: MenuButtonSize.Menu);
new MenuButton(assets, "BACK", "arrow-left", arrow: false, size: MenuButtonSize.Dialog);
new MenuButton(assets, "CANCEL", tone: MenuButtonTone.Danger, size: MenuButtonSize.Confirmation);
```

Preferred screen dimensions and sidebar breakpoints live in `GameThemes.DeepDrive.Layout`, not resize handlers. Main Menu, Settings, and Credits only declare their content and event handlers.

`MenuButton` icons are optional. Omit `icon` for centered text with no icon columns or spacing; set `arrow: true` explicitly if only a trailing chevron is wanted. Existing icon buttons retain their default chevron unless `arrow: false` is supplied. `tone: MenuButtonTone.Danger` uses the theme's `Danger` and `DangerHighlight` reds for explicit cancel/destructive actions, including hover/focus. Other buttons keep their normal monochrome/orange states.

`Game/UI/Dialog.cs` provides the game modal container: it fills its parent with the translucent `DialogScrim`, centers its single child, consumes pointer hits outside that child, and takes keyboard focus while visible. Settings and Credits use this component so the main menu remains visible but inactive beneath the overlay.

`Game/UI/ConfirmationDialog.cs` adds a reusable confirmation prompt with a title, optional live `Message`, configurable action captions, and confirm/cancel callbacks. It uses themed sizing, text-only buttons, and a red Cancel button. Add its `Overlay` to the screen's root once, call `Show()` when needed, and dispose it with the screen; the caller retains ownership of the supplied `MenuAssets`. A choice hides the prompt before invoking its action, while `Hide()` dismisses it without invoking either action. `Game/UI/Settings/DisplayConfirmation.cs` only binds this component to the display-preview countdown and Keep/Revert actions; the shared component has no display-settings dependency.

```csharp
var confirmation = new ConfirmationDialog(assets, "CONFIRM ACTION?", onConfirm, onCancel,
    message: "Optional message", confirmText: "CONFIRM", cancelText: "CANCEL");
root.Widgets.Add(confirmation.Overlay);
confirmation.Show();
```

The main menu uses [Lucide](https://lucide.dev/) icons, bundled as SVG sources and 96 px PNGs in `Content/Icons/Lucide` with their license and source revision. Normal builds use the PNGs. Menu spacing and type scale with the window. Its actions are Play, Settings, Credits and Quit; there are no Continue/New/Load buttons. Shared buttons use engine `UIAudioFeedback` for hover/focus/click one-shots without changing their rendering or layout.

The fullscreen CRT progress-slider runs from 0% to 100%, with its label, bar, and percentage inline. Zero disables processing and every non-zero value enables it; no separate checkbox or boolean is stored. The treatment is the original Aged profile: subtle horizontal scanlines, faint two-dimensional screen-space noise, radial falloff, and bloom. There is no direction selector or screen curvature. The CRT is a game-wide final-frame pass over the scene and UI, so the same preference remains active during gameplay. Menu buttons use ordinary Myra hover/focus states rather than per-button shader materials.

## Persistence

Use the static engine APIs from any scene. `GameHost` initializes storage and loads preferences before calling `Startup.Initialize` and constructing the first scene. `BootstrapScene` uses the shared `Game/Scenes/Loading` flow to prepare UI fonts, icon data, two synchronized music cues and UI sounds before opening the menu. Resource preparation advances once per rendered frame. Music decoding finishes before playback begins. Bootstrap does not create a session. Play uses the same loading view and publishes a new `SessionManager.ActiveSession` only after its preload succeeds; repeated clicks cannot queue duplicate loads.

`SessionManager.ActiveSession` in `Game/Domain/Sessions/SessionManager.cs` holds the in-memory session shared across scenes. Its getter returns a non-null `Session` or throws `InvalidOperationException` if none is set. Assign a session to activate it or `null` to clear it; `[AllowNull]` permits null assignment without making reads nullable. `Session.CurrentLoadout` exposes its mutable `Loadout` with explicit starter chassis, pilot, and left/right autocannon IDs. Sandbox entry spawns from that loadout. The loadout is a nested save contract: `LoadoutJsonConverter` saves `chassis`, `pilot`, `weaponLeft`, and `weaponRight` as stable Chisel slugs and resolves them back to current generated IDs when loading; missing or unknown selections fail at the save boundary. Keep these slugs stable when expanding tables. The session manager has no save-slot limit, disk operations, last-active-ID preference, or Continue logic; those workflows remain unimplemented.

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
dotnet run --project Tests/Graphite.Gameplay.Tests -p:TreatWarningsAsErrors=true
dotnet run --project Tests/Graphite.Audio.Tests -p:TreatWarningsAsErrors=true -- --assets
dotnet run --project Tests/Graphite.UI.Tests -- --graphics
```

Dependencies: MonoGame DesktopGL 3.8.5.1, Myra 1.6.5, and .NET 8 with newer-runtime roll-forward support.

## UI materials and animation

Wrap a Myra control before adding it to a layout. The host owns its outer position, size, alignment, margin, and visibility; keep the original control for text, events, enabled state, and focus. Material passes include the control's background, border, and children, and run in list order. These widget materials are separate from the game-wide CRT post-process.

Material captures preserve the back buffer while compositing, then restore its original usage policy. When drawing UI into a custom `RenderTarget2D`, create that destination with `RenderTargetUsage.PreserveContents` so nested material passes retain previously drawn content.

```csharp
var button = new Button();
var host = new UIMaterialHost(button, interactions: MenuPresentation.FadeStyle);
```

The menu has no assigned sound assets. The engine-wide `AudioManager.Current` owns Master, Music, FX, Ambience and UI buses, reactive persisted volume controls, cached WAV assets, 2D positional effects, ambience snapshots and adaptive music. `UI.Audio` delegates to its UI bus; scenes get an automatically disposed `AudioScope`. `GameHost` updates audio independently of UI. The old mute preference migrates to zero master volume. See [engine audio](Engine/Audio/README.md) for routing, examples, supplied-plugin analysis, tests and current limitations.

`Startup.Initialize` registers `CrtFilter` with `PostProcessing`; `GameHost` captures the complete scene and UI at backbuffer resolution, applies it once, and then presents the final frame. `CrtFilter` uses the dedicated graphics `ShaderScreenFilter` API and has no dependency on UI materials. Do not attach CRT to a widget or screen root. Buttons use standard background, border, text, and icon states for hover, focus, press, and disabled feedback.

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
