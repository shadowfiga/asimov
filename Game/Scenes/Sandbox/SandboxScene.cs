using Chisel.Generated;
using Graphite.Engine.Scenes;
using Graphite.Engine.Audio;
using Graphite.Engine.Objects;
using Graphite.Game.Audio;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Enemies;
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
        var playerObject = Objects.GetGameObjectByName(MechPrefab.PlayerObjectName);
        Objects.Spawn(new PlayerCameraPrefab()).SetTarget(playerObject);
        Objects.Spawn(new EnemyPrefab(ChiselEnemiesId.SWARMER), new Vector2(500, 0)).SetTarget(playerObject);
        _reticle = Objects.Spawn(new SandboxPresentationPrefab());
        _reticle.SetTarget(playerObject);
        _hud = UI.Open<SandboxUI>();
        _hud.BindHealth(playerObject.GetComponent<HealthComponent>());
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
        if (!Objects.Paused)
        {
            ResolveProjectileHits(Objects, _run);
        }
        AudioManager.Current.Listener.Position = _player.Position;
        _hud.Refresh();
    }

    internal static void ResolveProjectileHits(GameWorld objects, Run run)
    {
        var projectiles = objects.GetComponents<ProjectileComponent>().ToArray();
        var enemies = objects.GetComponents<EnemyController>().ToArray();
        foreach (var projectile in projectiles)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.IsDisposed || enemy.Health.IsDead
                    || !projectile.IntersectsCircle(enemy.Position, enemy.BodyRadius))
                {
                    continue;
                }
                var killed = enemy.Health.ApplyDamage(projectile.Damage);
                projectile.Owner.Destroy();
                if (killed)
                {
                    run.RecordKill(enemy.EnemyId);
                    enemy.Owner.Destroy();
                }
                break;
            }
        }
    }

    protected internal override void OnUnload()
    {
        Myra.MyraEnvironment.Game.IsMouseVisible = _previousCursorVisible;
        AudioManager.Current.Listener.Position = Vector2.Zero;
    }
}
