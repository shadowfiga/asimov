using Graphite.Engine.Objects;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Graphics;

public readonly record struct AnimationFrame(Rectangle Source, float Duration);

/// <summary>Optional sprite-sheet playback; never changes object transforms.</summary>
public sealed class FrameAnimator : Component
{
    private readonly SpriteRenderer _sprite;
    private readonly AnimationFrame[] _frames = [];
    private readonly double _duration;
    private double _time;
    public bool Loop { get; set; } = true;
    public bool Playing { get; set; } = true;
    public int FrameIndex
    {
        get; private set;
    }
    public override int UpdateOrder => 50;

    public FrameAnimator(SpriteRenderer sprite, IEnumerable<AnimationFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(sprite);
        ArgumentNullException.ThrowIfNull(frames);
        _sprite = sprite;
        _frames = frames.ToArray();
        if (_frames.Length == 0)
        {
            throw new ArgumentException("An animation requires at least one frame.", nameof(frames));
        }
        foreach (var frame in _frames)
        {
            if (!float.IsFinite(frame.Duration) || frame.Duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frames), "Frame durations must be finite and positive.");
            }
            sprite.ValidateFrame(frame.Source);
            _duration += frame.Duration;
        }
    }
    protected override void OnAdded()
    {
        if (_sprite.Owner != Owner)
        {
            throw new InvalidOperationException("The animated sprite must be attached to the same object first.");
        }
        _sprite.SourceRectangle = _frames[0].Source;
    }
    public void Restart()
    {
        _time = 0;
        FrameIndex = 0;
        Playing = true;
        _sprite.SourceRectangle = _frames[0].Source;
    }
    protected internal override void Update(float dt)
    {
        if (!Playing)
        {
            return;
        }
        ObjectDisposedException.ThrowIf(_sprite.IsDisposed, _sprite);
        _time += dt;
        if (Loop)
        {
            _time %= _duration;
        }
        else if (_time >= _duration)
        {
            _time = _duration;
            Playing = false;
        }
        var end = 0d;
        FrameIndex = _frames.Length - 1;
        for (var i = 0; i < _frames.Length; i++)
        {
            end += _frames[i].Duration;
            if (_time < end)
            {
                FrameIndex = i;
                break;
            }
        }
        _sprite.SourceRectangle = _frames[FrameIndex].Source;
    }
}
