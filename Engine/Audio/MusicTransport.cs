namespace Graphite.Engine.Audio;

/// <summary>Deterministic PCM transport. All intensity mixes share one output frame counter.</summary>
internal sealed class MusicTransport
{
    internal const int SampleRate = 48000;
    private Layer? _current;
    private Layer? _outgoing;
    private Pending? _pending;
    private float _intensityFrom;
    private float _intensityTo;
    private long _intensityStart;
    private long _intensityDuration;
    public long Frame
    {
        get; private set;
    }
    public bool Paused
    {
        get; set;
    }
    public string? CurrentCueId => _current?.Cue.Id;
    public bool HasAudio => _current is not null || _outgoing is not null || _pending is not null;
    public float Intensity => _intensityDuration == 0 ? _intensityTo :
        _intensityFrom + (_intensityTo - _intensityFrom) * Math.Clamp((Frame - _intensityStart) / (float)_intensityDuration, 0, 1);

    public void SetIntensity(float value, float seconds)
    {
        AudioMath.Unit(value, nameof(value));
        AudioMath.NonNegative(seconds, nameof(seconds));
        _intensityFrom = Intensity;
        _intensityTo = value;
        _intensityStart = Frame;
        _intensityDuration = Frames(seconds);
    }

    public void Play(MusicCue cue, MusicTransition transition, float fadeSeconds)
    {
        ArgumentNullException.ThrowIfNull(cue);
        AudioMath.NonNegative(fadeSeconds, nameof(fadeSeconds));
        if (!Enum.IsDefined(transition))
        {
            throw new ArgumentOutOfRangeException(nameof(transition));
        }
        if (ReferenceEquals(_current?.Cue, cue) && _current.End == long.MaxValue)
        {
            _pending = null;
            return;
        }
        var boundary = Math.Max(Frame, _outgoing?.End ?? Frame);
        if (transition == MusicTransition.NextBar && _current is not null)
        {
            var firstBeat = _current.Start - _current.OffsetFrames + _current.Cue.FirstBeatFrame * (SampleRate / (double)_current.Cue.SampleRate);
            var barFrames = SampleRate * 60d / _current.Cue.BeatsPerMinute * _current.Cue.BeatsPerBar;
            boundary = Math.Max(boundary, (long)Math.Ceiling(firstBeat +
                Math.Max(0, Math.Floor((boundary - firstBeat) / barFrames) + 1) * barFrames));
        }
        var offset = transition == MusicTransition.NextBar && _current is not null
            ? (long)Math.Round(cue.FirstBeatFrame * (SampleRate / (double)cue.SampleRate)) : 0;
        _pending = new Pending(cue, boundary, Frames(fadeSeconds), offset);
    }

    public void Stop(float fadeSeconds)
    {
        AudioMath.NonNegative(fadeSeconds, nameof(fadeSeconds));
        _pending = null;
        _current?.FadeOut(Frame, Frames(fadeSeconds));
        _outgoing?.FadeOut(Frame, Frames(fadeSeconds));
        RemoveFinished();
    }

    public void Render(Span<float> stereo)
    {
        if (stereo.Length % 2 != 0)
        {
            throw new ArgumentException("Expected interleaved stereo frames.", nameof(stereo));
        }
        stereo.Clear();
        if (Paused)
        {
            return;
        }
        for (var i = 0; i < stereo.Length; i += 2)
        {
            RemoveFinished();
            if (_pending is { } pending && Frame >= pending.Start)
            {
                _outgoing = _current;
                _outgoing?.FadeOut(Frame, pending.FadeFrames);
                _current = new Layer(pending.Cue, Frame, pending.FadeFrames, pending.OffsetFrames);
                _pending = null;
            }
            var intensity = Intensity;
            for (var channel = 0; channel < 2; channel++)
            {
                stereo[i + channel] = Read(_current, channel, intensity) + Read(_outgoing, channel, intensity);
            }
            Frame++;
        }
        RemoveFinished();
    }

    private float Read(Layer? layer, int channel, float intensity) => layer is null ? 0 :
        layer.Cue.Sample(Frame - layer.Start + layer.OffsetFrames, channel, intensity) * layer.Gain(Frame);

    private void RemoveFinished()
    {
        if (_outgoing is not null && Frame >= _outgoing.End)
        {
            _outgoing = null;
        }
        if (_current is not null && Frame >= _current.End)
        {
            _current = null;
        }
    }

    private static long Frames(float seconds) => checked((long)Math.Round(seconds * (double)SampleRate));
    private sealed record Pending(MusicCue Cue, long Start, long FadeFrames, long OffsetFrames);

    private sealed class Layer(MusicCue cue, long start, long fadeFrames, long offsetFrames)
    {
        private long _fadeOutStart;
        private float _fadeOutGain;
        public MusicCue Cue { get; } = cue;
        public long OffsetFrames { get; } = offsetFrames;
        public long Start { get; } = start;
        public long End { get; private set; } = long.MaxValue;

        public void FadeOut(long frame, long duration)
        {
            _fadeOutGain = Gain(frame);
            _fadeOutStart = frame;
            End = frame + duration;
        }

        public float Gain(long frame)
        {
            if (End != long.MaxValue)
            {
                return End <= _fadeOutStart ? 0 : _fadeOutGain * Math.Clamp((End - frame) / (float)(End - _fadeOutStart), 0, 1);
            }
            return fadeFrames == 0 ? 1 : Math.Clamp((frame - Start) / (float)fadeFrames, 0, 1);
        }
    }
}
