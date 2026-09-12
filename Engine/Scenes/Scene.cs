using Graphite.Engine.UI;

namespace Graphite.Engine.Scenes;

public abstract class Scene
{
    public UIScope UI { get; } = new();

    protected internal virtual void OnLoad() { }
    protected internal virtual void Update(float dt) { }
    protected internal virtual void OnUnload() { }

    internal void UnloadInternal()
    {
        try
        {
            OnUnload();
        }
        finally
        {
            UI.CloseAll();
        }
    }
}
