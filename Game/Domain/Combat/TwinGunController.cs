using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Combat;

public readonly record struct WeaponPose(Vector2 Pivot, Vector2 Direction, Vector2 Muzzle);
public readonly record struct Projectile(Vector2 Position, Vector2 Velocity, float RemainingLife);

/// <summary>Two mounts of the same weapon family; frame-rate independent cadence and bounded projectile lifetime.</summary>
public sealed class TwinGunController
{
    private readonly WeaponDefinition _definition;
    private readonly List<Projectile> _projectiles = [];
    private double _cooldown;
    public IReadOnlyList<Projectile> Projectiles
    {
        get;
    }

    public TwinGunController(WeaponDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        _definition = definition;
        Projectiles = _projectiles.AsReadOnly();
    }

    public void Update(float dt, bool firing, WeaponPose left, WeaponPose right)
    {
        if (!float.IsFinite(dt) || dt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dt));
        }
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            var life = projectile.RemainingLife - dt;
            if (life <= 0)
            {
                _projectiles.RemoveAt(i);
            }
            else
            {
                _projectiles[i] = projectile with
                {
                    Position = projectile.Position + projectile.Velocity * dt,
                    RemainingLife = life
                };
            }
        }
        var nextShot = _cooldown;
        if (firing)
        {
            // Skip already-expired rounds after unusually long updates.
            var interval = 1d / _definition.RoundsPerSecond;
            if (nextShot < dt - _definition.ProjectileLifetime)
            {
                nextShot += Math.Ceiling((dt - _definition.ProjectileLifetime - nextShot) / interval) * interval;
            }
            while (nextShot < dt)
            {
                var age = (float)(dt - nextShot);
                Spawn(left, age);
                Spawn(right, age);
                nextShot += interval;
            }
        }
        _cooldown = Math.Max(0, nextShot - dt);
    }

    private void Spawn(WeaponPose pose, float age)
    {
        if (age >= _definition.ProjectileLifetime)
        {
            return;
        }
        var velocity = pose.Direction * _definition.ProjectileSpeed;
        _projectiles.Add(new Projectile(pose.Muzzle + velocity * age, velocity, _definition.ProjectileLifetime - age));
    }
}
