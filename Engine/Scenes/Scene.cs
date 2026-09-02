using Graphite.Engine.Core;
using Graphite.Engine.UI;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Scenes;

public abstract class Scene
{
    private readonly List<Thing> _things = [];

    public IReadOnlyList<Thing> Things => _things;
    public UIScope UI { get; } = new();

    protected internal virtual void OnLoad() { }
    protected internal virtual void OnUnload() { }
    protected internal virtual void Draw(GameTime gameTime) { }

    public Thing Create(string name = "Thing")
    {
        var thing = new Thing(name, this);
        _things.Add(thing);
        return thing;
    }

    internal void UpdateInternal(float dt)
    {
        foreach (var behaviour in _things.SelectMany(thing => thing.Behaviours).Where(behaviour => behaviour.Enabled))
        {
            behaviour.EnsureStarted();
            behaviour.Update(dt);
        }

        foreach (var behaviour in _things.SelectMany(thing => thing.Behaviours).Where(behaviour => behaviour.Enabled))
        {
            behaviour.LateUpdate(dt);
        }
    }

    internal void UnloadInternal()
    {
        OnUnload();
        UI.CloseAll();
        foreach (var thing in _things)
        {
            thing.Destroy();
        }

        _things.Clear();
    }
}
