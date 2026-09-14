using Graphite.Engine.Persistence;

namespace Graphite.Game.Sessions;

[SaveContract("deep-drive.campaign", Version = 1)]
public sealed class Session : ISaveValidatable
{
    [SaveMember("playTimeSeconds")]
    public double PlayTimeSeconds { get; private set; }

    [SaveMember("clearedSectors")]
    private HashSet<string> _clearedSectors = [];

    public IReadOnlyCollection<string> ClearedSectors => _clearedSectors.ToArray();

    public void AddPlayTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || !double.IsFinite(PlayTimeSeconds + seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        PlayTimeSeconds += seconds;
    }

    public void ClearSector(string sectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorId);
        _clearedSectors.Add(sectorId);
    }

    public void Validate()
    {
        if (!double.IsFinite(PlayTimeSeconds) || PlayTimeSeconds < 0 || _clearedSectors.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("The campaign contains invalid play time or sector IDs.");
        }
    }
}
