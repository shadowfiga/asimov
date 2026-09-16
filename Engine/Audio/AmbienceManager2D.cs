using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Audio;

public sealed class AudioSnapshot
{
    public static AudioSnapshot Default { get; } = new(new Dictionary<AudioBus, float>());
    public IReadOnlyDictionary<AudioBus, float> Gains
    {
        get;
    }

    public AudioSnapshot(IReadOnlyDictionary<AudioBus, float> gains)
    {
        ArgumentNullException.ThrowIfNull(gains);
        var copy = new Dictionary<AudioBus, float>();
        foreach (var (bus, gain) in gains)
        {
            copy.Add(bus, AudioMath.Unit(gain, nameof(gains)));
        }
        Gains = new ReadOnlyDictionary<AudioBus, float>(copy);
    }
}

/// <summary>Axis-aligned world-space area. Higher priority wins overlaps; first registered wins ties.</summary>
public sealed record AmbienceZone2D
{
    public required Vector2 Center
    {
        get; init;
    }
    public required Vector2 HalfSize
    {
        get; init;
    }
    public required AudioSnapshot Snapshot
    {
        get; init;
    }
    public int Priority
    {
        get; init;
    }
    public float TransitionSeconds { get; init; } = .5f;
    public bool Enabled { get; set; } = true;

    internal bool Contains(Vector2 point) => Enabled &&
        MathF.Abs(point.X - Center.X) <= HalfSize.X && MathF.Abs(point.Y - Center.Y) <= HalfSize.Y;
}

/// <summary>2D counterpart to listener-triggered ambience snapshots. No physics/UI dependency.</summary>
public sealed class AmbienceManager2D
{
    private readonly AudioManager _audio;
    private readonly List<AmbienceZone2D> _zones = [];
    public AmbienceZone2D? ActiveZone
    {
        get; private set;
    }
    internal AmbienceManager2D(AudioManager audio) => _audio = audio;

    public IDisposable Add(AmbienceZone2D zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(zone.Snapshot);
        SpatialAudio2D.Finite(zone.Center, nameof(zone.Center));
        SpatialAudio2D.Finite(zone.HalfSize, nameof(zone.HalfSize));
        if (zone.HalfSize.X <= 0 || zone.HalfSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zone.HalfSize));
        }
        AudioMath.NonNegative(zone.TransitionSeconds, nameof(zone.TransitionSeconds));
        foreach (var bus in zone.Snapshot.Gains.Keys)
        {
            _audio.ValidateBus(bus);
        }
        if (_zones.Any(existing => ReferenceEquals(existing, zone)))
        {
            throw new ArgumentException("Zone is already registered.", nameof(zone));
        }
        _zones.Add(zone);
        return new Registration(this, zone);
    }

    internal void Update()
    {
        AmbienceZone2D? next = null;
        foreach (var zone in _zones)
        {
            if (zone.Contains(_audio.Listener.Position) && (next is null || zone.Priority > next.Priority))
            {
                next = zone;
            }
        }
        if (ReferenceEquals(next, ActiveZone))
        {
            return;
        }
        var duration = next?.TransitionSeconds ?? ActiveZone?.TransitionSeconds ?? .5f;
        ActiveZone = next;
        _audio.TransitionTo(next?.Snapshot ?? AudioSnapshot.Default, duration);
    }

    internal void Clear()
    {
        _zones.Clear();
        ActiveZone = null;
    }

    private sealed class Registration(AmbienceManager2D owner, AmbienceZone2D zone) : IDisposable
    {
        public void Dispose() => owner._zones.RemoveAll(existing => ReferenceEquals(existing, zone));
    }
}
