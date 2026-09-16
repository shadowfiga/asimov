using Graphite.Engine.Persistence;

namespace Graphite.Game.Configuration;

public static class PlayerPreferences
{
    public static PreferenceKey<float> MasterVolume => Engine.Audio.AudioPreferences.MasterVolume;
    public static PreferenceKey<float> MusicVolume => Engine.Audio.AudioPreferences.MusicVolume;
    public static PreferenceKey<float> FxVolume => Engine.Audio.AudioPreferences.FxVolume;
    public static PreferenceKey<float> CrtIntensity
    {
        get;
    } = new(
        "display.crtIntensity", 1f, value => float.IsFinite(value) && value is >= 0 and <= 1);
    public static void ApplyAudio()
    {
        Engine.Audio.AudioManager.Current.SynchronizePreferences(force: true);
    }
}
