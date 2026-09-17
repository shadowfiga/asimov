using Graphite.Engine.Persistence;
using Graphite.Game.Domain.Player;
using Graphite.Game.Domain.Run;

namespace Graphite.Game.Sessions;

[SaveContract("deep-drive.campaign", Version = 1)]
public sealed class Session
{
    [SaveMember("clearedSectors")]
    private HashSet<string> _clearedSectors = [];

    [SaveMember("currentLoadout")]
    private Loadout _currentLoadout = new();

    [SaveMember("currentRun")]
    private Run? _currentRun;

    public Loadout CurrentLoadout => _currentLoadout;

    public Run CurrentRun => _currentRun ?? throw new InvalidOperationException("No run has been started. Call Session.StartRun before entering gameplay.");

    public IReadOnlyCollection<string> ClearedSectors => _clearedSectors.ToList();

    public void StartRun()
    {
        _currentRun = new Run();
    }

    public void ClearSector(string sectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorId);
        _clearedSectors.Add(sectorId);
    }
}
