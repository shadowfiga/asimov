using Graphite.Engine.Persistence;

namespace Graphite.Game.Persistence;

public static class SessionMigrations
{
    // Version 1 is the initial format. Add one migration for each future schema increment.
    public static IReadOnlyList<SaveMigration> All { get; } = [];
}
