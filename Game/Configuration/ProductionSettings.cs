using Graphite.Engine.Configuration;
using Graphite.Game.UI.Theming;

namespace Graphite.Game.Configuration;

public sealed class ProductionSettings : GameSettings
{
    public override SettingsEnvironment Environment => SettingsEnvironment.Production;

    public override GameIdentitySettings Game { get; } = new()
    {
        Name = "AFTERGREEN",
        Icon = null,
        StartupScene = "Scenes/BootstrapScene"
    };

    public override WindowSettings Window { get; } = new()
    {
        Width = 1280,
        Height = 720,
        Fullscreen = false,
        Borderless = false,
        Resizable = true
    };

    public override GraphicsSettings Graphics { get; } = new()
    {
        ClearColor = GameThemes.Aftergreen.Background,
        VSync = true,
        PreferMultiSampling = false
    };

    public override RuntimeSettings Runtime { get; } = new()
    {
        MouseVisible = true,
        FixedTimeStep = true,
        TargetFramesPerSecond = 60
    };

    public override MenuSettings Menu { get; } = new()
    {
        DiscordUrl = null,
        Credits = []
    };
}
