namespace Graphite.Engine.Objects;

/// <summary>A scene-owned behavior. Attach once; removal/disposal is permanent.</summary>
public abstract class Component : IDisposable
{
    private GameObject? _owner;
    public GameObject Owner => _owner ?? throw new InvalidOperationException("Component is not attached.");
    public Transform2D Transform => Owner.Transform;
    public GameWorld World => Owner.World;
    public bool Enabled { get; set; } = true;
    public bool IsDisposed
    {
        get; private set;
    }
    public bool IsActive => !IsDisposed && Enabled && _owner?.ActiveInHierarchy == true;
    public virtual int UpdateOrder => 0;
    internal long RegistrationOrder
    {
        get; set;
    }

    protected virtual void OnAdded()
    {
    }
    protected virtual void OnRemoved()
    {
    }
    protected internal virtual void Update(float dt)
    {
    }
    protected internal virtual void LateUpdate(float dt)
    {
    }

    internal void Attach(GameObject owner)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_owner is not null)
        {
            throw new InvalidOperationException("A component can belong to only one object.");
        }
        _owner = owner;
    }

    internal void Initialize() => OnAdded();
    internal void Detach()
    {
        IsDisposed = true;
        try
        {
            OnRemoved();
        }
        finally
        {
            _owner = null;
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }
        if (_owner is not null)
        {
            _owner.RemoveComponent(this);
        }
        else
        {
            IsDisposed = true;
        }
    }
}
