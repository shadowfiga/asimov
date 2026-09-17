using Graphite.Engine.Objects;

namespace Graphite.Engine.Graphics;

public abstract class RenderComponent : Component
{
    public int Layer
    {
        get; set;
    }
    /// <summary>Draw in object-local coordinates; the engine applies the complete hierarchy and camera.</summary>
    protected internal abstract void Draw(RenderContext2D context);
}
