namespace Graphite.Game.UI.Theming;

public enum MenuButtonTextVariant
{
    Compact,
    Default,
    Prominent
}

public sealed record MenuButtonTokens
{
    public static MenuButtonTokens Default { get; } = new();

    public int HorizontalPadding { get; init; } = 24;
    public int VerticalPadding { get; init; }
    public int IconTextSpacing { get; init; } = 24;
    public int TextTrailingIconSpacing { get; init; } = 24;

    public int CompactFontSize { get; init; } = 24;
    public int CompactMinimumFontSize { get; init; } = 17;
    public int DefaultFontSize { get; init; } = 30;
    public int DefaultMinimumFontSize { get; init; } = 17;
    public int ProminentFontSize { get; init; } = 34;
    public int ProminentMinimumFontSize { get; init; } = 19;

    public int IconSize { get; init; } = 34;
    public int MinimumIconSize { get; init; } = 22;
    public int TrailingIconSize { get; init; } = 20;
    public int MinimumTrailingIconSize { get; init; } = 14;

    public int FontSize(MenuButtonTextVariant variant) => variant switch
    {
        MenuButtonTextVariant.Compact => CompactFontSize,
        MenuButtonTextVariant.Default => DefaultFontSize,
        MenuButtonTextVariant.Prominent => ProminentFontSize,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown menu-button text variant.")
    };

    public int MinimumFontSize(MenuButtonTextVariant variant) => variant switch
    {
        MenuButtonTextVariant.Compact => CompactMinimumFontSize,
        MenuButtonTextVariant.Default => DefaultMinimumFontSize,
        MenuButtonTextVariant.Prominent => ProminentMinimumFontSize,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown menu-button text variant.")
    };
}
