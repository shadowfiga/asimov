using System.Text.Json;
using System.Text.Json.Nodes;

namespace Graphite.Engine.Persistence;

public sealed class SaveStore
{
    private readonly AtomicFileStorage _storage;
    private readonly SaveSerializer _serializer;
    private readonly Dictionary<(string Contract, int Version), SaveMigration> _migrations = [];
    public string DirectoryPath => _storage.DirectoryPath;

    public SaveStore(string directory) : this(new AtomicFileStorage(directory), new SaveSerializer()) { }

    public SaveStore(AtomicFileStorage storage, SaveSerializer serializer, params SaveMigration[] migrations)
    {
        _storage = storage;
        _serializer = serializer;
        foreach (var migration in migrations)
        {
            if (string.IsNullOrWhiteSpace(migration.ContractId) || migration.FromVersion < 1 || migration.Upgrade is null
                || !_migrations.TryAdd((migration.ContractId, migration.FromVersion), migration))
            {
                throw new ArgumentException("Migrations need a unique contract/version, a positive version and an upgrade function.", nameof(migrations));
            }
        }
    }

    /// <summary>Captures JSON synchronously before returning. Call while the game state is stable.</summary>
    public Task<SaveResult<SaveSlotInfo>> SaveAsync<T>(Guid slotId, T value, string? name = null, CancellationToken cancellationToken = default)
    {
        ValidateSlotId(slotId);
        cancellationToken.ThrowIfCancellationRequested();
        var contract = _serializer.Contract<T>();
        var bytes = _serializer.Serialize(value);
        using var json = JsonDocument.Parse(bytes);
        return WriteAsync<T>(slotId, contract, json.RootElement.Clone(), name, cancellationToken);
    }

    private async Task<SaveResult<SaveSlotInfo>> WriteAsync<T>(Guid slotId, SaveContractAttribute contract, JsonElement data, string? name, CancellationToken cancellationToken)
    {
        var filename = Filename(slotId);
        try
        {
            using var lease = await _storage.LockAsync(filename, cancellationToken).ConfigureAwait(false);
            var current = await StoredDocument.ReadAsync(_storage, filename, cancellationToken).ConfigureAwait(false);
            if (current.IsSuccess)
            {
                var validation = await ReadValueAsync<T>(filename, slotId, contract, cancellationToken).ConfigureAwait(false);
                if (!validation.IsSuccess)
                {
                    current = new(validation.Status, Error: validation.Error);
                }
            }

            var previous = current;
            if (current.Status is SaveStatus.Corrupt or SaveStatus.NotFound)
            {
                previous = await StoredDocument.ReadAsync(_storage, filename + ".bak", cancellationToken).ConfigureAwait(false);
            }

            if (current.Status == SaveStatus.Incompatible || previous.Status == SaveStatus.Incompatible
                || previous.Value is { } old && (old.ContractId != contract.Id || old.SchemaVersion > contract.Version || old.SlotId != slotId))
            {
                return new(SaveStatus.Incompatible, Error: "The existing slot belongs to another contract or a newer version.");
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
            await _storage.WriteAsync(filename, JsonSerializer.SerializeToUtf8Bytes(document), current.IsSuccess, cancellationToken).ConfigureAwait(false);
            return new(SaveStatus.Success, Info(document, SaveStatus.Success));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, Error: exception.Message);
        }
    }

    public async Task<SaveResult<T>> LoadAsync<T>(Guid slotId, CancellationToken cancellationToken = default)
    {
        ValidateSlotId(slotId);
        var contract = _serializer.Contract<T>();
        var filename = Filename(slotId);
        try
        {
            using var lease = await _storage.LockAsync(filename, cancellationToken).ConfigureAwait(false);
            return await LoadLockedAsync<T>(filename, slotId, contract, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, Error: exception.Message);
        }
    }

    private async Task<SaveResult<T>> LoadLockedAsync<T>(string filename, Guid slotId, SaveContractAttribute contract, CancellationToken cancellationToken)
    {
        var result = await ReadValueAsync<T>(filename, slotId, contract, cancellationToken).ConfigureAwait(false);
        if (result.Status is not (SaveStatus.Corrupt or SaveStatus.NotFound))
        {
            return result;
        }

        var backup = await ReadValueAsync<T>(filename + ".bak", slotId, contract, cancellationToken).ConfigureAwait(false);
        return backup.IsSuccess ? backup with { RecoveredFromBackup = true }
            : backup.Status == SaveStatus.NotFound ? result : backup;
    }

    private async Task<SaveResult<T>> ReadValueAsync<T>(string filename, Guid slotId, SaveContractAttribute contract, CancellationToken cancellationToken)
    {
        var result = await StoredDocument.ReadAsync(_storage, filename, cancellationToken).ConfigureAwait(false);
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
                if (!_migrations.TryGetValue((contract.Id, version), out var migration))
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
    public async Task<IReadOnlyList<SaveSlotInfo>> ListSlotsAsync<T>(CancellationToken cancellationToken = default)
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
            .Where(value => Guid.TryParseExact(value, "N", out _)).Select(Guid.Parse).Distinct().ToArray();
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var lease = await _storage.LockAsync(Filename(id), cancellationToken).ConfigureAwait(false);
            var result = await LoadLockedAsync<T>(Filename(id), id, contract, cancellationToken).ConfigureAwait(false);
            if (result.Status == SaveStatus.NotFound)
            {
                continue;
            }

            var document = await StoredDocument.ReadAsync(_storage, Filename(id) + (result.RecoveredFromBackup ? ".bak" : ""), cancellationToken).ConfigureAwait(false);
            slots.Add(document.Value is { } value ? Info(value, result.Status, result.RecoveredFromBackup) with { Id = id }
                : new SaveSlotInfo(id, "", null, null, 0, result.Status));
        }

        return slots.OrderByDescending(slot => slot.UpdatedUtc).ToArray();
    }

    public async Task<SaveResult> DeleteAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        ValidateSlotId(slotId);
        var filename = Filename(slotId);
        try
        {
            using var lease = await _storage.LockAsync(filename, cancellationToken).ConfigureAwait(false);
            // Remove backup first so an interrupted delete cannot resurrect a deleted primary.
            _storage.Delete(filename + ".bak");
            _storage.Delete(filename);
            return new(SaveStatus.Success);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(SaveStatus.IoError, exception.Message);
        }
    }

    private static SaveSlotInfo Info(StoredDocument document, SaveStatus status, bool recovered = false)
        => new(document.SlotId, document.Name, document.CreatedUtc, document.UpdatedUtc, document.SchemaVersion, status, recovered);

    private static string Filename(Guid id) => id.ToString("N") + ".json";
    private static void ValidateSlotId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Save slots require a nonempty ID.", nameof(id));
        }
    }
}
