using System.Text.Json;

namespace Graphite.Engine.Persistence;

/// <summary>Typed preferences loaded before the first scene. Set/Remove persist immediately on the game thread.</summary>
public static class Preferences
{
    private const string Filename = "preferences.json";
    private const string ContractId = "graphite.preferences";
    private static AtomicFileStorage? _storage;
    private static Dictionary<string, Entry> _values = [];
    private static AtomicFileStorage Files => _storage ?? throw new InvalidOperationException("Preferences are not initialized. Start the game host first.");
    public static string FilePath => Files.PathFor(Filename);

    internal static SaveResult Initialize(string directory)
    {
        Shutdown();
        _storage = new AtomicFileStorage(directory);
        try
        {
            var read = Read();
            _values = read.Values;
            return new(read.Status, read.Error, read.Recovered);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, exception.Message);
        }
    }

    internal static void Shutdown()
    {
        _storage = null;
        _values = [];
    }

    public static T Get<T>(string key) => Get(new PreferenceKey<T>(key, Default<T>()));
    public static T Get<T>(string key, T defaultValue) => Get(new PreferenceKey<T>(key, defaultValue));
    public static void Set<T>(string key, T value) => Set(new PreferenceKey<T>(key, Default<T>()), value);
    public static bool Remove<T>(string key) => Remove(new PreferenceKey<T>(key, Default<T>()));

    public static T Get<T>(PreferenceKey<T> key)
    {
        var entry = Find(key);
        if (entry is not null)
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

    public static void Set<T>(PreferenceKey<T> key, T value)
    {
        var current = Find(key);
        if (!key.IsValid(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Invalid value for {key.Name}.");
        }

        var entry = new Entry(key.TypeTag, JsonSerializer.SerializeToElement(value));
        if (current?.Value.GetRawText() == entry.Value.GetRawText())
        {
            return;
        }

        var values = new Dictionary<string, Entry>(_values) { [key.Name] = entry };
        Persist(values);
    }

    public static bool Remove<T>(PreferenceKey<T> key)
    {
        if (Find(key) is null)
        {
            return false;
        }

        var values = new Dictionary<string, Entry>(_values);
        values.Remove(key.Name);
        Persist(values);
        return true;
    }

    private static Entry? Find<T>(PreferenceKey<T> key)
    {
        _ = Files;
        ArgumentNullException.ThrowIfNull(key);
        _values.TryGetValue(key.Name, out var entry);
        if (entry is not null && entry.Type != key.TypeTag)
        {
            throw new InvalidOperationException($"Preference '{key.Name}' is stored as {entry.Type}, not {key.TypeTag}.");
        }

        return entry;
    }

    private static T Default<T>() => (T)(Type.GetTypeCode(typeof(T)) switch
    {
        TypeCode.Boolean => (object)false,
        TypeCode.Int32 => 0,
        TypeCode.Int64 => 0L,
        TypeCode.Single => 0f,
        TypeCode.Double => 0d,
        TypeCode.String => string.Empty,
        _ => throw new NotSupportedException("Preferences support bool, int, long, float, double and string values.")
    });

    private static void Persist(Dictionary<string, Entry> values)
    {
        var previous = Read();
        if (previous.Status == SaveStatus.Incompatible)
        {
            throw new InvalidDataException(previous.Error);
        }

        var now = DateTimeOffset.UtcNow;
        var created = previous.Document?.CreatedUtc ?? now;
        var data = JsonSerializer.SerializeToElement(values);
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
        Files.Write(Filename, JsonSerializer.SerializeToUtf8Bytes(document), previous.Status == SaveStatus.Success && !previous.Recovered);
        _values = values;
    }

    private static ReadResult Read()
    {
        var primary = ReadFile(Filename);
        if (primary.Status is not (SaveStatus.Corrupt or SaveStatus.NotFound))
        {
            return primary;
        }

        var backup = ReadFile(Filename + ".bak");
        return backup.Status == SaveStatus.Success ? backup with { Recovered = true }
            : backup.Status == SaveStatus.NotFound ? primary : backup;
    }

    private static ReadResult ReadFile(string filename)
    {
        var result = StoredDocument.Read(Files, filename);
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
            if (values.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null || !ValidEntry(pair.Value)))
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

    private static bool ValidEntry(Entry entry) => entry.Type switch
    {
        "bool" => entry.Value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "string" => entry.Value.ValueKind == JsonValueKind.String,
        "int" => entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetInt32(out _),
        "long" => entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetInt64(out _),
        "float" => entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetSingle(out var value) && float.IsFinite(value),
        "double" => entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetDouble(out var value) && double.IsFinite(value),
        _ => false
    };

    private sealed record Entry(string Type, JsonElement Value);
    private sealed record ReadResult(SaveStatus Status)
    {
        public Dictionary<string, Entry> Values { get; init; } = [];
        public StoredDocument? Document { get; init; }
        public string? Error { get; init; }
        public bool Recovered { get; init; }
    }
}
