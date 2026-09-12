using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Audio;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Graphite.UI.Tests;

internal static class Program
{
    public static void Main(string[] args)
    {
        SceneChecks.Run();
        AnimationChecks();
        if (args.Contains("--graphics")) { using var game = new GraphicsChecks(); game.Run(); }
        Console.WriteLine("UI checks passed.");
    }
    internal static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
    internal static void Near(float actual, float expected, string message)
        => Check(MathF.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");

    private static void AnimationChecks()
    {
        using var audio = new FakeAudio();
        using var first = new UIAnimationPlayer(audio);
        using var second = new UIAnimationPlayer(audio);
        var parameter = new UIParameter<float>("amount", 7);
        var definition = new UIAnimation { Duration = 1, Tracks = [new UIParameterTrack<float>(parameter, 0, 10, MathHelper.Lerp)] };
        var one = first.Play(definition);
        first.Update(.2f); second.Play(definition); second.Update(.5f);
        Near(first.Parameters.Get(parameter), 2, "Independent first instance");
        Near(second.Parameters.Get(parameter), 5, "Independent second instance");
        var newer = first.Play(definition with { Tracks = [new UIParameterTrack<float>(parameter, 20, 30, MathHelper.Lerp)] });
        first.Update(.1f); Near(first.Parameters.Get(parameter), 21, "Newest parameter track wins");
        newer.Cancel(); first.Update(0); Near(first.Parameters.Get(parameter), 3, "Previous parameter track restored");
        one.Cancel(.1f); first.Update(.05f); Near(first.Parameters.Get(parameter), 5, "Cancellation settles toward base");
        first.Update(.05f); Near(first.Parameters.Get(parameter), 7, "Base restored after cancellation");
        Check(one.State == UIPlaybackState.Cancelled && one.Completion.IsCompleted, "Cancellation handle completes");
        second.Update(.5f); Near(second.Parameters.Get(parameter), 7, "Completed tracks release parameters");

        first.Play(new UIAnimation { Duration = 1, Tracks = [new UITransformTrack((frame, _) => { frame.Rotate(2); frame.Resize(new Vector2(2)); frame.Fade(.5f); })] });
        first.Play(new UIAnimation { Duration = 1, Tracks = [new UITransformTrack((frame, _) => { frame.Rotate(3); frame.Resize(new Vector2(3)); frame.Fade(.5f); })] });
        first.Update(.1f); Near(first.Frame.Rotation, 5, "Rotation adds"); Near(first.Frame.Scale.X, 6, "Scale multiplies"); Near(first.Frame.Opacity, .25f, "Opacity multiplies");
        first.CancelAll(); Check(first.Frame.IsIdentity, "Transforms restored");
        var loop = first.Play(definition with { Delay = .2f, Repeat = 0, Alternate = true });
        first.Update(.1f); Near(first.Parameters.Get(parameter), 7, "Delay leaves base untouched");
        first.Update(1.6f); Near(first.Parameters.Get(parameter), 5, "Alternating repeat"); loop.Cancel(); first.Update(0);
        var sound = first.Play(new UIAnimation { Duration = .1f, Sounds = [new UISoundCue { Asset = "fixture.wav", Delay = .2f, Volume = .4f, Pitch = .2f, Pan = -.3f }] });
        first.Update(.1f); Check(audio.Cues.Count == 0, "Delayed sound not started early");
        first.Update(.1f); Check(audio.Cues.Count == 1 && !sound.IsFinished, "Audio can outlive visual timeline");
        Check(audio.Cues[0].Pitch == .2f && audio.Cues[0].Pan == -.3f, "Sound parameters preserved");
        sound.Cancel(); Check(audio.Voices.All(voice => !voice.IsPlaying), "Cancel stops owned audio");
        first.Update(0);
        var finite = first.Play(new UIAnimation { Duration = .1f }); first.Update(.1f);
        Check(finite.State == UIPlaybackState.Completed, "Finite playback completes");

        var defaults = new UIInteractionDefaults();
        defaults.Set<Widget>(new UIInteractionStyle { Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Show] = new() { Animation = definition } } });
        defaults.Set<ButtonBase>(new UIInteractionStyle { Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Hover] = new() { Animation = definition } } });
        var resolved = defaults.Resolve(typeof(Button), new UIInteractionStyle { Bindings = new Dictionary<string, UIInteractionBinding> { [UITrigger.Hover] = UIInteractionBinding.Empty } });
        Check(resolved.Bindings[UITrigger.Show].Animation == definition && resolved.Bindings[UITrigger.Hover].Animation is null, "Defaults inherit and empty override disables");
    }
}

internal sealed class FakeAudio : IUIAudioService
{
    public float Volume { get; set; } = 1;
    public bool Muted { get; set; }
    public List<UISoundCue> Cues { get; } = [];
    public List<FakeVoice> Voices { get; } = [];
    public IUIAudioVoice Play(UISoundCue cue) { Cues.Add(cue); var voice = new FakeVoice(); Voices.Add(voice); return voice; }
    public void Update() { }
    public void Dispose()
    {
        foreach (var voice in Voices)
        {
            voice.Dispose();
        }
    }
}
internal sealed class FakeVoice : IUIAudioVoice
{
    public bool IsPlaying { get; private set; } = true;
    public void Stop() => IsPlaying = false;
    public void Dispose() => Stop();
}
