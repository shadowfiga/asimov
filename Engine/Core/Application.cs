namespace Graphite.Engine.Core;

public static class Application
{
    public static bool IsQuitRequested { get; private set; }

    public static void Quit()
    {
        IsQuitRequested = true;
    }

    internal static void Reset()
    {
        IsQuitRequested = false;
    }
}
