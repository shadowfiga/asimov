using Graphite.Engine.Objects;
using Chisel.Generated;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Domain.Player;

/// <summary>Assembles the reusable mech hierarchy once; scenes do not manage its individual parts.</summary>
public sealed class MechPrefab : Prefab<PlayerController>
{
    private readonly int _chassisId;
    private readonly ChiselPilotId _pilotId;
    private readonly ChiselWeaponsId _weaponLeftId;
    private readonly ChiselWeaponsId _weaponRightId;

    public MechPrefab(Loadout loadout) : base(loadout.ChassisId.ToString())
    {
        _chassisId = loadout.ChassisId.ToInt();
        _pilotId = loadout.PilotId;
        _weaponLeftId = loadout.WeaponLeftId;
        _weaponRightId = loadout.WeaponRightId;
    }

    protected internal override PlayerController Build(GameObject root)
    {
        var target = root.Transform.TransformPoint(new Vector2(0, -100));
        var bottom = root.CreateChild("Bottom");
        bottom.Transform.LocalRotation = -MathHelper.PiOver2;
        bottom.AddComponent(new MechPartRenderer(MechPart.Bottom, ChiselChassis.BodyRadius[_chassisId]) { Layer = 10 });
        var top = root.CreateChild("Top");
        var torsoAim = top.AddComponent(new AimController(target));
        top.AddComponent(new MechPartRenderer(MechPart.Top, ChiselChassis.BodyRadius[_chassisId]) { Layer = 30 });
        var left = CreateWeapon(top, "LeftWeapon", -ChiselChassis.ArmSpacing[_chassisId], _weaponLeftId, target);
        var right = CreateWeapon(top, "RightWeapon", ChiselChassis.ArmSpacing[_chassisId], _weaponRightId, target);
        return root.AddComponent(new PlayerController(ChiselChassis.MoveSpeed[_chassisId], _pilotId, bottom, torsoAim, left.Aim, right.Aim, left.Weapon, right.Weapon)
        {
            Controls = new PlayerControls(Vector2.Zero, target - root.Transform.WorldPosition, false)
        });
    }

    private static (AimController Aim, WeaponComponent Weapon) CreateWeapon(GameObject top, string name,
        float offset, ChiselWeaponsId weaponId, Vector2 target)
    {
        var gun = top.CreateChild(name);
        gun.Transform.LocalPosition = new Vector2(0, offset);
        var aim = gun.AddComponent(new AimController(target));
        var muzzle = gun.CreateChild("Muzzle");
        muzzle.Transform.LocalPosition = new Vector2(ChiselWeapons.BarrelLength[weaponId.ToInt()], 0);
        var weapon = gun.AddComponent(new WeaponComponent(weaponId, muzzle.Transform));
        gun.AddComponent(new MechPartRenderer(MechPart.Weapon, ChiselWeapons.BarrelLength[weaponId.ToInt()]) { Layer = 20 });
        return (aim, weapon);
    }
}
