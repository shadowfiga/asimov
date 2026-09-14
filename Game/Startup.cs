using Graphite.Game.Configuration;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;

namespace Graphite.Game;

public static class Startup
{
    public static void Initialize()
    {
        MyraTheme.Apply(GameThemes.DeepDrive);
        PlayerPreferences.ApplyAudio();
    }
}
