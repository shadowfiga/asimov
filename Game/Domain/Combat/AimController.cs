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
    /// <summary>Joint limits in radians relative to the parent; unrestricted by default.</summary>
    public float MinLocalAngle { get; init; } = -MathHelper.Pi;
    public float MaxLocalAngle { get; init; } = MathHelper.Pi;

    public AimController(Vector2 target) => Target = target;
    protected override void OnAdded() => Aim();
    protected internal override void Update(float dt) => Aim();

    private void Aim()
    {
        Transform.FaceWorldPoint(Target);
        Transform.LocalRotation = Math.Clamp(MathHelper.WrapAngle(Transform.LocalRotation), MinLocalAngle, MaxLocalAngle);
    }
}
