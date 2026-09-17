using Chisel.Generated;
using Graphite.Engine.Persistence;

namespace Graphite.Game.Domain;

[SaveContract("deep-drive.loadout", Version = 1)]
public sealed class Loadout
{
    [SaveMember("chassis")]
    public ChiselChassisId ChassisId = ChiselChassisId.STARTER_MECH;

    [SaveMember("pilot")]
    public ChiselPilotId PilotId = ChiselPilotId.STARTER_PILOT;

    [SaveMember("weaponLeft")]
    public ChiselWeaponsId WeaponLeftId = ChiselWeaponsId.AUTOCANNON;

    [SaveMember("weaponRight")]
    public ChiselWeaponsId WeaponRightId = ChiselWeaponsId.AUTOCANNON;
}
