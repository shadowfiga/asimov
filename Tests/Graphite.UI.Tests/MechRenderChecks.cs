using Chisel.Generated;
using Graphite.Engine.Graphics;
using Graphite.Engine.Objects;
using Graphite.Engine.Persistence;
using Graphite.Game.Domain.Player;
using Graphite.Game.Scenes;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.UI.Tests;

internal static class MechRenderChecks
{
    internal static void Run(GraphicsDevice device, string output)
    {
        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        using var renderer = new WorldRenderer2D(device);
        using var world = new GameWorld();
        var mech = world.Spawn(new MechPrefab(new Loadout()), Vector2.Zero);
        world.Spawn(new SandboxPresentationPrefab(mech));
        mech.Controls = new PlayerControls(Vector2.UnitX, new Vector2(350, -210), true);
        for (var frame = 0; frame < 50; frame++)
        {
            world.Update(1f / 60);
        }
        var camera = new Camera2D { Position = mech.Position };
        try
        {
            StaticLegPose(device, renderer);
            foreach (var size in new[] { new Point(1280, 720), new Point(2560, 1440) })
            {
                camera.SetViewport(size, size.Y / 900f);
                using var target = new RenderTarget2D(device, size.X, size.Y);
                Color[] baseline = [];
                foreach (var scale in new[] { .75f, 1.75f })
                {
                    Preferences.Set(RuntimePreferences.UiScale, scale);
                    Graphite.Engine.UI.UI.Update(0);
                    device.SetRenderTarget(target);
                    device.Clear(GameThemes.DeepDrive.Background);
                    renderer.Draw(world, camera);
                    device.SetRenderTarget(null);
                    var pixels = new Color[size.X * size.Y];
                    target.GetData(pixels);
                    if (baseline.Length > 0)
                    {
                        Program.Check(pixels.SequenceEqual(baseline), "Mech, world, projectiles and reticle do not scale with UI preferences");
                    }
                    baseline = pixels;
                    Program.Check(pixels.Count(pixel => pixel.R > 180) > 100, "Prototype visibly renders mech, aim reticle and shots");
                }
                using var stream = File.Create(Path.Combine(output, $"mech-prototype-{size.X}.png"));
                target.SaveAsPng(stream, size.X, size.Y);
            }
        }
        finally
        {
            device.SetRenderTarget(null);
            Preferences.Set(RuntimePreferences.UiScale, originalScale);
            Graphite.Engine.UI.UI.Update(0);
        }
    }

    private static void StaticLegPose(GraphicsDevice device, WorldRenderer2D renderer)
    {
        using var movingWorld = new GameWorld();
        using var idleWorld = new GameWorld();
        var moving = movingWorld.Spawn(new MechPrefab(new Loadout()), new Vector2(0, 28));
        moving.Controls = new PlayerControls(-Vector2.UnitY, new Vector2(0, -100), false);
        movingWorld.Update(.1f);
        var idle = idleWorld.Spawn(new MechPrefab(new Loadout()), moving.Position);
        var camera = new Camera2D { Position = moving.Position };
        camera.SetViewport(new Point(256, 256), 2);
        using var target = new RenderTarget2D(device, 256, 256);
        Color[] baseline = [];
        foreach (var player in new[] { idle, moving })
        {
            device.SetRenderTarget(target);
            device.Clear(GameThemes.DeepDrive.Background);
            renderer.Draw(player.World, camera);
            device.SetRenderTarget(null);
            var pixels = new Color[256 * 256];
            target.GetData(pixels);
            if (baseline.Length > 0)
            {
                Program.Check(pixels.SequenceEqual(baseline), "Moving and idle mechs keep the same static leg pose at matching position and facing");
            }
            baseline = pixels;
        }
    }
}
