using Graphite.Engine.Graphics;
using Graphite.Game.Domain.Player;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Graphics;

public sealed class ReticleRenderer : RenderComponent
{
    private readonly PlayerController _player;
    public ReticleRenderer(PlayerController player) => _player = player;
    protected internal override void Draw(RenderContext2D context)
    {
        if (_player.IsDisposed)
        {
            return;
        }
        var point = Transform.InverseTransformPoint(_player.AimPosition);
        var color = GameThemes.DeepDrive.PrimaryText;
        context.Ring(point, 9, color);
        context.Line(point + new Vector2(12, 0), point + new Vector2(17, 0), 1.5f, color);
        context.Line(point - new Vector2(12, 0), point - new Vector2(17, 0), 1.5f, color);
        context.Line(point + new Vector2(0, 12), point + new Vector2(0, 17), 1.5f, color);
        context.Line(point - new Vector2(0, 12), point - new Vector2(0, 17), 1.5f, color);
    }
}
