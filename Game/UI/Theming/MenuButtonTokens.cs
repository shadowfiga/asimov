namespace Graphite.Game.UI.Theming;

public enum MenuButtonTextVariant
{
    Compact,
    Default,
    Prominent
}

public enum MenuButtonTone
{
    Default,
    Danger
}

public enum MenuButtonSize
{
    Standard,
    Menu,
    Dialog,
    Confirmation
}

public sealed record MenuButtonTokens
{
    public static MenuButtonTokens Default { get; } = new();

    public MenuButtonStyle Standard { get; init; } = new();
    public MenuButtonStyle Menu
    {
        get; init;
    } = new()
    {
        Width = 416,
        Height = 54,
        HorizontalPadding = 19,
        IconTextSpacing = 19,
        TextTrailingIconSpacing = 19,
        CompactFontSize = 19,
        DefaultFontSize = 24,
        ProminentFontSize = 27,
        IconSize = 27,
        TrailingIconSize = 16
    };
    public MenuButtonStyle Dialog
    {
        get; init;
    } = new()
    {
        Width = 200,
        Height = 48,
        DefaultFontSize = 24,
        IconSize = 28
    };
    public MenuButtonStyle Confirmation
    {
        get; init;
    } = new()
    {
        Width = 200,
        Height = 44,
        DefaultFontSize = 24,
        IconSize = 28
    };

    public MenuButtonStyle Size(MenuButtonSize size) => size switch
    {
        MenuButtonSize.Standard => Standard,
        MenuButtonSize.Menu => Menu,
        MenuButtonSize.Dialog => Dialog,
        MenuButtonSize.Confirmation => Confirmation,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown menu-button size.")
    };
}

/// <summary>Complete logical dimensions and content metrics, resolved once per component.</summary>
public sealed record MenuButtonStyle
{
    public int Width { get; init; } = 520;
    public int Height { get; init; } = 68;

    public int HorizontalPadding { get; init; } = 24;
    public int VerticalPadding
    {
        get; init;
    }
    public int IconTextSpacing { get; init; } = 24;
    public int TextTrailingIconSpacing { get; init; } = 24;

    public int CompactFontSize { get; init; } = 24;
    public int DefaultFontSize { get; init; } = 30;
    public int ProminentFontSize { get; init; } = 34;

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
}
