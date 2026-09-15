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

namespace Graphite.UI.Tests;

internal static class StartupChecks
{
    internal static bool Configured { get; private set; }
    internal static bool Constructed { get; set; }
    internal static bool Loaded { get; set; }
    internal static bool Unloaded { get; set; }
    internal static double ExpectedTime { get; private set; }

    public static void Run()
    {
        var settings = new FixtureSettings("graphite-startup-test-" + Guid.NewGuid().ToString("N"));
        var root = PersistencePaths.ForGame(settings.Game.Id, settings.Environment);
        try
        {
            Preferences.Initialize(root);
            Preferences.Set(PlayerPreferences.MasterVolume, .25f);
            Preferences.Set(PlayerPreferences.CrtIntensity, .35f);
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
                Program.Check(!PostProcessing.IsActive && PostProcessing.AllocatedTargets == 0
                    && PostProcessing.TargetSize == Point.Zero,
                    "Game shutdown releases the global CRT and its frame targets");
                try { Storage.Load<Session>(); throw new Exception("Storage was not shut down"); }
                catch (InvalidOperationException) { }
                try { Preferences.Get(PlayerPreferences.MasterVolume); throw new Exception("Preferences were not shut down"); }
                catch (InvalidOperationException) { }
            }
        }
        finally
        {
            Preferences.Shutdown();
            Storage.Shutdown();
            var directory = Path.GetDirectoryName(root)!;
            if (Directory.Exists(directory)) { Directory.Delete(directory, recursive: true); }
        }
    }

    private sealed class FixtureSettings(string id) : Settings
    {
        private readonly StagingSettings _defaults = new();
        public override SettingsEnvironment Environment => SettingsEnvironment.Staging;
        public override GameIdentitySettings Game { get; } = new()
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
        Program.Near(Graphite.Engine.UI.UI.Audio.Volume, .25f, "Loaded audio preferences apply before the first scene");
        Program.Check(PostProcessing.IsActive, "Global CRT is available when the first scene is constructed");
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
