using System.Reflection;
using Graphite.Engine.Persistence;
using Graphite.Engine.Audio;
using Graphite.Engine.Scenes;
using Graphite.Game.Scenes;
using Graphite.Game.Sessions;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.UI.Tests;

internal static class BootstrapChecks
{
    internal static void Run(GraphicsDevice device, string output)
    {
        var desktop = (Desktop)typeof(Ui).GetField("_desktop", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        var session = new Session();
        session.ClearSector("fixture-sector");
        SessionManager.ActiveSession = session;
        try
        {
            using (var screen = Ui.Open<LoadingUI>())
            {
                screen.SetProgress(.5f);
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
                SceneManager.Draw(new GameTime());
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
            CheckPlay(desktop, session, device, output);
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

    private static void CheckPlay(Desktop desktop, Session original, GraphicsDevice device, string output)
    {
        Ui.Update(0);
        var buttons = desktop.Widgets.Single().GetChildren(true).OfType<MenuButton>().ToArray();
        string Text(MenuButton button) => button.GetChildren(true).OfType<Label>().Single().Text;
        Program.Check(buttons.Select(Text).SequenceEqual(new[] { "PLAY", "SETTINGS", "CREDITS", "QUIT" }),
            "One Play action replaces Continue, New Operation and Load Operation");
        Capture(device, output, "menu-play");
        var play = buttons.Single(button => Text(button) == "PLAY");
        var cached = AudioManager.Current.CachedClipCount;
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
        for (var frame = 0; frame < 2; frame++)
        {
            SceneManager.Draw(new GameTime());
            SceneManager.Update(0);
            SceneManager.CommitPendingChanges();
            Program.Check(ReferenceEquals(SessionManager.ActiveSession, original), "Partial preload does not replace active session");
        }
        Program.Check(meter.Value == 1, "Session preload reaches completion before entry");
        Ui.Update(0);
        Capture(device, output, "session-preloaded");
        SceneManager.Draw(new GameTime());
        SceneManager.Update(0);
        SceneManager.CommitPendingChanges();
        Program.Check(!ReferenceEquals(SessionManager.ActiveSession, original) && SessionManager.ActiveSession.ClearedSectors.Count == 0,
            "Successful preload publishes a fresh non-null session");
        Program.Check(desktop.Widgets.Count == 0 && AudioManager.Current.CachedClipCount == cached,
            "Session entry closes loading UI and reuses bootstrap audio assets");
        SceneManager.Load<MainMenuScene>();
        SceneManager.CommitPendingChanges();
        Program.Check(desktop.Widgets.Count == 1, "The main menu can be re-entered after playing");
        AudioManager.Current.MusicPlayer.Stop(0);
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
