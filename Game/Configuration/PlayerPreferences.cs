using Graphite.Engine.Persistence;

namespace Graphite.Game.Configuration;

public static class PlayerPreferences
{
    public static PreferenceKey<float> MasterVolume { get; } = new("audio.masterVolume", 1f, value => value is >= 0 and <= 1);
    public static PreferenceKey<float> MusicVolume { get; } = new("audio.musicVolume", 1f, value => value is >= 0 and <= 1);
    public static PreferenceKey<float> FxVolume { get; } = new("audio.fxVolume", 1f, value => value is >= 0 and <= 1);
    public static PreferenceKey<float> CrtIntensity
    {
        get;
    } = new(
        "display.crtIntensity", 1f, value => float.IsFinite(value) && value is >= 0 and <= 1);
    public static void ApplyAudio()
    {
        // Preserve an old mute choice as a visible zero master level, then retire the hidden switch.
        if (Preferences.Get("audio.muted", false))
        {
            Preferences.Set(MasterVolume, 0f);
        }
        Preferences.Remove<bool>("audio.muted");
        Engine.Audio.AudioMixer.Configure(Preferences.Get(MasterVolume), Preferences.Get(MusicVolume), Preferences.Get(FxVolume));
        // UI sound effects already pass through the global FX gain; do not multiply master twice.
        Engine.UI.UI.Audio.Volume = 1;
        Engine.UI.UI.Audio.Muted = false;
    }
}
