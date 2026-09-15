using System.Text.Json;
using System.Text.Json.Nodes;

namespace Graphite.Engine.Persistence;

/// <summary>Whole-object saves. Call synchronously on the game thread.</summary>
public static class Storage
{
    private static AtomicFileStorage? _storage;
    private static SaveSerializer _serializer = new();
    private static readonly Dictionary<(string Contract, int Version), SaveMigration> Migrations = [];
    private static AtomicFileStorage Files => _storage ?? throw new InvalidOperationException("Storage is not initialized. Start the game host first.");
    public static string DirectoryPath => Files.DirectoryPath;

    internal static void Initialize(string directory, SaveSerializer? serializer = null, params SaveMigration[] migrations)
    {
        Shutdown();
        _storage = new AtomicFileStorage(directory);
        _serializer = serializer ?? new SaveSerializer();
        foreach (var migration in migrations)
        {
            RegisterMigration(migration);
        }
    }

    internal static void Shutdown()
    {
        _storage = null;
        _serializer = new SaveSerializer();
        Migrations.Clear();
    }

    public static void RegisterMigration(SaveMigration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        if (string.IsNullOrWhiteSpace(migration.ContractId) || migration.FromVersion < 1 || migration.Upgrade is null
            || !Migrations.TryAdd((migration.ContractId, migration.FromVersion), migration))
        {
            throw new ArgumentException("Migrations need a unique contract/version, a positive version and an upgrade function.", nameof(migration));
        }
    }

    /// <summary>Serializes a single data object. Omitting the slot ID uses the default slot.</summary>
    public static SaveSlotInfo Save<T>(T value, Guid slotId = default, string? name = null)
    {
        _ = Files;
        var contract = _serializer.Contract<T>();
        using var json = JsonDocument.Parse(_serializer.Serialize(value));
        var data = json.RootElement.Clone();
        var filename = Filename(slotId);
        var current = StoredDocument.Read(Files, filename);
        if (current.IsSuccess)
        {
            var validation = ReadValue<T>(filename, slotId, contract);
            if (!validation.IsSuccess)
            {
                current = new(validation.Status, Error: validation.Error);
            }
        }

        var previous = current;
        if (current.Status is SaveStatus.Corrupt or SaveStatus.NotFound)
        {
            previous = StoredDocument.Read(Files, filename + ".bak");
        }

        if (current.Status == SaveStatus.Incompatible || previous.Status == SaveStatus.Incompatible
            || previous.Value is { } old && (old.ContractId != contract.Id || old.SchemaVersion > contract.Version || old.SlotId != slotId))
        {
            throw new InvalidDataException("The existing slot belongs to another contract or a newer version.");
        }

        var now = DateTimeOffset.UtcNow;
        var created = previous.Value?.CreatedUtc ?? now;
        var document = new StoredDocument
        {
            ContractId = contract.Id,
            SchemaVersion = contract.Version,
            SlotId = slotId,
            Name = name ?? previous.Value?.Name ?? "",
            CreatedUtc = created,
            UpdatedUtc = now < created ? created : now,
            Data = data,
            Checksum = StoredDocument.Hash(data)
        };
        Files.Write(filename, JsonSerializer.SerializeToUtf8Bytes(document), current.IsSuccess);
        return Info(document, SaveStatus.Success);
    }

    /// <summary>Returns the deserialized object; missing, invalid, and unreadable files throw.</summary>
    public static T Load<T>(Guid slotId = default)
    {
        var result = TryLoad<T>(slotId);
        return result.Status switch
        {
            SaveStatus.Success => result.Value!,
            SaveStatus.NotFound => throw new FileNotFoundException("The save slot does not exist.", Files.PathFor(Filename(slotId))),
            SaveStatus.IoError => throw new IOException(result.Error),
            _ => throw new InvalidDataException(result.Error)
        };
    }

    /// <summary>Use when a caller needs missing/corrupt/incompatible status or backup-recovery information.</summary>
    public static SaveResult<T> TryLoad<T>(Guid slotId = default)
    {
        _ = Files;
        var contract = _serializer.Contract<T>();
        var filename = Filename(slotId);
        try
        {
            return LoadResult<T>(filename, slotId, contract);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, Error: exception.Message);
        }
    }

    private static SaveResult<T> LoadResult<T>(string filename, Guid slotId, SaveContractAttribute contract)
    {
        var result = ReadValue<T>(filename, slotId, contract);
        if (result.Status is not (SaveStatus.Corrupt or SaveStatus.NotFound))
        {
            return result;
        }

        var backup = ReadValue<T>(filename + ".bak", slotId, contract);
        return backup.IsSuccess ? backup with
        {
            RecoveredFromBackup = true
        }
            : backup.Status == SaveStatus.NotFound ? result : backup;
    }

    private static SaveResult<T> ReadValue<T>(string filename, Guid slotId, SaveContractAttribute contract)
    {
        var result = StoredDocument.Read(Files, filename);
        if (!result.IsSuccess)
        {
            return new(result.Status, Error: result.Error);
        }

        var document = result.Value!;
        if (document.SlotId != slotId || document.ContractId != contract.Id || document.SchemaVersion > contract.Version)
        {
            return new(SaveStatus.Incompatible, Error: "Slot ID, contract or schema version does not match.");
        }

        try
        {
            var data = JsonNode.Parse(document.Data.GetRawText())!.AsObject();
            for (var version = document.SchemaVersion; version < contract.Version; version++)
            {
                if (!Migrations.TryGetValue((contract.Id, version), out var migration))
                {
                    return new(SaveStatus.Incompatible, Error: $"No migration from schema {version}.");
                }

                data = migration.Upgrade(data) ?? throw new InvalidDataException("Migration returned null.");
            }

            return new(SaveStatus.Success, _serializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(data)));
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or InvalidOperationException or OverflowException)
        {
            return new(SaveStatus.Corrupt, Error: exception.Message);
        }
    }

    /// <summary>Listing validates each slot, including migrations and game validation. Directory I/O failures throw.</summary>
    public static IReadOnlyList<SaveSlotInfo> ListSlots<T>()
    {
        var contract = _serializer.Contract<T>();
        if (!Directory.Exists(DirectoryPath))
        {
            return [];
        }

        var slots = new List<SaveSlotInfo>();
        var ids = Directory.EnumerateFiles(DirectoryPath, "*.json*")
            .Where(path => path.EndsWith(".json", StringComparison.Ordinal) || path.EndsWith(".json.bak", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path).Split('.')[0])
            .Where(value => value == "default" || Guid.TryParseExact(value, "N", out _))
            .Select(value => value == "default" ? Guid.Empty : Guid.Parse(value)).Distinct().ToArray();
        foreach (var id in ids)
        {
            var result = LoadResult<T>(Filename(id), id, contract);
            if (result.Status == SaveStatus.NotFound)
            {
                continue;
            }

            var document = StoredDocument.Read(Files, Filename(id) + (result.RecoveredFromBackup ? ".bak" : ""));
            slots.Add(document.Value is { } value ? Info(value, result.Status, result.RecoveredFromBackup) with
            {
                Id = id
            }
                : new SaveSlotInfo(id, "", null, null, 0, result.Status));
        }

        return slots.OrderByDescending(slot => slot.UpdatedUtc).ToArray();
    }

    public static void Delete(Guid slotId = default)
    {
        var filename = Filename(slotId);
        // Remove backup first so an interrupted delete cannot resurrect a deleted primary.
        Files.Delete(filename + ".bak");
        Files.Delete(filename);
    }

    private static SaveSlotInfo Info(StoredDocument document, SaveStatus status, bool recovered = false)
        => new(document.SlotId, document.Name, document.CreatedUtc, document.UpdatedUtc, document.SchemaVersion, status, recovered);

    private static string Filename(Guid id) => id == Guid.Empty ? "default.json" : id.ToString("N") + ".json";
}
