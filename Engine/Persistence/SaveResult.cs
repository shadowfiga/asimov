namespace Graphite.Engine.Persistence;

public enum SaveStatus { Success, NotFound, Corrupt, Incompatible, IoError }

public sealed record SaveResult(SaveStatus Status, string? Error = null, bool RecoveredFromBackup = false)
{
    public bool IsSuccess => Status == SaveStatus.Success;
}

public sealed record SaveResult<T>(SaveStatus Status, T? Value = default, string? Error = null, bool RecoveredFromBackup = false)
{
    public bool IsSuccess => Status == SaveStatus.Success;
}

public sealed record SaveSlotInfo(Guid Id, string Name, DateTimeOffset? CreatedUtc,
    DateTimeOffset? UpdatedUtc, int SchemaVersion, SaveStatus Status, bool RecoveredFromBackup = false);
