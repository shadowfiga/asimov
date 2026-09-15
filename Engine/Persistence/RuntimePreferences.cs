namespace Graphite.Engine.Persistence;

/// <summary>Engine-wide preferences, applied live and restored on startup.</summary>
public static class RuntimePreferences
{
    public static IReadOnlyList<float> UiScaleOptions { get; } = Array.AsReadOnly<float>([.75f, 1f, 1.25f, 1.5f, 1.75f]);

    /// <summary>Confirmed window mode and resolution. Empty uses the environment's configured defaults.</summary>
    public static PreferenceKey<string> Display
    {
        get;
    } = new(
        "display.configuration", "", value => value.Length == 0 || Graphics.DisplayConfiguration.TryParse(value, out _));
    /// <summary>UI magnification relative to automatic viewport scaling. 1 is 100%.</summary>
    public static PreferenceKey<float> UiScale
    {
        get;
    } = new(
        "display.uiScale", 1f, value => UiScaleOptions.Contains(value));

    internal static float GetUiScale()
    {
        // Read the original stored value before the stricter key validation discards legacy scales.
        // Run globally, not only when settings is open, so startup and every scene use the same limits.
        var stored = Preferences.Get(UiScale.Name, UiScale.DefaultValue);
        var preset = Math.Clamp(MathF.Round(stored * 4, MidpointRounding.AwayFromZero) / 4,
            UiScaleOptions[0], UiScaleOptions[^1]);
        if (stored != preset)
        {
            Preferences.Set(UiScale, preset);
        }
        return preset;
    }
}
