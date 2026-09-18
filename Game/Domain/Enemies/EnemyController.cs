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
    public ChiselEnemiesId EnemyId
    {
        get;
    }
    public PlayerController Target
    {
        get;
    }
    public HealthComponent Health
    {
        get;
    }
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

    public EnemyController(ChiselEnemiesId enemyId, PlayerController target, HealthComponent health)
    {
        var index = enemyId.ToInt();
        EnemyId = enemyId;
        Target = target;
        Health = health;
        MoveSpeed = ChiselEnemies.MoveSpeed[index];
        BodyRadius = ChiselEnemies.BodyRadius[index];
        ContactDamage = ChiselEnemies.ContactDamage[index];
    }

    protected override void OnAdded()
    {
        if (Target.World != World || Health.Owner != Owner)
        {
            throw new InvalidOperationException("An enemy and its target must belong to the same world, and health must belong to the enemy root.");
        }
    }

    protected internal override void Update(float dt)
    {
        if (Target.Health.IsDead)
        {
            return;
        }
        var offset = Target.Position - Position;
        var distance = offset.Length();
        var contactDistance = BodyRadius + Target.BodyRadius;
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
        Target.Health.ApplyDamage(ContactDamage);
        Owner.Destroy();
    }
}
