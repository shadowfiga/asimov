using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Graphics;

/// <summary>A world-owned camera, with position-only following and reference-resolution framing.</summary>
public sealed class CameraComponent : Component
{
    public Camera2D Camera { get; } = new();
    public Point ReferenceSize
    {
        get;
    }
    public Transform2D? FollowTarget
    {
        get; set;
    }
    public override int UpdateOrder => int.MaxValue;

    public CameraComponent(Point referenceSize)
    {
        if (referenceSize.X <= 0 || referenceSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceSize), "Camera reference dimensions must be positive.");
        }
        ReferenceSize = referenceSize;
    }

    protected override void OnAdded() => Refresh();
    protected internal override void LateUpdate(float dt) => Refresh();
    protected override void OnRemoved() => FollowTarget = null;

    public void SetTarget(GameObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ObjectDisposedException.ThrowIf(target.IsDestroyed, target);
        if (target.World != World)
        {
            throw new InvalidOperationException("Camera follow target must belong to the same world.");
        }
        FollowTarget = target.Transform;
        Refresh();
    }

    // The scene backend calls this before input and drawing, even when simulation is paused.
    internal void Refresh(Point? viewportSize = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (FollowTarget is { } target)
        {
            ObjectDisposedException.ThrowIf(target.Owner.IsDestroyed, target.Owner);
            if (target.Owner.World != World)
            {
                throw new InvalidOperationException("Camera follow target must belong to the same world.");
            }
            Transform.WorldPosition = target.WorldPosition;
        }
        Camera.Position = Transform.WorldPosition;
        if (viewportSize is { } size)
        {
            Camera.SetViewport(size, Math.Min(size.X / (float)ReferenceSize.X, size.Y / (float)ReferenceSize.Y));
        }
    }
}
