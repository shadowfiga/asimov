using System.Reflection;
using Graphite.Engine.Persistence;
using Graphite.Engine.Scenes;
using Graphite.Game.Scenes;
using Graphite.Game.Sessions;
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
        session.AddPlayTime(42);
        SessionManager.ActiveSession = session;
        try
        {
            using (var screen = Ui.Open<BootstrapUI>())
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
            Program.Check(ReferenceEquals(SessionManager.ActiveSession, session) && session.PlayTimeSeconds == 42,
                "Bootstrap preloads only UI and does not replace, load, or advance the active session");
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
