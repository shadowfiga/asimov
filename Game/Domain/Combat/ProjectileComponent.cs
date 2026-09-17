using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Combat;

public sealed class ProjectileComponent : Component
{
    public Vector2 Velocity
    {
        get;
    }
    public float RemainingLife
    {
        get; private set;
    }
    public Vector2 Position => Transform.WorldPosition;
    public ProjectileComponent(Vector2 velocity, float lifetime)
    {
        Transform2D.Validate(velocity);
        if (!float.IsFinite(lifetime) || lifetime <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }
        Velocity = velocity;
        RemainingLife = lifetime;
    }
    protected internal override void Update(float dt)
    {
        RemainingLife -= dt;
        if (RemainingLife <= 0)
        {
            Owner.Destroy();
            return;
        }
        Transform.WorldPosition += Velocity * dt;
    }
}
