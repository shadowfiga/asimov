using Graphite.Engine.Graphics;
using Graphite.Engine.Persistence;
using Graphite.Game.Configuration;
using Graphite.Game.Graphics;
using Graphite.Game.UI.Theming;

namespace Graphite.Game;

public static class Startup
{
    public static void Initialize()
    {
        MyraTheme.Apply(GameThemes.DeepDrive);
        var theme = GameThemes.DeepDrive;
        EnvironmentOverlay.Configure(ThemeAssets.ResolveFont(ThemeAssets.Font(18), 1),
            theme.Danger, theme.DeepBlack, theme.Spacing.Md);
        PlayerPreferences.ApplyAudio();
        PostProcessing.Set(CrtPresentation.Aged);
        CrtFilter.Configure(PostProcessing.Parameters, Preferences.Get(PlayerPreferences.CrtIntensity));
    }
}
