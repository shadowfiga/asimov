using Graphite.Engine.Configuration;
using Graphite.Game.UI.Theming;

namespace Graphite.Game.Configuration;

public sealed class StagingSettings : GameSettings
{
    public override SettingsEnvironment Environment => SettingsEnvironment.Staging;

    public override GameIdentitySettings Game
    {
        get;
    } = new()
    {
        Id = "deep-drive",
        Name = "DEEP // DRIVE",
        Icon = null,
        StartupScene = "Scenes/BootstrapScene"
    };

    public override WindowSettings Window
    {
        get;
    } = new()
    {
        Width = 2560,
        Height = 1440,
        Fullscreen = true,
        Borderless = true,
        Resizable = true
    };

    public override GraphicsSettings Graphics
    {
        get;
    } = new()
    {
        ClearColor = GameThemes.DeepDrive.Background,
        VSync = true,
        PreferMultiSampling = false
    };

    public override RuntimeSettings Runtime
    {
        get;
    } = new()
    {
        MouseVisible = true,
        FixedTimeStep = true,
        TargetFramesPerSecond = 60
    };

    public override MenuSettings Menu
    {
        get;
    } = new()
    {
        Credits = []
    };
}
