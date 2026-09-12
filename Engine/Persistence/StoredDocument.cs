using System.Security.Cryptography;
using System.Text.Json;

namespace Graphite.Engine.Persistence;

internal sealed class StoredDocument
{
    public int FormatVersion { get; init; } = 1;
    public string ContractId { get; init; } = "";
    public int SchemaVersion { get; init; }
    public Guid SlotId { get; init; }
    public string Name { get; init; } = "";
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
    public JsonElement Data { get; init; }
    public string Checksum { get; init; } = "";

    public static string Hash(JsonElement data) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data)));

    public static SaveResult<StoredDocument> Read(AtomicFileStorage storage, string name)
    {
        try
        {
            var bytes = storage.Read(name);
            if (bytes is null)
            {
                return new(SaveStatus.NotFound);
            }

            var document = JsonSerializer.Deserialize<StoredDocument>(bytes);
            if (document is null)
            {
                return new(SaveStatus.Corrupt, Error: "Missing document.");
            }

            if (document.FormatVersion != 1)
            {
                return new(SaveStatus.Incompatible, Error: "Unsupported file format version.");
            }

            if (document.SchemaVersion < 1 || string.IsNullOrWhiteSpace(document.ContractId) || document.Name is null
                || document.CreatedUtc == default || document.UpdatedUtc < document.CreatedUtc
                || document.Data.ValueKind != JsonValueKind.Object || document.Checksum != Hash(document.Data))
            {
                return new(SaveStatus.Corrupt, Error: "Invalid save metadata or checksum.");
            }

            return new(SaveStatus.Success, document);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or EndOfStreamException)
        {
            return new(SaveStatus.Corrupt, Error: exception.Message);
        }
    }
}
