using Graphite.Engine.Configuration;

namespace Graphite.Game.Configuration;

public abstract class GameSettings : Settings
{
    private static readonly Lazy<GameSettings> Shared = new(() => Create(
        SelectEnvironment(System.Environment.GetEnvironmentVariable("DEEP_DRIVE_ENVIRONMENT"))));

    public static GameSettings Instance => Shared.Value;
    public abstract MenuSettings Menu
    {
        get;
    }

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
            _ => throw new InvalidDataException("DEEP_DRIVE_ENVIRONMENT must be staging or production.")
        };
    }

    public override void Validate()
    {
        base.Validate();
        if (Menu is null)
        {
            throw new InvalidDataException("menu settings cannot be null.");
        }
        if (Menu.Credits.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("menu.credits must be an array of non-empty strings.");
        }
    }
}

public sealed class MenuSettings
{
    public IReadOnlyList<string> Credits { get; init; } = [];
}
