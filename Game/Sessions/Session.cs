using Graphite.Engine.Persistence;

namespace Graphite.Game.Sessions;

[SaveContract("aftergreen.session", Version = 1)]
public sealed class Session : ISaveValidatable
{
    [SaveMember("playTimeSeconds")]
    public double PlayTimeSeconds { get; private set; }

    [SaveMember("completedSites")]
    private HashSet<string> _completedSites = [];

    public IReadOnlyCollection<string> CompletedSites => _completedSites.ToArray();

    public void AddPlayTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || !double.IsFinite(PlayTimeSeconds + seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        PlayTimeSeconds += seconds;
    }

    public void CompleteSite(string siteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siteId);
        _completedSites.Add(siteId);
    }

    public void Validate()
    {
        if (!double.IsFinite(PlayTimeSeconds) || PlayTimeSeconds < 0 || _completedSites.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("The session contains invalid play time or site IDs.");
        }
    }
}
