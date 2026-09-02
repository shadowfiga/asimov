namespace Graphite.Engine.UI.Controls;

public abstract class UIControl : IDisposable
{
    private bool _disposed;

    protected UIControl(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeCore() { }
}
