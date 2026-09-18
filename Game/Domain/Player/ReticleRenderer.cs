using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

public sealed class ReticleRenderer : RenderComponent
{
    private PlayerController? _player;

    public void SetTarget(GameObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ObjectDisposedException.ThrowIf(target.IsDestroyed, target);
        if (target.World != World)
        {
            throw new InvalidOperationException("Reticle target must belong to the same world.");
        }
        _player = target.GetComponent<PlayerController>();
    }

    protected internal override void Draw(RenderContext2D context)
    {
        var player = _player ?? throw new InvalidOperationException("Reticle target has not been assigned.");
        ObjectDisposedException.ThrowIf(player.IsDisposed, player);
        var point = Transform.InverseTransformPoint(player.AimPosition);
        var color = GameThemes.DeepDrive.PrimaryText;
        context.Ring(point, 9, color);
        context.Line(point + new Vector2(12, 0), point + new Vector2(17, 0), 1.5f, color);
        context.Line(point - new Vector2(12, 0), point - new Vector2(17, 0), 1.5f, color);
        context.Line(point + new Vector2(0, 12), point + new Vector2(0, 17), 1.5f, color);
        context.Line(point - new Vector2(0, 12), point - new Vector2(0, 17), 1.5f, color);
    }
}
