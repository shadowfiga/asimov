using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Graphics;

public enum RobotPart
{
    Bottom,
    Top,
    Weapon
}

/// <summary>Static local-space placeholder art. Replace with SpriteRenderer when art is ready.</summary>
public sealed class RobotPartRenderer : RenderComponent
{
    private readonly RobotPart _part;
    private readonly float _size;
    public RobotPartRenderer(RobotPart part, float size)
    {
        _part = part;
        _size = size;
    }
    protected internal override void Draw(RenderContext2D context)
    {
        var theme = GameThemes.DeepDrive;
        switch (_part)
        {
            case RobotPart.Bottom:
                for (var sign = -1; sign <= 1; sign += 2)
                {
                    var foot = new Vector2(-_size * .25f, _size * .65f * sign);
                    context.Box(foot, new Vector2(_size * 1.8f, _size * .65f), theme.Disabled);
                    context.Box(foot + new Vector2(_size * .55f, 0), new Vector2(_size * .3f, _size * .5f), theme.PrimaryText);
                }
                context.Box(Vector2.Zero, new Vector2(_size * .8f, _size * 1.8f), theme.Border);
                break;
            case RobotPart.Top:
                context.Box(Vector2.Zero, new Vector2(_size * 1.6f, _size * 1.8f), theme.SecondaryText);
                context.Box(Vector2.Zero, new Vector2(_size * 1.35f, _size * 1.5f), theme.ControlSurface);
                context.Box(new Vector2(_size * .45f, 0), new Vector2(_size * .45f, _size * 1.1f), theme.PrimaryText);
                context.Box(new Vector2(_size * .7f, 0), new Vector2(_size * .15f, _size * .65f), theme.Selection);
                break;
            case RobotPart.Weapon:
                context.Box(Vector2.Zero, new Vector2(17, 16), theme.SecondaryText);
                context.Line(Vector2.Zero, new Vector2(_size, 0), 9, theme.Border);
                context.Line(new Vector2(5, 0), new Vector2(_size, 0), 4, theme.SecondaryText);
                break;
            default:
                throw new InvalidOperationException("Unknown robot part.");
        }
    }
}
