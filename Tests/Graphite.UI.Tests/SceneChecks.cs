using Graphite.Engine.Scenes;
using Graphite.Engine.Objects;
using Graphite.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.UI.Tests;

internal static class SceneChecks
{
    private static readonly List<string> Events = [];
    private static float _elapsed;
    private static float _lateElapsed;
    private static float _objectElapsed;
    private static Point _inputViewport;

    public static void Run()
    {
        CameraLifecycle();
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

            const float frameDelta = 1f / 60;
            SceneManager.Update(frameDelta);
            Program.Check(_elapsed == frameDelta && _lateElapsed == frameDelta,
                "Update and late update receive the same unmodified float delta in seconds");
            SceneManager.Update(0);
            Program.Check(_elapsed == 0 && _lateElapsed == 0,
                "A zero-duration frame does not retain the previous elapsed time");
            var beforeInvalid = Events.Count;
            foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                try
                {
                    SceneManager.Update(invalid);
                    throw new InvalidOperationException("Expected invalid delta time to fail");
                }
                catch (ArgumentOutOfRangeException)
                {
                    Program.Check(Events.Count == beforeInvalid && _elapsed == 0 && _lateElapsed == 0,
                        "Invalid frame durations fail before scene callbacks run");
                }
            }

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
            owned.Objects.MaxDeltaTime = .1f;
            SceneManager.Update(1f);
            Program.Check(_elapsed == 1f && _lateElapsed == 1f && _objectElapsed == .1f,
                "Object simulation clamping does not shorten the scene's delta time");
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

    private static void CameraLifecycle()
    {
        var scene = new MenuScene();
        try
        {
            var target = scene.Objects.Spawn(new ObjectPrefab("Follow target"), new Vector2(100, 200));
            target.AddComponent(new ObjectProbe { LateMovement = new Vector2(10, 20) });
            var camera = scene.Objects.Spawn(new ObjectPrefab("Camera"))
                .AddComponent(new CameraComponent(new Point(1600, 900)) { FollowTarget = target.Transform });
            Program.Check(ReferenceEquals(scene.Camera, camera.Camera), "Scene resolves the camera owned by its object world");
            scene.UpdateInternal(.5f, new Point(1280, 720));
            Program.Check(_inputViewport == new Point(1280, 720), "Viewport sizing happens before scene input on the first frame");
            Program.Check(camera.Camera.Position == target.Transform.WorldPosition && camera.Camera.Position == new Vector2(105, 210),
                "Camera follows after target late update without a one-frame lag");
            foreach (var size in new[] { new Point(2560, 1440), new Point(900, 1600), new Point(3440, 1440) })
            {
                scene.Objects.Paused = true;
                scene.UpdateInternal(0, size);
                Program.Check(scene.Camera.ViewportSize == size && _inputViewport == size,
                    "Window, fullscreen and aspect-ratio changes refresh the camera before input even while paused");
                Program.Near(scene.Camera.Zoom, Math.Min(size.X / 1600f, size.Y / 900f), "Reference-resolution framing follows the viewport");
                Program.Check(scene.Camera.WorldToScreen(target.Transform.WorldPosition) == size.ToVector2() * .5f,
                    "The followed object stays centered after resizing");
                var point = target.Transform.WorldPosition + new Vector2(70, -40);
                Program.Near(Vector2.Distance(scene.Camera.ScreenToWorld(scene.Camera.WorldToScreen(point)), point), 0,
                    "Resized camera preserves aim-coordinate conversion");
            }
            var second = scene.Objects.Spawn(new ObjectPrefab("Second camera"))
                .AddComponent(new CameraComponent(new Point(1600, 900)) { Enabled = false });
            Program.Check(ReferenceEquals(scene.Camera, camera.Camera), "Disabled cameras do not participate in selection");
            second.Enabled = true;
            ExpectNoSingleCamera();
            camera.Owner.Active = false;
            Program.Check(ReferenceEquals(scene.Camera, second.Camera), "Inactive camera objects are excluded from selection");
            second.Dispose();
            ExpectNoSingleCamera();
            camera.Owner.Active = true;
            camera.Owner.Destroy();
            ExpectNoSingleCamera();
            scene.DrawInternal(new GameTime(), null);
            Program.Check(camera.IsDisposed && camera.FollowTarget is null, "Destroyed cameras are detached without stale scene references");

            void ExpectNoSingleCamera()
            {
                try
                {
                    _ = scene.Camera;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                throw new InvalidOperationException("Expected missing or ambiguous active camera to fail");
            }
        }
        finally
        {
            scene.UnloadInternal();
        }
    }

    internal static void CameraRendering(GraphicsDevice device)
    {
        var scene = new MenuScene();
        using var pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.Red });
        var size = new Point(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);
        using var target = new RenderTarget2D(device, size.X, size.Y);
        try
        {
            var subject = scene.Objects.Spawn(new ObjectPrefab("Subject"), new Vector2(800, -300));
            subject.AddComponent(new SpriteRenderer(pixel));
            subject.Transform.LocalScale = new Vector2(20);
            var camera = scene.Objects.Spawn(new ObjectPrefab("Camera"))
                .AddComponent(new CameraComponent(new Point(1600, 900)) { FollowTarget = subject.Transform });
            // A draw can precede the first update, or follow a resize while simulation is paused.
            scene.Objects.Paused = true;
            camera.Camera.SetViewport(new Point(100, 100));
            device.SetRenderTarget(target);
            device.Clear(Color.Black);
            scene.DrawInternal(new GameTime(), device);
            device.SetRenderTarget(null);
            var pixels = new Color[size.X * size.Y];
            target.GetData(pixels);
            Program.Check(camera.Camera.ViewportSize == size && pixels[size.Y / 2 * size.X + size.X / 2] == Color.Red,
                "Automatic scene rendering uses the camera object and actual backbuffer size before the first update");
            camera.Enabled = false;
            device.SetRenderTarget(target);
            device.Clear(Color.Black);
            scene.DrawInternal(new GameTime(), device);
            device.SetRenderTarget(null);
            target.GetData(pixels);
            Program.Check(pixels.All(color => color == Color.Black), "Without an enabled camera, the scene does not render world objects");
        }
        finally
        {
            device.SetRenderTarget(null);
            scene.UnloadInternal();
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
        protected internal override void OnLoad() => Objects.Spawn(new ObjectProbePrefab());
        protected internal override void Update(float dt)
        {
            _elapsed = dt;
            Events.Add("scene input");
        }
        protected internal override void LateUpdate(float dt)
        {
            _lateElapsed = dt;
            Events.Add("scene late");
        }
    }

    private sealed class FailedObjectScene : Scene
    {
        public FailedObjectScene()
        {
        }
        protected internal override void OnLoad()
        {
            Objects.Spawn(new ObjectProbePrefab());
            throw new NotSupportedException("Fixture load failure");
        }
    }

    private sealed class ObjectProbePrefab() : Prefab<ObjectProbe>("Root")
    {
        protected internal override ObjectProbe Build(GameObject root) => root.CreateChild("Child").AddComponent(new ObjectProbe());
    }

    private sealed class ObjectProbe : Component
    {
        public Vector2 LateMovement;
        protected internal override void Update(float dt)
        {
            _objectElapsed = dt;
            Events.Add("component update");
        }
        protected internal override void LateUpdate(float dt)
        {
            Transform.WorldPosition += LateMovement * dt;
            Events.Add("component late");
        }
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
            _inputViewport = Objects.GetComponents<CameraComponent>().Any(camera => camera.IsActive) ? Camera.ViewportSize : Point.Zero;
            Events.Add("menu update");
        }

        protected internal override void LateUpdate(float dt) => _lateElapsed = dt;

        protected internal override void Draw(GameTime gameTime) => Events.Add("menu draw");

        protected internal override void OnUnload() => Events.Add("menu unload");
    }
}
