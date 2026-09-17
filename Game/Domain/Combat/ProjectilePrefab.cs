using Graphite.Engine.Objects;
using Graphite.Game.Graphics;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Combat;

public sealed class ProjectilePrefab(Vector2 velocity, float lifetime) : Prefab<ProjectileComponent>("Projectile")
{
    protected internal override ProjectileComponent Build(GameObject root)
    {
        var projectile = root.AddComponent(new ProjectileComponent(velocity, lifetime));
        root.AddComponent(new ProjectileRenderer());
        return projectile;
    }
}
