using System.Reflection;
using Chisel.Generated;
using Graphite.Engine.Persistence;
using Graphite.Engine.Audio;
using Graphite.Engine.Graphics;
using Graphite.Engine.Scenes;
using Graphite.Engine.Objects;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Scenes;
using Graphite.Game.Sessions;
using Graphite.Game.UI;
using Graphite.Game.UI.Hud;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.UI.Tests;

internal static class BootstrapChecks
{
    internal static void Run(GraphicsDevice device, string output, GraphicsDeviceManager graphics)
    {
        var desktop = (Desktop)typeof(Ui).GetField("_desktop", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        var session = new Session();
        session.ClearSector("fixture-sector");
        session.StartRun();
        var originalRun = session.CurrentRun;
        originalRun.XP = 99;
        SessionManager.ActiveSession = session;
        try
        {
            CheckHudReadouts(desktop, originalRun);
            using (var screen = Ui.Open<LoadingUI>())
            {
                screen.SetProgress(.5f);
                foreach (var invalid in new[] { -.1f, 1.1f, float.NaN, float.PositiveInfinity })
                {
                    var rejected = false;
                    try
                    {
                        screen.SetProgress(invalid);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        rejected = true;
                    }
                    Program.Check(rejected, "Broken loading progress throws instead of being clamped");
                }
                foreach (var scale in new[] { .75f, 1.75f, 1f })
                {
                    Preferences.Set(RuntimePreferences.UiScale, scale);
                    Ui.Update(0);
                    var root = desktop.Widgets.Single();
                    var progress = root.GetChildren(true).OfType<HorizontalProgressBar>().Single();
                    var content = progress.Parent;
                    var center = content.ToGlobal(new Vector2(content.Bounds.Width / 2f, content.Bounds.Height / 2f));
                    Program.Check(Math.Abs(center.X - Ui.ViewportWidth / 2f) <= Ui.Scale
                        && Math.Abs(center.Y - Ui.ViewportHeight / 2f) <= Ui.Scale
                        && progress.Value == .5f && content.Width == GameThemes.DeepDrive.Layout.LoadingWidth,
                        "Loading UI stays centered and retains progress across global scale changes");
                    Capture(device, output, $"bootstrap-scale-{scale:F2}");
                }
            }
            Ui.Update(0);
            SceneManager.Load<BootstrapScene>();
            SceneManager.CommitPendingChanges();
            var loading = desktop.Widgets.Single();
            var meter = loading.GetChildren(true).OfType<HorizontalProgressBar>().Single();
            SceneManager.Update(0);
            SceneManager.CommitPendingChanges();
            Program.Check(desktop.Widgets.Contains(loading) && meter.Value == 0,
                "Bootstrap keeps its loading screen until the first draw instead of immediately opening the menu");

            var steps = 0;
            var showedComplete = false;
            for (var frame = 0; frame < 100 && desktop.Widgets.Contains(loading); frame++)
            {
                Ui.Update(0);
                var before = meter.Value;
                SceneManager.Draw(new GameTime(), device);
                if (before == 1)
                {
                    showedComplete = true;
                }
                Capture(device, output, before == 0 ? "bootstrap-start" : before == 1 ? "bootstrap-complete" : "bootstrap-preloading");
                SceneManager.Update(0);
                var after = meter.Value;
                Program.Check(after >= before && after <= 1, "Preload progress advances monotonically through real resources");
                if (after > before)
                {
                    steps++;
                }
                SceneManager.Update(0);
                Program.Check(meter.Value == after, "Multiple updates without a draw cannot skip preload presentation");
                SceneManager.CommitPendingChanges();
            }
            Program.Check(!desktop.Widgets.Contains(loading) && Ui.HostCount > 0 && steps > 1 && showedComplete,
                "Bootstrap prepares UI resources and presents completed progress before opening the menu");
            Program.Check(ReferenceEquals(SessionManager.ActiveSession, session) && session.ClearedSectors.Contains("fixture-sector"),
                "Bootstrap preloads presentation assets without replacing or advancing the active session");
            Program.Check(ReferenceEquals(session.CurrentRun, originalRun) && originalRun.XP == 99,
                "Bootstrap does not create or reset a run");
            CheckSandboxRequiresRun(session);
            CheckPlay(desktop, session, device, output, graphics);
            SceneManager.Shutdown();
            Program.Check(Ui.HostCount == 0, "Scene teardown releases menu hosts after bootstrap");
        }
        finally
        {
            SceneManager.Shutdown();
            SessionManager.ActiveSession = null;
            Preferences.Set(RuntimePreferences.UiScale, originalScale);
            Ui.Update(0);
        }
    }

    private static void CheckSandboxRequiresRun(Session original)
    {
        var unprepared = new Session();
        SessionManager.ActiveSession = unprepared;
        var cursorVisible = Myra.MyraEnvironment.Game.IsMouseVisible;
        var rejected = false;
        SceneManager.Load<SandboxScene>();
        try
        {
            SceneManager.CommitPendingChanges();
        }
        catch (InvalidOperationException exception)
        {
            rejected = exception.Message.Contains("Session.StartRun", StringComparison.Ordinal);
        }
        Program.Check(rejected, "Direct sandbox entry fails when the session has no run");
        Program.Check(Myra.MyraEnvironment.Game.IsMouseVisible == cursorVisible,
            "Rejected sandbox entry preserves the previous cursor visibility");
        var stillAbsent = false;
        try
        {
            _ = unprepared.CurrentRun;
        }
        catch (InvalidOperationException)
        {
            stillAbsent = true;
        }
        Program.Check(stillAbsent, "Sandbox must not create a missing run itself");
        SessionManager.ActiveSession = original;
        SceneManager.Load<MainMenuScene>();
        SceneManager.CommitPendingChanges();
    }

    private static void CheckPlay(Desktop desktop, Session original, GraphicsDevice device, string output, GraphicsDeviceManager graphics)
    {
        Ui.Update(0);
        var buttons = desktop.Widgets.Single().GetChildren(true).OfType<MenuButton>().ToArray();
        string Text(MenuButton button) => button.GetChildren(true).OfType<Label>().Single().Text;
        Program.Check(buttons.Select(Text).SequenceEqual(new[] { "PLAY", "SETTINGS", "CREDITS", "QUIT" }),
            "One Play action replaces Continue, New Operation and Load Operation");
        Capture(device, output, "menu-play");
        var play = buttons.Single(button => Text(button) == "PLAY");
        var cached = AudioManager.Current.CachedClipCount;
        var cursorVisible = Myra.MyraEnvironment.Game.IsMouseVisible;
        var originalRun = original.CurrentRun;
        play.DoClick();
        play.DoClick();
        Program.Check(!play.Enabled && ReferenceEquals(SessionManager.ActiveSession, original),
            "Double-click cannot queue duplicate loads or publish an unprepared session");
        SceneManager.CommitPendingChanges();
        var loading = desktop.Widgets.Single();
        var meter = loading.GetChildren(true).OfType<HorizontalProgressBar>().Single();
        SceneManager.Update(0);
        Program.Check(meter.Value == 0 && ReferenceEquals(SessionManager.ActiveSession, original),
            "Play presents loading before preparing the new session");
        for (var frame = 0; frame < 10 && meter.Value < 1; frame++)
        {
            SceneManager.Draw(new GameTime(), device);
            SceneManager.Update(0);
            SceneManager.CommitPendingChanges();
            Program.Check(ReferenceEquals(SessionManager.ActiveSession, original), "Partial preload does not replace active session");
            Program.Check(ReferenceEquals(original.CurrentRun, originalRun) && originalRun.XP == 99,
                "Partial preload leaves the previous run untouched");
        }
        Program.Check(meter.Value == 1, "Session preload reaches completion before entry");
        Ui.Update(0);
        Capture(device, output, "session-preloaded");
        SceneManager.Draw(new GameTime(), device);
        SceneManager.Update(0);
        Program.Check(!ReferenceEquals(SessionManager.ActiveSession, original) && SessionManager.ActiveSession.ClearedSectors.Count == 0,
            "Successful preload publishes a fresh non-null session");
        var run = SessionManager.ActiveSession.CurrentRun;
        Program.Check(desktop.Widgets.Contains(loading) && run.XP == 0 && run.Kills == 0
            && run.Duration == TimeSpan.Zero && run.Modifiers.Length == 0,
            "Loading creates and publishes an initialized run before Sandbox OnLoad executes");
        SceneManager.CommitPendingChanges();
        Program.Check(ReferenceEquals(SessionManager.ActiveSession.CurrentRun, run), "Sandbox consumes the prepared run without replacing it");
        Program.Check(desktop.Widgets.Count == 1 && desktop.Widgets.Single().GetChildren(true).OfType<ResourceHud>().Count() == 1
            && AudioManager.Current.CachedClipCount == cached,
            "Session entry replaces loading UI with the resource HUD and reuses bootstrap audio assets");
        var root = desktop.Widgets.Single();
        var hud = root.GetChildren(true).OfType<ResourceHud>().Single();
        Program.Check(hud.OreAmount.Text == "0", "Sandbox HUD begins with the actual zero Ore balance");
        run.AddOre(4820);
        Program.Check(hud.OreAmount.Text == "4,820", "Sandbox HUD reacts directly to active run changes");
        Program.Check(run.TrySpendOre(20) && hud.OreAmount.Text == "4,800", "Spending Ore refreshes the HUD");
        Program.Check(!run.TrySpendOre(4801) && hud.OreAmount.Text == "4,800", "Unaffordable purchases do not change the HUD");
        run.AddOre(20);
        run.RecordKill(ChiselEnemiesIds.SWARMER);
        SceneManager.Update(0);
        Program.Check(run.Kills == 1 && run.XP == ChiselEnemies.Experience[ChiselEnemiesIds.SWARMER] && run.Ore == 4820,
            "Enemy reward entry point advances XP without paying an Ore bounty");
        Program.Check(root.GetChildren(true).OfType<Label>().Any(label => label.Text == $"{run.XP} XP"),
            "The sandbox refreshes the XP readout from the run each frame");
        Program.Check(hud.GetChildren(true).OfType<Label>().Select(label => label.Text).SequenceEqual(new[] { "ORE", "4,820" }),
            "The resource HUD shows only Ore, not Gold, Credits, or an XP wallet");
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        foreach (var scale in new[] { .75f, 1.75f, originalScale })
        {
            Preferences.Set(RuntimePreferences.UiScale, scale);
            Ui.Update(0);
            Program.Check(hud.Width == GameThemes.DeepDrive.ResourceHud.Width && hud.Bounds.Width <= Ui.LayoutSize.X,
                "The compact Ore HUD uses its theme width at every supported scale");
            var icon = hud.GetChildren(true).OfType<Image>().Single();
            Program.Check(icon.Bounds.Width == icon.Bounds.Height && icon.Bounds.Width == GameThemes.DeepDrive.ResourceHud.IconSize,
                "The Ore icon stays square when scaling the HUD");
            Program.Check(hud.InputFallsThrough(Point.Zero) && !hud.AcceptsKeyboardFocus,
                "The read-only resource HUD does not capture gameplay input or keyboard focus");
        }
        Ui.Update(0);
        Program.Check(!Myra.MyraEnvironment.Game.IsMouseVisible, "Play hides the system pointer for the world-space reticle");
        using (var target = new RenderTarget2D(device, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight))
        {
            device.SetRenderTarget(target);
            device.Clear(GameThemes.DeepDrive.Background);
            SceneManager.Draw(new GameTime(), device);
            device.SetRenderTarget(null);
            var pixels = new Color[target.Width * target.Height];
            target.GetData(pixels);
            Program.Check(pixels.Count(pixel => pixel.R > 180) > 100, "Play draws the mech as scene content independently of the HUD");
            device.SetRenderTarget(target);
            device.Clear(GameThemes.DeepDrive.Background);
            SceneManager.Draw(new GameTime(), device);
            Ui.Draw();
            device.SetRenderTarget(null);
            var withHud = new Color[pixels.Length];
            target.GetData(withHud);
            for (var y = target.Height / 3; y < target.Height * 2 / 3; y++)
            {
                Program.Check(withHud.AsSpan(y * target.Width + target.Width / 3, target.Width / 3)
                    .SequenceEqual(pixels.AsSpan(y * target.Width + target.Width / 3, target.Width / 3)),
                    "The HUD leaves the central gameplay area unobstructed");
            }
            Program.Check(!withHud.SequenceEqual(pixels), "Resource HUD visibly renders over gameplay");
            using var stream = File.Create(Path.Combine(output, "session-mech.png"));
            target.SaveAsPng(stream, target.Width, target.Height);
        }
        CheckHudLayout(device, output, graphics, root);
        SceneManager.Load<MainMenuScene>();
        SceneManager.CommitPendingChanges();
        Program.Check(desktop.Widgets.Count == 1, "The main menu can be re-entered after playing");
        Program.Check(Myra.MyraEnvironment.Game.IsMouseVisible == cursorVisible, "Leaving play restores the menu cursor");
        run.AddOre(9);
        Program.Check(hud.OreAmount.Text == "4,820", "Scene teardown removes the HUD's Ore subscription");
        AudioManager.Current.MusicPlayer.Stop(0);
    }

    private static void CheckHudReadouts(Desktop desktop, Graphite.Game.Domain.Run.Run run)
    {
        var originalXp = run.XP;
        var originalDuration = run.Duration;
        try
        {
            using var screen = Ui.Open<SandboxUI>();
            var widgets = desktop.Widgets.Single().GetChildren(true).ToArray();
            var timer = widgets.OfType<Label>().Single(label => label.Id == "hud-timer");
            var xp = widgets.OfType<HorizontalProgressBar>().Single(bar => bar.Id == "hud-xp-bar");
            var hp = widgets.OfType<HorizontalProgressBar>().Single(bar => bar.Id == "hud-hp-bar");
            Program.Check(timer.Text == "10:00" && xp.Value == 0 && hp.Value == 1,
                "HUD starts with ten minutes, an unconfigured XP progress bar and full health before scene binding");
            using var healthWorld = new GameWorld();
            var health = healthWorld.Spawn(new ObjectPrefab("Health")).AddComponent(new HealthComponent(100));
            screen.BindHealth(health);
            health.ApplyDamage(25);
            screen.Refresh();
            var healthValue = widgets.OfType<Label>().Single(label => label.Id == "hud-hp-value");
            Program.Check(hp.Value == .75f && healthValue.Text == "75%",
                "HUD health widgets read the bound player health component");
            foreach (var (milliseconds, expected) in new[] { (1, "10:00"), (1000, "09:59"), (59_999, "09:01"),
                (60_000, "09:00"), (599_999, "00:01"), (600_000, "00:00"), (610_000, "00:00") })
            {
                run.Duration = TimeSpan.FromMilliseconds(milliseconds);
                screen.Refresh();
                Program.Check(timer.Text == expected, "Timer counts remaining whole seconds and stops at zero");
            }
            run.Duration = TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1);
            screen.Refresh();
            Program.Check(timer.Text == "00:01", "A final partial second remains visible until the full ten minutes elapse");
            run.Duration = TimeSpan.FromMinutes(10);
            screen.Refresh();
            Program.Check(timer.Text == "00:00", "The TimeSpan deadline displays zero exactly");
            run.XP = 1250;
            screen.Refresh();
            Program.Check(widgets.OfType<Label>().Any(label => label.Text == "1,250 XP") && xp.Value == 0,
                "XP total uses run data without inventing a level-up threshold");
            Program.Check(widgets.OfType<Label>().All(label => !label.Text.Contains("WAVE") && !label.Text.Contains("OBJECTIVE")),
                "No wave or objective content is added to the stub HUD");
        }
        finally
        {
            run.XP = originalXp;
            run.Duration = originalDuration;
        }
    }

    private static void CheckHudLayout(GraphicsDevice device, string output, GraphicsDeviceManager graphics, Widget root)
    {
        var originalSize = new Point(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        var children = root.GetChildren(true).ToArray();
        Widget Find(string id) => children.Single(widget => widget.Id == id);
        Rectangle Bounds(Widget widget)
        {
            var start = widget.ToGlobal(Vector2.Zero);
            var end = widget.ToGlobal(new Vector2(widget.Bounds.Width, widget.Bounds.Height));
            return new Rectangle((int)start.X, (int)start.Y, (int)(end.X - start.X), (int)(end.Y - start.Y));
        }
        try
        {
            foreach (var size in new[] { new Point(1280, 720), new Point(1024, 768), new Point(2560, 1440) })
            {
                graphics.PreferredBackBufferWidth = size.X;
                graphics.PreferredBackBufferHeight = size.Y;
                graphics.ApplyChanges();
                foreach (var scale in new[] { .75f, 1f, 1.75f })
                {
                    Preferences.Set(RuntimePreferences.UiScale, scale);
                    Ui.Update(0);
                    var ore = Bounds(Find("hud-ore"));
                    var experience = Bounds(Find("hud-experience"));
                    var map = Bounds(Find("hud-map"));
                    var pilot = Bounds(Find("hud-pilot"));
                    var regions = new[] { ore, experience, map, pilot };
                    var viewport = new Rectangle(Point.Zero, size);
                    Program.Check(regions.All(viewport.Contains), "Every HUD region stays on screen across resolutions and UI scales");
                    for (var i = 0; i < regions.Length; i++)
                    {
                        for (var j = i + 1; j < regions.Length; j++)
                        {
                            Program.Check(!regions[i].Intersects(regions[j]), "HUD regions do not overlap at any supported UI scale");
                        }
                    }
                    Program.Check(ore.Center.X > size.X / 2 && ore.Center.Y < size.Y / 2
                        && pilot.Center.X > size.X / 2 && pilot.Center.Y > size.Y / 2
                        && map.Center.X < size.X / 2 && map.Center.Y > size.Y / 2,
                        "Ore stays top-right, pilot bottom-right, and the minimap bottom-left");
                    Program.Check(Math.Abs(experience.Center.X - size.X / 2) <= 2 && experience.Center.Y < size.Y / 2,
                        "XP and timer stay at top center");
                    Program.Check(Bounds(Find("hud-map-name")).Bottom <= Bounds(Find("hud-minimap")).Top,
                        "The map name is above the minimap, not in the top-left corner");
                    Program.Check(root.InputFallsThrough(new Point(size.X / 2, size.Y / 2))
                        && children.All(widget => !widget.AcceptsKeyboardFocus), "The HUD does not capture central gameplay input or focus");
                    CaptureSandbox(device, output, $"sandbox-hud-{size.X}x{size.Y}-{scale:F2}");
                    if (size.X == 2560 && scale == 1)
                    {
                        CaptureSandbox(device, output, "sandbox-hud-final");
                    }
                }
            }
        }
        finally
        {
            graphics.PreferredBackBufferWidth = originalSize.X;
            graphics.PreferredBackBufferHeight = originalSize.Y;
            graphics.ApplyChanges();
            Preferences.Set(RuntimePreferences.UiScale, originalScale);
            Ui.Update(0);
        }
    }

    private static void CaptureSandbox(GraphicsDevice device, string output, string name)
    {
        PostProcessing.Render(new GameTime(), GameThemes.DeepDrive.Background, time =>
        {
            SceneManager.Draw(time, device);
            Ui.Draw();
        });
        EnvironmentOverlay.Draw();
        var size = device.PresentationParameters;
        var pixels = new Color[size.BackBufferWidth * size.BackBufferHeight];
        device.GetBackBufferData(pixels);
        using var capture = new Texture2D(device, size.BackBufferWidth, size.BackBufferHeight);
        capture.SetData(pixels);
        using var stream = File.Create(Path.Combine(output, name + ".png"));
        capture.SaveAsPng(stream, capture.Width, capture.Height);
        if (name == "sandbox-hud-final")
        {
            using var preview = File.Create(Path.Combine(output, name + ".jpg"));
            capture.SaveAsJpeg(preview, capture.Width, capture.Height);
        }
    }

    private static void Capture(GraphicsDevice device, string output, string name)
    {
        using var target = new RenderTarget2D(device, Ui.ViewportWidth, Ui.ViewportHeight);
        device.SetRenderTarget(target);
        device.Clear(GameThemes.DeepDrive.Background);
        Ui.Draw();
        device.SetRenderTarget(null);
        using var stream = File.Create(Path.Combine(output, name + ".png"));
        target.SaveAsPng(stream, target.Width, target.Height);
    }
}
