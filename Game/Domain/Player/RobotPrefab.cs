using Graphite.Engine.Objects;
using Chisel.Generated;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Graphics;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

/// <summary>Assembles the reusable robot hierarchy once; scenes do not manage its individual parts.</summary>
public sealed class RobotPrefab : Prefab<PlayerController>
{
    private readonly RobotDefinition _definition;

    public RobotPrefab(ChiselRobotsId id) : this(RobotDefinition.FromChisel(id))
    {
    }

    public RobotPrefab(RobotDefinition definition) : base("Robot")
    {
        _definition = definition;
    }

    protected internal override PlayerController Build(GameObject root)
    {
        var definition = _definition;
        var target = root.Transform.TransformPoint(new Vector2(0, -100));
        var bottom = root.CreateChild("Bottom");
        bottom.Transform.LocalRotation = -MathHelper.PiOver2;
        bottom.AddComponent(new RobotPartRenderer(RobotPart.Bottom, definition.BodyRadius) { Layer = 10 });
        var top = root.CreateChild("Top");
        var torsoAim = top.AddComponent(new AimController(target));
        top.AddComponent(new RobotPartRenderer(RobotPart.Top, definition.BodyRadius) { Layer = 30 });
        var left = CreateWeapon(top, "LeftWeapon", -definition.ArmSpacing, definition.Weapon, target);
        var right = CreateWeapon(top, "RightWeapon", definition.ArmSpacing, definition.Weapon, target);
        return root.AddComponent(new PlayerController(definition.MoveSpeed, bottom, torsoAim, left.Aim, right.Aim, left.Weapon, right.Weapon)
        {
            Controls = new PlayerControls(Vector2.Zero, target - root.Transform.WorldPosition, false)
        });
    }

    private static (AimController Aim, WeaponComponent Weapon) CreateWeapon(GameObject top, string name,
        float offset, WeaponDefinition definition, Vector2 target)
    {
        var gun = top.CreateChild(name);
        gun.Transform.LocalPosition = new Vector2(0, offset);
        var aim = gun.AddComponent(new AimController(target));
        var muzzle = gun.CreateChild("Muzzle");
        muzzle.Transform.LocalPosition = new Vector2(definition.BarrelLength, 0);
        var weapon = gun.AddComponent(new WeaponComponent(definition, muzzle.Transform));
        gun.AddComponent(new RobotPartRenderer(RobotPart.Weapon, definition.BarrelLength) { Layer = 20 });
        return (aim, weapon);
    }
}
