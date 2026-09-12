using Graphite.Engine.Scenes;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    protected internal override void OnLoad() => SceneManager.Load<MainMenuScene>();
}
