namespace Graphite.Engine.Core;

public abstract class Component
{
    public Thing Thing { get; internal set; } = null!;
    public Transform Transform => Thing.Transform;
    public bool Enabled { get; set; } = true;
}
