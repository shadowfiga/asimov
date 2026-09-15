using Graphite.Engine.Scenes;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

public sealed class MainMenuScene : Scene
{
    private MainMenuUI _screen = null!;
    private KeyboardState _previous;

    protected internal override void OnLoad()
    {
        _screen = UI.Open<MainMenuUI>();
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
