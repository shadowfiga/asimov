using Graphite.Engine.Scenes;
using Graphite.Game.UI.Theming;
using Graphite.Game.UI.Materials;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    protected internal override void OnLoad()
    {
        MyraTheme.Apply(GameThemes.Aftergreen);
        MenuPresentation.Initialize();
        SceneManager.Load<MainMenuScene>();
    }
}
