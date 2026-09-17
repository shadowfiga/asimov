namespace Graphite.Game.Data;

/// <summary>Game-side access to Chisel's unchanged enum-indexed array exports.</summary>
public static class ChiselIdExtensions
{
    /// <summary>Converts a generated ID to its array index. Column access enforces bounds.</summary>
    public static int ToInt<TId>(this TId id) where TId : struct, Enum
        => Convert.ToInt32(id);
}
