using Chisel.Generated;
using Graphite.Game.Data;

namespace Graphite.Game.Domain.Combat;

public sealed record WeaponDefinition(float RoundsPerSecond, float ProjectileSpeed, float ProjectileLifetime, float BarrelLength)
{
    public static WeaponDefinition FromChisel(ChiselWeaponsId id)
    {
        var definition = new WeaponDefinition(ChiselWeapons.RoundsPerSecond[id.ToInt()], ChiselWeapons.ProjectileSpeed[id.ToInt()],
            ChiselWeapons.ProjectileLifetime[id.ToInt()], ChiselWeapons.BarrelLength[id.ToInt()]);
        definition.Validate();
        return definition;
    }

    public void Validate()
    {
        foreach (var value in new[] { RoundsPerSecond, ProjectileSpeed, ProjectileLifetime, BarrelLength })
        {
            if (!float.IsFinite(value) || value <= 0)
            {
                throw new InvalidDataException("Weapon values must be finite and positive.");
            }
        }
        if (RoundsPerSecond > 60 || ProjectileLifetime > 10)
        {
            throw new InvalidDataException("Prototype weapons support up to 60 rounds/s and a 10-second projectile lifetime.");
        }
    }
}
