using Graphite.Engine.Graphics;

namespace Graphite.Engine.Objects;

/// <summary>Owns roots and component scheduling. New components start updating on the next tick.</summary>
public sealed class GameWorld : IDisposable
{
    private readonly List<GameObject> _roots = [];
    private readonly List<Component> _components = [];
    private readonly List<Component> _updates = [];
    private readonly List<RenderComponent> _draws = [];
    private long _nextOrder;
    private bool _updating;
    private bool _disposed;
    private float _maxDeltaTime = float.PositiveInfinity;
    public IReadOnlyList<GameObject> Roots
    {
        get;
    }
    public bool Paused
    {
        get; set;
    }
    public int ComponentCount => _components.Count;
    internal bool HasRenderers => _components.Any(component => component is RenderComponent && component.IsActive);
    public float MaxDeltaTime
    {
        get => _maxDeltaTime;
        set
        {
            if (float.IsNaN(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _maxDeltaTime = value;
        }
    }
    public GameWorld() => Roots = _roots.AsReadOnly();

    public GameObject Create(string name, GameObject? parent = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (parent is not null && (parent.World != this || parent.IsDestroyed))
        {
            throw new InvalidOperationException("Parent must be a live object in this world.");
        }
        var value = new GameObject(this, name);
        AddRoot(value);
        value.SetParent(parent);
        return value;
    }

    public IEnumerable<T> GetComponents<T>() where T : Component => _components.OfType<T>();
    internal void AddRoot(GameObject value) => _roots.Add(value);
    internal void RemoveRoot(GameObject value) => _roots.Remove(value);
    internal void Register(Component component)
    {
        component.RegistrationOrder = _nextOrder++;
        _components.Add(component);
    }
    internal void Unregister(Component component) => _components.Remove(component);

    public void Update(float dt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(dt) || dt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dt));
        }
        if (_updating)
        {
            throw new InvalidOperationException("World updates cannot be nested.");
        }
        if (Paused)
        {
            return;
        }
        dt = Math.Min(dt, MaxDeltaTime);
        _updates.Clear();
        _updates.AddRange(_components);
        _updates.Sort(static (a, b) =>
        {
            var order = a.UpdateOrder.CompareTo(b.UpdateOrder);
            if (order != 0)
            {
                return order;
            }
            var depth = a.Owner.Depth.CompareTo(b.Owner.Depth);
            return depth != 0 ? depth : a.RegistrationOrder.CompareTo(b.RegistrationOrder);
        });
        _updating = true;
        try
        {
            foreach (var component in _updates)
            {
                if (component.IsActive)
                {
                    component.Update(dt);
                }
            }
            foreach (var component in _updates)
            {
                if (component.IsActive)
                {
                    component.LateUpdate(dt);
                }
            }
        }
        finally
        {
            _updating = false;
            _updates.Clear();
        }
    }

    internal IReadOnlyList<RenderComponent> RenderQueue()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _draws.Clear();
        foreach (var component in _components)
        {
            if (component is RenderComponent drawable && drawable.IsActive)
            {
                _draws.Add(drawable);
            }
        }
        _draws.Sort(static (a, b) => Compare(a.Layer, b.Layer, a, b));
        return _draws;
    }

    private static int Compare(int aOrder, int bOrder, Component a, Component b)
    {
        var order = aOrder.CompareTo(bOrder);
        return order != 0 ? order : a.RegistrationOrder.CompareTo(b.RegistrationOrder);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        List<Exception> errors = [];
        while (_roots.Count > 0)
        {
            try
            {
                _roots[^1].Destroy();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        _draws.Clear();
        if (errors.Count > 0)
        {
            throw new AggregateException("World cleanup failed.", errors);
        }
    }
}
