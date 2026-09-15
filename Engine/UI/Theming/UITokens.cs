namespace Graphite.Engine.UI.Theming;

public sealed record UISpacing
{
    public static UISpacing Default { get; } = new();
    public int Xs { get; init; } = 4;
    public int Sm { get; init; } = 8;
    public int Md { get; init; } = 16;
    public int Lg { get; init; } = 24;
    public int Xl { get; init; } = 32;
}

public sealed record UIBorderRadii
{
    public static UIBorderRadii Default { get; } = new();
    public static UIBorderRadii Square
    {
        get;
    } = new()
    {
        Xs = 0,
        Sm = 0,
        Md = 0,
        Lg = 0,
        Xl = 0,
        Full = 0
    };
    public int Zero => 0;
    public int Xs { get; init; } = 2;
    public int Sm { get; init; } = 4;
    public int Md { get; init; } = 8;
    public int Lg { get; init; } = 12;
    public int Xl { get; init; } = 16;
    /// <summary>Clamped to half the shortest side when rendered, producing a pill or circle.</summary>
    public int Full { get; init; } = int.MaxValue;
}
