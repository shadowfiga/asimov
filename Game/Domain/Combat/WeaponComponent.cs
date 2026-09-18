using Chisel.Generated;
using Graphite.Engine.Objects;

namespace Graphite.Game.Domain.Combat;

/// <summary>One mounted weapon, with independent cadence. Its caller supplies the trigger, not raw input.</summary>
public sealed class WeaponComponent : Component
{
    private double _cooldown;
    public int WeaponId
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

    public WeaponComponent(int weaponId, Transform2D muzzle)
    {
        var roundsPerSecond = ChiselWeapons.RoundsPerSecond[weaponId];
        var projectileLifetime = ChiselWeapons.ProjectileLifetime[weaponId];
        // Non-positive cadence would make the firing loop stop advancing.
        if (!float.IsFinite(roundsPerSecond) || roundsPerSecond <= 0)
        {
            throw new InvalidDataException("Weapon firing rate must be finite and positive.");
        }
        // Catch-up skips expired rounds; an invalid lifetime would otherwise silently suppress every shot.
        if (!float.IsFinite(projectileLifetime) || projectileLifetime <= 0)
        {
            throw new InvalidDataException("Weapon projectile lifetime must be finite and positive.");
        }
        WeaponId = weaponId;
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
            var interval = 1d / ChiselWeapons.RoundsPerSecond[WeaponId];
            var projectileLifetime = ChiselWeapons.ProjectileLifetime[WeaponId];
            if (nextShot < dt - projectileLifetime)
            {
                nextShot += Math.Ceiling((dt - projectileLifetime - nextShot) / interval) * interval;
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
        var projectileLifetime = ChiselWeapons.ProjectileLifetime[WeaponId];
        if (age >= projectileLifetime)
        {
            return;
        }
        var velocity = Muzzle.Forward * ChiselWeapons.ProjectileSpeed[WeaponId];
        var origin = Muzzle.WorldPosition;
        // Scene root, deliberately not a weapon child: shots outlive and move independently of their gun.
        World.Spawn(new ProjectilePrefab(velocity, projectileLifetime - age,
                ChiselWeapons.ProjectileDamage[WeaponId], origin),
            origin + velocity * age, Muzzle.WorldRotation);
    }
}
