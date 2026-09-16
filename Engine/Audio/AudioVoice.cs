using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Graphite.Engine.Audio;

public sealed record AudioPlayOptions
{
    public AudioBus? Bus
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
    /// <summary>Higher priorities survive voice-budget pressure. Equal priority steals the oldest.</summary>
    public int Priority
    {
        get; init;
    }
}

/// <summary>Owned playback handle. Dispose/Stop releases its native voice, not the shared clip.</summary>
public sealed class AudioVoice : IDisposable
{
    private readonly AudioManager _manager;
    private IAudioInstance? _instance;
    private readonly SpatialAudioSettings? _spatial;
    private readonly float _pan;
    private Vector2 _position;
    private float _volume;
    private bool _manuallyPaused;

    internal AudioVoice(AudioManager manager, IAudioInstance? instance, AudioBus bus,
        AudioPlayOptions options, Vector2 position, SpatialAudioSettings? spatial)
    {
        _manager = manager;
        _instance = instance;
        Bus = bus;
        _volume = options.Volume;
        Priority = options.Priority;
        _pan = options.Pan;
        _position = position;
        _spatial = spatial;
    }

    public AudioBus Bus
    {
        get;
    }
    public int Priority
    {
        get;
    }
    /// <summary>True while playing or paused; false after completion, rejection, stop or disposal.</summary>
    public bool IsPlaying => _instance is { State: not SoundState.Stopped };
    public bool IsPaused => _instance?.State == SoundState.Paused;
    public float EffectiveVolume
    {
        get; private set;
    }
    public float EffectivePan
    {
        get; private set;
    }
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = AudioMath.Unit(value, nameof(Volume));
            Refresh();
        }
    }
    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = SpatialAudio2D.Finite(value, nameof(Position));
            Refresh();
        }
    }

    public void Pause()
    {
        _manuallyPaused = true;
        Refresh();
    }

    public void Resume()
    {
        _manuallyPaused = false;
        Refresh();
    }

    internal void Refresh()
    {
        if (_instance is null)
        {
            return;
        }
        var spatial = _spatial is null ? new SpatialAudioMix(1, _pan)
            : SpatialAudio2D.Calculate(_manager.Listener, Position, _spatial);
        EffectiveVolume = Math.Clamp(Volume * Bus.EffectiveVolume * spatial.Gain, 0, 1);
        EffectivePan = spatial.Pan;
        _instance.Volume = EffectiveVolume;
        _instance.Pan = EffectivePan;
        var pause = _manuallyPaused || (_manager.IsPaused && Bus.PauseWithGame);
        if (pause && _instance.State == SoundState.Playing)
        {
            _instance.Pause();
        }
        else if (!pause && _instance.State == SoundState.Paused)
        {
            _instance.Resume();
        }
    }

    public void Stop() => Dispose();

    public void Dispose()
    {
        if (_instance is not { } instance)
        {
            return;
        }
        _instance = null;
        instance.Stop();
        instance.Dispose();
        EffectiveVolume = 0;
    }
}
