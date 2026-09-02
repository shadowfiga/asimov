namespace Graphite.Engine.UI;

public static class UI
{
    internal static void Initialize() { }

    public static T Open<T>() where T : UIScreen, new()
    {
        var screen = new T();
        screen.OpenInternal();
        return screen;
    }

    public static void Close(UIScreen screen) => screen.CloseInternal();
}
