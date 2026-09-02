using Microsoft.Xna.Framework;

namespace Graphite.Engine.Core;

public static class Time
{
    public static float DeltaTime { get; private set; }
    public static double TotalSeconds { get; private set; }

    internal static void Update(GameTime gameTime)
    {
        DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        TotalSeconds = gameTime.TotalGameTime.TotalSeconds;
    }
}
