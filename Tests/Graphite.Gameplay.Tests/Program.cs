using Chisel.Generated;
using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Gameplay.Tests;

internal static class Program
{
    private static int _assertions;
    private static readonly RobotDefinition Definition = RobotDefinition.FromChisel(ChiselRobotsId.STARTER_MECH);

    public static void Main()
    {
        Check(Definition.MoveSpeed == 280 && Definition.Weapon.RoundsPerSecond == 8, "Actual exported robot and weapon definitions load");
        Throws<ArgumentOutOfRangeException>(() => RobotDefinition.FromChisel(ChiselRobotsId.Invalid));
        Throws<InvalidDataException>(() => (Definition with { MoveSpeed = float.NaN }).Validate());
        ChiselIds();
        ObjectChecks.Run();
        Movement();
        Shooting();
        Inputs();
        Console.WriteLine($"Gameplay checks passed ({_assertions} assertions).");
    }

    private static void ChiselIds()
    {
        var id = ChiselRobotsId.STARTER_MECH;
        Check(id.ToInt() == 0, "Generated enum IDs convert to array indexes with ToInt");
        Near(ChiselRobots.MoveSpeed[id.ToInt()], 280, "Robot column access uses the extension inline");
        Check(ChiselRobots.Slugs[id.ToInt()] == "STARTER_MECH", "Stable slug lookup uses the same extension");
        var weapon = ChiselRobots.Weapon[id.ToInt()];
        Near(ChiselWeapons.RoundsPerSecond[weapon.ToInt()], 8, "Referenced IDs use the extension without table-specific helpers");
        Check(ChiselInputBindings.Bindings[ChiselInputBindingsId.FIRE.ToInt()].SequenceEqual(new[] { "MOUSE_BUTTON_LEFT" }),
            "System table IDs work with the extension as well");
        Check(ChiselRobots.MoveSpeed.GetType() == typeof(float[]) && ChiselRobots.Weapon.GetType() == typeof(ChiselWeaponsId[]),
            "Chisel's generated array contract stays unchanged");
        Throws<ArgumentOutOfRangeException>(() => ChiselRobotsId.Invalid.ToInt());
        Throws<ArgumentOutOfRangeException>(() => ((ChiselRobotsId)(-2)).ToInt());
        Throws<ArgumentOutOfRangeException>(() => ((ChiselRobotsId)ChiselRobots.Count).ToInt());
        Throws<ArgumentOutOfRangeException>(() => ((ChiselRobotsId)int.MaxValue).ToInt());
        Throws<ArgumentOutOfRangeException>(() => WeaponDefinition.FromChisel(ChiselWeaponsId.Invalid));
    }

    private static void Movement()
    {
        using var world = new GameWorld();
        var robot = RobotFactory.Create(world, Definition, Vector2.Zero);
        Step(robot, .1f, new PlayerControls(Vector2.UnitX, new Vector2(0, -300), false));
        Near(robot.Position.X, 28, "Move speed comes from Chisel");
        Near(robot.LegsAngle, 0, "Legs face movement");
        Near(robot.TorsoAngle, -MathHelper.PiOver2, "Torso aims independently of movement");
        Near(robot.AimPosition.X, robot.Position.X, "Aim follows cursor offset during camera movement");
        foreach (var gun in new[] { robot.LeftWeapon, robot.RightWeapon })
        {
            Near(Vector2.Dot(gun.Transform.Forward, Vector2.Normalize(robot.AimPosition - gun.Transform.WorldPosition)), 1, "Each arm converges independently on the reticle");
            Near(Vector2.Distance(gun.Muzzle.WorldPosition, gun.Transform.WorldPosition), Definition.Weapon.BarrelLength, "Muzzle uses authored barrel length");
        }
        Check(robot.LeftWeapon.Transform.Forward != robot.RightWeapon.Transform.Forward, "Separated arm mounts do not use parallel aiming");
        Step(robot, .1f, new PlayerControls(Vector2.Zero, new Vector2(-200, 0), false));
        Near(robot.LegsAngle, 0, "Idle legs retain the last driving direction");
        Near(MathHelper.WrapAngle(robot.TorsoAngle - MathHelper.Pi), 0, "Stationary torso can turn behind the legs");
        var diagonal = RobotFactory.Create(world, Definition, Vector2.Zero);
        Step(diagonal, .1f, new PlayerControls(Vector2.One, Vector2.Zero, false));
        Near(diagonal.Position.Length(), 28, "Diagonal movement is normalized");
        Check(float.IsFinite(diagonal.TorsoAngle) && float.IsFinite(diagonal.LeftWeapon.Transform.Forward.X), "Aiming exactly at the robot stays finite");
        var stationary = RobotFactory.Create(world, Definition, Vector2.Zero);
        Step(stationary, .1f, new PlayerControls(Vector2.Zero, stationary.LeftWeapon.Transform.WorldPosition, true));
        Check(Projectiles(stationary).All(p => float.IsFinite(p.Position.X)), "Aiming at an arm pivot does not create NaN projectiles");
        Throws<ArgumentOutOfRangeException>(() => Step(robot, -1, new PlayerControls()));
    }

    private static void Shooting()
    {
        using var world = new GameWorld();
        var robot = RobotFactory.Create(world, Definition, Vector2.Zero);
        var firing = new PlayerControls(Vector2.Zero, new Vector2(500, -200), true);
        Step(robot, .01f, firing);
        Check(Projectiles(robot).Length == 2, "Mouse-down fires bullets from both guns immediately");
        var first = Projectiles(robot)[0];
        Near(Vector2.Distance(first.Position, robot.LeftWeapon.Muzzle.WorldPosition), Definition.Weapon.ProjectileSpeed * .01f, "First shot advances for its time inside the frame");
        Step(robot, .01f, firing with
        {
            Fire = false
        });
        Step(robot, .01f, firing);
        Check(Projectiles(robot).Length == 2, "Rapid release/repress cannot bypass weapon cooldown");
        foreach (var fps in new[] { 32, 64, 128 })
        {
            using var heldWorld = new GameWorld();
            var held = RobotFactory.Create(heldWorld, Definition, Vector2.Zero);
            for (var frame = 0; frame < fps; frame++)
            {
                Step(held, 1f / fps, firing);
            }
            Check(Projectiles(held).Length == 16, "Holding fire produces eight paired rounds per second independent of frame rate");
        }
        for (var frame = 0; frame < 4000; frame++)
        {
            Step(robot, 1f / 60, firing);
        }
        Check(Projectiles(robot).Length <= 22, "Long firing does not accumulate unbounded projectiles");
        Step(robot, 2, firing with
        {
            Fire = false
        });
        Check(Projectiles(robot).Length == 0, "Released shots expire and are removed");
        Step(robot, 50, firing);
        Check(Projectiles(robot).Length <= 22, "Long steps skip expired catch-up rounds");
    }

    private static void Inputs()
    {
        var actions = new ChiselInput();
        Throws<InvalidOperationException>(() => actions.IsActionPressed(ChiselInputBindingsId.FIRE));
        actions.Update(new KeyboardState(), Mouse(false));
        Throws<ArgumentOutOfRangeException>(() => actions.IsActionPressed(ChiselInputBindingsId.Invalid));
        var camera = new Camera2D { Position = new Vector2(300, -500) };
        var input = new PlayerInput();
        foreach (var size in new[] { new Point(1280, 720), new Point(2560, 1440), new Point(1800, 1200) })
        {
            camera.SetViewport(size, Math.Min(size.X / 1600f, size.Y / 900f));
            var world = new Vector2(450, 120);
            Near(Vector2.Distance(camera.ScreenToWorld(camera.WorldToScreen(world)), world), 0, "Camera screen/world roundtrip survives resizing");
            Near(Vector2.Distance(Vector2.Transform(world, camera.Transform), camera.WorldToScreen(world)), 0, "Sprite transform matches mouse inverse");
        }
        camera.SetViewport(new Point(2560, 1440), 1.6f);
        var client = new Point(1280, 720);
        var controls = input.Read(new KeyboardState(Keys.D, Keys.W), Mouse(true), client, camera, true);
        Check(!controls.Fire, "Held Play click is suppressed on entry");
        Near(controls.AimOffset.X, 200, "Client mouse coordinates map into world space at high resolution");
        Near(controls.AimOffset.Y, 0, "Aim offset is independent of world/camera position");
        Check(controls.Movement == new Vector2(1, -1), "WASD uses exported movement actions");
        input.Read(new KeyboardState(), Mouse(false), client, camera, true);
        Check(input.Read(new KeyboardState(Keys.Right, Keys.Up), Mouse(true), client, camera, true).Fire, "Released then held left mouse fires through exported action");
        Check(input.Read(new KeyboardState(Keys.Left, Keys.Right), Mouse(false), client, camera, true).Movement == Vector2.Zero, "Opposite movement inputs cancel");
        controls = input.Read(new KeyboardState(Keys.D), Mouse(true), client, camera, false);
        Check(!controls.Fire && controls.Movement == Vector2.Zero, "Inactive window suppresses gameplay input");
        Check(!input.Read(new KeyboardState(), Mouse(true), client, camera, true).Fire, "Focus regain requires a fresh fire release");
        input.Read(new KeyboardState(), Mouse(false), client, camera, true);
        Check(!input.Read(new KeyboardState(), Mouse(true, -10), client, camera, true).Fire, "Cursor outside the client cannot shoot");
        input.Read(new KeyboardState(Keys.Escape), Mouse(false), client, camera, true);
        Check(input.BackRequested, "Escape is a Chisel action edge");
        input.Read(new KeyboardState(Keys.Escape), Mouse(false), client, camera, true);
        Check(!input.BackRequested, "Holding Escape does not retrigger");
    }

    private static ProjectileComponent[] Projectiles(PlayerController player) => player.World.GetComponents<ProjectileComponent>().ToArray();
    private static void Step(PlayerController player, float dt, PlayerControls controls)
    {
        player.Controls = controls;
        player.World.Update(dt);
    }

    private static MouseState Mouse(bool down, int x = 800) => new(x, 360, 0,
        down ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    internal static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .002f, message);
    internal static void Check(bool result, string message)
    {
        _assertions++;
        if (!result)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            _assertions++;
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
