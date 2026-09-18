using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Combat;

public sealed class ProjectileComponent : Component
{
    private readonly Vector2? _initialPreviousPosition;
    public Vector2 Velocity
    {
        get;
    }
    public float RemainingLife
    {
        get; private set;
    }
    public int Damage
    {
        get;
    }
    public Vector2 Position => Transform.WorldPosition;
    public Vector2 PreviousPosition
    {
        get; private set;
    }
    public ProjectileComponent(Vector2 velocity, float lifetime, int damage = 1, Vector2? previousPosition = null)
    {
        // A non-finite lifetime would leave an immortal projectile in the scene.
        if (!float.IsFinite(lifetime) || lifetime <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }
        if (damage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }
        if (previousPosition.HasValue)
        {
            Transform2D.Validate(previousPosition.Value);
        }
        Velocity = velocity;
        RemainingLife = lifetime;
        Damage = damage;
        _initialPreviousPosition = previousPosition;
    }
    protected override void OnAdded() => PreviousPosition = _initialPreviousPosition ?? Position;
    protected internal override void Update(float dt)
    {
        PreviousPosition = Position;
        RemainingLife -= dt;
        if (RemainingLife <= 0)
        {
            Owner.Destroy();
            return;
        }
        Transform.WorldPosition += Velocity * dt;
    }

    public bool IntersectsCircle(Vector2 center, float radius)
    {
        Transform2D.Validate(center);
        if (!float.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }
        var segment = Position - PreviousPosition;
        var lengthSquared = segment.LengthSquared();
        var amount = lengthSquared == 0
            ? 0
            : Math.Clamp(Vector2.Dot(center - PreviousPosition, segment) / lengthSquared, 0, 1);
        var closest = PreviousPosition + segment * amount;
        return Vector2.DistanceSquared(closest, center) <= radius * radius;
    }
}
