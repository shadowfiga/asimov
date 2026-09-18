using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

public sealed class ReticleRenderer : RenderComponent
{
    protected internal override void Draw(RenderContext2D context)
    {
        var mouse = Mouse.GetState();
        var client = Myra.MyraEnvironment.Game.Window.ClientBounds.Size;
        var pixel = new Vector2(
            mouse.X * context.Camera.ViewportSize.X / (float)client.X,
            mouse.Y * context.Camera.ViewportSize.Y / (float)client.Y);
        var point = Transform.InverseTransformPoint(context.Camera.ScreenToWorld(pixel));
        var color = GameThemes.DeepDrive.PrimaryText;
        context.Ring(point, 9, color);
        context.Line(point + new Vector2(12, 0), point + new Vector2(17, 0), 1.5f, color);
        context.Line(point - new Vector2(12, 0), point - new Vector2(17, 0), 1.5f, color);
        context.Line(point + new Vector2(0, 12), point + new Vector2(0, 17), 1.5f, color);
        context.Line(point - new Vector2(0, 12), point - new Vector2(0, 17), 1.5f, color);
    }
}
