using Graphite.Engine.Scenes;
using Graphite.Engine.Audio;
using Graphite.Engine.Graphics;
using Graphite.Game.Audio;
using Graphite.Game.Domain.Player;
using Graphite.Game.Graphics;
using Graphite.Game.Sessions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

/// <summary>Sandbox for the top-down movement/aim/fire prototype. Game behavior stays in PlayerController.</summary>
public sealed class SandboxScene : Scene
{
    private readonly PlayerInput _input = new();
    private PlayerController _player = null!;
    private ReticleRenderer _reticle = null!;
    private bool _previousCursorVisible;

    protected internal override void OnLoad()
    {
        var game = Myra.MyraEnvironment.Game;
        _previousCursorVisible = game.IsMouseVisible;
        var session = SessionManager.ActiveSession;
        _ = session.CurrentRun;
        var loadout = session.CurrentLoadout;
        Objects.MaxDeltaTime = .1f;
        _player = Objects.Spawn(new RobotPrefab(loadout), Vector2.Zero);
        _reticle = Objects.Spawn(new SandboxPresentationPrefab(_player));
        game.IsMouseVisible = false;
        RefreshCamera();
        GameAudio.PlaySession();
    }

    protected internal override void Update(float dt)
    {
        var game = Myra.MyraEnvironment.Game;
        RefreshCamera();
        var client = game.Window.ClientBounds.Size;
        Objects.Paused = !game.IsActive || client.X <= 0 || client.Y <= 0;
        _reticle.Enabled = !Objects.Paused;
        if (client.X <= 0 || client.Y <= 0)
        {
            return;
        }
        var controls = _input.Read(Keyboard.GetState(), Mouse.GetState(), client, Camera, game.IsActive);
        if (_input.BackRequested)
        {
            Objects.Paused = true;
            SceneManager.Load<MainMenuScene>();
            return;
        }
        game.IsMouseVisible = !game.IsActive;
        if (!game.IsActive)
        {
            return;
        }
        _player.Controls = controls;
    }

    protected internal override void LateUpdate(float dt)
    {
        Camera.Position = _player.Position;
        AudioManager.Current.Listener.Position = _player.Position;
    }

    protected internal override void Draw(GameTime gameTime)
    {
        RefreshCamera();
    }

    private void RefreshCamera()
    {
        var presentation = Myra.MyraEnvironment.Game.GraphicsDevice.PresentationParameters;
        var size = new Point(presentation.BackBufferWidth, presentation.BackBufferHeight);
        Camera.SetViewport(size, Math.Min(size.X / 1600f, size.Y / 900f));
        Camera.Position = _player.Position;
    }

    protected internal override void OnUnload()
    {
        Myra.MyraEnvironment.Game.IsMouseVisible = _previousCursorVisible;
        AudioManager.Current.Listener.Position = Vector2.Zero;
    }
}
