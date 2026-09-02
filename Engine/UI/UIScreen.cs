using Graphite.Engine.UI.Gum;

namespace Graphite.Engine.UI;

public abstract class UIScreen : IDisposable
{
    private GumScreenInstance? _visual;

    public bool IsOpen { get; private set; }
    protected virtual string LayoutName => GetType().Name;

    internal void OpenInternal()
    {
        if (IsOpen)
            return;

        try
        {
            _visual = GumScreenInstance.Open(LayoutName);
            UIElementBinder.Bind(this, _visual);
            IsOpen = true;
            Awake();
        }
        catch
        {
            IsOpen = false;
            _visual?.Dispose();
            _visual = null;
            throw;
        }
    }

    internal void CloseInternal()
    {
        if (!IsOpen)
            return;

        try
        {
            OnDestroy();
        }
        finally
        {
            _visual?.Dispose();
            _visual = null;
            IsOpen = false;
        }
    }

    protected virtual void Awake() { }
    protected virtual void OnDestroy() { }

    public void Dispose() => CloseInternal();
}
