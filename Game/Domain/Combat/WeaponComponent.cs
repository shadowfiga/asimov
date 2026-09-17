using Graphite.Engine.Objects;
using Graphite.Game.Graphics;

namespace Graphite.Game.Domain.Combat;

/// <summary>One mounted weapon, with independent cadence. Its caller supplies the trigger, not raw input.</summary>
public sealed class WeaponComponent : Component
{
    private double _cooldown;
    public WeaponDefinition Definition
    {
        get;
    }
    public Transform2D Muzzle
    {
        get;
    }
    public bool TriggerHeld
    {
        get; set;
    }
    public override int UpdateOrder => 200;

    public WeaponComponent(WeaponDefinition definition, Transform2D muzzle)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(muzzle);
        definition.Validate();
        Definition = definition;
        Muzzle = muzzle;
    }

    protected override void OnAdded()
    {
        if (Muzzle.Owner.Parent != Owner || Muzzle.Owner.IsDestroyed)
        {
            throw new InvalidOperationException("The muzzle must be a live child of the weapon object.");
        }
    }
    protected internal override void Update(float dt)
    {
        var nextShot = _cooldown;
        if (TriggerHeld)
        {
            ObjectDisposedException.ThrowIf(Muzzle.Owner.IsDestroyed, Muzzle.Owner);
            var interval = 1d / Definition.RoundsPerSecond;
            if (nextShot < dt - Definition.ProjectileLifetime)
            {
                nextShot += Math.Ceiling((dt - Definition.ProjectileLifetime - nextShot) / interval) * interval;
            }
            while (nextShot < dt)
            {
                Spawn((float)(dt - nextShot));
                nextShot += interval;
            }
        }
        _cooldown = Math.Max(0, nextShot - dt);
    }

    private void Spawn(float age)
    {
        if (age >= Definition.ProjectileLifetime)
        {
            return;
        }
        var velocity = Muzzle.Forward * Definition.ProjectileSpeed;
        // Scene root, deliberately not a weapon child: shots outlive and move independently of their gun.
        var bullet = World.Create("Projectile");
        bullet.Transform.WorldPosition = Muzzle.WorldPosition + velocity * age;
        bullet.Transform.WorldRotation = Muzzle.WorldRotation;
        bullet.AddComponent(new ProjectileComponent(velocity, Definition.ProjectileLifetime - age));
        bullet.AddComponent(new ProjectileRenderer());
    }
}
