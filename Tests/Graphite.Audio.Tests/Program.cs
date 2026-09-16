using System.Text;
using Graphite.Engine.Audio;
using Graphite.Engine.Persistence;
using Graphite.Engine.UI.Audio;
using Graphite.Game.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Graphite.Audio.Tests;

internal static class Program
{
    private static int _assertions;

    public static void Main(string[] args)
    {
        SpatialChecks();
        ManagerChecks();
        ScopeChecks();
        AmbienceChecks();
        WaveChecks();
        MusicChecks();
        PreferenceAndUiChecks();
        if (args.Contains("--assets"))
        {
            AssetChecks();
        }
        if (args.Contains("--native"))
        {
            NativeChecks();
        }
        Console.WriteLine($"Audio checks passed ({_assertions} assertions).");
    }

    private static AudioClip Constant(float value = .2f, int frames = 48000, int sampleRate = 48000)
        => new(Enumerable.Repeat(value, frames).ToArray(), sampleRate, 1);

    private static void SpatialChecks()
    {
        var listener = new AudioListener2D();
        var settings = new SpatialAudioSettings { InnerRadius = 10, OuterRadius = 100, PanDistance = 50 };
        var center = SpatialAudio2D.Calculate(listener, Vector2.Zero, settings);
        Near(center.Gain, 1, "Coincident emitter stays finite and full volume");
        Near(center.Pan, 0, "Coincident emitter centered");
        var right = SpatialAudio2D.Calculate(listener, new Vector2(55, 0), settings);
        Near(right.Gain, .5f, "World distance attenuation");
        Near(right.Pan, 1, "Right channel panning");
        Near(SpatialAudio2D.Calculate(listener, new Vector2(-55, 0), settings).Pan, -1, "Left channel panning");
        Near(SpatialAudio2D.Calculate(listener, new Vector2(0, 55), settings).Pan, 0, "Vertical offset is centered");
        Near(SpatialAudio2D.Calculate(listener, new Vector2(100, 0), settings).Gain, 0, "Outer radius silent");
        listener.Right = Vector2.UnitY;
        Near(SpatialAudio2D.Calculate(listener, new Vector2(0, 55), settings).Pan, 1, "Rotated listener orientation");
        Throws<ArgumentOutOfRangeException>(() => listener.Right = Vector2.Zero, "Zero listener orientation rejected");
        Throws<ArgumentException>(() => SpatialAudio2D.Calculate(listener, Vector2.Zero, settings with { OuterRadius = 10 }), "Invalid falloff rejected");
        Throws<ArgumentOutOfRangeException>(() => listener.Position = new Vector2(float.NaN, 0), "NaN position rejected");
    }

    private static void ManagerChecks()
    {
        var backend = new FakeBackend();
        using var audio = new AudioManager(backend, maxVoices: 3);
        var clip = Constant();
        audio.Master.Volume = .5f;
        audio.Music.Volume = .6f;
        audio.Fx.Volume = .4f;
        audio.Ui.Volume = .25f;
        audio.Ambience.Volume = .8f;
        Near(audio.Music.EffectiveVolume, .3f, "Music = master * music");
        Near(audio.Ui.EffectiveVolume, .05f, "UI = master * FX * UI, once each");
        Near(audio.Ambience.EffectiveVolume, .4f, "Ambience independent of FX");
        var first = audio.Play(clip, new AudioPlayOptions { Volume = .5f, Loop = true, Priority = 1 });
        var ui = audio.Play(clip, new AudioPlayOptions { Bus = audio.Ui, Loop = true, Priority = 100 });
        var moving = audio.Play2D(clip, new Vector2(55, 0),
            new SpatialAudioSettings { InnerRadius = 10, OuterRadius = 100, PanDistance = 50 });
        Near(first.EffectiveVolume, .1f, "Per-voice gain");
        Near(moving.EffectiveVolume, .1f, "Spatial gain multiplied once");
        audio.Listener.Position = new Vector2(55, 0);
        audio.Update(0);
        Near(moving.EffectiveVolume, .2f, "Moving listener refreshes playing sound");
        moving.Position = new Vector2(-100, 0);
        Near(moving.EffectiveVolume, 0, "Moving emitter refreshes immediately");
        Check(moving.IsPlaying, "Inaudible loop keeps playing for re-entry");
        moving.Pause();
        audio.Pause();
        Check(first.IsPaused && moving.IsPaused && !ui.IsPaused, "Game pause leaves UI bus active");
        audio.Resume();
        Check(!first.IsPaused && moving.IsPaused, "Resume preserves individually paused voices");
        moving.Resume();
        audio.MusicPlayer.Pause();
        audio.Pause();
        audio.Resume();
        Check(audio.MusicPlayer.IsPaused, "Game resume preserves individually paused music");
        audio.MusicPlayer.Resume();
        var rejected = audio.Play(clip, new AudioPlayOptions { Priority = -1 });
        Check(!rejected.IsPlaying && audio.ActiveVoiceCount == 3, "Low priority dropped at budget");
        var replacement = audio.Play(clip, new AudioPlayOptions { Priority = 0 });
        Check(!moving.IsPlaying && replacement.IsPlaying && first.IsPlaying && ui.IsPlaying, "Lowest priority stolen");
        audio.Fx.Volume = 0;
        Near(first.EffectiveVolume, 0, "Live FX changes affect existing voices");
        Near(ui.EffectiveVolume, 0, "UI inherits FX");
        Near(audio.Music.EffectiveVolume, .3f, "FX does not attenuate music");
        Near(audio.Ambience.EffectiveVolume, .4f, "FX does not attenuate ambience");
        audio.Master.Volume = 0;
        Near(audio.Music.EffectiveVolume, 0, "Master silences music");
        Near(audio.Ambience.EffectiveVolume, 0, "Master silences ambience");
        Throws<ArgumentOutOfRangeException>(() => audio.Master.Volume = float.NaN, "NaN volume rejected");
        Throws<ArgumentOutOfRangeException>(() => audio.Play(clip, new AudioPlayOptions { Pitch = 2 }), "Invalid pitch rejected");
        using var other = new AudioManager(new FakeBackend());
        Throws<ArgumentException>(() => audio.Play(clip, new AudioPlayOptions { Bus = other.Fx }), "Foreign bus rejected");
        var custom = audio.CreateBus("dialogue", audio.Master);
        Check(custom.Parent == audio.Master, "Additional channels supported");
        Throws<ArgumentException>(() => audio.CreateBus("dialogue"), "Duplicate channels rejected");
        backend.Instances.Last().Stop();
        audio.Update(0);
        Check(!replacement.IsPlaying && backend.Instances.Last().Disposed, "Natural completion releases voice");
        audio.Dispose();
        Check(backend.Disposed && backend.Instances.All(instance => instance.Disposed), "Manager releases voices and backend");
        Throws<ObjectDisposedException>(() => audio.Play(clip), "Disposed manager fails fast");
    }

    private static void ScopeChecks()
    {
        using var audio = new AudioManager(new FakeBackend());
        using var scope = new AudioScope();
        var scoped = scope.Own(audio.Play(Constant(), new AudioPlayOptions { Loop = true }));
        var other = audio.Play(Constant(), new AudioPlayOptions { Loop = true });
        scope.Own(audio.AmbienceZones.Add(new AmbienceZone2D
        {
            Center = Vector2.Zero,
            HalfSize = Vector2.One,
            TransitionSeconds = 0,
            Snapshot = new AudioSnapshot(new Dictionary<AudioBus, float> { [audio.Ambience] = .2f })
        }));
        audio.Update(0);
        Check(audio.AmbienceZones.ActiveZone is not null, "Scoped zone registers");
        scope.Dispose();
        audio.Update(0);
        Check(!scoped.IsPlaying && other.IsPlaying, "Scope stops only its own voices");
        Check(audio.AmbienceZones.ActiveZone is null, "Scope unregisters ambience zones");
        Near(audio.Ambience.EffectiveVolume, 1, "Scope teardown restores environment mix");
        Throws<ObjectDisposedException>(() => scope.Own(other), "Disposed scope rejects new ownership");
    }

    private static void AmbienceChecks()
    {
        using var audio = new AudioManager(new FakeBackend());
        audio.Ambience.Volume = .5f;
        var snapshot = new AudioSnapshot(new Dictionary<AudioBus, float> { [audio.Ambience] = .2f, [audio.Music] = .4f });
        audio.TransitionTo(snapshot, 1);
        audio.Update(.5f);
        Near(audio.Ambience.EffectiveVolume, .3f, "Snapshot fades separately from user volume");
        Near(audio.Music.EffectiveVolume, .7f, "Snapshot buses transition in parallel");
        audio.TransitionTo(AudioSnapshot.Default, 1);
        audio.Update(.5f);
        Near(audio.Ambience.EffectiveVolume, .4f, "Interrupted fade starts at current gain");
        audio.Update(.5f);
        var root = new AmbienceZone2D
        {
            Center = Vector2.Zero,
            HalfSize = new Vector2(100),
            TransitionSeconds = 0,
            Snapshot = new AudioSnapshot(new Dictionary<AudioBus, float> { [audio.Ambience] = .6f })
        };
        var inner = root with
        {
            HalfSize = new Vector2(20),
            Priority = 10,
            Snapshot = snapshot
        };
        using var outerRegistration = audio.AmbienceZones.Add(root);
        using var innerRegistration = audio.AmbienceZones.Add(inner);
        audio.Update(0);
        Check(ReferenceEquals(audio.AmbienceZones.ActiveZone, inner), "Higher priority overlapping zone wins");
        Near(audio.Ambience.EffectiveVolume, .1f, "Inner zone applies snapshot");
        audio.Listener.Position = new Vector2(50, 0);
        audio.Update(0);
        Near(audio.Ambience.EffectiveVolume, .3f, "Exiting child restores outer zone");
        Near(audio.Music.EffectiveVolume, 1, "Omitted bus returns to unity");
        outerRegistration.Dispose();
        audio.Update(0);
        Near(audio.Ambience.EffectiveVolume, .5f, "Unregistered zone restores baseline");
        Near(audio.Ambience.Volume, .5f, "Snapshots never overwrite saved user volume");
        audio.Listener.Position = Vector2.Zero;
        inner.Enabled = false;
        audio.Update(0);
        Check(audio.AmbienceZones.ActiveZone is null, "Disabled zones ignored");
    }

    private static void WaveChecks()
    {
        foreach (var bits in new[] { 8, 16, 24, 32 })
        {
            using var wave = Wave(bits);
            var clip = AudioClip.ReadWave(wave);
            Check(clip.SampleRate == 48000 && clip.FrameCount == 3 && clip.Channels == 1, $"PCM{bits} header");
            Near(clip.Sample(0, 0), -1, $"PCM{bits} negative sign");
            Near(clip.Sample(1, 0), 0, $"PCM{bits} zero");
            Check(clip.Sample(2, 0) > .99f, $"PCM{bits} positive maximum");
            Check(wave.CanRead, "Reader leaves caller's stream open");
        }
        using var floatWave = Wave(32, floating: true);
        Near(AudioClip.ReadWave(floatWave).Sample(2, 0), .75f, "IEEE float WAV supported");
        using var stereoWave = Wave(24, stereo: true, extensible: true);
        var stereo = AudioClip.ReadWave(stereoWave);
        Check(stereo.Channels == 2 && stereo.FrameCount == 3, "Extensible stereo PCM24 supported");
        using var truncated = new MemoryStream([82, 73, 70, 70, 99, 0, 0, 0]);
        Throws<InvalidDataException>(() => AudioClip.ReadWave(truncated), "Truncated WAV rejected");
        Throws<ArgumentException>(() => new AudioClip([float.NaN], 48000, 1), "Invalid PCM rejected");
        var decode = new AudioClip([-.5f, .5f], 48000, 1);
        Near(decode.Sample(.5, 0), 0, "Linear sample interpolation");
        Check(decode.ToPcm16().Length == 4, "16-bit output conversion");
        Check(stereo.ToPcm16(mono: true).Length == stereo.FrameCount * 2, "Spatial clips downmix to mono");
    }

    private static void MusicChecks()
    {
        var cue = new MusicCue("combat", [Constant(.1f), Constant(.3f), Constant(.5f)], 120);
        var music = new MusicTransport();
        music.Play(cue, MusicTransition.Immediate, 0);
        Near(Render(music, 1)[0], .1f, "Low intensity starts immediately");
        music.SetIntensity(.25f, 0);
        Near(Render(music, 1)[0], .2f, "Only adjacent alternate mixes blend");
        music.SetIntensity(1, 1);
        Render(music, 24000);
        Near(music.Intensity, .625f, "Ramp is based on sample count");
        Render(music, 24000);
        Near(music.Intensity, 1, "Intensity reaches exact target across loops");
        Near(Render(music, 1)[0], .5f, "High intensity remains phase-aligned");
        var frame = music.Frame;
        music.Paused = true;
        Check(Render(music, 20).All(value => value == 0) && music.Frame == frame, "Pause freezes musical position");
        music.Paused = false;
        Near(Render(music, 1)[0], .5f, "Resume continues without restart");
        var boss = new MusicCue("boss", [Constant(.8f)], 120);
        music.Play(boss, MusicTransition.NextBar, 0);
        Render(music, (int)(96000 - music.Frame));
        Check(music.CurrentCueId == "combat", "Track waits until next bar");
        Near(Render(music, 1)[0], .8f, "Track switches on exact bar boundary");
        Check(music.CurrentCueId == "boss", "Boss cue selected");
        music.Stop(.5f);
        Render(music, 12000);
        Near(Render(music, 1)[0], .4f, "Stop fades by sample count");
        Render(music, 12000);
        Check(!music.HasAudio, "Fade releases finished cue");
        music.Play(cue, MusicTransition.Immediate, 0);
        Render(music, 1);
        music.Play(boss, MusicTransition.NextBar, 0);
        music.Play(cue, MusicTransition.NextBar, 0);
        Render(music, 100000);
        Check(music.CurrentCueId == "combat", "Returning to current cue cancels stale transition");

        var waveform = new AudioClip([.1f, .2f, .3f, .4f], 48000, 1);
        var loop = new MusicCue("loop", [waveform], 120, loopStartFrame: 1, loopEndFrame: 3);
        var looping = new MusicTransport();
        looping.Play(loop, MusicTransition.Immediate, 0);
        var samples = Render(looping, 6);
        Near(samples[6], .6f, "Reverb tail overlaps next loop");
        Near(samples[8], .3f, "Loop stays sample-exact");
        Throws<ArgumentException>(() => new MusicCue("bad", [Constant(), Constant(frames: 4)], 120), "Misaligned variants rejected");
        Throws<ArgumentOutOfRangeException>(() => new MusicCue("bad", [Constant()], 0), "Invalid tempo rejected");

        var oneBlock = new MusicTransport();
        var manyBlocks = new MusicTransport();
        foreach (var transport in new[] { oneBlock, manyBlocks })
        {
            transport.Play(cue, MusicTransition.Immediate, .25f);
            transport.SetIntensity(1, 2);
        }
        var expected = Render(oneBlock, 96000);
        var actual = new List<float>();
        for (var remaining = 96000; remaining > 0;)
        {
            var count = Math.Min(remaining, 137);
            actual.AddRange(Render(manyBlocks, count));
            remaining -= count;
        }
        Check(expected.SequenceEqual(actual), "Rendering is invariant to frame/update chunk size");

        var intro = new AudioClip([.1f, .2f, .6f, .7f], 48000, 1);
        var nextWithIntro = new MusicCue("intro", [intro], 120, firstBeatFrame: 2);
        var alignment = new MusicTransport();
        alignment.Play(cue, MusicTransition.Immediate, 0);
        Render(alignment, 1);
        alignment.Play(nextWithIntro, MusicTransition.NextBar, 0);
        Render(alignment, 95999);
        Near(Render(alignment, 1)[0], .6f, "Incoming cue lands on its authored first beat");

        var fade = new MusicTransport();
        fade.Play(cue, MusicTransition.Immediate, 0);
        Render(fade, 1);
        fade.Play(boss, MusicTransition.Immediate, 1);
        Render(fade, 24000);
        Near(Render(fade, 1)[0], .45f, "Track crossfade weights sum to unity");
        fade.Play(nextWithIntro, MusicTransition.Immediate, 0);
        Render(fade, 24000);
        Check(fade.CurrentCueId == "intro", "Rapid requests wait for prior crossfade to finish");
    }

    private static float[] Render(MusicTransport transport, int frames)
    {
        var samples = new float[frames * 2];
        transport.Render(samples);
        return samples;
    }

    private static void PreferenceAndUiChecks()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "audio-checks", Guid.NewGuid().ToString("N")));
        Preferences.Initialize(root);
        using var backend = new FakeBackend();
        using var manager = new AudioManager(backend);
        try
        {
            Preferences.Set(AudioPreferences.MasterVolume, .5f);
            Preferences.Set(AudioPreferences.MusicVolume, .7f);
            Preferences.Set(AudioPreferences.FxVolume, .4f);
            Preferences.Set(AudioPreferences.AmbienceVolume, .6f);
            Preferences.Set(AudioPreferences.UiVolume, .25f);
            manager.Update(0);
            Near(manager.Ui.EffectiveVolume, .05f, "Preferences update without any UI screen");
            Preferences.Initialize(root);
            manager.Update(0);
            Near(manager.Ambience.Volume, .6f, "Ambience preference survives reload");
            Near(manager.Ui.Volume, .25f, "UI preference survives reload");
            var path = Path.Combine(root, "fixture.wav");
            using (var output = File.Create(path))
            using (var wave = Wave(24))
            {
                wave.CopyTo(output);
            }
            Check(ReferenceEquals(manager.LoadClip(path), manager.LoadClip(path)), "Decoded clips cached");
            using var ui = new UIAudioService(manager);
            using var voice = ui.Play(new UISoundCue { Asset = path, Volume = .8f, Loop = true });
            Near(backend.Instances.Last().Volume, .04f, "UI routes through shared bus once");
            ui.Volume = .5f;
            Near(backend.Instances.Last().Volume, .02f, "UI service volume affects active cues immediately");
            ui.Muted = true;
            Near(backend.Instances.Last().Volume, 0, "Service-local mute");
            ui.Muted = false;
            Preferences.Set(AudioPreferences.MasterVolume, .25f);
            manager.Update(0);
            Near(backend.Instances.Last().Volume, .01f, "Preference changes affect existing UI playback");
            Preferences.Set("audio.muted", true);
            manager.Update(0);
            Near(manager.Master.Volume, 0, "Legacy mute migrates");
            Check(!Preferences.Remove<bool>("audio.muted"), "Legacy mute retired");
            ui.Dispose();
            Check(!voice.IsPlaying, "UI service disposes only its voices");
            Check(manager.CachedClipCount == 1, "UI disposal preserves shared asset cache");
        }
        finally
        {
            Preferences.Shutdown();
        }
    }

    private static void AssetChecks()
    {
        using var manager = new AudioManager(new FakeBackend());
        foreach (var step in GameAudio.PreloadSteps(manager))
        {
            step();
        }
        Check(manager.CachedClipCount == 8, "Only six music variants and two UI effects are preloaded");
        var menu = GameAudio.MenuCue(manager);
        var session = GameAudio.SessionCue(manager);
        foreach (var cue in new[] { menu, session })
        {
            Check(cue.Intensities.Count == 3 && cue.SampleRate == 48000, "Imported low/medium/full variants are aligned at 48 kHz");
            Check(cue.BeatsPerMinute == 112, "Cue tempo matches embedded vendor metadata");
            Check(cue.Intensities[0].FrameCount - cue.LoopEndFrame == 309840, "Vendor's 6.455-second tail is excluded from loop body");
            foreach (var intensity in new[] { 0f, .25f, .5f, 1f })
            {
                Check(float.IsFinite(cue.Sample(cue.LoopEndFrame, 0, intensity)), "Licensed cue can render its loop/tail boundary at every intensity");
            }
        }
        Check(menu.Intensities[0].FrameCount == 4629429 && session.Intensities[0].FrameCount == 5246590,
            "Selected files preserve their full source sample counts");
        Check(ReferenceEquals(menu.Intensities[0], GameAudio.MenuCue(manager).Intensities[0]) && manager.CachedClipCount == 8,
            "Scene entry reuses preloaded clips instead of decoding or duplicating them");
        var transport = new MusicTransport();
        transport.Play(menu, MusicTransition.Immediate, 0);
        Render(transport, 4800);
        transport.Play(session, MusicTransition.Immediate, .1f);
        transport.SetIntensity(.25f, .2f);
        var samples = Render(transport, 12000);
        Check(transport.CurrentCueId == session.Id && samples.All(float.IsFinite) && samples.Any(sample => Math.Abs(sample) > .001f),
            "Real imported tracks crossfade with non-silent finite output");
        Near(transport.Intensity, .25f, "Session preparation intensity ramps without restarting its cue");
        Console.WriteLine("Imported Ovani asset checks passed (offline rendering, no playback).");
    }

    private static void NativeChecks()
    {
        using var manager = new AudioManager();
        manager.Master.Volume = 0;
        var silent = Constant(0);
        using var voice = manager.Play2D(silent, new Vector2(100, 0), options: new AudioPlayOptions { Loop = true });
        Check(voice.IsPlaying, "Native spatial voice starts");
        using var highRate = manager.Play(Constant(0, frames: 960, sampleRate: 96000));
        Check(highRate.IsPlaying, "Native high-sample-rate WAV playback");
        manager.MusicPlayer.Play(new MusicCue("native-silence", [silent, silent, silent], 120));
        for (var i = 0; i < 15; i++)
        {
            FrameworkDispatcher.Update();
            manager.Update(.016f);
            Thread.Sleep(16);
        }
        Check(manager.MusicPlayer.RenderedFrames > 3072, "Native PCM stream refills");
        Check(manager.MusicPlayer.UnderrunCount == 0, "Normal update cadence keeps the music queue filled");
        var frame = manager.MusicPlayer.RenderedFrames;
        manager.Pause();
        manager.Update(.1f);
        Check(voice.IsPaused && manager.MusicPlayer.RenderedFrames == frame, "Native pause preserves queued music");
        manager.Resume();
        Check(!voice.IsPaused, "Native resume");
        manager.MusicPlayer.Stop(0);
        voice.Stop();
        Check(!voice.IsPlaying, "Native stop");
        Console.WriteLine("Native OpenAL/MonoGame smoke checks passed (silent fixture).");
    }

    private static MemoryStream Wave(int bits, bool floating = false, bool stereo = false, bool extensible = false)
    {
        var channels = stereo ? 2 : 1;
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            var bytes = 3 * channels * bits / 8;
            var fmt = extensible ? 40 : 16;
            writer.Write("RIFF"u8);
            writer.Write(4 + 8 + fmt + 10 + 8 + bytes + (bytes & 1));
            writer.Write("WAVEfmt "u8);
            writer.Write(fmt);
            writer.Write((ushort)(extensible ? 0xfffe : floating ? 3 : 1));
            writer.Write((ushort)channels);
            writer.Write(48000);
            writer.Write(48000 * channels * bits / 8);
            writer.Write((ushort)(channels * bits / 8));
            writer.Write((ushort)bits);
            if (extensible)
            {
                writer.Write((ushort)22);
                writer.Write((ushort)bits);
                writer.Write(3);
                writer.Write(new Guid("00000001-0000-0010-8000-00aa00389b71").ToByteArray());
            }
            writer.Write("JUNK"u8);
            writer.Write(1);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write("data"u8);
            writer.Write(bytes);
            for (var i = 0; i < 3; i++)
            {
                for (var channel = 0; channel < channels; channel++)
                {
                    if (floating)
                    {
                        writer.Write(i == 0 ? -.75f : i == 1 ? 0 : .75f);
                    }
                    else
                    {
                        var value = bits == 8 ? i == 0 ? 0 : i == 1 ? 128 : 255 :
                            i == 0 ? -(1L << (bits - 1)) : i == 1 ? 0 : (1L << (bits - 1)) - 1;
                        for (var b = 0; b < bits / 8; b++)
                        {
                            writer.Write((byte)(value >> (b * 8)));
                        }
                    }
                }
            }
            if ((bytes & 1) != 0)
            {
                writer.Write((byte)0);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Near(float actual, float expected, string message)
        => Check(MathF.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            _assertions++;
            return;
        }
        throw new InvalidOperationException(message);
    }
}

internal sealed class FakeBackend : IAudioBackend
{
    public List<FakeInstance> Instances { get; } = [];
    public bool Disposed
    {
        get; private set;
    }
    public void Prepare(AudioClip clip, bool spatial)
    {
    }
    public IAudioInstance Create(AudioClip clip, bool spatial)
    {
        var instance = new FakeInstance();
        Instances.Add(instance);
        return instance;
    }
    public void Dispose() => Disposed = true;
}

internal sealed class FakeInstance : IAudioInstance
{
    public SoundState State { get; private set; } = SoundState.Stopped;
    public float Volume
    {
        get; set;
    }
    public float Pan
    {
        get; set;
    }
    public float Pitch
    {
        get; set;
    }
    public bool Loop
    {
        get; set;
    }
    public bool Disposed
    {
        get; private set;
    }
    public void Play() => State = SoundState.Playing;
    public void Pause() => State = SoundState.Paused;
    public void Resume() => State = SoundState.Playing;
    public void Stop() => State = SoundState.Stopped;
    public void Dispose() => Disposed = true;
}
