using Graphite.Engine.Configuration;

namespace Graphite.Game.Configuration;

public abstract class GameSettings : Settings
{
    private static readonly Lazy<GameSettings> Shared = new(() => Create(
        SelectEnvironment(System.Environment.GetEnvironmentVariable("AFTERGREEN_ENVIRONMENT"))));

    public static GameSettings Instance => Shared.Value;
    public abstract SliceConfig Slice { get; }
    public abstract MenuSettings Menu { get; }

    internal static GameSettings Create(SettingsEnvironment environment)
    {
        GameSettings settings = environment switch
        {
            SettingsEnvironment.Staging => new StagingSettings(),
            SettingsEnvironment.Production => new ProductionSettings(),
            _ => throw new ArgumentOutOfRangeException(nameof(environment))
        };
        settings.Validate();
        return settings;
    }

    internal static SettingsEnvironment SelectEnvironment(string? value)
    {
        if (value is null)
        {
#if DEBUG
            return SettingsEnvironment.Staging;
#else
            return SettingsEnvironment.Production;
#endif
        }
        return value.Trim().ToLowerInvariant() switch
        {
            "staging" => SettingsEnvironment.Staging,
            "production" => SettingsEnvironment.Production,
            _ => throw new InvalidDataException("AFTERGREEN_ENVIRONMENT must be staging or production.")
        };
    }

    public override void Validate()
    {
        base.Validate();
        if (Slice is null || Menu is null)
        {
            throw new InvalidDataException("slice and menu sections cannot be null.");
        }
        if (Menu.DiscordUrl is { } url && (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidDataException("menu.discordUrl must be null or an absolute HTTPS URL.");
        }
        if (Menu.Credits.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("menu.credits must be an array of non-empty strings.");
        }
        Slice.Validate();
    }
}

public sealed class MenuSettings
{
    public required Uri? DiscordUrl { get; init; }
    public IReadOnlyList<string> Credits { get; init; } = [];
}

public sealed class SliceConfig
{
    public required float TargetMass { get; init; }
    public required float PlayerSpeed { get; init; }
    public required float VacuumRange { get; init; }
    public required float PullSpeed { get; init; }
    public required float HopperCapacity { get; init; }
    public required float UpgradedCapacity { get; init; }
    public required float ExcavationSeconds { get; init; }
    private IReadOnlyList<int> _upgradeCosts = [];
    public required IReadOnlyList<int> UpgradeCosts
    {
        get => _upgradeCosts;
        init => _upgradeCosts = value is null ? [] : Array.AsReadOnly(value.ToArray());
    }
    public required int BotCost { get; init; }
    public required float BotSpeed { get; init; }
    public required float BotCapacity { get; init; }
    public required int LightCount { get; init; }
    public required int MediumCount { get; init; }
    public required int HeavyCount { get; init; }
    public required float LightMass { get; init; }
    public required float MediumMass { get; init; }
    public required float HeavyMass { get; init; }
    public required float AnchorMass { get; init; }
    public required float StructureMass { get; init; }

    internal void Validate()
    {
        float[] positiveValues = [TargetMass, PlayerSpeed, VacuumRange, PullSpeed, HopperCapacity,
            UpgradedCapacity, ExcavationSeconds, BotSpeed, BotCapacity, LightMass, MediumMass,
            HeavyMass, AnchorMass, StructureMass];
        if (positiveValues.Any(value => !float.IsFinite(value) || value <= 0)
            || UpgradeCosts.Count != 3 || UpgradeCosts.Any(x => x < 0)
            || HopperCapacity < 30 || UpgradedCapacity < HopperCapacity || BotCapacity < 6
            || BotCost < 0 || LightCount < 0 || MediumCount < 0 || HeavyCount < 0
            || StructureMass <= AnchorMass * 2 || HeavyMass > HopperCapacity || AnchorMass > HopperCapacity
            || LightMass > HopperCapacity || MediumMass > HopperCapacity)
        {
            throw new InvalidDataException("Invalid slice tuning values.");
        }
    }
}
