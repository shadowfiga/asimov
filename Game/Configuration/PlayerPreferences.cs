using Graphite.Engine.Persistence;

namespace Graphite.Game.Configuration;

public static class PlayerPreferences
{
    public static PreferenceKey<float> MasterVolume { get; } = new("audio.masterVolume", 1f, value => value is >= 0 and <= 1);
    public static PreferenceKey<bool> Muted { get; } = new("audio.muted", false);

    public static void ApplyAudio()
    {
        Engine.UI.UI.Audio.Volume = Preferences.Get(MasterVolume);
        Engine.UI.UI.Audio.Muted = Preferences.Get(Muted);
    }
}
