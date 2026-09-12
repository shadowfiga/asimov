using Graphite.Engine.Configuration;

namespace Graphite.Engine.Persistence;

public static class PersistencePaths
{
    public static string ForGame(string gameId, SettingsEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(gameId) || gameId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            throw new ArgumentException("Use a stable game ID containing letters, numbers, hyphens or underscores.", nameof(gameId));
        }

        var environmentName = environment switch
        {
            SettingsEnvironment.Staging => "staging",
            SettingsEnvironment.Production => "production",
            _ => throw new ArgumentOutOfRangeException(nameof(environment))
        };
        var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(directory))
        {
            throw new IOException("The user data directory is unavailable.");
        }

        return Path.Combine(directory, gameId, environmentName);
    }
}
