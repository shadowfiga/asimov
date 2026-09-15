using Graphite.Engine.Scenes;
using Microsoft.Xna.Framework;

namespace Graphite.UI.Tests;

internal static class SceneChecks
{
    private static readonly List<string> Events = [];
    private static float _elapsed;

    public static void Run()
    {
        Events.Clear();
        try
        {
            SceneManager.Load<StartupScene>();
            Program.Check(Events.Count == 0, "Scene loading waits for the update boundary");
            SceneManager.CommitPendingChanges();
            Program.Check(Events.SequenceEqual(new[] { "startup load", "startup unload", "menu load" }),
                "Bootstrap finishes and unloads before the menu opens");

            SceneManager.Update(.25f);
            Program.Near(_elapsed, .25f, "Active scene receives elapsed time");
            Program.Check(Events[^1] == "menu update", "Only the active scene updates");
            SceneManager.Draw(new GameTime(TimeSpan.FromSeconds(.25), TimeSpan.FromSeconds(.25)));
            Program.Check(Events[^1] == "menu draw", "Only the active scene draws");

            SceneManager.Load<MenuScene>();
            SceneManager.CommitPendingChanges();
            Program.Check(Events.TakeLast(2).SequenceEqual(new[] { "menu unload", "menu load" }),
                "Replacing a scene unloads its previous instance");

            SceneManager.Load<StartupScene>();
            SceneManager.Shutdown();
            var eventCount = Events.Count;
            SceneManager.CommitPendingChanges();
            SceneManager.Update(.5f);
            SceneManager.Shutdown();
            Program.Check(Events.Count == eventCount && Events[^1] == "menu unload",
                "Shutdown cancels queued loads and unloads once");
        }
        finally
        {
            SceneManager.Shutdown();
            Events.Clear();
        }
    }

    private sealed class StartupScene : Scene
    {
        public StartupScene()
        {
        }

        protected internal override void OnLoad()
        {
            Events.Add("startup load");
            SceneManager.Load<MenuScene>();
        }

        protected internal override void OnUnload() => Events.Add("startup unload");
    }

    private sealed class MenuScene : Scene
    {
        public MenuScene()
        {
        }

        protected internal override void OnLoad() => Events.Add("menu load");

        protected internal override void Update(float dt)
        {
            _elapsed = dt;
            Events.Add("menu update");
        }

        protected internal override void Draw(GameTime gameTime) => Events.Add("menu draw");

        protected internal override void OnUnload() => Events.Add("menu unload");
    }
}
