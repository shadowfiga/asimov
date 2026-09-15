using Graphite.Engine.Configuration;
using Graphite.Engine.Graphics;
using Graphite.Engine.Persistence;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.UI.Tests;

internal static class EnvironmentOverlayChecks
{
    public static void Run(GraphicsDevice device, string output)
    {
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        var theme = GameThemes.DeepDrive;
        var font = ThemeAssets.ResolveFont(ThemeAssets.Font(18), 1);
        try
        {
            Program.Check(EnvironmentOverlay.IsInitialized && EnvironmentOverlay.Text == "STAGING",
                "The environment print exists without an open scene or UI screen");
            Preferences.Set(RuntimePreferences.UiScale, .75f);
            var small = Render(1280, 720);
            Program.Check(small.Any(pixel => pixel.R > 100), "The environment print renders in the bottom-left corner");
            Program.Check(small.Any(pixel => pixel.R > 100 && pixel.R > pixel.G * 1.5f && pixel.R > pixel.B * 1.5f),
                "The environment print uses the theme's red");
            Preferences.Set(RuntimePreferences.UiScale, 1.75f);
            var large = Render(2560, 1440);
            Program.Check(small.AsSpan().SequenceEqual(large),
                "Environment glyphs and bottom-left offsets are pixel-identical across resolution and UI scale changes");

            EnvironmentOverlay.Shutdown();
            Program.Check(!EnvironmentOverlay.IsInitialized && EnvironmentOverlay.Text.Length == 0,
                "Overlay shutdown releases its renderer and environment");
            Program.Check(Render(1280, 720).All(pixel => pixel == theme.Background),
                "A shut-down overlay no longer draws");

            EnvironmentOverlay.Initialize(device, SettingsEnvironment.Production);
            EnvironmentOverlay.Configure(font, theme.Danger, theme.DeepBlack, theme.Spacing.Md);
            Program.Check(EnvironmentOverlay.Text == "PRODUCTION" && Render(1280, 720).Any(pixel => pixel.R > 100),
                "The print uses the host's active environment, including production");
        }
        finally
        {
            Preferences.Set(RuntimePreferences.UiScale, originalScale);
            EnvironmentOverlay.Initialize(device, SettingsEnvironment.Staging);
            EnvironmentOverlay.Configure(font, theme.Danger, theme.DeepBlack, theme.Spacing.Md);
        }

        Color[] Render(int width, int height)
        {
            using var target = new RenderTarget2D(device, width, height);
            device.SetRenderTarget(target);
            device.Clear(theme.Background);
            EnvironmentOverlay.Draw();
            device.SetRenderTarget(null);
            var pixels = new Color[200 * 60];
            target.GetData(0, new Rectangle(0, height - 60, 200, 60), pixels, 0, pixels.Length);
            if (width == 2560)
            {
                using var file = File.Create(Path.Combine(output, "environment-overlay-1440p.png"));
                target.SaveAsPng(file, width, height);
            }
            return pixels;
        }
    }
}
