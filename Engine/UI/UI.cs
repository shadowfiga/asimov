using Myra;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

public static class UI
{
    private static Desktop? _desktop;

    internal static void Initialize(Microsoft.Xna.Framework.Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        Shutdown();
        MyraEnvironment.Game = game;
        _desktop = new Desktop();
    }

    public static T Open<T>() where T : UIScreen, new()
    {
        var screen = new T();
        screen.OpenInternal();
        return screen;
    }

    public static void Close(UIScreen screen) => screen.CloseInternal();

    internal static void Attach(Widget root)
    {
        var desktop = _desktop
            ?? throw new InvalidOperationException("Graphite UI has not been initialized.");
        desktop.Widgets.Add(root);
    }

    internal static void Detach(Widget root)
    {
        _desktop?.Widgets.Remove(root);
    }

    internal static void Draw()
    {
        _desktop?.Render();
    }

    internal static void Shutdown()
    {
        _desktop?.Dispose();
        _desktop = null;
    }
}
