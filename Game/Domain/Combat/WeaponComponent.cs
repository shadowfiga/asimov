using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Data;

namespace Graphite.Game.Domain.Combat;

/// <summary>One mounted weapon, with independent cadence. Its caller supplies the trigger, not raw input.</summary>
public sealed class WeaponComponent : Component
{
    private double _cooldown;
    public ChiselWeaponsId WeaponId
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

    public WeaponComponent(ChiselWeaponsId weaponId, Transform2D muzzle)
    {
        var roundsPerSecond = ChiselWeapons.RoundsPerSecond[weaponId.ToInt()];
        var projectileLifetime = ChiselWeapons.ProjectileLifetime[weaponId.ToInt()];
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
            var interval = 1d / ChiselWeapons.RoundsPerSecond[WeaponId.ToInt()];
            var projectileLifetime = ChiselWeapons.ProjectileLifetime[WeaponId.ToInt()];
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
        var projectileLifetime = ChiselWeapons.ProjectileLifetime[WeaponId.ToInt()];
        if (age >= projectileLifetime)
        {
            return;
        }
        var velocity = Muzzle.Forward * ChiselWeapons.ProjectileSpeed[WeaponId.ToInt()];
        // Scene root, deliberately not a weapon child: shots outlive and move independently of their gun.
        World.Spawn(new ProjectilePrefab(velocity, projectileLifetime - age),
            Muzzle.WorldPosition + velocity * age, Muzzle.WorldRotation);
    }
}
