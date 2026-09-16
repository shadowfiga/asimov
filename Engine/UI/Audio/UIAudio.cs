using Graphite.Engine.Audio;

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
    private readonly AudioManager _manager;
    private readonly List<Voice> _voices = [];
    private float _volume = 1;
    private bool _muted;
    private bool _disposed;

    public UIAudioService(AudioManager? manager = null) => _manager = manager ?? AudioManager.Current;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = AudioMath.Unit(value, nameof(Volume));
            RefreshVoices();
        }
    }
    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            RefreshVoices();
        }
    }

    public IUIAudioVoice Play(UISoundCue cue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(cue);
        AudioMath.Unit(cue.Volume, nameof(cue.Volume));
        // UIAnimationPlayer already schedules cue.Delay; do not apply the delay a second time.
        var voice = new Voice(_manager.Play(cue.Asset, new AudioPlayOptions
        {
            Bus = _manager.Ui,
            Volume = Muted ? 0 : cue.Volume * Volume,
            Pitch = cue.Pitch,
            Pan = cue.Pan,
            Loop = cue.Loop,
            Priority = 100
        }), cue.Volume);
        _voices.Add(voice);
        return voice;
    }

    public void Update()
    {
        for (var i = _voices.Count - 1; i >= 0; i--)
        {
            var voice = _voices[i];
            if (!voice.IsPlaying)
            {
                voice.Dispose();
                _voices.RemoveAt(i);
            }
        }
    }

    private void RefreshVoices()
    {
        foreach (var voice in _voices)
        {
            voice.SetVolume(Muted ? 0 : voice.Volume * Volume);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        foreach (var voice in _voices)
        {
            voice.Dispose();
        }

        _voices.Clear();
    }

    private sealed class Voice(AudioVoice instance, float volume) : IUIAudioVoice
    {
        public float Volume { get; } = volume;
        public bool IsPlaying => instance.IsPlaying;
        public void SetVolume(float value) => instance.Volume = value;
        public void Stop() => instance.Stop();
        public void Dispose() => instance.Dispose();
    }
}
