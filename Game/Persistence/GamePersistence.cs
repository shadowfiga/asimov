using Graphite.Engine.Persistence;
using Graphite.Game.Configuration;

namespace Graphite.Game.Persistence;

public static class GamePersistence
{
    public static string RootDirectory { get; } = PersistencePaths.ForGame("aftergreen", GameSettings.Instance.Environment);
    public static SaveStore Saves { get; } = new(new AtomicFileStorage(Path.Combine(RootDirectory, "saves")),
        new SaveSerializer(), SessionMigrations.All.ToArray());
    public static Engine.Persistence.Preferences Preferences { get; } = new(RootDirectory);
}
