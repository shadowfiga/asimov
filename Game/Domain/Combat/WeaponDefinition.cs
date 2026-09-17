using Chisel.Generated;
using Graphite.Game.Data;

namespace Graphite.Game.Domain.Combat;

public sealed record WeaponDefinition(float RoundsPerSecond, float ProjectileSpeed, float ProjectileLifetime, float BarrelLength)
{
    public static WeaponDefinition FromChisel(ChiselWeaponsId id)
    {
        var definition = new WeaponDefinition(ChiselWeapons.RoundsPerSecond[id.ToInt()], ChiselWeapons.ProjectileSpeed[id.ToInt()],
            ChiselWeapons.ProjectileLifetime[id.ToInt()], ChiselWeapons.BarrelLength[id.ToInt()]);
        return definition;
    }
}
