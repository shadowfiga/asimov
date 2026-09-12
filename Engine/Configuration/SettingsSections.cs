using Microsoft.Xna.Framework;

namespace Graphite.Engine.Configuration;

public sealed class GameIdentitySettings
{
    public required string Name { get; init; }
    public required string? Icon { get; init; }
    public required string StartupScene { get; init; }
}

public sealed class WindowSettings
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required bool Fullscreen { get; init; }
    public required bool Borderless { get; init; }
    public required bool Resizable { get; init; }
}

public sealed class GraphicsSettings
{
    public required Color ClearColor { get; init; }
    public required bool VSync { get; init; }
    public required bool PreferMultiSampling { get; init; }
}

public sealed class RuntimeSettings
{
    public required bool MouseVisible { get; init; }
    public required bool FixedTimeStep { get; init; }
    public required int TargetFramesPerSecond { get; init; }
}
