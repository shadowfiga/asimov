using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.UI.Tests;

internal static class WorldRenderingChecks
{
    internal static void Run(GraphicsDevice device)
    {
        using var texture = new Texture2D(device, 4, 2);
        texture.SetData(new[] { Color.Red, Color.Red, Color.Green, Color.Green, Color.Red, Color.Red, Color.Green, Color.Green });
        using var world = new GameWorld();
        using var renderer = new WorldRenderer2D(device);
        using var target = new RenderTarget2D(device, 128, 128);
        var camera = new Camera2D();
        camera.SetViewport(new Point(128, 128), 3);
        var parent = world.Create("Parent");
        parent.Transform.LocalScale = new Vector2(2, 3);
        parent.Transform.LocalRotation = .4f;
        var child = parent.CreateChild("Sprite");
        child.Transform.LocalPosition = new Vector2(3, 2);
        child.Transform.LocalRotation = -.6f;
        var sprite = child.AddComponent(new SpriteRenderer(texture) { SourceRectangle = new Rectangle(0, 0, 2, 2), Layer = 10 });
        var animator = child.AddComponent(new FrameAnimator(sprite, new[]
        {
            new AnimationFrame(new Rectangle(0, 0, 2, 2), .125f),
            new AnimationFrame(new Rectangle(2, 0, 2, 2), .125f)
        }));
        var pose = child.Transform.WorldMatrix;
        try
        {
            Program.Check(RenderCenter() == Color.Red, "Sprite rendering applies the complete parent transform, including nonuniform scale");
            world.Update(.125f);
            Program.Check(animator.FrameIndex == 1 && RenderCenter() == Color.Green, "Frame animation advances the sprite-sheet source rectangle");
            world.Update(.125f);
            Program.Check(animator.FrameIndex == 0 && RenderCenter() == Color.Red, "Frame animation loops on exact frame boundaries");
            animator.Loop = false;
            animator.Restart();
            world.Update(1);
            Program.Check(animator.FrameIndex == 1 && !animator.Playing && child.Transform.WorldMatrix == pose,
                "Non-looping playback holds the final frame without moving the object");
            animator.Restart();
            animator.Enabled = false;
            world.Update(.15f);
            Program.Check(animator.FrameIndex == 0, "Disabling an animator freezes frame playback");

            var overlay = world.Create("Overlay");
            overlay.Transform.WorldPosition = child.Transform.WorldPosition;
            overlay.Transform.LocalScale = new Vector2(4);
            overlay.AddComponent(new SpriteRenderer(texture) { SourceRectangle = new Rectangle(2, 0, 2, 2), Layer = 20 });
            Program.Check(RenderCenter() == Color.Green, "Higher layers draw over earlier hierarchy branches");
            sprite.Layer = 30;
            Program.Check(RenderCenter() == Color.Red, "Layer changes reorder rendering independently of parenting");
            parent.Active = false;
            Program.Check(RenderCenter() == Color.Green, "Inactive parents hide their child renderers");
            parent.Active = true;
            world.Dispose();
            Program.Check(!texture.IsDisposed && world.ComponentCount == 0, "Object cleanup releases components without disposing borrowed textures");
        }
        finally
        {
            device.SetRenderTarget(null);
        }

        Color RenderCenter()
        {
            device.SetRenderTarget(target);
            device.Clear(Color.Black);
            renderer.Draw(world, camera);
            device.SetRenderTarget(null);
            var pixels = new Color[128 * 128];
            target.GetData(pixels);
            var screen = camera.WorldToScreen(child.Transform.WorldPosition);
            return pixels[(int)MathF.Round(screen.Y) * 128 + (int)MathF.Round(screen.X)];
        }
    }
}
