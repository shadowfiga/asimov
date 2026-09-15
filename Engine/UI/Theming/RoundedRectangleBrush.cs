using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI.Styles;

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
        if (outerRadius == 0)
        {
            DrawSquare(context, dest, fillColor, borderColor,
                Math.Clamp(borderWidth, 0, Math.Min(dest.Width, dest.Height) / 2));
            return;
        }
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

    private static void DrawSquare(RenderContext context, Rectangle bounds, Color fill, Color border, int stroke)
    {
        if (stroke == 0)
        {
            context.FillRectangle(bounds, fill);
            return;
        }

        var transform = MyraRenderTransform.GetMatrix(context);
        if (transform.M11 > 0 && transform.M22 > 0 && Math.Abs(transform.M12) < .00001f && Math.Abs(transform.M21) < .00001f)
        {
            DrawPixelAlignedSquare(context, bounds, fill, border, stroke, transform);
            return;
        }

        // Continuous rectangles avoid seams between individually rasterized rows at fractional UI scales.
        // The regions do not overlap, so translucent dialog/hover fills retain their original opacity.
        var middleHeight = bounds.Height - stroke * 2;
        context.FillRectangle(new Rectangle(bounds.X, bounds.Y, bounds.Width, stroke), border);
        context.FillRectangle(new Rectangle(bounds.X, bounds.Bottom - stroke, bounds.Width, stroke), border);
        if (middleHeight <= 0)
        {
            return;
        }
        context.FillRectangle(new Rectangle(bounds.X, bounds.Y + stroke, stroke, middleHeight), border);
        context.FillRectangle(new Rectangle(bounds.Right - stroke, bounds.Y + stroke, stroke, middleHeight), border);
        if (bounds.Width > stroke * 2)
        {
            context.FillRectangle(new Rectangle(bounds.X + stroke, bounds.Y + stroke,
                bounds.Width - stroke * 2, middleHeight), fill);
        }
    }

    private static void DrawPixelAlignedSquare(RenderContext context, Rectangle bounds, Color fill, Color border,
        int stroke, Matrix transform)
    {
        // Keep the outline inside its transformed bounds and at least one physical pixel wide.
        // Logical one-pixel strips otherwise become subpixel strips and can vanish at 0.5x UI scale.
        var left = MathF.Ceiling(bounds.Left * transform.M11 + transform.M41);
        var top = MathF.Ceiling(bounds.Top * transform.M22 + transform.M42);
        var right = MathF.Floor(bounds.Right * transform.M11 + transform.M41);
        var bottom = MathF.Floor(bounds.Bottom * transform.M22 + transform.M42);
        var width = right - left;
        var height = bottom - top;
        if (width <= 0 || height <= 0)
        {
            return;
        }
        var horizontalStroke = Math.Min(Math.Max(1, MathF.Round(stroke * transform.M11)), width / 2);
        var verticalStroke = Math.Min(Math.Max(1, MathF.Round(stroke * transform.M22)), height / 2);
        var middle = height - verticalStroke * 2;
        Draw(left, top, width, verticalStroke, border);
        Draw(left, bottom - verticalStroke, width, verticalStroke, border);
        Draw(left, top + verticalStroke, horizontalStroke, middle, border);
        Draw(right - horizontalStroke, top + verticalStroke, horizontalStroke, middle, border);
        Draw(left + horizontalStroke, top + verticalStroke, width - horizontalStroke * 2, middle, fill);

        void Draw(float x, float y, float w, float h, Color tint)
        {
            if (w <= 0 || h <= 0 || tint.A == 0)
            {
                return;
            }
            var white = Stylesheet.Current.WhiteRegion;
            // Myra's FillRectangle truncates fractional local coordinates. Draw preserves them,
            // allowing these screen-pixel edges to survive the current transform exactly once.
            context.Draw(white.Texture, new Vector2((x - transform.M41) / transform.M11, (y - transform.M42) / transform.M22),
                white.Bounds, tint, 0, new Vector2(w / transform.M11 / white.Bounds.Width, h / transform.M22 / white.Bounds.Height), 0);
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
