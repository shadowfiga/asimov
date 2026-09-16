using Graphite.Engine.Audio;
using Graphite.Engine.UI.Audio;

namespace Graphite.Game.Audio;

/// <summary>Game asset choices only. Playback, mixing and ownership stay in the engine.</summary>
internal static class GameAudio
{
    private const string Root = "Content/Audio/Ovani/";
    private static readonly string[] Variants = ["low", "medium", "full"];
    internal static UISoundCue Hover { get; } = new() { Asset = Root + "UI/hover.wav", Volume = .18f };
    internal static UISoundCue Click { get; } = new() { Asset = Root + "UI/click.wav", Volume = .35f };

    internal static IEnumerable<Action> PreloadSteps(AudioManager audio)
    {
        foreach (var sound in new[] { Hover, Click })
        {
            yield return () =>
            {
                LoadClip(audio, sound.Asset);
                audio.PreloadEffect(sound.Asset);
            };
        }
        foreach (var cue in new[] { "fallen-angel", "chin-surgery" })
        {
            foreach (var variant in Variants)
            {
                var path = MusicPath(cue, variant);
                yield return () => LoadClip(audio, path);
            }
        }
        yield return () => MenuCue(audio);
        yield return () => SessionCue(audio);
    }

    internal static MusicCue MenuCue(AudioManager audio) => Cue(audio, "fallen-angel");
    internal static MusicCue SessionCue(AudioManager audio) => Cue(audio, "chin-surgery");

    internal static void PlayMenu()
    {
        var audio = AudioManager.Current;
        audio.MusicPlayer.Play(MenuCue(audio), MusicTransition.Immediate, fadeSeconds: 2);
        audio.MusicPlayer.SetIntensity(0, rampSeconds: 2);
    }

    internal static void PlaySession()
    {
        var audio = AudioManager.Current;
        audio.MusicPlayer.Play(SessionCue(audio), MusicTransition.Immediate, fadeSeconds: 2);
        // Preparation mix; gameplay will drive pressure/intensity when it exists.
        audio.MusicPlayer.SetIntensity(.25f, rampSeconds: 4);
    }

    private static MusicCue Cue(AudioManager audio, string id)
    {
        var clips = Variants.Select(variant => LoadClip(audio, MusicPath(id, variant))).ToArray();
        // BPM is embedded in the WAV cue labels; RT is supplied in both source folder names.
        // Use a timed crossfade until meter/downbeats have been auditioned, not guessed beat matching.
        var tailFrames = (int)Math.Round(6.455 * clips[0].SampleRate);
        return new MusicCue(id, clips, beatsPerMinute: 112,
            loopEndFrame: clips[0].FrameCount - tailFrames);
    }

    private static string MusicPath(string cue, string variant) => $"{Root}Music/{cue}-{variant}.wav";

    private static AudioClip LoadClip(AudioManager audio, string path)
    {
        try
        {
            return audio.LoadClip(path);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                "Import the licensed audio with python Scripts/import_audio.py <your packs folder>, then rebuild.", exception);
        }
    }
}
