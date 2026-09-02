using Graphite.Engine.Core;
using Graphite.Engine.UI;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Scenes;

public abstract class Scene
{
    private readonly List<Entity> _entities = [];

    public IReadOnlyList<Entity> Entities => _entities;
    public UIScope UI { get; } = new();

    protected internal virtual void OnLoad() { }
    protected internal virtual void OnUnload() { }
    protected internal virtual void Draw(GameTime gameTime) { }

    public Entity Create(string name = "Entity")
    {
        var entity = new Entity(name, this);
        _entities.Add(entity);
        return entity;
    }

    internal void UpdateInternal(float dt)
    {
        foreach (var behaviour in _entities.SelectMany(e => e.Behaviours).Where(b => b.Enabled))
        {
            behaviour.EnsureStarted();
            behaviour.Update(dt);
        }

        foreach (var behaviour in _entities.SelectMany(e => e.Behaviours).Where(b => b.Enabled))
            behaviour.LateUpdate(dt);
    }

    internal void UnloadInternal()
    {
        OnUnload();
        UI.CloseAll();
        foreach (var entity in _entities)
            entity.Destroy();
        _entities.Clear();
    }
}
