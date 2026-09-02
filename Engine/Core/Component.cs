namespace Graphite.Engine.Core;

public abstract class Component
{
    public Entity Entity { get; internal set; } = null!;
    public Transform Transform => Entity.Transform;
    public bool Enabled { get; set; } = true;
}
