namespace Graphite.Engine.UI;

public abstract class UIScreen : IDisposable
{
    public bool IsOpen { get; private set; }

    internal void OpenInternal()
    {
        if (IsOpen) return;
        IsOpen = true;
        OnOpen();
    }

    internal void CloseInternal()
    {
        if (!IsOpen) return;
        OnClose();
        IsOpen = false;
    }

    protected abstract void OnOpen();
    protected abstract void OnClose();

    public void Dispose() => CloseInternal();
}
