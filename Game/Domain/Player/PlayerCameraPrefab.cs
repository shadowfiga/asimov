using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

/// <summary>Independent camera root ready for scene-owned target assignment.</summary>
public sealed class PlayerCameraPrefab : Prefab<CameraComponent>
{
    public PlayerCameraPrefab() : base("Player camera")
    {
    }

    protected internal override CameraComponent Build(GameObject root)
        => root.AddComponent(new CameraComponent(new Point(1600, 900)));
}
