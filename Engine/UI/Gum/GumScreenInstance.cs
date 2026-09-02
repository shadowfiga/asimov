using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;

namespace Graphite.Engine.UI.Gum;

internal sealed class GumScreenInstance : IDisposable
{
    private readonly GraphicalUiElement _root;

    private GumScreenInstance(GraphicalUiElement root)
    {
        _root = root;
    }

    public static GumScreenInstance Open(string screenName)
    {
        var screen = ObjectFinder.Self.GetScreen(screenName)
            ?? throw new InvalidOperationException($"Gum screen '{screenName}' was not found in the loaded project.");
        var root = screen.ToGraphicalUiElement(GumService.Default.SystemManagers);
        root.AddToRoot();
        return new GumScreenInstance(root);
    }

    public T Get<T>(string elementName) where T : FrameworkElement
    {
        try
        {
            return _root.FindFormsControl<T>(elementName)
                ?? throw new InvalidOperationException($"No matching Gum Forms control was found.");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Gum element '{elementName}' on screen '{_root.Name}' could not bind to {typeof(T).Name}.",
                exception);
        }
    }

    public void Dispose()
    {
        _root.RemoveFromManagers();
    }
}
