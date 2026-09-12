using Graphite.Engine.Persistence;
using Graphite.Game.Persistence;

namespace Graphite.Game.Preferences;

public static class PlayerPreferences
{
    public static PreferenceKey<float> MasterVolume { get; } = new("audio.masterVolume", 1f, value => value is >= 0 and <= 1);
    public static PreferenceKey<bool> Muted { get; } = new("audio.muted", false);

    public static void ApplyAudio()
    {
        Engine.UI.UI.Audio.Volume = GamePersistence.Preferences.Get(MasterVolume);
        Engine.UI.UI.Audio.Muted = GamePersistence.Preferences.Get(Muted);
    }
}
