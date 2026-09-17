using Graphite.Engine.Persistence;
using Graphite.Game.Domain;

namespace Graphite.Game.Sessions;

[SaveContract("deep-drive.campaign", Version = 1)]
public sealed class Session
{
    [SaveMember("clearedSectors")]
    private HashSet<string> _clearedSectors = [];

    [SaveMember("currentLoadout")]
    private Loadout _currentLoadout = new();

    public Loadout CurrentLoadout => _currentLoadout;

    public IReadOnlyCollection<string> ClearedSectors => _clearedSectors.ToList();

    public void ClearSector(string sectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorId);
        _clearedSectors.Add(sectorId);
    }
}
