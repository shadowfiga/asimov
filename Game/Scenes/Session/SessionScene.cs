using Graphite.Engine.Scenes;
using Chisel.Generated;
using Graphite.Engine.Audio;
using Graphite.Engine.Graphics;
using Graphite.Game.Audio;
using Graphite.Game.Domain.Player;
using Graphite.Game.Graphics;
using Graphite.Game.Sessions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

/// <summary>First top-down movement/aim/fire prototype. Game behavior stays in PlayerController.</summary>
public sealed class SessionScene : Scene
{
    private readonly PlayerInput _input = new();
    private readonly Camera2D _camera = new();
    private PlayerController _player = null!;
    private RobotRenderer _renderer = null!;
    private bool _previousCursorVisible;

    protected internal override void OnLoad()
    {
        _ = SessionManager.ActiveSession;
        _player = new PlayerController(RobotDefinition.FromChisel(ChiselRobotsId.STARTER_MECH), Vector2.Zero);
        var game = Myra.MyraEnvironment.Game;
        _renderer = new RobotRenderer(game.GraphicsDevice);
        _previousCursorVisible = game.IsMouseVisible;
        game.IsMouseVisible = false;
        RefreshCamera();
        GameAudio.PlaySession();
    }

    protected internal override void Update(float dt)
    {
        var game = Myra.MyraEnvironment.Game;
        RefreshCamera();
        var client = game.Window.ClientBounds.Size;
        if (client.X <= 0 || client.Y <= 0)
        {
            return;
        }
        var controls = _input.Read(Keyboard.GetState(), Mouse.GetState(), client, _camera, game.IsActive);
        if (_input.BackRequested)
        {
            SceneManager.Load<MainMenuScene>();
            return;
        }
        game.IsMouseVisible = !game.IsActive;
        if (!game.IsActive)
        {
            return;
        }
        // Do not fast-forward movement or burst a backlog of shots after a window drag/stall.
        dt = Math.Clamp(dt, 0, .1f);
        _player.Update(dt, controls);
        _camera.Position = _player.Position;
        AudioManager.Current.Listener.Position = _player.Position;
    }

    protected internal override void Draw(GameTime gameTime)
    {
        RefreshCamera();
        _renderer.Draw(_player, _camera, Myra.MyraEnvironment.Game.IsActive);
    }

    private void RefreshCamera()
    {
        var presentation = Myra.MyraEnvironment.Game.GraphicsDevice.PresentationParameters;
        var size = new Point(presentation.BackBufferWidth, presentation.BackBufferHeight);
        _camera.SetViewport(size, Math.Min(size.X / 1600f, size.Y / 900f));
        _camera.Position = _player.Position;
    }

    protected internal override void OnUnload()
    {
        _renderer.Dispose();
        Myra.MyraEnvironment.Game.IsMouseVisible = _previousCursorVisible;
        AudioManager.Current.Listener.Position = Vector2.Zero;
    }
}
