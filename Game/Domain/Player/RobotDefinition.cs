using Chisel.Generated;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;

namespace Graphite.Game.Domain.Player;

/// <summary>Validated immutable projection of authored Chisel data, not mutable session state.</summary>
public sealed record RobotDefinition(float MoveSpeed, float BodyRadius, float ArmSpacing, WeaponDefinition Weapon)
{
    public static RobotDefinition FromChisel(ChiselRobotsId id)
    {
        var definition = new RobotDefinition(ChiselRobots.MoveSpeed[id.ToInt()], ChiselRobots.BodyRadius[id.ToInt()],
            ChiselRobots.ArmSpacing[id.ToInt()], WeaponDefinition.FromChisel(ChiselRobots.Weapon[id.ToInt()]));
        return definition;
    }
}
