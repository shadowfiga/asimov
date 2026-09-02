using Graphite.Engine.Scenes;

namespace Graphite.Engine.Core;

public sealed class Thing
{
    private readonly List<Component> _components = [];

    internal Thing(string name, Scene scene)
    {
        Name = name;
        Scene = scene;
        Transform = new Transform();
    }

    public string Name { get; set; }
    public Scene Scene { get; }
    public Transform Transform { get; }
    public IReadOnlyList<Component> Components => _components;

    public T Add<T>() where T : Component, new()
    {
        var component = new T { Thing = this };
        _components.Add(component);
        if (component is Behaviour behaviour)
        {
            behaviour.Awake();
        }

        return component;
    }

    public T? Get<T>() where T : Component
        => _components.OfType<T>().FirstOrDefault();

    internal IEnumerable<Behaviour> Behaviours
        => _components.OfType<Behaviour>();

    internal void Destroy()
    {
        foreach (var behaviour in Behaviours)
        {
            behaviour.OnDestroy();
        }

        _components.Clear();
    }
}
