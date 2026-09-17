using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Graphics;

public sealed class ProjectileRenderer : RenderComponent
{
    protected internal override void Draw(RenderContext2D context)
        => context.Line(new Vector2(-18, 0), Vector2.Zero, 3, GameThemes.DeepDrive.SelectionHighlight);
}
