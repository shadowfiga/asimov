# Prefabs, objects and components

`Graphite.Engine.Objects` supplies a lightweight scene-owned hierarchy, not an ECS. Every `Scene` has `Objects` (`GameWorld`) and `Camera`. Scenes spawn prefabs in `OnLoad`; prefabs assemble objects/components, and the engine owns their subsequent update, rendering and destruction. Do not manually call component updates or dispose every child from a scene.

```csharp
var player = Objects.Spawn(
    new RobotPrefab(SessionManager.ActiveSession.CurrentLoadout),
    new Vector2(100, 200));
```

The game-specific `RobotPrefab` assembles the complete robot and returns its `PlayerController`. It accepts the game's `Loadout` and captures the selected chassis, pilot, and separate arm weapon IDs. It reads chassis/weapon columns directly; movement speed and pilot identity are passed to the controller. Pilot identity is cosmetic and does not modify combat stats. Other entities can attach the same game `WeaponComponent` with a Chisel weapon ID without player input: set `TriggerHeld`, and optionally add an `AimController` with a world-space `Target`. Chisel remains the definition authority; components reference Chisel IDs instead of copying export data into wrapper definitions. Runtime objects, textures and components are not save records.

## Writing a prefab

Derive from `Prefab<T>`, supply the root name, and implement `Build(GameObject root)`. `T` is a reference-type result, normally the root or a component that callers need. The engine supplies a live root with the requested position and rotation already applied; use `root.World` for the owning world. Example game-assembly code:

```csharp
public sealed class GunPrefab(ChiselWeaponsId weaponId) : Prefab<WeaponComponent>("Gun")
{
    protected internal override WeaponComponent Build(GameObject root)
    {
        var muzzle = root.CreateChild("Muzzle");
        muzzle.Transform.LocalPosition = new Vector2(ChiselWeapons.BarrelLength[weaponId.ToInt()], 0);
        return root.AddComponent(new WeaponComponent(weaponId, muzzle.Transform));
    }
}
```

An external assembly overrides `Build` as `protected override` instead. Scenes/components call `World.Spawn(prefab, position, rotation)` (position defaults to zero; rotation defaults to zero radians). There is no public raw-root factory. Use `CreateChild` for ordinary hierarchy nodes; a muzzle or leg does not need a separate prefab class.

- A prefab is a reusable recipe, not a component or a live object. Store construction inputs in it; create new mutable components/objects inside each `Build`. Spawning the same recipe twice creates independent runtime state.
- Return a non-null result and leave the supplied root alive and unparented. Object/component results must belong to that root's hierarchy. Reference-type result bundles are also possible; their contents are the prefab's responsibility.
- Component `OnAdded` still runs immediately on attachment, in build order. Position/rotation are already set, but the rest of the hierarchy may still be under construction. Do not tick or render the world during a build.
- Build only new objects in the supplied world. Do not mutate pre-existing objects, create objects in other worlds, or acquire unowned resources in `Build`; those side effects are not transactional. Attach owned resource cleanup to components via `OnRemoved`.
- The engine tracks all objects created during the build. Failure destroys partial objects, detached children, and nested root spawns. It rethrows the original exception; if cleanup also fails, an aggregate preserves both errors. Cleanup cannot spawn more objects during rollback. Game prefabs need no defensive catch/recovery wrapper.
- `ProjectilePrefab` creates scene-root bullets through the same API; they are deliberately not children of the firing weapon. `SandboxPresentationPrefab` groups the sandbox grid and reticle.

## Transforms and ownership

- Every object has one `Transform2D`. Local position, rotation (radians), and positive scale compose through all parents; world matrices/positions are immediately reactive without an explicit refresh. Local +X is forward. Sprite mirroring uses `SpriteEffects`, not negative transform scales.
- Use `WorldPosition`, `WorldRotation` or `FaceWorldPoint(target)` when working in world coordinates. Aiming converts through the parent, including its scale, so it does not apply torso rotation twice. A coincident target keeps the existing direction.
- `SetParent(parent)` keeps local values. `SetParent(parent, keepWorldTransform: true)` preserves world pose where a local position/rotation/scale can represent it. It rejects shear/reflection before changing ownership. Full affine parent transforms still render correctly; this restriction applies only to decomposing a preserved world pose during reparenting.
- `SetParent(null)` makes a scene root. Cycles, destroyed parents, and cross-world parenting fail immediately.
- Parents own children. `Destroy()` recursively removes children and components once. `Active = false` suppresses the entire subtree without destroying it; `Component.Enabled = false` suppresses only that component.
- In-flight bullets belong to scene roots. They continue moving when the robot turns, is disabled, or is destroyed, and remove their own object when their lifetime expires.

## Component lifecycle

Derive from `Component`; use `Owner`, `Transform` and `World` after attachment. `AddComponent(instance)` runs `OnAdded` once. `GetComponent<T>()` throws when missing; `TryGetComponent<T>()` returns null. A component can belong to only one object. Removal/disposal is permanent; instantiate another component to attach again. Release owned resources in `OnRemoved` (also called after failed initialization). Other components may already be removed during teardown, so retain any resources/references needed for cleanup yourself.

Each engine tick runs:

1. `Scene.Update`: read input and supply commands.
2. Component `Update`: lower `UpdateOrder` first, then parents before children, then attachment order.
3. Component `LateUpdate`: after all component updates.
4. `Scene.LateUpdate`: camera/listener following sees the final simulation positions.

Movement currently uses order 0, frame playback 50, aiming 100, and firing 200. Newly attached components start updating next tick; removals take effect immediately. Reparenting during a tick never causes a duplicate update; its new ordering applies next tick. `Objects.Paused` stops component simulation, not drawing. `Objects.MaxDeltaTime` optionally caps simulation steps (the Play scene uses 0.1 seconds). Scene unload and failed scene load clean up objects, GPU rendering resources, audio and UI scopes.

## Rendering and frames

`RenderComponent` adds a `Layer` and draws in object-local coordinates using `RenderContext2D`. The engine applies the full hierarchy matrix and scene camera. Draw order is layer first, then stable attachment order, independent of parent/child order. The scene's `Draw` hook runs before automatic object rendering. Ordinary world rendering is independent of UI scale and still passes through the host's fullscreen CRT filter.

`SpriteRenderer` borrows a texture and exposes `SourceRectangle`, normalized `Pivot`, local `Offset`, `Tint`, and `Effects`. Texture ownership stays with the asset loader/caller. `FrameAnimator` drives source rectangles without moving transforms:

```csharp
var sprite = bottom.AddComponent(new SpriteRenderer(texture) { Layer = 10 });
var animator = bottom.AddComponent(new FrameAnimator(sprite, new[]
{
    new AnimationFrame(new Rectangle(0, 0, 32, 32), .1f),
    new AnimationFrame(new Rectangle(32, 0, 32, 32), .1f)
}));
```

Attach the sprite before its animator on the same object. Frames must fit the texture and have positive durations. Playback loops by default; `Loop = false` holds the last frame. `Playing = false` pauses, and `Restart()` starts from frame zero. No robot animations or new art are enabled by this infrastructure.
