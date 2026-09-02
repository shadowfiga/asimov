namespace Graphite.Engine.UI;

public sealed class UIScope
{
    private readonly List<UIScreen> _screens = [];

    public T Open<T>() where T : UIScreen, new()
    {
        var screen = UI.Open<T>();
        _screens.Add(screen);
        return screen;
    }

    public void Close(UIScreen screen)
    {
        UI.Close(screen);
        _screens.Remove(screen);
    }

    internal void CloseAll()
    {
        foreach (var screen in _screens.ToArray())
        {
            UI.Close(screen);
        }

        _screens.Clear();
    }
}
