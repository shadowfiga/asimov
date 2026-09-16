using Graphite.Engine.Scenes;
using Graphite.Game.Audio;
using Graphite.Game.Sessions;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

/// <summary>Session entry point. Gameplay/world rendering is not implemented yet.</summary>
public sealed class SessionScene : Scene
{
    private KeyboardState _previous;

    protected internal override void OnLoad()
    {
        _ = SessionManager.ActiveSession;
        GameAudio.PlaySession();
        _previous = Keyboard.GetState();
    }

    protected internal override void Update(float dt)
    {
        var keyboard = Keyboard.GetState();
        if (Myra.MyraEnvironment.Game.IsActive && keyboard.IsKeyDown(Keys.Escape) && !_previous.IsKeyDown(Keys.Escape))
        {
            SceneManager.Load<MainMenuScene>();
        }
        _previous = keyboard;
    }
}
