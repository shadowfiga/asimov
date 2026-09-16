using Graphite.Engine.Persistence;

namespace Graphite.Engine.Audio;

/// <summary>Persisted engine-wide channel levels. Applying snapshots never changes these values.</summary>
public static class AudioPreferences
{
    public static PreferenceKey<float> MasterVolume { get; } = VolumeKey("audio.masterVolume");
    public static PreferenceKey<float> MusicVolume { get; } = VolumeKey("audio.musicVolume");
    public static PreferenceKey<float> FxVolume { get; } = VolumeKey("audio.fxVolume");
    public static PreferenceKey<float> AmbienceVolume { get; } = VolumeKey("audio.ambienceVolume");
    public static PreferenceKey<float> UiVolume { get; } = VolumeKey("audio.uiVolume");

    private static PreferenceKey<float> VolumeKey(string name) => new(name, 1f, value => float.IsFinite(value) && value is >= 0 and <= 1);

    internal static void Apply(AudioManager audio)
    {
        if (Preferences.Get("audio.muted", false))
        {
            Preferences.Set(MasterVolume, 0f);
        }
        Preferences.Remove<bool>("audio.muted");
        audio.Master.Volume = Preferences.Get(MasterVolume);
        audio.Music.Volume = Preferences.Get(MusicVolume);
        audio.Fx.Volume = Preferences.Get(FxVolume);
        audio.Ambience.Volume = Preferences.Get(AmbienceVolume);
        audio.Ui.Volume = Preferences.Get(UiVolume);
    }
}
