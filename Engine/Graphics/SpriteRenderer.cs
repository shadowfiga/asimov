using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Graphics;

/// <summary>Borrowed texture; the asset owner, not the component, disposes it.</summary>
public sealed class SpriteRenderer : RenderComponent
{
    private Rectangle _source;
    public Texture2D Texture
    {
        get;
    }
    public Vector2 Pivot { get; set; } = new(.5f);
    public Vector2 Offset
    {
        get; set;
    }
    public Color Tint { get; set; } = Color.White;
    public SpriteEffects Effects
    {
        get; set;
    }
    public Rectangle SourceRectangle
    {
        get => _source;
        set
        {
            ValidateFrame(value);
            _source = value;
        }
    }
    public SpriteRenderer(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Texture = texture;
        _source = texture.Bounds;
    }
    internal void ValidateFrame(Rectangle frame)
    {
        if (frame.Width <= 0 || frame.Height <= 0 || !Texture.Bounds.Contains(frame))
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Sprite frame must fit inside its texture.");
        }
    }
    protected internal override void Draw(RenderContext2D context)
        => context.Batch.Draw(Texture, Offset, _source, Tint, 0,
            new Vector2(_source.Width, _source.Height) * Pivot, Vector2.One, Effects, 0);
}
