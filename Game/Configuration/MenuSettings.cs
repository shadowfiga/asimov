namespace Graphite.Game.Configuration;

public sealed class MenuSettings
{
    private IReadOnlyList<string> _credits = [];

    public required Uri? DiscordUrl { get; init; }
    public required IReadOnlyList<string> Credits
    {
        get => _credits;
        init => _credits = value is null ? [] : Array.AsReadOnly(value.ToArray());
    }
}
