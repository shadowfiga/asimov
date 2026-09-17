using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

/// <summary>Independent camera root following the player without inheriting mech rotation or scale.</summary>
public sealed class PlayerCameraPrefab(PlayerController player) : Prefab<CameraComponent>("Player camera")
{
    protected internal override CameraComponent Build(GameObject root)
        => root.AddComponent(new CameraComponent(new Point(1600, 900)) { FollowTarget = player.Transform });
}
