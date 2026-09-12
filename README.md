# AFTERGREEN

Project foundation using Graphite / MonoGame DesktopGL. Startup runs `BootstrapScene` → `MainMenuScene`. Gameplay has been removed for rebuilding; the play button is disabled. The design documents remain as reference.

## Run

On macOS, double-click `Play AFTERGREEN.command`. Alternatively, use `./run.sh` (macOS/Linux) or `run.cmd` (Windows). For initial setup, use `./setup.sh` or `setup.cmd`.

## Settings

`Game/Configuration/StagingSettings.cs` and `ProductionSettings.cs` inherit the shared `GameSettings` contract in `RuntimeSettings.cs`. Read values through `GameSettings.Instance`. Menu settings belong to the game layer; window, graphics, and runtime settings belong to the engine.

Debug defaults to staging; Release defaults to production. Override with `AFTERGREEN_ENVIRONMENT=staging` or `production`. Rebuild after editing settings. The main menu shows the DEMO banner only in staging.

## Checks

```sh
dotnet build --warnaserror
dotnet format Graphite.csproj --verify-no-changes --severity warn
```

Dependencies: MonoGame DesktopGL 3.8.5.1, Myra 1.6.5, and .NET 8 with newer-runtime roll-forward support.
