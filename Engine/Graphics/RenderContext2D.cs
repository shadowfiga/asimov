using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Graphics;

public sealed class RenderContext2D
{
    private readonly Texture2D _pixel;
    public SpriteBatch Batch
    {
        get;
    }
    public Camera2D Camera { get; internal set; } = new();
    internal RenderContext2D(SpriteBatch batch, Texture2D pixel)
    {
        Batch = batch;
        _pixel = pixel;
    }
    public void Box(Vector2 center, Vector2 size, Color color, float rotation = 0)
        => Batch.Draw(_pixel, center, null, color, rotation, new Vector2(.5f), size, SpriteEffects.None, 0);
    public void Line(Vector2 start, Vector2 end, float width, Color color)
    {
        var delta = end - start;
        Box((start + end) * .5f, new Vector2(delta.Length(), width), color, MathF.Atan2(delta.Y, delta.X));
    }
    public void Ring(Vector2 center, float radius, Color color, int segments = 24)
    {
        for (var i = 0; i < segments; i++)
        {
            var a = i * MathHelper.TwoPi / segments;
            var b = (i + 1) * MathHelper.TwoPi / segments;
            Line(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius,
                center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, 1.5f, color);
        }
    }
}
