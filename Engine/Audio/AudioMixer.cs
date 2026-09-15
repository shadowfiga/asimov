using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Graphite.Engine.Audio;

/// <summary>Game-wide channel gains, including sounds already playing.</summary>
public static class AudioMixer
{
    public static float MasterVolume { get; private set; } = 1;
    public static float MusicVolume { get; private set; } = 1;
    public static float FxVolume { get; private set; } = 1;

    public static void Configure(float master, float music, float fx)
    {
        MasterVolume = Clamp(master);
        MusicVolume = Clamp(music);
        FxVolume = Clamp(fx);
        // SoundEffect covers game effects and UI cues; streamed songs use MediaPlayer.
        SoundEffect.MasterVolume = MasterVolume * FxVolume;
        MediaPlayer.Volume = MasterVolume * MusicVolume;
    }

    private static float Clamp(float value) => float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
}
