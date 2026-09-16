namespace Graphite.Engine.Audio;

/// <summary>A mixer channel. User volume and temporary environmental gain are independent.</summary>
public sealed class AudioBus
{
    private readonly Action _changed;
    private float _volume = 1;

    internal AudioBus(string name, AudioBus? parent, bool pauseWithGame, Action changed)
    {
        Name = name;
        Parent = parent;
        PauseWithGame = pauseWithGame;
        _changed = changed;
    }

    public string Name
    {
        get;
    }
    public AudioBus? Parent
    {
        get;
    }
    public bool PauseWithGame
    {
        get;
    }
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = AudioMath.Unit(value, nameof(Volume));
            _changed();
        }
    }

    public float EffectiveVolume => Volume * SnapshotGain * (Parent?.EffectiveVolume ?? 1);
    internal float SnapshotGain { get; set; } = 1;
}

internal static class AudioMath
{
    internal static float Unit(float value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1, name);
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name);
        }
        return value;
    }

    internal static float NonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
        return value;
    }
}
