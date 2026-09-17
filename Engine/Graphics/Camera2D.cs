using Microsoft.Xna.Framework;

namespace Graphite.Engine.Graphics;

/// <summary>World/backbuffer conversion, independent of the UI scale preference.</summary>
public sealed class Camera2D
{
    public Vector2 Position
    {
        get; set;
    }
    public Point ViewportSize { get; private set; } = new(1, 1);
    public float Zoom { get; private set; } = 1;
    public Matrix Transform => Matrix.CreateTranslation(-Position.X, -Position.Y, 0)
        * Matrix.CreateScale(Zoom, Zoom, 1)
        * Matrix.CreateTranslation(ViewportSize.X * .5f, ViewportSize.Y * .5f, 0);

    public void SetViewport(Point size, float zoom = 1)
    {
        if (size.X <= 0 || size.Y <= 0 || !float.IsFinite(zoom) || zoom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Camera requires a positive viewport and zoom.");
        }
        ViewportSize = size;
        Zoom = zoom;
    }

    public Vector2 ScreenToWorld(Vector2 position) => (position - ViewportSize.ToVector2() * .5f) / Zoom + Position;
    public Vector2 WorldToScreen(Vector2 position) => (position - Position) * Zoom + ViewportSize.ToVector2() * .5f;
}
