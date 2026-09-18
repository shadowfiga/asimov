using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Enemies;

/// <summary>Asset-free prototype silhouette for the first enemy.</summary>
public sealed class EnemyRenderer(float radius) : RenderComponent
{
    protected internal override void Draw(RenderContext2D context)
    {
        var theme = GameThemes.DeepDrive;
        context.Box(Vector2.Zero, new Vector2(radius * 1.25f), theme.Danger, MathHelper.PiOver4);
        context.Ring(Vector2.Zero, radius, theme.DangerHighlight, 12);
        context.Line(Vector2.Zero, new Vector2(radius, 0), 2, theme.DeepBlack);
    }
}
