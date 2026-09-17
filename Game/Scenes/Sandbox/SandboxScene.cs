using Graphite.Engine.Scenes;
using Graphite.Engine.Audio;
using Graphite.Game.Audio;
using Graphite.Game.Domain.Player;
using Graphite.Game.Domain.Run;
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
    private SandboxUI _hud = null!;
    private Run _run = null!;
    private bool _previousCursorVisible;

    protected internal override void OnLoad()
    {
        var game = Myra.MyraEnvironment.Game;
        _previousCursorVisible = game.IsMouseVisible;
        var session = SessionManager.ActiveSession;
        _run = session.CurrentRun;
        var loadout = session.CurrentLoadout;
        Objects.MaxDeltaTime = .1f;
        _player = Objects.Spawn(new MechPrefab(loadout), Vector2.Zero);
        Objects.Spawn(new PlayerCameraPrefab(_player));
        _reticle = Objects.Spawn(new SandboxPresentationPrefab(_player));
        _hud = UI.Open<SandboxUI>();
        game.IsMouseVisible = false;
        GameAudio.PlaySession();
    }

    protected internal override void Update(float dt)
    {
        var game = Myra.MyraEnvironment.Game;
        var client = game.Window.ClientBounds.Size;
        Objects.Paused = !game.IsActive || client.X <= 0 || client.Y <= 0;
        _reticle.Enabled = !Objects.Paused;
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
        _run.AddTime(TimeSpan.FromSeconds(dt));
    }

    protected internal override void LateUpdate(float dt)
    {
        AudioManager.Current.Listener.Position = _player.Position;
        _hud.Refresh();
    }

    protected internal override void OnUnload()
    {
        Myra.MyraEnvironment.Game.IsMouseVisible = _previousCursorVisible;
        AudioManager.Current.Listener.Position = Vector2.Zero;
    }
}
