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
        definition.Validate();
        return definition;
    }

    public void Validate()
    {
        foreach (var value in new[] { MoveSpeed, BodyRadius, ArmSpacing })
        {
            if (!float.IsFinite(value) || value <= 0)
            {
                throw new InvalidDataException("Robot dimensions and movement values must be finite and positive.");
            }
        }
        ArgumentNullException.ThrowIfNull(Weapon);
        Weapon.Validate();
    }
}
