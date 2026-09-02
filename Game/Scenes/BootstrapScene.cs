using Graphite.Engine.Scenes;

namespace Graphite.Game.Scenes;

public class BootstrapScene : Scene
{
    protected internal override void OnLoad()
    {
        LoadData();
        SceneManager.Load<MainMenuScene>();
    }

    private void LoadData()
    {
        // TODO: Load all save files
    }
}