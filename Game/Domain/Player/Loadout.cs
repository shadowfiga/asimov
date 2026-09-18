using Chisel.Generated;
using Graphite.Engine.Persistence;
using System.Text.Json;

namespace Graphite.Game.Domain.Player;

[SaveContract("deep-drive.loadout", Version = 1)]
public sealed class Loadout
{
    [SaveMember("chassis")]
    private string ChassisSlug
    {
        get => Slug(ChiselChassis.Slugs, ChassisId);
        set => ChassisId = Id(ChiselChassis.Slugs, value);
    }

    [SaveMember("pilot")]
    private string PilotSlug
    {
        get => Slug(ChiselPilot.Slugs, PilotId);
        set => PilotId = Id(ChiselPilot.Slugs, value);
    }

    [SaveMember("weaponLeft")]
    private string WeaponLeftSlug
    {
        get => Slug(ChiselWeapons.Slugs, WeaponLeftId);
        set => WeaponLeftId = Id(ChiselWeapons.Slugs, value);
    }

    [SaveMember("weaponRight")]
    private string WeaponRightSlug
    {
        get => Slug(ChiselWeapons.Slugs, WeaponRightId);
        set => WeaponRightId = Id(ChiselWeapons.Slugs, value);
    }

    public int ChassisId = ChiselChassisIds.STARTER_MECH;
    public int PilotId = ChiselPilotIds.STARTER_PILOT;
    public int WeaponLeftId = ChiselWeaponsIds.AUTOCANNON;
    public int WeaponRightId = ChiselWeaponsIds.AUTOCANNON;

    private static int Id(string[] slugs, string slug)
    {
        var id = Array.IndexOf(slugs, slug);
        if (id < 0)
        {
            throw new JsonException($"Unknown Chisel id: {slug}");
        }
        return id;
    }

    private static string Slug(string[] slugs, int id)
    {
        if (id < 0 || id >= slugs.Length)
        {
            throw new JsonException($"Invalid Chisel id: {id}");
        }
        return slugs[id];
    }
}
