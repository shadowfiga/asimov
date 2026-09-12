using System.Reflection;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

/// <summary>Myra 1.6.5 exposes drawing separately but keeps event dispatch internal.
/// Keep this version-specific bridge in one place; never process input from a material pass.</summary>
internal static class MyraInput
{
    private static readonly Action<Widget, InputContext> ProcessWidget = typeof(Widget)
        .GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<Action<Widget, InputContext>>();
    private static readonly Type Events = typeof(Widget).Assembly.GetType("Myra.Graphics2D.UI.InputEventsManager", true)!;
    private static readonly Action Dispatch = Events.GetMethod("ProcessEvents", BindingFlags.Public | BindingFlags.Static)!
        .CreateDelegate<Action>();
    private static readonly Action<Widget, InputEventType> Queue = Events.GetMethod("Queue", BindingFlags.Public | BindingFlags.Static)!
        .CreateDelegate<Action<Widget, InputEventType>>();
    private static readonly InputContext Context = new();

    internal static void Update(Desktop desktop)
    {
        desktop.UpdateLayout();
        desktop.UpdateInput();
        Context.Reset();
        foreach (var widget in desktop.Widgets.OrderBy(widget => widget.ZIndex).Reverse().ToArray())
        {
            ProcessWidget(widget, Context);
        }

        if (Context.MouseWheelWidget is { } wheel)
        {
            Queue(wheel, InputEventType.MouseWheel);
        }

        Dispatch();
        desktop.UpdateLayout();
    }
}
