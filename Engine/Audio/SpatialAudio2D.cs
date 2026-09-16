using Microsoft.Xna.Framework;

namespace Graphite.Engine.Audio;

/// <summary>World-space listener; independent of UI scale, resolution and camera zoom.</summary>
public sealed class AudioListener2D
{
    private Vector2 _position;
    private Vector2 _right = Vector2.UnitX;

    public Vector2 Position
    {
        get => _position;
        set => _position = SpatialAudio2D.Finite(value, nameof(Position));
    }

    public Vector2 Right
    {
        get => _right;
        set
        {
            SpatialAudio2D.Finite(value, nameof(Right));
            var length = value.Length();
            if (!float.IsFinite(length) || length < .0001f)
            {
                throw new ArgumentOutOfRangeException(nameof(Right));
            }
            _right = value / length;
        }
    }
}

public sealed record SpatialAudioSettings
{
    public float InnerRadius { get; init; } = 64;
    public float OuterRadius { get; init; } = 1000;
    public float PanDistance { get; init; } = 400;
    public float Rolloff { get; init; } = 1;

    internal void Validate()
    {
        AudioMath.NonNegative(InnerRadius, nameof(InnerRadius));
        AudioMath.NonNegative(OuterRadius, nameof(OuterRadius));
        AudioMath.NonNegative(PanDistance, nameof(PanDistance));
        AudioMath.NonNegative(Rolloff, nameof(Rolloff));
        if (OuterRadius <= InnerRadius || PanDistance == 0 || Rolloff == 0)
        {
            throw new ArgumentException("Spatial audio requires outer > inner, pan distance > 0 and rolloff > 0.");
        }
    }
}

public readonly record struct SpatialAudioMix(float Gain, float Pan);

public static class SpatialAudio2D
{
    public static SpatialAudioMix Calculate(AudioListener2D listener, Vector2 emitter, SpatialAudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        Finite(emitter, nameof(emitter));
        var offset = emitter - listener.Position;
        var distance = offset.Length();
        if (!float.IsFinite(distance) || distance >= settings.OuterRadius)
        {
            return new(0, 0);
        }
        var normalized = Math.Clamp((distance - settings.InnerRadius) / (settings.OuterRadius - settings.InnerRadius), 0, 1);
        return new(MathF.Pow(1 - normalized, settings.Rolloff),
            Math.Clamp(Vector2.Dot(offset, listener.Right) / settings.PanDistance, -1, 1));
    }

    internal static Vector2 Finite(Vector2 value, string name)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentOutOfRangeException(name);
        }
        return value;
    }
}
