namespace Graphite.Game.UI.Theming;

/// <summary>Logical resource-panel sizes; viewport and preference scaling remain engine-owned.</summary>
public sealed record ResourceHudTokens
{
    public int Width { get; init; } = 180;
    public int IconSize { get; init; } = 24;
    public int LabelFontSize { get; init; } = 14;
    public int ValueFontSize { get; init; } = 20;
    public float SurfaceOpacity { get; init; } = .9f;
}
