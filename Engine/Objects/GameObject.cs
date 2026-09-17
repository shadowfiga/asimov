namespace Graphite.Engine.Objects;

public sealed class GameObject : IDisposable
{
    private readonly List<GameObject> _children = [];
    private readonly List<Component> _components = [];
    public string Name
    {
        get;
    }
    public GameWorld World
    {
        get;
    }
    public Transform2D Transform
    {
        get;
    }
    public GameObject? Parent
    {
        get; private set;
    }
    public IReadOnlyList<GameObject> Children
    {
        get;
    }
    public IReadOnlyList<Component> Components
    {
        get;
    }
    public bool Active { get; set; } = true;
    public bool IsDestroyed
    {
        get; private set;
    }
    public bool ActiveInHierarchy => !IsDestroyed && Active && (Parent?.ActiveInHierarchy ?? true);
    internal int Depth => (Parent?.Depth ?? -1) + 1;

    internal GameObject(GameWorld world, string name)
    {
        World = world;
        Name = name;
        Transform = new Transform2D(this);
        Children = _children.AsReadOnly();
        Components = _components.AsReadOnly();
    }

    public GameObject CreateChild(string name) => World.CreateChild(this, name);
    public T AddComponent<T>(T component) where T : Component
    {
        ArgumentNullException.ThrowIfNull(component);
        ObjectDisposedException.ThrowIf(IsDestroyed, this);
        component.Attach(this);
        _components.Add(component);
        World.Register(component);
        try
        {
            component.Initialize();
            return component;
        }
        catch (Exception failure)
        {
            try
            {
                RemoveComponent(component);
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Component initialization and cleanup failed.", failure, cleanupError);
            }
            throw;
        }
    }

    public T? TryGetComponent<T>() where T : Component => _components.OfType<T>().FirstOrDefault();
    public T GetComponent<T>() where T : Component => TryGetComponent<T>()
        ?? throw new InvalidOperationException($"{Name} has no {typeof(T).Name} component.");
    public bool RemoveComponent(Component component)
    {
        if (!_components.Remove(component))
        {
            return false;
        }
        World.Unregister(component);
        component.Detach();
        return true;
    }

    public void SetParent(GameObject? parent, bool keepWorldTransform = false)
    {
        ObjectDisposedException.ThrowIf(IsDestroyed, this);
        if (parent is not null && (parent.IsDestroyed || parent.World != World))
        {
            throw new InvalidOperationException("Parent must be a live object in the same world.");
        }
        for (var ancestor = parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor == this)
            {
                throw new InvalidOperationException("Object hierarchies cannot contain cycles.");
            }
        }
        if (parent == Parent)
        {
            return;
        }
        var local = keepWorldTransform
            ? Transform2D.Decompose(Transform.WorldMatrix * (parent is null ? Microsoft.Xna.Framework.Matrix.Identity : Microsoft.Xna.Framework.Matrix.Invert(parent.Transform.WorldMatrix)))
            : (Transform.LocalPosition, Transform.LocalRotation, Transform.LocalScale);
        Unlink();
        Parent = parent;
        if (parent is null)
        {
            World.AddRoot(this);
        }
        else
        {
            parent._children.Add(this);
        }
        Transform.LocalPosition = local.Item1;
        Transform.LocalRotation = local.Item2;
        Transform.LocalScale = local.Item3;
    }

    private void Unlink()
    {
        if (Parent is null)
        {
            World.RemoveRoot(this);
        }
        else
        {
            Parent._children.Remove(this);
        }
    }

    public void Destroy()
    {
        if (IsDestroyed)
        {
            return;
        }
        IsDestroyed = true;
        // Unlink before callbacks: a child's cleanup may destroy its former parent.
        Unlink();
        List<Exception> errors = [];
        while (_children.Count > 0)
        {
            try
            {
                _children[^1].Destroy();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        while (_components.Count > 0)
        {
            try
            {
                RemoveComponent(_components[^1]);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        Parent = null;
        if (errors.Count > 0)
        {
            throw new AggregateException("Object cleanup failed.", errors);
        }
    }

    public void Dispose() => Destroy();
}
