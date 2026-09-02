using Graphite.Engine.UI.Gum;
using Graphite.Engine.UI.Controls;

namespace Graphite.Engine.UI;

public abstract class UIScreen : IDisposable
{
    private GumScreenInstance? _visual;
    private IReadOnlyList<UIControl> _controls = [];

    public bool IsOpen { get; private set; }
    protected virtual string LayoutName => GetType().Name;

    internal void OpenInternal()
    {
        if (IsOpen)
        {
            return;
        }

        try
        {
            _visual = GumScreenInstance.Open(LayoutName);
            _controls = UIElementBinder.Bind(this, _visual);
            IsOpen = true;
            Awake();
        }
        catch
        {
            IsOpen = false;
            ReleaseVisual();
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
            ReleaseVisual();
            IsOpen = false;
        }
    }

    protected virtual void Awake() { }
    protected virtual void OnDestroy() { }

    public void Dispose() => CloseInternal();

    private void ReleaseVisual()
    {
        for (var index = _controls.Count - 1; index >= 0; index--)
        {
            _controls[index].Dispose();
        }

        _controls = [];
        _visual?.Dispose();
        _visual = null;
    }
}
