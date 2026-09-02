using Microsoft.Xna.Framework;

namespace Graphite.Engine.Core;

public sealed class Transform
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
}
