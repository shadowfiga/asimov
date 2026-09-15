using Microsoft.Xna.Framework.Audio;

namespace Graphite.Engine.UI.Audio;

public sealed record UISoundCue
{
    public required string Asset
    {
        get; init;
    }
    public float Delay
    {
        get; init;
    }
    public float Volume { get; init; } = 1;
    public float Pitch
    {
        get; init;
    }
    public float Pan
    {
        get; init;
    }
    public bool Loop
    {
        get; init;
    }
}

public interface IUIAudioVoice : IDisposable
{
    bool IsPlaying
    {
        get;
    }
    void Stop();
}

public interface IUIAudioService : IDisposable
{
    float Volume
    {
        get; set;
    }
    bool Muted
    {
        get; set;
    }
    IUIAudioVoice Play(UISoundCue cue);
    void Update();
}

public sealed class UIAudioService : IUIAudioService
{
    private readonly Dictionary<string, SoundEffect> _assets = [];
    private readonly List<Voice> _voices = [];
    private float _volume = 1;
    public float Volume
    {
        get => _volume; set => _volume = Math.Clamp(value, 0, 1);
    }
    public bool Muted
    {
        get; set;
    }

    public IUIAudioVoice Play(UISoundCue cue)
    {
        if (!_assets.TryGetValue(cue.Asset, out var asset))
        {
            using var stream = File.OpenRead(Path.GetFullPath(cue.Asset, AppContext.BaseDirectory));
            asset = SoundEffect.FromStream(stream);
            _assets.Add(cue.Asset, asset);
        }
        var instance = asset.CreateInstance();
        try
        {
            instance.Volume = Muted ? 0 : Math.Clamp(cue.Volume * Volume, 0, 1);
            instance.Pitch = Math.Clamp(cue.Pitch, -1, 1);
            instance.Pan = Math.Clamp(cue.Pan, -1, 1);
            instance.IsLooped = cue.Loop;
            var voice = new Voice(instance, cue.Volume);
            instance.Play();
            _voices.Add(voice);
            return voice;
        }
        catch
        {
            instance.Dispose();
            throw;
        }
    }

    public void Update()
    {
        foreach (var voice in _voices.ToArray())
        {
            if (!voice.IsPlaying)
            {
                voice.Dispose();
                _voices.Remove(voice);
            }
            else
            {
                voice.SetVolume(Muted ? 0 : Math.Clamp(voice.Volume * Volume, 0, 1));
            }
        }
    }

    public void Dispose()
    {
        foreach (var voice in _voices)
        {
            voice.Dispose();
        }

        _voices.Clear();
        foreach (var asset in _assets.Values)
        {
            asset.Dispose();
        }

        _assets.Clear();
    }

    private sealed class Voice(SoundEffectInstance instance, float volume) : IUIAudioVoice
    {
        private bool _disposed;
        public float Volume { get; } = volume;
        public bool IsPlaying => !_disposed && instance.State != SoundState.Stopped;
        public void SetVolume(float value)
        {
            if (!_disposed)
            {
                instance.Volume = value;
            }
        }
        public void Stop()
        {
            if (!_disposed)
            {
                instance.Stop();
            }
        }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            instance.Stop();
            instance.Dispose();
            _disposed = true;
        }
    }
}
