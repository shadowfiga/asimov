using Microsoft.Xna.Framework;
using Myra.Graphics2D;

namespace Graphite.Engine.UI.Theming;

/// <summary>A rounded fill and border without per-widget textures or shader resources.</summary>
public sealed class RoundedRectangleBrush(Color fill, int radius, Color border = default, int borderWidth = 0) : IBrush
{
    public int Radius { get; } = radius;

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        if (dest.Width <= 0 || dest.Height <= 0)
        {
            return;
        }

        var outerRadius = Math.Clamp(Radius, 0, Math.Min(dest.Width, dest.Height) / 2f);
        var stroke = Math.Clamp(borderWidth, 0, Math.Min(dest.Width, dest.Height) / 2f);
        var innerRadius = Math.Max(0, outerRadius - stroke);
        var fillColor = new Color(fill.ToVector4() * color.ToVector4());
        var borderColor = new Color(border.ToVector4() * color.ToVector4());
        for (var y = 0; y < dest.Height; y++)
        {
            var inset = Inset(outerRadius, y + .5f, dest.Height);
            var left = dest.Left + inset;
            var right = dest.Right - inset;
            if (stroke == 0)
            {
                Span(context, left, right, dest.Top + y, fillColor);
            }
            else if (y + .5f < stroke || y + .5f >= dest.Height - stroke)
            {
                Span(context, left, right, dest.Top + y, borderColor);
            }
            else
            {
                var innerInset = stroke + Inset(innerRadius, y + .5f - stroke, dest.Height - stroke * 2);
                Span(context, left, dest.Left + innerInset, dest.Top + y, borderColor);
                Span(context, dest.Left + innerInset, dest.Right - innerInset, dest.Top + y, fillColor);
                Span(context, dest.Right - innerInset, right, dest.Top + y, borderColor);
            }
        }
    }

    private static float Inset(float radius, float y, float height)
    {
        var dy = Math.Max(0, radius - Math.Min(y, height - y));
        return radius - MathF.Sqrt(Math.Max(0, radius * radius - dy * dy));
    }

    private static void Span(RenderContext context, float left, float right, int y, Color color)
    {
        if (right <= left || color.A == 0)
        {
            return;
        }

        var first = (int)MathF.Floor(left);
        var last = (int)MathF.Ceiling(right) - 1;
        if (first == last)
        {
            context.FillRectangle(new Rectangle(first, y, 1, 1), color * (right - left));
            return;
        }

        context.FillRectangle(new Rectangle(first, y, 1, 1), color * (first + 1 - left));
        if (last > first + 1)
        {
            context.FillRectangle(new Rectangle(first + 1, y, last - first - 1, 1), color);
        }

        context.FillRectangle(new Rectangle(last, y, 1, 1), color * (right - last));
    }
}
