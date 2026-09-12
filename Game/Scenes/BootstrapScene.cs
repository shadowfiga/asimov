using Graphite.Engine.Scenes;
using Graphite.Game.Configuration;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    protected internal override void OnLoad()
    {
        LoadData();
        SceneManager.Load<MainMenuScene>();
    }

    private static void LoadData()
    {
        // Shared initialization runs here before the main menu opens.
        _ = GameSettings.Instance;
    }
}
