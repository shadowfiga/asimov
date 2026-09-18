using Chisel.Generated;
using Graphite.Engine.Audio;
using Graphite.Engine.UI.Audio;

namespace Graphite.Game.Audio;

/// <summary>Game asset choices only. Playback, mixing and ownership stay in the engine.</summary>
internal static class GameAudio
{
    private static readonly ChiselAssetId[] FallenAngel =
    [
        ChiselAssetId.MUSIC_FALLEN_ANGEL_LOW,
        ChiselAssetId.MUSIC_FALLEN_ANGEL_MEDIUM,
        ChiselAssetId.MUSIC_FALLEN_ANGEL_FULL
    ];
    private static readonly ChiselAssetId[] ChinSurgery =
    [
        ChiselAssetId.MUSIC_CHIN_SURGERY_LOW,
        ChiselAssetId.MUSIC_CHIN_SURGERY_MEDIUM,
        ChiselAssetId.MUSIC_CHIN_SURGERY_FULL
    ];
    internal static UISoundCue Hover { get; } = new() { Asset = AssetPath(ChiselAssetId.UI_HOVER), Volume = .18f };
    internal static UISoundCue Click { get; } = new() { Asset = AssetPath(ChiselAssetId.UI_CLICK), Volume = .35f };

    internal static void ValidateAssets()
    {
        foreach (var asset in FallenAngel.Concat(ChinSurgery).Append(ChiselAssetId.UI_HOVER).Append(ChiselAssetId.UI_CLICK))
        {
            var path = AssetPath(asset);
            var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Managed Chisel audio asset '{asset}' is missing. Run 'git lfs pull', or reimport the owned Ovani archives with Scripts/import_audio.py, then rebuild.",
                    fullPath);
            }
        }
    }

    internal static IEnumerable<Action> PreloadSteps(AudioManager audio)
    {
        foreach (var sound in new[] { Hover, Click })
        {
            yield return () =>
            {
                audio.LoadClip(sound.Asset);
                audio.PreloadEffect(sound.Asset);
            };
        }
        foreach (var cue in new[] { FallenAngel, ChinSurgery })
        {
            foreach (var asset in cue)
            {
                yield return () => audio.LoadClip(AssetPath(asset));
            }
        }
        yield return () => MenuCue(audio);
        yield return () => SessionCue(audio);
    }

    internal static MusicCue MenuCue(AudioManager audio) => Cue(audio, "fallen-angel", FallenAngel);
    internal static MusicCue SessionCue(AudioManager audio) => Cue(audio, "chin-surgery", ChinSurgery);

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

    private static MusicCue Cue(AudioManager audio, string id, ChiselAssetId[] assets)
    {
        var clips = assets.Select(asset => audio.LoadClip(AssetPath(asset))).ToArray();
        // BPM is embedded in the WAV cue labels; RT is supplied in both source folder names.
        // Use a timed crossfade until meter/downbeats have been auditioned, not guessed beat matching.
        var tailFrames = (int)Math.Round(6.455 * clips[0].SampleRate);
        return new MusicCue(id, clips, beatsPerMinute: 112,
            loopEndFrame: clips[0].FrameCount - tailFrames);
    }

    private static string AssetPath(ChiselAssetId asset) => $"Content/{ChiselAssets.Path(asset)}";

}
