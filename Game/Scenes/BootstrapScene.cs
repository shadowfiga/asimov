using Graphite.Engine.Scenes;
using Graphite.Game.Aftergreen;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    protected internal override void OnLoad()
    {
        LoadData();

        if (SliceDiagnostics.RenderDirectory != null)
        {
            SceneManager.Load<AftergreenScene>();
        }
        else
        {
            SceneManager.Load<MainMenuScene>();
        }
    }

    private static void LoadData()
    {
        // Run shared startup initialization here before opening the menu or gameplay.
        // Validate authored balance data early; expedition saves load when Play is chosen.
        _ = SliceConfig.Load();
    }
}
