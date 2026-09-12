using System.Text.Json;

namespace Graphite.Engine.Persistence;

/// <summary>Thread-safe preference values. Set/Remove change memory; FlushAsync persists only local changes.</summary>
public sealed class Preferences
{
    private const string Filename = "preferences.json";
    private const string ContractId = "graphite.preferences";
    private readonly AtomicFileStorage _storage;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _dirty = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _operations = new(1);
    private long _revision;

    public Preferences(string directory) : this(new AtomicFileStorage(directory)) { }
    public Preferences(AtomicFileStorage storage) => _storage = storage;
    public string FilePath => _storage.PathFor(Filename);

    public T Get<T>(PreferenceKey<T> key)
    {
        lock (_gate)
        {
            Register(key);
            if (_values.TryGetValue(key.Name, out var entry) && entry.Type == key.TypeTag)
            {
                try
                {
                    var value = entry.Value.Deserialize<T>();
                    if (value is not null && key.IsValid(value))
                    {
                        return value;
                    }
                }
                catch (JsonException) { }
            }

            return key.DefaultValue;
        }
    }

    public void Set<T>(PreferenceKey<T> key, T value)
    {
        if (!key.IsValid(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Invalid value for {key.Name}.");
        }

        lock (_gate)
        {
            Register(key);
            _values[key.Name] = new Entry(key.TypeTag, JsonSerializer.SerializeToElement(value));
            _dirty[key.Name] = ++_revision;
        }
    }

    public void Remove<T>(PreferenceKey<T> key)
    {
        lock (_gate)
        {
            Register(key);
            _values.Remove(key.Name);
            _dirty[key.Name] = ++_revision;
        }
    }

    private void Register<T>(PreferenceKey<T> key)
    {
        if (_types.TryGetValue(key.Name, out var type) && type != key.TypeTag)
        {
            throw new InvalidOperationException($"Preference {key.Name} is already registered as {type}.");
        }

        _types[key.Name] = key.TypeTag;
    }

    public async Task<SaveResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var lease = await _storage.LockAsync(Filename, cancellationToken).ConfigureAwait(false);
            var read = await ReadAsync(cancellationToken).ConfigureAwait(false);
            if (read.Status is SaveStatus.Success or SaveStatus.NotFound)
            {
                lock (_gate)
                {
                    foreach (var key in _values.Keys.Where(key => !_dirty.ContainsKey(key)).ToArray())
                    {
                        _values.Remove(key);
                    }

                    foreach (var (key, entry) in read.Values)
                    {
                        if (!_dirty.ContainsKey(key))
                        {
                            _values[key] = entry;
                        }
                    }
                }
            }

            return new(read.Status, read.Error, read.Recovered);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, exception.Message);
        }
        finally { _operations.Release(); }
    }

    public async Task<SaveResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Dictionary<string, (long Revision, Entry? Value)> changes;
            lock (_gate)
            {
                changes = _dirty.ToDictionary(pair => pair.Key, pair => (pair.Value, _values.GetValueOrDefault(pair.Key)), StringComparer.Ordinal);
            }

            if (changes.Count == 0)
            {
                return new(SaveStatus.Success);
            }

            using var lease = await _storage.LockAsync(Filename, cancellationToken).ConfigureAwait(false);
            var read = await ReadAsync(cancellationToken).ConfigureAwait(false);
            if (read.Status is not (SaveStatus.Success or SaveStatus.NotFound))
            {
                return new(read.Status, read.Error);
            }

            foreach (var (key, change) in changes)
            {
                if (change.Value is { } value)
                {
                    read.Values[key] = value;
                }
                else
                {
                    read.Values.Remove(key);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var created = read.Document?.CreatedUtc ?? now;
            var data = JsonSerializer.SerializeToElement(read.Values);
            var document = new StoredDocument
            {
                ContractId = ContractId,
                SchemaVersion = 1,
                Name = "preferences",
                CreatedUtc = created,
                UpdatedUtc = now < created ? created : now,
                Data = data,
                Checksum = StoredDocument.Hash(data)
            };
            await _storage.WriteAsync(Filename, JsonSerializer.SerializeToUtf8Bytes(document), read.Status == SaveStatus.Success && !read.Recovered, cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                foreach (var (key, change) in changes)
                {
                    if (_dirty.GetValueOrDefault(key) == change.Revision)
                    {
                        _dirty.Remove(key);
                    }
                }

                foreach (var key in _values.Keys.Where(key => !_dirty.ContainsKey(key)).ToArray())
                {
                    _values.Remove(key);
                }

                foreach (var (key, entry) in read.Values)
                {
                    if (!_dirty.ContainsKey(key))
                    {
                        _values[key] = entry;
                    }
                }
            }

            return new(SaveStatus.Success, RecoveredFromBackup: read.Recovered);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, exception.Message);
        }
        finally { _operations.Release(); }
    }

    private async Task<ReadResult> ReadAsync(CancellationToken cancellationToken)
    {
        var primary = await ReadFileAsync(Filename, cancellationToken).ConfigureAwait(false);
        if (primary.Status is not (SaveStatus.Corrupt or SaveStatus.NotFound))
        {
            return primary;
        }

        var backup = await ReadFileAsync(Filename + ".bak", cancellationToken).ConfigureAwait(false);
        return backup.Status == SaveStatus.Success ? backup with { Recovered = true }
            : backup.Status == SaveStatus.NotFound ? primary : backup;
    }

    private async Task<ReadResult> ReadFileAsync(string filename, CancellationToken cancellationToken)
    {
        var result = await StoredDocument.ReadAsync(_storage, filename, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return new(result.Status) { Error = result.Error };
        }

        var document = result.Value!;
        if (document.ContractId != ContractId || document.SchemaVersion != 1 || document.SlotId != Guid.Empty)
        {
            return new(SaveStatus.Incompatible) { Error = "Unsupported preferences schema." };
        }

        try
        {
            var values = document.Data.Deserialize<Dictionary<string, Entry>>() ?? [];
            if (values.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null || string.IsNullOrWhiteSpace(pair.Value.Type)
                || pair.Value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null))
            {
                return new(SaveStatus.Corrupt) { Error = "Invalid preference entry." };
            }

            return new(SaveStatus.Success) { Document = document, Values = values };
        }
        catch (JsonException exception)
        {
            return new(SaveStatus.Corrupt) { Error = exception.Message };
        }
    }

    private sealed record Entry(string Type, JsonElement Value);
    private sealed record ReadResult(SaveStatus Status)
    {
        public Dictionary<string, Entry> Values { get; init; } = [];
        public StoredDocument? Document { get; init; }
        public string? Error { get; init; }
        public bool Recovered { get; init; }
    }
}
