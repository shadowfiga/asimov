using Graphite.Engine.Configuration;
using Microsoft.Xna.Framework;

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
        ClearColor = new Color(17, 17, 17),
        VSync = true,
        PreferMultiSampling = false
    };

    public override RuntimeSettings Runtime { get; } = new()
    {
        MouseVisible = true,
        FixedTimeStep = true,
        TargetFramesPerSecond = 60
    };

    public override SliceConfig Slice { get; } = new()
    {
        TargetMass = 1000,
        PlayerSpeed = 150,
        VacuumRange = 145,
        PullSpeed = 95,
        HopperCapacity = 100,
        UpgradedCapacity = 160,
        ExcavationSeconds = 3.2f,
        UpgradeCosts = [65, 110, 150],
        BotCost = 125,
        BotSpeed = 100,
        BotCapacity = 24,
        LightCount = 220,
        MediumCount = 80,
        HeavyCount = 18,
        LightMass = 1,
        MediumMass = 3,
        HeavyMass = 15,
        AnchorMass = 20,
        StructureMass = 95
    };

    public override MenuSettings Menu { get; } = new()
    {
        DiscordUrl = null,
        Credits = []
    };
}
