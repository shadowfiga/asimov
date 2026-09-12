using Graphite.Engine.Configuration;

namespace Graphite.Game.Configuration;

internal static class GameSettingsSelfTest
{
    internal static void Run()
    {
        Check(ReferenceEquals(GameSettings.Instance, GameSettings.Instance), "One settings singleton");
        Check(GameSettings.SelectEnvironment("STAGING") == SettingsEnvironment.Staging
            && GameSettings.SelectEnvironment("production") == SettingsEnvironment.Production, "Explicit environment selection");
#if DEBUG
        Check(GameSettings.SelectEnvironment(null) == SettingsEnvironment.Staging, "Debug defaults to staging");
#else
        Check(GameSettings.SelectEnvironment(null) == SettingsEnvironment.Production, "Release defaults to production");
#endif
        foreach (var invalid in new[] { "", "development" })
        {
            try
            {
                _ = GameSettings.SelectEnvironment(invalid);
                throw new InvalidOperationException("Invalid environment was accepted.");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("PASS: Invalid environment rejected");
            }
        }
        var staging = GameSettings.Create(SettingsEnvironment.Staging);
        var production = GameSettings.Create(SettingsEnvironment.Production);
        Check(staging is StagingSettings && production is ProductionSettings,
            "Each environment selects its compiled settings class");
        Check(staging.Environment == SettingsEnvironment.Staging && production.Environment == SettingsEnvironment.Production,
            "Both environment configurations validate");
        Check(!ReferenceEquals(staging.Window, production.Window) && !ReferenceEquals(staging.Slice, production.Slice),
            "Environment settings are independent");
        foreach (var settings in new[] { staging, production })
        {
            Settings engineSettings = settings;
            Check(ReferenceEquals(engineSettings.Window, settings.Window), "Engine and game share one settings snapshot");
            Check(((ICollection<int>)settings.Slice.UpgradeCosts).IsReadOnly
                && ((ICollection<string>)settings.Menu.Credits).IsReadOnly, "Settings collections remain read-only");
        }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException("FAILED: " + label);
        }
        Console.WriteLine("PASS: " + label);
    }
}
