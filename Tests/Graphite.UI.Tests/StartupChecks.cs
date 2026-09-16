using Graphite.Engine.Configuration;
using Graphite.Engine.Core;
using Graphite.Engine.Graphics;
using Graphite.Engine.Persistence;
using Graphite.Engine.Scenes;
using Graphite.Game;
using Graphite.Game.Configuration;
using Graphite.Game.Graphics;
using Graphite.Game.Sessions;
using Microsoft.Xna.Framework;
using Graphite.Game.UI.Theming;

namespace Graphite.UI.Tests;

internal static class StartupChecks
{
    internal static bool Configured
    {
        get; private set;
    }
    internal static bool Constructed
    {
        get; set;
    }
    internal static bool Loaded
    {
        get; set;
    }
    internal static bool Unloaded
    {
        get; set;
    }
    internal static double ExpectedTime
    {
        get; private set;
    }

    public static void Run()
    {
        var settings = new FixtureSettings("graphite-startup-test-" + Guid.NewGuid().ToString("N"));
        var root = PersistencePaths.ForGame(settings.Game.Id, settings.Environment);
        try
        {
            Preferences.Initialize(root);
            Preferences.Set(PlayerPreferences.MasterVolume, .25f);
            Preferences.Set(PlayerPreferences.MusicVolume, .4f);
            Preferences.Set(PlayerPreferences.FxVolume, .6f);
            Preferences.Set(PlayerPreferences.CrtIntensity, .35f);
            Preferences.Set(RuntimePreferences.UiScale, 1.25f);
            Preferences.Set(RuntimePreferences.Display, "Windowed:1280x720");
            Storage.Initialize(Path.Combine(root, "saves"));
            var session = new Session();
            session.AddPlayTime(12);
            Storage.Save(session);
            Preferences.Shutdown();
            Storage.Shutdown();
            for (var run = 0; run < 2; run++)
            {
                ExpectedTime = 12 + run * 3;
                Configured = Constructed = Loaded = Unloaded = false;
                using (var game = new GameHost(settings, () =>
                {
                    Program.Check(Preferences.Get(PlayerPreferences.MasterVolume) == .25f
                        && Storage.Load<Session>().PlayTimeSeconds == ExpectedTime, "Engine persistence is ready before game initialization");
                    Startup.Initialize();
                    Program.Check(EnvironmentOverlay.IsInitialized && EnvironmentOverlay.Text == "STAGING",
                        "The host initializes its environment print before the first scene");
                    Program.Check(DisplaySettings.Current == new DisplayConfiguration(WindowMode.Windowed, new Point(1280, 720)),
                        "Saved display settings override staging defaults before game initialization");
                    Program.Check(PostProcessing.IsActive,
                        "Game startup installs the global CRT before the first scene");
                    Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), .035f,
                        "Game startup restores the persisted global CRT intensity");
                    Configured = true;
                }))
                {
                    game.Run();
                }
                Program.Check(Constructed && Loaded && Unloaded,
                    "First scene uses persistence throughout its lifecycle");
                Program.Check(!DisplaySettings.IsInitialized, "Display service releases the game on shutdown");
                Program.Check(!EnvironmentOverlay.IsInitialized, "The host releases its environment renderer on shutdown");
                Program.Check(ThemeAssets.ResolutionFontCount == 0, "Shutdown releases all resolution-specific font systems");
                Program.Check(!PostProcessing.IsActive && PostProcessing.AllocatedTargets == 0
                    && PostProcessing.TargetSize == Point.Zero,
                    "Game shutdown releases the global CRT and its frame targets");
                try
                {
                    Storage.Load<Session>();
                    throw new Exception("Storage was not shut down");
                }
                catch (InvalidOperationException) { }
                try
                {
                    Preferences.Get(PlayerPreferences.MasterVolume);
                    throw new Exception("Preferences were not shut down");
                }
                catch (InvalidOperationException) { }
            }
        }
        finally
        {
            Preferences.Shutdown();
            Storage.Shutdown();
            var directory = Path.GetDirectoryName(root)!;
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class FixtureSettings(string id) : Settings
    {
        private readonly StagingSettings _defaults = new();
        public override SettingsEnvironment Environment => SettingsEnvironment.Staging;
        public override GameIdentitySettings Game
        {
            get;
        } = new()
        {
            Id = id,
            Name = "Startup checks",
            Icon = null,
            StartupScene = "Graphite.UI.Tests.StartupProbeScene"
        };
        public override WindowSettings Window => _defaults.Window;
        public override GraphicsSettings Graphics => _defaults.Graphics;
        public override RuntimeSettings Runtime => _defaults.Runtime;
    }
}

public sealed class StartupProbeScene : Scene
{
    public StartupProbeScene()
    {
        Program.Check(StartupChecks.Configured && Preferences.Get(PlayerPreferences.MasterVolume) == .25f
            && Storage.Load<Session>().PlayTimeSeconds == StartupChecks.ExpectedTime,
            "All initialization finishes before the first scene is constructed");
        Program.Near(Graphite.Engine.Audio.AudioManager.Current.Fx.EffectiveVolume, .15f, "Loaded master and FX apply before the first scene");
        Program.Near(Graphite.Engine.Audio.AudioManager.Current.Music.EffectiveVolume, .1f, "Loaded master and music apply before the first scene");
        Program.Check(PostProcessing.IsActive, "Global CRT is available when the first scene is constructed");
        Program.Near(Graphite.Engine.UI.UI.Scale,
            Math.Min(Graphite.Engine.UI.UI.ViewportWidth / 1600f, Graphite.Engine.UI.UI.ViewportHeight / 900f) * 1.25f,
            "Engine restores global UI scale before constructing the first scene");
        StartupChecks.Constructed = true;
    }

    protected internal override void OnLoad()
    {
        var session = Storage.Load<Session>();
        session.AddPlayTime(3);
        Storage.Save(session);
        Preferences.Set("startup.fixture", true);
        Program.Check(Preferences.Get<bool>("startup.fixture"), "Preferences.Set works in the first scene");
        StartupChecks.Loaded = true;
        Application.Quit();
    }

    protected internal override void OnUnload()
    {
        Program.Check(Storage.Load<Session>().PlayTimeSeconds == StartupChecks.ExpectedTime + 3
            && Preferences.Remove<bool>("startup.fixture"), "Persistence stays available during scene teardown");
        StartupChecks.Unloaded = true;
    }
}
