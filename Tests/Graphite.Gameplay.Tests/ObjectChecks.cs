using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;
using Microsoft.Xna.Framework;
using static Graphite.Gameplay.Tests.Program;

namespace Graphite.Gameplay.Tests;

internal static class ObjectChecks
{
    internal static void Run()
    {
        Transforms();
        Lifecycle();
        Mutation();
        Weapons();
    }

    private static void Transforms()
    {
        using var world = new GameWorld();
        var root = world.Spawn(new ObjectPrefab("Robot"));
        root.Transform.LocalPosition = new Vector2(10, 20);
        root.Transform.LocalRotation = MathHelper.PiOver2;
        root.Transform.LocalScale = new Vector2(2, 3);
        var top = root.CreateChild("Top");
        top.Transform.LocalPosition = new Vector2(4, 0);
        Near(Vector2.Distance(top.Transform.WorldPosition, new Vector2(10, 28)), 0, "Child inherits parent translation, rotation and scale");
        var muzzle = top.CreateChild("Muzzle");
        muzzle.Transform.LocalPosition = new Vector2(5, 2);
        var point = new Vector2(3, -8);
        Near(Vector2.Distance(muzzle.Transform.InverseTransformPoint(muzzle.Transform.TransformPoint(point)), point), 0, "Nested world/local conversion roundtrips");
        root.Transform.LocalPosition += new Vector2(40, 60);
        Near(Vector2.Distance(top.Transform.WorldPosition, new Vector2(50, 88)), 0, "Parent edits propagate immediately without an update or resize call");
        top.Transform.WorldPosition = new Vector2(80, -40);
        Near(Vector2.Distance(top.Transform.WorldPosition, new Vector2(80, -40)), 0, "Setting world position accounts for a scaled rotated parent");
        var target = new Vector2(-100, 70);
        top.Transform.FaceWorldPoint(target);
        Near(Vector2.Dot(top.Transform.Forward, Vector2.Normalize(target - top.Transform.WorldPosition)), 1, "World aiming is not rotated twice under a nonuniform parent");
        var previous = top.Transform.WorldRotation;
        top.Transform.FaceWorldPoint(top.Transform.WorldPosition);
        Near(top.Transform.WorldRotation, previous, "Zero-distance aim preserves facing");
        Throws<InvalidOperationException>(() => root.SetParent(muzzle));
        Throws<InvalidOperationException>(() => top.SetParent(top));
        using var anotherWorld = new GameWorld();
        Throws<InvalidOperationException>(() => top.SetParent(anotherWorld.Spawn(new ObjectPrefab("Other"))));
        Throws<ArgumentOutOfRangeException>(() => root.Transform.LocalScale = Vector2.Zero);
        Throws<ArgumentOutOfRangeException>(() => top.Transform.LocalRotation = float.NaN);

        var oldParent = world.Spawn(new ObjectPrefab("Old"));
        oldParent.Transform.LocalPosition = new Vector2(40, 100);
        oldParent.Transform.LocalRotation = .4f;
        oldParent.Transform.LocalScale = new Vector2(2);
        var child = oldParent.CreateChild("Child");
        child.Transform.LocalPosition = new Vector2(10, -30);
        var oldPoint = child.Transform.TransformPoint(new Vector2(9, 8));
        var newParent = world.Spawn(new ObjectPrefab("New"));
        newParent.Transform.LocalRotation = -.5f;
        child.SetParent(newParent, keepWorldTransform: true);
        Near(Vector2.Distance(child.Transform.TransformPoint(new Vector2(9, 8)), oldPoint), 0, "Reparent can preserve world pose");
        Check(oldParent.Children.Count == 0 && newParent.Children.Single() == child, "Reparent transfers ownership without duplication");
        child.SetParent(null, keepWorldTransform: true);
        Check(world.Roots.Contains(child), "Unparented objects become scene roots");
        Near(Vector2.Distance(child.Transform.TransformPoint(new Vector2(9, 8)), oldPoint), 0, "Unparent can preserve world pose");
        newParent.Transform.LocalScale = new Vector2(2, 1);
        var sheared = newParent.CreateChild("Sheared");
        sheared.Transform.LocalRotation = .7f;
        Throws<InvalidOperationException>(() => sheared.SetParent(null, keepWorldTransform: true));
        Check(sheared.Parent == newParent, "Unsupported shear fails before changing the hierarchy");
        var aimParent = world.Spawn(new ObjectPrefab("Aim parent"));
        var aimChild = aimParent.CreateChild("Aim child");
        aimChild.Transform.LocalPosition = new Vector2(0, 20);
        aimChild.AddComponent(new AimController(new Vector2(400, -100)));
        aimParent.AddComponent(new AimController(new Vector2(-200, 50)));
        world.Update(0);
        Near(Vector2.Dot(aimChild.Transform.Forward, Vector2.Normalize(new Vector2(400, -100) - aimChild.Transform.WorldPosition)), 1,
            "Equal-priority parent aiming runs before child aiming regardless of component attachment order");
    }

    private static void Lifecycle()
    {
        using var world = new GameWorld();
        var parent = world.Spawn(new ObjectPrefab("Parent"));
        var child = parent.CreateChild("Child");
        var probe = child.AddComponent(new Probe());
        Check(probe.Added == 1 && child.GetComponent<Probe>() == probe, "Attaching initializes and exposes a component once");
        Throws<InvalidOperationException>(() => parent.AddComponent(probe));
        Check(world.ComponentCount == 1, "Duplicate ownership is rejected without registration leaks");
        world.Update(.1f);
        Check(probe.Updates == 1 && probe.LateUpdates == 1, "World schedules component update and late update");
        parent.Active = false;
        world.Update(.1f);
        Check(probe.Updates == 1, "Inactive ancestors suppress descendants");
        parent.Active = true;
        probe.Enabled = false;
        world.Update(.1f);
        Check(probe.Updates == 1, "Disabled components do not update");
        probe.Enabled = true;
        world.Paused = true;
        world.Update(.1f);
        Check(probe.Updates == 1, "World pause suppresses simulation");
        world.Paused = false;
        world.MaxDeltaTime = .05f;
        world.Update(1);
        Near(probe.LastDelta, .05f, "World caps simulation time when configured");
        var failure = new Probe { FailAdd = true };
        Throws<InvalidOperationException>(() => child.AddComponent(failure));
        Check(failure.IsDisposed && failure.Removed == 1 && world.ComponentCount == 1, "Failed initialization rolls back registration and cleans up");
        parent.Destroy();
        parent.Destroy();
        Check(probe.Removed == 1 && child.IsDestroyed && world.Roots.Count == 0 && world.ComponentCount == 0, "Destruction cascades and cleanup runs exactly once");
        Throws<ObjectDisposedException>(() => child.AddComponent(new Probe()));
        Throws<ObjectDisposedException>(() => world.Spawn(new ObjectPrefab("Live")).AddComponent(probe));
        var reentrant = world.Spawn(new ObjectPrefab("Reentrant"));
        reentrant.CreateChild("Child").AddComponent(new Probe { Removing = reentrant.Destroy }).Owner.Destroy();
        Check(reentrant.IsDestroyed, "Child cleanup can destroy its parent without reentrant loops");
        var broken = world.Spawn(new ObjectPrefab("Broken")).AddComponent(new Probe { FailRemove = true });
        var survivor = world.Spawn(new ObjectPrefab("Survivor")).AddComponent(new Probe());
        Throws<AggregateException>(world.Dispose);
        Check(broken.IsDisposed && survivor.IsDisposed && world.Roots.Count == 0 && world.ComponentCount == 0, "One cleanup failure does not leak other objects");
        Throws<ObjectDisposedException>(() => world.Spawn(new ObjectPrefab("Too late")));
    }

    private static void Mutation()
    {
        using var world = new GameWorld();
        var parent = world.Spawn(new ObjectPrefab("Parent"));
        var child = parent.CreateChild("Child");
        var later = child.AddComponent(new Probe { Order = 10 });
        var added = new Probe();
        var once = true;
        parent.AddComponent(new Probe
        {
            Tick = () =>
            {
                if (!once)
                {
                    return;
                }
                once = false;
                later.Dispose();
                child.SetParent(null);
                child.AddComponent(added);
            }
        });
        world.Update(.1f);
        Check(later.Updates == 0 && later.Removed == 1 && added.Updates == 0, "Removal is immediate; additions wait until the next tick");
        world.Update(.1f);
        Check(added.Updates == 1 && added.LateUpdates == 1, "Reparented components update only once");
        Throws<ArgumentOutOfRangeException>(() => world.Update(float.NaN));
        Throws<ArgumentOutOfRangeException>(() => world.Update(-1));
    }

    private static void Weapons()
    {
        using var world = new GameWorld();
        var player = world.Spawn(new RobotPrefab(new Loadout()), Vector2.Zero);
        Check(player.Owner.Children.Select(value => value.Name).SequenceEqual(new[] { "Bottom", "Top" }), "Robot has bottom and top children");
        Check(player.Top.Children.Select(value => value.Name).SequenceEqual(new[] { "LeftWeapon", "RightWeapon" }), "Weapons are children of the torso");
        Check(player.LeftWeapon.Muzzle.Owner.Parent == player.LeftWeapon.Owner, "Each weapon owns a muzzle child");
        player.Controls = new PlayerControls(Vector2.UnitX, new Vector2(400, -100), true);
        world.Update(.01f);
        var bullets = world.GetComponents<ProjectileComponent>().ToArray();
        Check(bullets.Length == 2 && bullets.All(bullet => bullet.Owner.Parent is null), "Each weapon spawns a scene-root projectile");
        var shot = bullets[0];
        var position = shot.Position;
        player.Transform.WorldPosition += new Vector2(500, 300);
        player.Top.Transform.LocalRotation += 2;
        Near(Vector2.Distance(shot.Position, position), 0, "Moving and turning the robot does not drag its shots");
        player.Owner.Destroy();
        Check(player.LeftWeapon.IsDisposed && !shot.IsDisposed, "Removing the robot destroys its weapons but not in-flight bullets");
        world.Update(.1f);
        Near(Vector2.Distance(shot.Position, position + shot.Velocity * .1f), 0, "Detached shots keep simulating after the shooter is gone");
        world.Update(2);
        Check(world.Roots.Count == 0 && world.ComponentCount == 0, "Expired bullets destroy their scene objects and rendering components");

        foreach (var rate in new[] { 4f, 8f })
        {
            var weaponId = new Loadout().WeaponLeftId;
            WithChiselValue(ChiselWeapons.RoundsPerSecond, weaponId.ToInt(), rate, () =>
            {
                using var gunWorld = new GameWorld();
                var gun = gunWorld.Spawn(new ObjectPrefab("Independent gun"));
                var muzzle = gun.CreateChild("Muzzle");
                muzzle.Transform.LocalPosition = new Vector2(ChiselWeapons.BarrelLength[weaponId.ToInt()], 0);
                gun.AddComponent(new WeaponComponent(weaponId, muzzle.Transform) { TriggerHeld = true });
                gunWorld.Update(1);
                Check(gunWorld.GetComponents<ProjectileComponent>().Count() == rate,
                    "Weapon components use the authored rate without any player or mouse input");
            });
        }
    }

    private sealed class Probe : Component
    {
        public int Added;
        public int Removed;
        public int Updates;
        public int LateUpdates;
        public float LastDelta;
        public int Order;
        public bool FailAdd;
        public bool FailRemove;
        public Action? Tick;
        public Action? Removing;
        public override int UpdateOrder => Order;
        protected override void OnAdded()
        {
            Added++;
            if (FailAdd)
            {
                throw new InvalidOperationException("Fixture add failure");
            }
        }
        protected override void OnRemoved()
        {
            Removed++;
            Removing?.Invoke();
            if (FailRemove)
            {
                throw new InvalidOperationException("Fixture cleanup failure");
            }
        }
        protected override void Update(float dt)
        {
            Updates++;
            LastDelta = dt;
            Tick?.Invoke();
        }
        protected override void LateUpdate(float dt) => LateUpdates++;
    }
}
