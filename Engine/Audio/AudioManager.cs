using Microsoft.Xna.Framework;

namespace Graphite.Engine.Audio;

/// <summary>Game-thread audio service. Owns buses, cached assets, voices, ambience and music.</summary>
public sealed class AudioManager : IDisposable
{
    private static AudioManager? _current;
    public static AudioManager Current => _current ??= new AudioManager();
    private readonly IAudioBackend _backend;
    private readonly Dictionary<string, AudioClip> _clips = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly List<AudioVoice> _voices = [];
    private readonly Dictionary<string, AudioBus> _buses = new(StringComparer.Ordinal);
    private readonly Dictionary<AudioBus, (float From, float To)> _snapshot = [];
    private float _snapshotElapsed;
    private float _snapshotDuration;
    private bool _disposed;
    private long _preferencesRevision = -1;

    public AudioManager(int maxVoices = 64) : this(new MonoGameAudioBackend(), maxVoices)
    {
    }

    internal AudioManager(IAudioBackend backend, int maxVoices = 64)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxVoices, 1);
        _backend = backend;
        MaxVoices = maxVoices;
        Master = new AudioBus("master", null, true, RefreshMix);
        _buses.Add(Master.Name, Master);
        Music = CreateBus("music", Master);
        Fx = CreateBus("fx", Master);
        Ambience = CreateBus("ambience", Master);
        Ui = CreateBus("ui", Fx, pauseWithGame: false);
        MusicPlayer = new AdaptiveMusicPlayer(Music);
        AmbienceZones = new AmbienceManager2D(this);
    }

    public AudioBus Master
    {
        get;
    }
    public AudioBus Music
    {
        get;
    }
    public AudioBus Fx
    {
        get;
    }
    public AudioBus Ambience
    {
        get;
    }
    public AudioBus Ui
    {
        get;
    }
    public AudioListener2D Listener { get; } = new();
    public AdaptiveMusicPlayer MusicPlayer
    {
        get;
    }
    public AmbienceManager2D AmbienceZones
    {
        get;
    }
    public int MaxVoices
    {
        get;
    }
    public int ActiveVoiceCount => _voices.Count(voice => voice.IsPlaying);
    public int CachedClipCount => _clips.Count;
    public bool IsPaused
    {
        get; private set;
    }

    public AudioBus CreateBus(string name, AudioBus? parent = null, bool pauseWithGame = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        parent ??= Master;
        ValidateBus(parent);
        var bus = new AudioBus(name, parent, pauseWithGame, RefreshMix);
        if (!_buses.TryAdd(name, bus))
        {
            throw new ArgumentException($"Audio bus '{name}' already exists.", nameof(name));
        }
        return bus;
    }

    public AudioClip LoadClip(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);
        if (!_clips.TryGetValue(fullPath, out var clip))
        {
            using var stream = File.OpenRead(fullPath);
            clip = AudioClip.ReadWave(stream);
            _clips.Add(fullPath, clip);
        }
        return clip;
    }

    /// <summary>Decode and upload an effect during loading. Spatial effects are downmixed to mono.</summary>
    public AudioClip PreloadEffect(string path, bool spatial = false)
    {
        var clip = LoadClip(path);
        _backend.Prepare(clip, spatial);
        return clip;
    }

    public AudioVoice Play(string path, AudioPlayOptions? options = null) => Play(LoadClip(path), options);
    public AudioVoice Play(AudioClip clip, AudioPlayOptions? options = null) => PlayInternal(clip, options ?? new(), Vector2.Zero, null);
    public AudioVoice Play2D(string path, Vector2 position, SpatialAudioSettings? spatial = null, AudioPlayOptions? options = null)
        => Play2D(LoadClip(path), position, spatial, options);
    public AudioVoice Play2D(AudioClip clip, Vector2 position, SpatialAudioSettings? spatial = null, AudioPlayOptions? options = null)
        => PlayInternal(clip, options ?? new(), position, spatial ?? new());

    private AudioVoice PlayInternal(AudioClip clip, AudioPlayOptions options, Vector2 position, SpatialAudioSettings? spatial)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(clip);
        AudioMath.Unit(options.Volume, nameof(options.Volume));
        AudioMath.Unit((options.Pan + 1) / 2, nameof(options.Pan));
        AudioMath.Unit((options.Pitch + 1) / 2, nameof(options.Pitch));
        SpatialAudio2D.Finite(position, nameof(position));
        spatial?.Validate();
        var bus = options.Bus ?? Fx;
        ValidateBus(bus);
        RemoveFinishedVoices();
        if (_voices.Count >= MaxVoices)
        {
            var oldestLowest = _voices.MinBy(voice => voice.Priority)!;
            if (oldestLowest.Priority > options.Priority)
            {
                return new AudioVoice(this, null, bus, options, position, spatial);
            }
            oldestLowest.Dispose();
            _voices.Remove(oldestLowest);
        }
        var instance = _backend.Create(clip, spatial is not null);
        var result = new AudioVoice(this, instance, bus, options, position, spatial);
        try
        {
            instance.Loop = options.Loop;
            instance.Pitch = options.Pitch;
            result.Refresh();
            instance.Play();
            result.Refresh();
            _voices.Add(result);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsPaused = true;
        MusicPlayer.SetGamePaused(true);
        RefreshMix();
    }

    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsPaused = false;
        MusicPlayer.SetGamePaused(false);
        RefreshMix();
    }

    /// <summary>Replace temporary gains; omitted buses return to unity. Saved volume is never changed.</summary>
    public void TransitionTo(AudioSnapshot snapshot, float seconds = .5f)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        AudioMath.NonNegative(seconds, nameof(seconds));
        foreach (var bus in snapshot.Gains.Keys)
        {
            ValidateBus(bus);
        }
        _snapshot.Clear();
        foreach (var bus in _buses.Values)
        {
            _snapshot.Add(bus, (bus.SnapshotGain, snapshot.Gains.GetValueOrDefault(bus, 1)));
        }
        _snapshotElapsed = 0;
        _snapshotDuration = seconds;
        AdvanceSnapshot(0);
        RefreshMix();
    }

    public void Update(float deltaSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        AudioMath.NonNegative(deltaSeconds, nameof(deltaSeconds));
        SynchronizePreferences();
        if (!IsPaused)
        {
            AmbienceZones.Update();
            AdvanceSnapshot(deltaSeconds);
        }
        RemoveFinishedVoices();
        RefreshMix();
        MusicPlayer.Update();
    }

    public void SynchronizePreferences(bool force = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Persistence.Preferences.IsInitialized || (!force && _preferencesRevision == Persistence.Preferences.Revision))
        {
            return;
        }
        AudioPreferences.Apply(this);
        _preferencesRevision = Persistence.Preferences.Revision;
    }

    private void AdvanceSnapshot(float dt)
    {
        _snapshotElapsed += dt;
        var amount = _snapshotDuration <= 0 ? 1 : Math.Clamp(_snapshotElapsed / _snapshotDuration, 0, 1);
        foreach (var (bus, gain) in _snapshot)
        {
            bus.SnapshotGain = MathHelper.Lerp(gain.From, gain.To, amount);
        }
        if (amount == 1)
        {
            _snapshot.Clear();
        }
    }

    private void RefreshMix()
    {
        foreach (var voice in _voices)
        {
            voice.Refresh();
        }
        MusicPlayer?.RefreshVolume();
    }

    private void RemoveFinishedVoices()
    {
        for (var i = _voices.Count - 1; i >= 0; i--)
        {
            if (!_voices[i].IsPlaying)
            {
                _voices[i].Dispose();
                _voices.RemoveAt(i);
            }
        }
    }

    internal void ValidateBus(AudioBus bus)
    {
        if (!_buses.TryGetValue(bus.Name, out var registered) || !ReferenceEquals(bus, registered))
        {
            throw new ArgumentException("The bus belongs to another audio manager.", nameof(bus));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        foreach (var voice in _voices)
        {
            voice.Dispose();
        }
        _voices.Clear();
        MusicPlayer.Dispose();
        AmbienceZones.Clear();
        _backend.Dispose();
        _clips.Clear();
        _snapshot.Clear();
    }

    internal static void Shutdown()
    {
        _current?.Dispose();
        _current = null;
    }
}
