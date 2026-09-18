using Graphite.Engine.Graphics;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Objects;

/// <summary>Owns roots and component scheduling. New components start updating on the next tick.</summary>
public sealed class GameWorld : IDisposable
{
    private readonly List<GameObject> _roots = [];
    private readonly List<Component> _components = [];
    private readonly List<Component> _updates = [];
    private readonly List<RenderComponent> _draws = [];
    private readonly List<GameObject> _building = [];
    private int _buildDepth;
    private bool _rollingBack;
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

    public T Spawn<T>(Prefab<T> prefab, Vector2 position = default, float rotation = 0) where T : class
    {
        EnsureCanCreate();
        ArgumentNullException.ThrowIfNull(prefab);
        Transform2D.Validate(position);
        if (!float.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }
        var start = _building.Count;
        _buildDepth++;
        try
        {
            var root = CreateObject(prefab.Name);
            root.Transform.LocalPosition = position;
            root.Transform.LocalRotation = rotation;
            var result = prefab.Build(root)
                ?? throw new InvalidOperationException($"Prefab '{prefab.Name}' returned null.");
            if (root.IsDestroyed || root.Parent is not null)
            {
                throw new InvalidOperationException("A prefab must leave its supplied root alive and unparented.");
            }
            var resultObject = result switch
            {
                GameObject value => value,
                Component component => component.Owner,
                _ => null
            };
            if (resultObject is not null)
            {
                var ancestor = resultObject;
                while (ancestor != root && ancestor.Parent is not null)
                {
                    ancestor = ancestor.Parent;
                }
                if (resultObject.IsDestroyed || ancestor != root)
                {
                    throw new InvalidOperationException("A prefab's object/component result must belong to its supplied root.");
                }
            }
            return result;
        }
        catch (Exception failure)
        {
            List<Exception> cleanupErrors = [];
            _rollingBack = true;
            try
            {
                // Track every new object, including detached children and nested root spawns.
                for (var index = _building.Count - 1; index >= start; index--)
                {
                    try
                    {
                        _building[index].Destroy();
                    }
                    catch (Exception cleanupError)
                    {
                        cleanupErrors.Add(cleanupError);
                    }
                }
            }
            finally
            {
                _rollingBack = false;
                _building.RemoveRange(start, _building.Count - start);
            }
            if (cleanupErrors.Count > 0)
            {
                throw new AggregateException("Prefab construction and cleanup failed.", new[] { failure }.Concat(cleanupErrors));
            }
            throw;
        }
        finally
        {
            _buildDepth--;
            if (_buildDepth == 0)
            {
                _building.Clear();
            }
        }
    }

    internal GameObject CreateChild(GameObject parent, string name)
    {
        EnsureCanCreate();
        ArgumentNullException.ThrowIfNull(parent);
        if (parent.World != this || parent.IsDestroyed)
        {
            throw new InvalidOperationException("Parent must be a live object in this world.");
        }
        var child = CreateObject(name);
        child.SetParent(parent);
        return child;
    }

    private GameObject CreateObject(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var value = new GameObject(this, name);
        AddRoot(value);
        if (_buildDepth > 0)
        {
            _building.Add(value);
        }
        return value;
    }

    private void EnsureCanCreate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_rollingBack)
        {
            throw new InvalidOperationException("Objects cannot be created during prefab rollback.");
        }
    }

    public IEnumerable<T> GetComponents<T>() where T : Component => _components.OfType<T>();
    public GameObject GetGameObjectByName(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        GameObject? result = null;
        foreach (var root in _roots)
        {
            Find(root);
        }
        return result ?? throw new InvalidOperationException($"The world has no game object named '{name}'.");

        void Find(GameObject gameObject)
        {
            if (gameObject.Name == name)
            {
                if (result is not null)
                {
                    throw new InvalidOperationException($"The world has multiple game objects named '{name}'.");
                }
                result = gameObject;
            }
            foreach (var child in gameObject.Children)
            {
                Find(child);
            }
        }
    }
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
        if (_updating || _buildDepth > 0)
        {
            throw new InvalidOperationException("World updates cannot be nested or run during prefab construction.");
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
        if (_buildDepth > 0)
        {
            throw new InvalidOperationException("A world cannot render during prefab construction.");
        }
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
