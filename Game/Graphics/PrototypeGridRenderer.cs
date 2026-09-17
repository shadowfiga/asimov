using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Graphics;

public sealed class PrototypeGridRenderer : RenderComponent
{
    protected internal override void Draw(RenderContext2D context)
    {
        var topLeft = Transform.InverseTransformPoint(context.Camera.ScreenToWorld(Vector2.Zero));
        var bottomRight = Transform.InverseTransformPoint(context.Camera.ScreenToWorld(context.Camera.ViewportSize.ToVector2()));
        var color = GameThemes.DeepDrive.Border * .28f;
        for (var x = MathF.Floor(topLeft.X / 64) * 64; x <= bottomRight.X; x += 64)
        {
            context.Line(new Vector2(x, topLeft.Y), new Vector2(x, bottomRight.Y), 1, color);
        }
        for (var y = MathF.Floor(topLeft.Y / 64) * 64; y <= bottomRight.Y; y += 64)
        {
            context.Line(new Vector2(topLeft.X, y), new Vector2(bottomRight.X, y), 1, color);
        }
    }
}
