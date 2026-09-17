using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Combat;

/// <summary>World-space targeting, usable by player input or AI.</summary>
public sealed class AimController : Component
{
    public override int UpdateOrder => 100;
    public Vector2 Target
    {
        get; set;
    }
    public AimController(Vector2 target) => Target = target;
    protected override void OnAdded() => Transform.FaceWorldPoint(Target);
    protected internal override void Update(float dt) => Transform.FaceWorldPoint(Target);
}
