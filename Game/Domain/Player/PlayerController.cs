using Graphite.Game.Domain.Combat;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

public readonly record struct PlayerControls(Vector2 Movement, Vector2 AimOffset, bool Fire);

/// <summary>Game-side movement and independent lower-body, torso and arm aiming.</summary>
public sealed class PlayerController
{
    public RobotDefinition Definition
    {
        get;
    }
    public TwinGunController Guns
    {
        get;
    }
    public Vector2 Position
    {
        get; private set;
    }
    public Vector2 AimPosition
    {
        get; private set;
    }
    public float LegsAngle { get; private set; } = -MathHelper.PiOver2;
    public float TorsoAngle { get; private set; } = -MathHelper.PiOver2;
    public bool IsMoving
    {
        get; private set;
    }
    public WeaponPose LeftWeapon
    {
        get; private set;
    }
    public WeaponPose RightWeapon
    {
        get; private set;
    }

    public PlayerController(RobotDefinition definition, Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        ValidatePosition(position);
        Definition = definition;
        Guns = new TwinGunController(definition.Weapon);
        Position = position;
        AimPosition = position - new Vector2(0, 100);
        UpdatePose();
    }

    public void Update(float dt, PlayerControls controls)
    {
        ValidatePosition(controls.Movement);
        ValidatePosition(controls.AimOffset);
        var move = controls.Movement;
        move.Normalize();
        IsMoving = move.LengthSquared() > .0001f;
        if (IsMoving)
        {
            LegsAngle = MathF.Atan2(move.Y, move.X);
            Position += move * (Definition.MoveSpeed * dt);
        }
        AimPosition = Position + controls.AimOffset;
        var aim = AimPosition - Position;
        if (aim.LengthSquared() > .0001f)
        {
            TorsoAngle = MathF.Atan2(aim.Y, aim.X);
        }
        UpdatePose();
        Guns.Update(dt, controls.Fire, LeftWeapon, RightWeapon);
    }

    private void UpdatePose()
    {
        var forward = new Vector2(MathF.Cos(TorsoAngle), MathF.Sin(TorsoAngle));
        var side = new Vector2(-forward.Y, forward.X) * Definition.ArmSpacing;
        LeftWeapon = Pose(Position - side, forward);
        RightWeapon = Pose(Position + side, forward);
    }

    private WeaponPose Pose(Vector2 pivot, Vector2 fallback)
    {
        var direction = AimPosition - pivot;
        direction = direction.LengthSquared() > .0001f ? Vector2.Normalize(direction) : fallback;
        return new WeaponPose(pivot, direction, pivot + direction * Definition.Weapon.BarrelLength);
    }

    private static void ValidatePosition(Vector2 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Player input and positions must be finite.");
        }
    }
}
