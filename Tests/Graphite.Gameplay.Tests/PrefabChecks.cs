using System.Reflection;
using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;
using Microsoft.Xna.Framework;
using static Graphite.Gameplay.Tests.Program;

namespace Graphite.Gameplay.Tests;

internal static class PrefabChecks
{
    internal static void Run()
    {
        Construction();
        IndependentMechs();
        Rollback();
        InvalidResults();
        FailureDuringComponentInitialization();
    }

    private static void Construction()
    {
        using var world = new GameWorld();
        var position = new Vector2(30, 50);
        var prefab = new Recipe<Probe>(root =>
        {
            Check(root.World == world && world.Roots.Contains(root), "Prefab receives a registered root in the requested world");
            Check(root.Transform.WorldPosition == position, "Spawn position is set before Build");
            Near(root.Transform.WorldRotation, MathHelper.PiOver2, "Spawn rotation is set before Build");
            root.CreateChild("Child").Transform.LocalPosition = new Vector2(10, 0);
            var probe = root.AddComponent(new Probe());
            Check(probe.Added == 1 && probe.Updates == 0, "Component initialization is automatic; simulation waits for a tick");
            Throws<InvalidOperationException>(() => world.Update(0));
            return probe;
        });
        var result = world.Spawn(prefab, position, MathHelper.PiOver2);
        Check(result.Owner.Name == "Fixture" && world.Roots.Count == 1, "Spawn returns the typed result, not the recipe");
        Near(Vector2.Distance(result.Owner.Children.Single().Transform.WorldPosition, new Vector2(30, 60)), 0,
            "Prefab children inherit the positioned and rotated root");
        world.Update(.1f);
        Check(result.Updates == 1, "A completed prefab uses the existing world scheduler");
        Check(typeof(GameWorld).GetMethod("Create", BindingFlags.Instance | BindingFlags.Public) is null
            && typeof(GameObject).GetConstructors().Length == 0, "Raw root construction is not a public API");
        Check(typeof(PlayerController).GetProperty("Definition") is null, "PlayerController no longer retains the entire mech definition");
        Throws<ArgumentNullException>(() => world.Spawn<Probe>(null!));
        Throws<ArgumentOutOfRangeException>(() => world.Spawn(prefab, new Vector2(float.NaN, 0)));
        Throws<ArgumentOutOfRangeException>(() => world.Spawn(prefab, rotation: float.PositiveInfinity));
        Check(world.Roots.Count == 1, "Invalid spawn parameters do not allocate roots");
        world.Dispose();
        Check(result.IsDisposed && result.Removed == 1, "Scene/world disposal cleans up prefab components once");
        Throws<ObjectDisposedException>(() => world.Spawn(new ObjectPrefab("Too late")));
    }

    private static void IndependentMechs()
    {
        using var world = new GameWorld();
        var prefab = new MechPrefab(new Loadout());
        var first = world.Spawn(prefab, new Vector2(100, 200));
        var second = world.Spawn(prefab, new Vector2(-300, 400));
        Check(first.Owner != second.Owner && first.Bottom != second.Bottom && first.Top != second.Top
            && first.LeftWeapon != second.LeftWeapon && first.RightWeapon != second.RightWeapon
            && first.LeftWeapon.Muzzle != second.LeftWeapon.Muzzle,
            "Reusing a recipe creates independent roots, children, components and muzzles");
        Near(first.MoveSpeed, ChiselChassis.MoveSpeed[(int)ChiselChassisId.STARTER_MECH], "The prefab supplies only authored movement speed to the controller");
        first.Controls = new PlayerControls(Vector2.UnitX, new Vector2(300, -100), true);
        world.Update(.1f);
        Check(first.IsMoving && !second.IsMoving && second.Position == new Vector2(-300, 400), "Movement state is independent between prefab instances");
        Check(world.GetComponents<ProjectileComponent>().Count() == 2 && !second.LeftWeapon.TriggerHeld,
            "Weapon state and scene-owned bullets are independent between prefab instances");
        first.Owner.Destroy();
        Check(!second.IsDisposed && world.GetComponents<ProjectileComponent>().Count() == 2,
            "Destroying one instance does not destroy another instance or existing projectiles");
        var rotated = world.Spawn(prefab, new Vector2(20, 30), MathHelper.PiOver2);
        Near(rotated.TorsoAngle, 0, "Initial aiming respects the prefab spawn rotation");
        world.Update(0);
        Near(rotated.TorsoAngle, 0, "Default player intent retains the spawned orientation");
    }

    private static void Rollback()
    {
        using var world = new GameWorld();
        var existing = world.Spawn(new ObjectPrefab("Existing"));
        List<GameObject> created = [];
        List<Probe> probes = [];
        var failure = new InvalidOperationException("Build failed");
        var prefab = new Recipe<GameObject>(root =>
        {
            created.Add(root);
            probes.Add(root.AddComponent(new Probe()));
            var child = root.CreateChild("Detached child");
            created.Add(child);
            probes.Add(child.AddComponent(new Probe()));
            child.SetParent(null);
            var nested = root.World.Spawn(new ObjectPrefab("Nested spawn"));
            created.Add(nested);
            probes.Add(nested.AddComponent(new Probe()));
            throw failure;
        });
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var caught = Capture(() => world.Spawn(prefab));
            Check(ReferenceEquals(caught, failure), "Successful rollback preserves the original exception instance");
            Check(world.Roots.SequenceEqual(new[] { existing }) && world.ComponentCount == 0,
                "Rollback removes partial roots, detached children and nested spawns without touching existing objects");
            Check(created.All(value => value.IsDestroyed) && probes.All(value => value.Removed == 1),
                "Every partial component is cleaned up exactly once across repeated failures");
        }
        var good = world.Spawn(new ObjectPrefab("After failure"));
        Check(world.Roots.Count == 2 && !good.IsDestroyed, "The world remains usable after a failed spawn");

        var cleanupFailure = new InvalidOperationException("Cleanup failed");
        var errors = Capture(() => world.Spawn(new Recipe<GameObject>(root =>
        {
            root.AddComponent(new Probe { Removing = () => throw cleanupFailure });
            root.CreateChild("Child").AddComponent(new Probe());
            throw failure;
        })));
        Check(errors is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure)
            && aggregate.Flatten().InnerExceptions.Contains(cleanupFailure), "Rollback reports both build and cleanup failures");
        Check(world.Roots.Count == 2 && world.ComponentCount == 0, "A failing cleanup still releases all partial registrations");

        var cleanupSpawn = Capture(() => world.Spawn(new Recipe<GameObject>(root =>
        {
            root.AddComponent(new Probe { Removing = () => world.Spawn(new ObjectPrefab("Leaked from cleanup")) });
            throw failure;
        })));
        Check(cleanupSpawn is AggregateException && world.Roots.Count == 2 && world.ComponentCount == 0,
            "Rollback rejects new spawns from cleanup callbacks instead of leaking objects or looping");

        // A nested failure can be caught by an outer builder without corrupting its transaction bookkeeping.
        var outer = world.Spawn(new Recipe<GameObject>(root =>
        {
            Check(ReferenceEquals(Capture(() => world.Spawn(prefab)), failure), "Nested failure propagates unchanged");
            root.CreateChild("Survivor").AddComponent(new Probe());
            return root;
        }));
        Check(world.Roots.Count == 3 && world.ComponentCount == 1 && outer.Children.Count == 1,
            "A caught nested failure cleans only its own creations");
    }

    private static void InvalidResults()
    {
        using var world = new GameWorld();
        using var other = new GameWorld();
        var foreign = other.Spawn(new ObjectPrefab("Foreign"));
        Throws<InvalidOperationException>(() => world.Spawn(new Recipe<GameObject>(_ => null!)));
        Throws<InvalidOperationException>(() => world.Spawn(new Recipe<GameObject>(_ => foreign)));
        Throws<InvalidOperationException>(() => world.Spawn(new Recipe<Probe>(_ => new Probe())));
        Throws<InvalidOperationException>(() => world.Spawn(new Recipe<GameObject>(root =>
        {
            root.Destroy();
            return root;
        })));
        var existing = world.Spawn(new ObjectPrefab("Existing"));
        Throws<InvalidOperationException>(() => world.Spawn(new Recipe<GameObject>(root =>
        {
            root.SetParent(existing);
            return root;
        })));
        Check(world.Roots.SequenceEqual(new[] { existing }) && world.ComponentCount == 0 && existing.Children.Count == 0
            && !foreign.IsDestroyed, "Invalid build results are rolled back without touching unrelated objects");
    }

    private static void FailureDuringComponentInitialization()
    {
        using var world = new GameWorld();
        var addedFailure = new InvalidOperationException("OnAdded failed");
        var removedFailure = new InvalidOperationException("OnRemoved failed");
        var result = Capture(() => world.Spawn(new Recipe<Probe>(root => root.AddComponent(new Probe
        {
            Adding = () => throw addedFailure,
            Removing = () => throw removedFailure
        }))));
        Check(result is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(addedFailure)
            && aggregate.Flatten().InnerExceptions.Contains(removedFailure),
            "A component cleanup failure must not mask the original initialization bug");
        Check(world.Roots.Count == 0 && world.ComponentCount == 0, "Initialization failure leaves no prefab root or components");
    }

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }
        throw new InvalidOperationException("Expected a failure.");
    }

    private sealed class Recipe<T>(Func<GameObject, T> build) : Prefab<T>("Fixture") where T : class
    {
        protected override T Build(GameObject root) => build(root);
    }

    private sealed class Probe : Component
    {
        public int Added;
        public int Removed;
        public int Updates;
        public Action? Adding;
        public Action? Removing;
        protected override void OnAdded()
        {
            Added++;
            Adding?.Invoke();
        }
        protected override void OnRemoved()
        {
            Removed++;
            Removing?.Invoke();
        }
        protected override void Update(float dt) => Updates++;
    }
}
