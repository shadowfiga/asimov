using Graphite.Engine.UI.Audio;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.UI.Animation;

public sealed record UIAnimation
{
    public float Duration { get; init; } = .3f;
    public float Delay
    {
        get; init;
    }
    /// <summary>Zero repeats indefinitely.</summary>
    public int Repeat { get; init; } = 1;
    public bool Alternate
    {
        get; init;
    }
    public IReadOnlyList<UIAnimationTrack> Tracks { get; init; } = [];
    public IReadOnlyList<UISoundCue> Sounds { get; init; } = [];
    public bool IsInfinite => Repeat == 0 || Sounds.Any(cue => cue.Loop);

    internal void Validate()
    {
        if (!float.IsFinite(Duration) || Duration <= 0 || !float.IsFinite(Delay) || Delay < 0 || Repeat < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Duration), "Animation timing must be finite and positive; repeat must be nonnegative.");
        }

        foreach (var sound in Sounds)
        {
            if (!float.IsFinite(sound.Delay) || sound.Delay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Sounds));
            }
        }
    }
}

public abstract class UIAnimationTrack
{
    public Func<float, float> Easing { get; init; } = UIEasing.Linear;
    public abstract void Apply(UIAnimationFrame frame, float progress);
}

public static class UIEasing
{
    public static float Linear(float t) => t;
    public static float Smooth(float t) => t * t * (3 - 2 * t);
    public static float OutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
}

public sealed class UIAnimationFrame(UIParameters parameters)
{
    public UIParameters Parameters { get; } = parameters;
    public Vector2 Translation
    {
        get; private set;
    }
    public float Rotation
    {
        get; private set;
    }
    public Vector2 Scale { get; private set; } = Vector2.One;
    public float Opacity { get; private set; } = 1;
    internal float Weight { get; set; } = 1;
    public void Translate(Vector2 value) => Translation += value * Weight;
    public void Rotate(float degrees) => Rotation += degrees * Weight;
    public void Resize(Vector2 value) => Scale *= Vector2.Lerp(Vector2.One, value, Weight);
    public void Fade(float value) => Opacity *= MathHelper.Lerp(1, Math.Clamp(value, 0, 1), Weight);
    public void Set<T>(UIParameter<T> parameter, T value, Func<T, T, float, T> interpolate) where T : struct
        => Parameters.Set(parameter, interpolate(Parameters.Get(parameter), value, Weight));
    public bool IsIdentity => Translation == Vector2.Zero && Rotation == 0 && Scale == Vector2.One && Opacity == 1;
}

public sealed class UIParameterTrack<T>(UIParameter<T> parameter, T from, T to,
    Func<T, T, float, T> interpolate) : UIAnimationTrack where T : struct
{
    public override void Apply(UIAnimationFrame frame, float progress)
        => frame.Set(parameter, interpolate(from, to, progress), interpolate);
}

public sealed class UITransformTrack(Action<UIAnimationFrame, float> sample) : UIAnimationTrack
{
    public override void Apply(UIAnimationFrame frame, float progress) => sample(frame, progress);
}

public enum UIPlaybackState
{
    Running, Completed, Cancelled
}

public sealed class UIPlayback
{
    private readonly TaskCompletionSource<UIPlaybackState> _completion = new();
    private readonly Action<float> _cancel;
    internal UIPlayback(Action<float> cancel) => _cancel = cancel;
    public UIPlaybackState State
    {
        get; private set;
    }
    public bool IsFinished => State != UIPlaybackState.Running;
    public Task<UIPlaybackState> Completion => _completion.Task;
    public void Cancel(float settleSeconds = 0)
    {
        if (!float.IsFinite(settleSeconds) || settleSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settleSeconds));
        }

        if (!IsFinished)
        {
            _cancel(settleSeconds);
        }
    }
    internal void Finish(UIPlaybackState state)
    {
        if (IsFinished)
        {
            return;
        }

        State = state;
        _completion.TrySetResult(state);
    }
}

/// <summary>Per-component clock and animation state. Definitions may be shared freely.</summary>
public sealed class UIAnimationPlayer(IUIAudioService audio) : IDisposable
{
    private readonly List<Playback> _active = [];
    public UIParameters BaseParameters { get; } = new();
    public UIParameters Parameters { get; } = new();
    public UIAnimationFrame Frame { get; private set; } = new(new UIParameters());
    public int ActiveCount => _active.Count;
    public float ElapsedTime
    {
        get; private set;
    }

    public UIPlayback Play(UIAnimation animation)
    {
        animation.Validate();
        // Snapshot collection membership; later edits cannot change a running timeline.
        var snapshot = animation with
        {
            Tracks = animation.Tracks.ToArray(),
            Sounds = animation.Sounds.ToArray()
        };
        var playback = new Playback(snapshot, audio);
        _active.Add(playback);
        Evaluate();
        return playback.Handle;
    }

    public void Update(float dt)
    {
        if (!float.IsFinite(dt) || dt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dt));
        }

        ElapsedTime += dt;
        foreach (var playback in _active.ToArray())
        {
            playback.Update(dt);
        }

        _active.RemoveAll(playback => playback.Handle.IsFinished);
        Evaluate();
    }

    private void Evaluate()
    {
        Parameters.CopyFrom(BaseParameters);
        Frame = new UIAnimationFrame(Parameters);
        foreach (var playback in _active)
        {
            playback.Apply(Frame);
        }
    }

    public void CancelAll()
    {
        foreach (var playback in _active.ToArray())
        {
            playback.Handle.Cancel();
        }

        _active.Clear();
        Evaluate();
    }
    public void Dispose() => CancelAll();

    private sealed class Playback
    {
        private readonly UIAnimation _animation;
        private readonly IUIAudioService _audio;
        private readonly List<IUIAudioVoice> _voices = [];
        private readonly HashSet<int> _playedSounds = [];
        private float _time;
        private float _settleTime;
        private float _settleDuration;
        private bool _cancelled;
        public UIPlayback Handle
        {
            get;
        }
        public Playback(UIAnimation animation, IUIAudioService audio)
        {
            _animation = animation;
            _audio = audio;
            Handle = new UIPlayback(Cancel);
        }
        private void Cancel(float settle)
        {
            if (_cancelled)
            {
                if (settle == 0)
                {
                    Finish(UIPlaybackState.Cancelled);
                }
                return;
            }
            _cancelled = true;
            _settleDuration = settle;
            StopSounds();
            if (settle == 0)
            {
                Finish(UIPlaybackState.Cancelled);
            }
        }
        public void Update(float dt)
        {
            if (Handle.IsFinished)
            {
                return;
            }

            if (_cancelled)
            {
                _settleTime += dt;
                if (_settleTime >= _settleDuration)
                {
                    Finish(UIPlaybackState.Cancelled);
                }

                return;
            }
            _time += dt;
            for (var i = 0; i < _animation.Sounds.Count; i++)
            {
                if (_time < _animation.Delay + _animation.Sounds[i].Delay || !_playedSounds.Add(i))
                {
                    continue;
                }

                _voices.Add(_audio.Play(_animation.Sounds[i]));
            }
            if (_animation.Repeat > 0 && _time >= _animation.Delay + _animation.Duration * _animation.Repeat &&
                _playedSounds.Count == _animation.Sounds.Count && _voices.All(voice => !voice.IsPlaying))
            {
                Finish(UIPlaybackState.Completed);
            }
        }
        public void Apply(UIAnimationFrame frame)
        {
            if (Handle.IsFinished || _time < _animation.Delay)
            {
                return;
            }

            var time = _time - _animation.Delay;
            // Audio may outlive visual tracks; completed tracks release their parameter ownership.
            if (_animation.Repeat > 0 && time >= _animation.Duration * _animation.Repeat)
            {
                return;
            }

            var cycle = (int)(time / _animation.Duration);
            var progress = time % _animation.Duration / _animation.Duration;
            if (_animation.Alternate && cycle % 2 == 1)
            {
                progress = 1 - progress;
            }

            frame.Weight = _cancelled ? 1 - UIEasing.Smooth(Math.Clamp(_settleTime / _settleDuration, 0, 1)) : 1;
            foreach (var track in _animation.Tracks)
            {
                track.Apply(frame, track.Easing(progress));
            }

            frame.Weight = 1;
        }
        private void StopSounds()
        {
            foreach (var voice in _voices)
            {
                voice.Dispose();
            }
            _voices.Clear();
        }
        private void Finish(UIPlaybackState state)
        {
            StopSounds();
            Handle.Finish(state);
        }
    }
}
