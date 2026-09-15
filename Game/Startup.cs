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
        PlayerPreferences.ApplyAudio();
        PostProcessing.Set(CrtPresentation.Aged);
        CrtFilter.Configure(PostProcessing.Parameters, Preferences.Get(PlayerPreferences.CrtIntensity));
    }
}
