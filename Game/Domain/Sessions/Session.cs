using Graphite.Engine.Persistence;

namespace Graphite.Game.Sessions;

[SaveContract("deep-drive.campaign", Version = 1)]
public sealed class Session
{
    [SaveMember("clearedSectors")]
    private HashSet<string> _clearedSectors = [];

    public IReadOnlyCollection<string> ClearedSectors => _clearedSectors.ToList();

    public void ClearSector(string sectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorId);
        _clearedSectors.Add(sectorId);
    }
}
