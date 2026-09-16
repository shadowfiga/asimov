using System.Diagnostics.CodeAnalysis;

namespace Graphite.Game.Sessions;

/// <summary>The active in-memory session, shared across scenes. No persistence or slot management.</summary>
public static class SessionManager
{
    private static Session? _activeSession;

    /// <summary>Returns the active session or throws if none is set. Assign null to clear it.</summary>
    [AllowNull]
    public static Session ActiveSession
    {
        get => _activeSession ?? throw new InvalidOperationException("No active session is set. Set SessionManager.ActiveSession before accessing it.");
        set => _activeSession = value;
    }
}
