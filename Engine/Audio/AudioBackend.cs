using Microsoft.Xna.Framework.Audio;

namespace Graphite.Engine.Audio;

internal interface IAudioBackend : IDisposable
{
    void Prepare(AudioClip clip, bool spatial);
    IAudioInstance Create(AudioClip clip, bool spatial);
}

internal interface IAudioInstance : IDisposable
{
    SoundState State
    {
        get;
    }
    float Volume
    {
        set;
    }
    float Pan
    {
        set;
    }
    float Pitch
    {
        set;
    }
    bool Loop
    {
        set;
    }
    void Play();
    void Pause();
    void Resume();
    void Stop();
}

internal sealed class MonoGameAudioBackend : IAudioBackend
{
    private readonly Dictionary<(AudioClip Clip, bool Spatial), SoundEffect> _sounds = [];

    public void Prepare(AudioClip clip, bool spatial)
    {
        if (!_sounds.ContainsKey((clip, spatial)))
        {
            // Managed buses own every gain, including music. Never put FX gain on this global knob.
            SoundEffect.MasterVolume = 1;
            var outputRate = Math.Min(clip.SampleRate, 48000);
            var sound = new SoundEffect(clip.ToPcm16(mono: spatial, sampleRate: outputRate), outputRate,
                spatial ? AudioChannels.Mono : (AudioChannels)clip.Channels);
            _sounds.Add((clip, spatial), sound);
        }
    }

    public IAudioInstance Create(AudioClip clip, bool spatial)
    {
        Prepare(clip, spatial);
        return new Instance(_sounds[(clip, spatial)].CreateInstance());
    }

    public void Dispose()
    {
        foreach (var sound in _sounds.Values)
        {
            sound.Dispose();
        }
        _sounds.Clear();
    }

    private sealed class Instance(SoundEffectInstance source) : IAudioInstance
    {
        public SoundState State => source.State;
        public float Volume
        {
            set => source.Volume = value;
        }
        public float Pan
        {
            set => source.Pan = value;
        }
        public float Pitch
        {
            set => source.Pitch = value;
        }
        public bool Loop
        {
            set => source.IsLooped = value;
        }
        public void Play() => source.Play();
        public void Pause() => source.Pause();
        public void Resume() => source.Resume();
        public void Stop() => source.Stop();
        public void Dispose() => source.Dispose();
    }
}
