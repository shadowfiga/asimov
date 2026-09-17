# Objects and components

`Graphite.Engine.Objects` supplies a lightweight scene-owned hierarchy, not an ECS. Every `Scene` has `Objects` (`GameWorld`) and `Camera`. Scenes declare objects/components in `OnLoad`; the engine owns their subsequent update, rendering and destruction. Do not manually call component updates or dispose every child from a scene.

```csharp
var robot = Objects.Create("Robot");
var bottom = robot.CreateChild("Bottom");
var top = robot.CreateChild("Top");
var gun = top.CreateChild("LeftWeapon");
gun.Transform.LocalPosition = new Vector2(0, -26);
var muzzle = gun.CreateChild("Muzzle");
muzzle.Transform.LocalPosition = new Vector2(30, 0);
gun.AddComponent(new WeaponComponent(definition, muzzle.Transform));
```

The game-specific `RobotFactory.Create(Objects, definition, position)` assembles the complete robot, returning its `PlayerController`. Other entities can attach the same game `WeaponComponent` without player input: set `TriggerHeld`, and optionally add an `AimController` with a world-space `Target`. Chisel remains the definition authority; component instances contain runtime state, not export data. Runtime objects, textures and components are not save records.

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
