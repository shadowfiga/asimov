using Chisel.Generated;
using Graphite.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Domain.Player;

/// <summary>One generated input adapter per play scene; bindings are owned by Chisel.</summary>
public sealed class PlayerInput
{
    private readonly ChiselInput _actions = new();
    private bool _fireArmed;
    public bool BackRequested
    {
        get; private set;
    }

    public PlayerControls Read(KeyboardState keyboard, MouseState mouse, Point clientSize, Camera2D camera, bool active)
    {
        _actions.Update(keyboard, mouse);
        BackRequested = active && _actions.IsActionJustPressed(ChiselInputBindingsId.BACK);
        var firing = _actions.IsActionPressed(ChiselInputBindingsId.FIRE);
        if (!active)
        {
            _fireArmed = false;
        }
        else if (!firing)
        {
            // Require release after clicking Play or regaining focus; never shoot through a menu.
            _fireArmed = true;
        }
        var movement = active ? new Vector2(
            _actions.GetActionStrength(ChiselInputBindingsId.MOVE_RIGHT) - _actions.GetActionStrength(ChiselInputBindingsId.MOVE_LEFT),
            _actions.GetActionStrength(ChiselInputBindingsId.MOVE_DOWN) - _actions.GetActionStrength(ChiselInputBindingsId.MOVE_UP)) : Vector2.Zero;
        var pixel = new Vector2(mouse.X * camera.ViewportSize.X / (float)clientSize.X, mouse.Y * camera.ViewportSize.Y / (float)clientSize.Y);
        var inside = mouse.X >= 0 && mouse.Y >= 0 && mouse.X < clientSize.X && mouse.Y < clientSize.Y;
        // The play camera follows the mech; retain the cursor offset while the mech moves.
        return new PlayerControls(movement, camera.ScreenToWorld(pixel) - camera.Position, active && inside && _fireArmed && firing);
    }
}
