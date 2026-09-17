using Graphite.Engine.Graphics;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Game.Graphics;

/// <summary>Code-drawn prototype art. Owns GPU resources; the player model contains none.</summary>
public sealed class RobotRenderer : IDisposable
{
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;

    public RobotRenderer(GraphicsDevice device)
    {
        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(PlayerController player, Camera2D camera, bool reticle = true)
    {
        var theme = GameThemes.DeepDrive;
        _batch.Begin(transformMatrix: camera.Transform, samplerState: SamplerState.PointClamp);
        try
        {
            var topLeft = camera.ScreenToWorld(Vector2.Zero);
            var bottomRight = camera.ScreenToWorld(camera.ViewportSize.ToVector2());
            for (var x = MathF.Floor(topLeft.X / 64) * 64; x <= bottomRight.X; x += 64)
            {
                Line(new Vector2(x, topLeft.Y), new Vector2(x, bottomRight.Y), 1, theme.Border * .28f);
            }
            for (var y = MathF.Floor(topLeft.Y / 64) * 64; y <= bottomRight.Y; y += 64)
            {
                Line(new Vector2(topLeft.X, y), new Vector2(bottomRight.X, y), 1, theme.Border * .28f);
            }
            foreach (var projectile in player.Guns.Projectiles)
            {
                Line(projectile.Position - Vector2.Normalize(projectile.Velocity) * 18, projectile.Position, 3, theme.SelectionHighlight);
            }
            var radius = player.Definition.BodyRadius;
            var legsForward = Direction(player.LegsAngle);
            var legsSide = new Vector2(-legsForward.Y, legsForward.X);
            foreach (var sign in new[] { -1, 1 })
            {
                var foot = player.Position + legsSide * (radius * .65f * sign) - legsForward * (radius * .25f);
                Box(foot, new Vector2(radius * 1.8f, radius * .65f), player.LegsAngle, theme.Disabled);
                Box(foot + legsForward * (radius * .55f), new Vector2(radius * .3f, radius * .5f), player.LegsAngle, theme.PrimaryText);
            }
            Box(player.Position, new Vector2(radius * .8f, radius * 1.8f), player.LegsAngle, theme.Border);
            Gun(player.LeftWeapon);
            Gun(player.RightWeapon);
            Box(player.Position, new Vector2(radius * 1.6f, radius * 1.8f), player.TorsoAngle, theme.SecondaryText);
            Box(player.Position, new Vector2(radius * 1.35f, radius * 1.5f), player.TorsoAngle, theme.ControlSurface);
            var forward = Direction(player.TorsoAngle);
            Box(player.Position + forward * (radius * .45f), new Vector2(radius * .45f, radius * 1.1f), player.TorsoAngle, theme.PrimaryText);
            Box(player.Position + forward * (radius * .7f), new Vector2(radius * .15f, radius * .65f), player.TorsoAngle, theme.Selection);
            if (reticle)
            {
                Ring(player.AimPosition, 9, theme.PrimaryText);
                foreach (var axis in new[] { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY })
                {
                    Line(player.AimPosition + axis * 12, player.AimPosition + axis * 17, 1.5f, theme.PrimaryText);
                }
            }
        }
        finally
        {
            _batch.End();
        }
    }

    private void Gun(WeaponPose pose)
    {
        var theme = GameThemes.DeepDrive;
        var angle = MathF.Atan2(pose.Direction.Y, pose.Direction.X);
        Box(pose.Pivot, new Vector2(17, 16), angle, theme.SecondaryText);
        Line(pose.Pivot, pose.Muzzle, 9, theme.Border);
        Line(pose.Pivot + pose.Direction * 5, pose.Muzzle, 4, theme.SecondaryText);
    }

    private static Vector2 Direction(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    private void Box(Vector2 center, Vector2 size, float angle, Color color)
        => _batch.Draw(_pixel, center, null, color, angle, new Vector2(.5f), size, SpriteEffects.None, 0);

    private void Line(Vector2 start, Vector2 end, float width, Color color)
    {
        var delta = end - start;
        Box((start + end) * .5f, new Vector2(delta.Length(), width), MathF.Atan2(delta.Y, delta.X), color);
    }

    private void Ring(Vector2 center, float radius, Color color)
    {
        const int segments = 24;
        for (var i = 0; i < segments; i++)
        {
            Line(center + Direction(i * MathHelper.TwoPi / segments) * radius,
                center + Direction((i + 1) * MathHelper.TwoPi / segments) * radius, 1.5f, color);
        }
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _batch.Dispose();
    }
}
