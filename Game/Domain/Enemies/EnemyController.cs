using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Enemies;

/// <summary>Minimal Swarmer behavior: pursue the player and deal one contact hit.</summary>
public sealed class EnemyController : Component
{
    private GameObject? _target;
    private HealthComponent? _targetHealth;
    private float _targetBodyRadius;
    public ChiselEnemiesId EnemyId
    {
        get;
    }
    public GameObject Target => _target
        ?? throw new InvalidOperationException("Enemy target has not been assigned.");
    public HealthComponent Health => Owner.GetComponent<HealthComponent>();
    public float MoveSpeed
    {
        get;
    }
    public float BodyRadius
    {
        get;
    }
    public int ContactDamage
    {
        get;
    }
    public Vector2 Position => Transform.WorldPosition;

    public EnemyController(ChiselEnemiesId enemyId)
    {
        var index = enemyId.ToInt();
        EnemyId = enemyId;
        MoveSpeed = ChiselEnemies.MoveSpeed[index];
        BodyRadius = ChiselEnemies.BodyRadius[index];
        ContactDamage = ChiselEnemies.ContactDamage[index];
    }

    public void SetTarget(GameObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ObjectDisposedException.ThrowIf(target.IsDestroyed, target);
        if (target.World != World)
        {
            throw new InvalidOperationException("Enemy target must belong to the same world.");
        }
        var health = target.GetComponent<HealthComponent>();
        var player = target.GetComponent<PlayerController>();
        _target = target;
        _targetHealth = health;
        _targetBodyRadius = player.BodyRadius;
    }

    protected internal override void Update(float dt)
    {
        var target = Target;
        var targetHealth = _targetHealth
            ?? throw new InvalidOperationException("Enemy target health has not been assigned.");
        ObjectDisposedException.ThrowIf(target.IsDestroyed, target);
        ObjectDisposedException.ThrowIf(targetHealth.IsDisposed, targetHealth);
        if (targetHealth.IsDead)
        {
            return;
        }
        var offset = target.Transform.WorldPosition - Position;
        var distance = offset.Length();
        var contactDistance = BodyRadius + _targetBodyRadius;
        Transform.WorldRotation = MathF.Atan2(offset.Y, offset.X);
        if (distance <= contactDistance)
        {
            Contact();
            return;
        }
        var direction = offset / distance;
        var remaining = distance - contactDistance;
        var movement = MoveSpeed * dt;
        if (movement >= remaining)
        {
            Transform.WorldPosition += direction * remaining;
            Contact();
            return;
        }
        Transform.WorldPosition += direction * movement;
    }

    private void Contact()
    {
        _targetHealth!.ApplyDamage(ContactDamage);
        Owner.Destroy();
    }
}
