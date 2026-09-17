using Graphite.Engine.Scenes;
using Graphite.Engine.Objects;
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

            SceneManager.Load<ObjectScene>();
            SceneManager.CommitPendingChanges();
            var owned = ObjectScene.Instance ?? throw new InvalidOperationException("Missing fixture scene");
            Events.Clear();
            SceneManager.Update(.1f);
            Program.Check(Events.SequenceEqual(new[] { "scene input", "component update", "component late", "scene late" }),
                "Scenes automatically schedule object updates between input and the scene late-update hook");
            SceneManager.Draw(new GameTime());
            SceneManager.Shutdown();
            Program.Check(owned.Objects.Roots.Count == 0 && owned.Objects.ComponentCount == 0 && Events.Count(value => value == "component removed") == 1,
                "Scene shutdown disposes its objects exactly once without explicit scene cleanup code");
            SceneManager.Load<FailedObjectScene>();
            try
            {
                SceneManager.CommitPendingChanges();
                throw new InvalidOperationException("Expected scene-load failure");
            }
            catch (NotSupportedException)
            {
                Program.Check(Events.Count(value => value == "component removed") == 2,
                    "Failed scene loading also cleans up objects already created");
            }
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

    private sealed class ObjectScene : Scene
    {
        public static ObjectScene? Instance;
        public ObjectScene() => Instance = this;
        protected internal override void OnLoad() => Objects.Create("Root").CreateChild("Child").AddComponent(new ObjectProbe());
        protected internal override void Update(float dt) => Events.Add("scene input");
        protected internal override void LateUpdate(float dt) => Events.Add("scene late");
    }

    private sealed class FailedObjectScene : Scene
    {
        public FailedObjectScene()
        {
        }
        protected internal override void OnLoad()
        {
            Objects.Create("Partial load").AddComponent(new ObjectProbe());
            throw new NotSupportedException("Fixture load failure");
        }
    }

    private sealed class ObjectProbe : Component
    {
        protected internal override void Update(float dt) => Events.Add("component update");
        protected internal override void LateUpdate(float dt) => Events.Add("component late");
        protected override void OnRemoved() => Events.Add("component removed");
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
