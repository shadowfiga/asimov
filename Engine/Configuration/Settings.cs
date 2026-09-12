namespace Graphite.Engine.Configuration;

public enum SettingsEnvironment
{
    Staging,
    Production
}

public abstract class Settings
{
    public abstract SettingsEnvironment Environment { get; }
    public abstract GameIdentitySettings Game { get; }
    public abstract WindowSettings Window { get; }
    public abstract GraphicsSettings Graphics { get; }
    public abstract RuntimeSettings Runtime { get; }

    public string? IconPath => Game.Icon is null ? null : Path.GetFullPath(Game.Icon, AppContext.BaseDirectory);

    public virtual void Validate()
    {
        if (Game is null || Window is null || Graphics is null
            || Runtime is null)
        {
            throw new InvalidDataException("Settings sections cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(Game.Name) || string.IsNullOrWhiteSpace(Game.StartupScene))
        {
            throw new InvalidDataException("game.name and game.startupScene must be non-empty strings.");
        }
        if (Game.Icon is not null && string.IsNullOrWhiteSpace(Game.Icon))
        {
            throw new InvalidDataException("game.icon must be null or a non-empty path.");
        }
        if (Window.Width is < 1 or > 16384 || Window.Height is < 1 or > 16384)
        {
            throw new InvalidDataException("window.width and window.height must be between 1 and 16384.");
        }
        if (Runtime.TargetFramesPerSecond is < 1 or > 1000)
        {
            throw new InvalidDataException("runtime.targetFramesPerSecond must be between 1 and 1000.");
        }
    }
}
