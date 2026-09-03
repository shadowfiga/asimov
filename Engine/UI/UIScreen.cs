using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

public abstract class UIScreen : IDisposable
{
    private Widget? _root;

    public bool IsOpen { get; private set; }

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

    internal void CloseInternal()
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
        }
    }

    protected virtual void Awake() { }
    protected virtual void OnDestroy() { }

    public void Dispose() => CloseInternal();

    private void ReleaseRoot()
    {
        if (_root is null)
        {
            return;
        }

        UI.Detach(_root);
        _root = null;
    }
}
