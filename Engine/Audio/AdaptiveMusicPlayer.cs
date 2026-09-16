using System.Buffers.Binary;
using Microsoft.Xna.Framework.Audio;

namespace Graphite.Engine.Audio;

/// <summary>Sample-synchronized intensity mixes and bar-scheduled track transitions.</summary>
public sealed class AdaptiveMusicPlayer : IDisposable
{
    private const int BufferFrames = 1024;
    private const int QueuedBuffers = 3;
    private readonly AudioBus _bus;
    private readonly MusicTransport _transport = new();
    private readonly float[] _mix = new float[BufferFrames * 2];
    private readonly byte[] _pcm = new byte[BufferFrames * 4];
    private DynamicSoundEffectInstance? _output;
    private bool _disposed;
    private bool _started;
    private bool _manuallyPaused;
    private bool _gamePaused;

    internal AdaptiveMusicPlayer(AudioBus bus) => _bus = bus;
    public string? CurrentCueId => _transport.CurrentCueId;
    public float Intensity => _transport.Intensity;
    /// <summary>Rendered timeline, ahead of the speakers by at most the queued audio buffers.</summary>
    public long RenderedFrames => _transport.Frame;
    public bool IsPaused => _transport.Paused;
    public int UnderrunCount
    {
        get; private set;
    }

    public void Play(MusicCue cue, MusicTransition transition = MusicTransition.NextBar, float fadeSeconds = 1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _transport.Play(cue, transition, fadeSeconds);
    }

    public void SetIntensity(float intensity, float rampSeconds = 1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _transport.SetIntensity(intensity, rampSeconds);
    }

    public void Stop(float fadeSeconds = 1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _transport.Stop(fadeSeconds);
        if (fadeSeconds == 0)
        {
            _output?.Stop();
            _started = false;
        }
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _manuallyPaused = true;
        RefreshPause();
    }

    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _manuallyPaused = false;
        RefreshPause();
    }

    internal void SetGamePaused(bool paused)
    {
        _gamePaused = paused;
        RefreshPause();
    }

    private void RefreshPause()
    {
        _transport.Paused = _manuallyPaused || _gamePaused;
        if (IsPaused)
        {
            _output?.Pause();
        }
        else if (_output?.State == SoundState.Paused)
        {
            _output.Resume();
        }
    }

    internal void RefreshVolume()
    {
        if (_output is not null)
        {
            _output.Volume = _bus.EffectiveVolume;
        }
    }

    internal void Update()
    {
        if (_disposed || IsPaused)
        {
            return;
        }
        if (!_transport.HasAudio)
        {
            if (_output?.PendingBufferCount == 0)
            {
                _output.Stop();
                _started = false;
            }
            return;
        }
        if (_output is null)
        {
            SoundEffect.MasterVolume = 1;
            _output = new DynamicSoundEffectInstance(MusicTransport.SampleRate, AudioChannels.Stereo);
        }
        if (_started && _output.PendingBufferCount == 0)
        {
            UnderrunCount++;
        }
        while (_output.PendingBufferCount < QueuedBuffers && _transport.HasAudio)
        {
            _transport.Render(_mix);
            for (var i = 0; i < _mix.Length; i++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(_pcm.AsSpan(i * 2), AudioClip.ToInt16(_mix[i]));
            }
            _output.SubmitBuffer(_pcm);
        }
        RefreshVolume();
        if (_output.State != SoundState.Playing)
        {
            _output.Play();
        }
        _started = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _transport.Stop(0);
        _output?.Dispose();
        _output = null;
    }
}
