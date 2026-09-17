namespace Graphite.Game.Data;

/// <summary>Game-side access to Chisel's unchanged enum-indexed array exports.</summary>
public static class ChiselIdExtensions
{
    /// <summary>Returns a valid generated ID's array index; invalid or undefined IDs fail immediately.</summary>
    public static int ToInt<TId>(this TId id) where TId : struct, Enum
    {
        var index = Convert.ToInt32(id);
        if (index < 0 || !Enum.IsDefined(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Expected a valid Chisel ID.");
        }
        return index;
    }
}
