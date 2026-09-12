namespace Graphite.Engine.UI;

public sealed class UIScope
{
    private readonly List<UIScreen> _screens = [];

    public T Open<T>() where T : UIScreen, new()
    {
        var screen = UI.Open<T>();
        _screens.Add(screen);
        screen.Closed += OnScreenClosed;
        return screen;
    }

    public void Close(UIScreen screen)
    {
        UI.Close(screen);
        if (!screen.IsOpen)
        {
            _screens.Remove(screen);
        }
    }

    private void OnScreenClosed(UIScreen screen)
    {
        screen.Closed -= OnScreenClosed;
        _screens.Remove(screen);
    }

    internal void CloseAll()
    {
        foreach (var screen in _screens.ToArray())
        {
            UI.Close(screen, immediate: true);
        }

        _screens.Clear();
    }
}
