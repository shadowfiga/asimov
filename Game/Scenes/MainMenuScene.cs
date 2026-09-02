using Graphite.Engine.Scenes;
using Graphite.Game.UI;

namespace Graphite.Game.Scenes;

public sealed class MainMenuScene : Scene
{
    protected internal override void OnLoad()
    {
        UI.Open<MainMenuScreen>();
    }
}
