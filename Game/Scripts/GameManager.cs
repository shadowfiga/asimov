using Graphite.Engine.Core;

namespace Graphite.Game.Scripts;

public sealed class GameManager : Singleton<GameManager>
{
    private Session? _currentSession;

    private GameManager() { }

    public Session CurrentSession
    {
        get => _currentSession
            ?? throw new InvalidOperationException("No session is currently active.");
        set => _currentSession = value
            ?? throw new ArgumentNullException(nameof(value));
    }

    public bool HasCurrentSession => _currentSession is not null;

    public Session StartPrototypeSession(string slotName = "Prototype Slot")
    {
        CurrentSession = new Session(slotName);
        return CurrentSession;
    }
}
