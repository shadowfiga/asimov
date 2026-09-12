using Graphite.Engine.Core;
using Graphite.Engine.Persistence;
using Graphite.Game.Persistence;

namespace Graphite.Game.Sessions;

/// <summary>Owns the active data session. Mutate session data and request snapshots at the game update boundary.</summary>
public sealed class SessionManager : Singleton<SessionManager>
{
    private readonly SaveStore _saves;
    private readonly object _gate = new();
    private IReadOnlyList<SaveSlotInfo> _slots = [];
    private Session? _activeSession;
    private Guid? _activeSlot;
    private string? _slotName;
    private long _loadGeneration;

    private SessionManager() : this(GamePersistence.Saves) { }
    public SessionManager(SaveStore saves) => _saves = saves;

    public Session? ActiveSession { get { lock (_gate) { return _activeSession; } } }
    public Guid? ActiveSlotId { get { lock (_gate) { return _activeSlot; } } }
    public IReadOnlyList<SaveSlotInfo> Slots { get { lock (_gate) { return _slots; } } }

    public Session StartNew(string name = "")
    {
        lock (_gate)
        {
            _loadGeneration++;
            _activeSlot = Guid.NewGuid();
            _slotName = name;
            return _activeSession = new Session();
        }
    }

    public void EndSession()
    {
        lock (_gate)
        {
            _loadGeneration++;
            _activeSession = null;
            _activeSlot = null;
            _slotName = null;
        }
    }

    public async Task<SaveResult<Session>> LoadAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        long generation;
        lock (_gate) { generation = ++_loadGeneration; }
        var result = await _saves.LoadAsync<Session>(slotId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (generation != _loadGeneration)
            {
                throw new OperationCanceledException("The session load was superseded.");
            }

            if (result.IsSuccess)
            {
                _activeSession = result.Value!;
                _activeSlot = slotId;
                _slotName = null;
            }
        }

        return result;
    }

    public Task<SaveResult<SaveSlotInfo>> SaveAsync(CancellationToken cancellationToken = default)
    {
        Task<SaveResult<SaveSlotInfo>> pending;
        lock (_gate)
        {
            if (_activeSession is null || _activeSlot is null)
            {
                throw new InvalidOperationException("No session is active.");
            }

            // SaveAsync serializes before returning, before the next game update can mutate the session.
            pending = _saves.SaveAsync(_activeSlot.Value, _activeSession, _slotName, cancellationToken);
        }

        return RecordSaveAsync(pending);
    }

    private async Task<SaveResult<SaveSlotInfo>> RecordSaveAsync(Task<SaveResult<SaveSlotInfo>> pending)
    {
        var result = await pending.ConfigureAwait(false);
        if (result.Value is { } slot && result.IsSuccess)
        {
            lock (_gate)
            {
                _slots = Array.AsReadOnly(_slots.Where(item => item.Id != slot.Id).Append(slot).OrderByDescending(item => item.UpdatedUtc).ToArray());
            }
        }

        return result;
    }

    public async Task RefreshSlotsAsync(CancellationToken cancellationToken = default)
    {
        var slots = await _saves.ListSlotsAsync<Session>(cancellationToken).ConfigureAwait(false);
        lock (_gate) { _slots = Array.AsReadOnly(slots.ToArray()); }
    }
}
