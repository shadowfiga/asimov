using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Configuration;

public sealed class GraphiteSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public GameIdentitySettings Game { get; init; } = new();
    public WindowSettings Window { get; init; } = new();
    public GraphicsSettings Graphics { get; init; } = new();
    public RuntimeSettings Runtime { get; init; } = new();
    public ContentSettings Content { get; init; } = new();
    public UISettings UI { get; init; } = new();

    [JsonIgnore]
    public string SourcePath { get; private set; } = string.Empty;

    public static GraphiteSettings Load(string? path = null)
    {
        var sourcePath = Path.GetFullPath(path ?? Path.Combine(AppContext.BaseDirectory, "settings.json"));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Graphite requires a settings.json file beside the executable.", sourcePath);

        try
        {
            using var stream = File.OpenRead(sourcePath);
            var settings = JsonSerializer.Deserialize<GraphiteSettings>(stream, SerializerOptions)
                ?? throw new InvalidDataException($"Graphite settings are empty: {sourcePath}");

            settings.SourcePath = sourcePath;
            settings.Validate();
            return settings;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Invalid Graphite settings at {sourcePath}:{exception.LineNumber}:{exception.BytePositionInLine}.",
                exception);
        }
    }

    public string? ResolveIconPath()
    {
        if (Game.Icon is null)
            return null;

        var settingsDirectory = Path.GetDirectoryName(SourcePath)
            ?? throw new InvalidOperationException($"Settings path has no parent directory: {SourcePath}");
        return Path.GetFullPath(Game.Icon, settingsDirectory);
    }

    private void Validate()
    {
        if (Game is null || Window is null || Graphics is null || Runtime is null || Content is null || UI is null)
            throw new InvalidDataException("Graphite settings sections cannot be null.");
        if (string.IsNullOrWhiteSpace(Game.Name))
            throw new InvalidDataException("game.name must not be empty.");
        if (Game.Icon is not null && string.IsNullOrWhiteSpace(Game.Icon))
            throw new InvalidDataException("game.icon must be null or a non-empty path.");
        if (Window.Width is < 1 or > 16384 || Window.Height is < 1 or > 16384)
            throw new InvalidDataException("window.width and window.height must be between 1 and 16384.");
        if (Runtime.TargetFramesPerSecond is < 1 or > 1000)
            throw new InvalidDataException("runtime.targetFramesPerSecond must be between 1 and 1000.");
        if (string.IsNullOrWhiteSpace(Content.RootDirectory))
            throw new InvalidDataException("content.rootDirectory must not be empty.");
        if (string.IsNullOrWhiteSpace(UI.Project))
            throw new InvalidDataException("ui.project must not be empty.");

        Graphics.ParseClearColor();
    }
}

public sealed class GameIdentitySettings
{
    public string Name { get; init; } = "Graphite";
    public string? Icon { get; init; }
}

public sealed class WindowSettings
{
    public int Width { get; init; } = 960;
    public int Height { get; init; } = 540;
    public bool Fullscreen { get; init; }
    public bool Borderless { get; init; }
    public bool Resizable { get; init; } = true;
}

public sealed class GraphicsSettings
{
    public string ClearColor { get; init; } = "#16181F";
    public bool VSync { get; init; } = true;
    public bool PreferMultiSampling { get; init; }

    public Color ParseClearColor()
    {
        if (string.IsNullOrWhiteSpace(ClearColor))
            throw new InvalidDataException("graphics.clearColor must not be empty.");

        var value = ClearColor.Trim();
        if (value.StartsWith('#'))
            value = value[1..];
        if (value.Length is not (6 or 8))
            throw new InvalidDataException("graphics.clearColor must use #RRGGBB or #RRGGBBAA.");

        try
        {
            var red = ParseByte(value, 0);
            var green = ParseByte(value, 2);
            var blue = ParseByte(value, 4);
            var alpha = value.Length == 8 ? ParseByte(value, 6) : byte.MaxValue;
            return new Color(red, green, blue, alpha);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("graphics.clearColor must contain only hexadecimal digits.", exception);
        }
    }

    private static byte ParseByte(string value, int start)
        => byte.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}

public sealed class RuntimeSettings
{
    public bool MouseVisible { get; init; } = true;
    public bool FixedTimeStep { get; init; } = true;
    public int TargetFramesPerSecond { get; init; } = 60;
}

public sealed class ContentSettings
{
    public string RootDirectory { get; init; } = "Content";
}

public sealed class UISettings
{
    public string Project { get; init; } = "GumProject/Graphite.gumx";
}
