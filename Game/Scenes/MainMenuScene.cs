using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Game.UI;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

public sealed class MainMenuScene : Scene
{
    protected internal override void OnLoad()
    {
        var screen = UI.Open<MainMenuScreen>();
        Create("Main Menu Input").Add<MainMenuController>().Screen = screen;
    }
}

public sealed class MainMenuController : Behaviour
{
    public MainMenuScreen Screen { get; set; } = null!;
    private KeyboardState _previous;

    protected internal override void Start()
    {
        _previous = Keyboard.GetState();
    }

    protected internal override void Update(float dt)
    {
        var keyboard = Keyboard.GetState();
        if (Myra.MyraEnvironment.Game.IsActive && keyboard.IsKeyDown(Keys.Enter) && !_previous.IsKeyDown(Keys.Enter))
        {
            Screen.PlayGame();
        }
        _previous = keyboard;
    }
}
