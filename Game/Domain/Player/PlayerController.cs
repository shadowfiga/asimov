using Graphite.Engine.Objects;
using Graphite.Game.Domain.Combat;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

public readonly record struct PlayerControls(Vector2 Movement, Vector2 AimOffset, bool Fire);

/// <summary>Player intent only. Transforms, weapon cadence and rendering are separate components.</summary>
public sealed class PlayerController : Component
{
    private readonly AimController[] _aims = [];
    public float MoveSpeed
    {
        get;
    }
    public GameObject Bottom
    {
        get;
    }
    public GameObject Top => _aims[0].Owner;
    public WeaponComponent LeftWeapon
    {
        get;
    }
    public WeaponComponent RightWeapon
    {
        get;
    }
    public Vector2 Position => Transform.WorldPosition;
    public Vector2 AimPosition
    {
        get; private set;
    }
    public float LegsAngle => Bottom.Transform.WorldRotation;
    public float TorsoAngle => Top.Transform.WorldRotation;
    public bool IsMoving
    {
        get; private set;
    }
    public PlayerControls Controls { get; set; } = new(Vector2.Zero, new Vector2(0, -100), false);

    internal PlayerController(float moveSpeed, GameObject bottom, AimController torsoAim,
        AimController leftAim, AimController rightAim, WeaponComponent leftWeapon, WeaponComponent rightWeapon)
    {
        MoveSpeed = moveSpeed;
        Bottom = bottom;
        _aims = [torsoAim, leftAim, rightAim];
        LeftWeapon = leftWeapon;
        RightWeapon = rightWeapon;
        AimPosition = torsoAim.Target;
    }

    protected internal override void Update(float dt)
    {
        var move = Controls.Movement;
        if (move.LengthSquared() > 1)
        {
            move.Normalize();
        }
        IsMoving = move != Vector2.Zero;
        if (IsMoving)
        {
            Bottom.Transform.WorldRotation = MathF.Atan2(move.Y, move.X);
            Transform.WorldPosition += move * (MoveSpeed * dt);
        }
        AimPosition = Position + Controls.AimOffset;
        foreach (var aim in _aims)
        {
            aim.Target = AimPosition;
        }
        LeftWeapon.TriggerHeld = Controls.Fire;
        RightWeapon.TriggerHeld = Controls.Fire;
    }

    protected override void OnRemoved()
    {
        LeftWeapon.TriggerHeld = false;
        RightWeapon.TriggerHeld = false;
    }
}
