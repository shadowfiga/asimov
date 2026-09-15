using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

public abstract class UIScreen : IDisposable
{
    private Widget? _root;
    private Task<Animation.UIPlaybackState>[] _exits = [];
    public bool IsClosing
    {
        get; private set;
    }
    internal event Action<UIScreen>? Closed;

    public bool IsOpen
    {
        get; private set;
    }

    protected abstract Widget Build();

    internal void OpenInternal()
    {
        if (IsOpen)
        {
            return;
        }

        try
        {
            _root = Build()
                ?? throw new InvalidOperationException($"{GetType().FullName}.{nameof(Build)}() returned null.");
            UI.Attach(_root);
            IsOpen = true;
            Awake();
        }
        catch
        {
            IsOpen = false;
            ReleaseRoot();
            throw;
        }
    }

    internal void CloseInternal(bool immediate = false)
    {
        if (!IsOpen)
        {
            return;
        }

        if (immediate)
        {
            CompleteClose();
            return;
        }
        if (IsClosing)
        {
            return;
        }

        IsClosing = true;
        if (_root is not null)
        {
            _root.Enabled = false;
        }

        _exits = _root is null ? [] : UI.ExitHostsWithin(_root).Select(host => host.Hide()).ToArray();
        UpdateClose();
    }

    internal void UpdateClose()
    {
        if (IsClosing && _exits.All(exit => exit.IsCompleted))
        {
            CompleteClose();
        }
    }

    private void CompleteClose()
    {
        if (!IsOpen)
        {
            return;
        }

        try
        {
            OnDestroy();
        }
        finally
        {
            ReleaseRoot();
            IsOpen = false;
            IsClosing = false;
            _exits = [];
            Closed?.Invoke(this);
        }
    }

    protected virtual void Awake()
    {
    }
    protected virtual void OnDestroy()
    {
    }

    public void Dispose() => CloseInternal(true);

    private void ReleaseRoot()
    {
        if (_root is null)
        {
            return;
        }

        foreach (var host in UI.HostsWithin(_root))
        {
            host.Dispose();
        }

        UI.Detach(_root);
        _root = null;
    }
}
