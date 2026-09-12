using Graphite.Engine.Scenes;
using Graphite.Game.UI;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

public sealed class MainMenuScene : Scene
{
    private MainMenuScreen _screen = null!;
    private KeyboardState _previous;

    protected internal override void OnLoad()
    {
        _screen = UI.Open<MainMenuScreen>();
        _previous = Keyboard.GetState();
    }

    protected internal override void Update(float dt)
    {
        var keyboard = Keyboard.GetState();
        if (Myra.MyraEnvironment.Game.IsActive && keyboard.IsKeyDown(Keys.Escape) && !_previous.IsKeyDown(Keys.Escape))
        {
            _screen.BackToMenu();
        }
        _previous = keyboard;
    }
}
