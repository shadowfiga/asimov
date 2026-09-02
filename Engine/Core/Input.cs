using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Engine.Core;

public static class Input
{
    public static bool ShouldExit()
        => Keyboard.GetState().IsKeyDown(Keys.Escape)
           || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
}
