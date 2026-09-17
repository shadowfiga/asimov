using Microsoft.Xna.Framework;
using Graphite.Engine.UI.Theming;

namespace Graphite.Game.UI.Theming;

public sealed class GameTheme
{
    public UISpacing Spacing { get; init; } = UISpacing.Default;
    public UIBorderRadii BorderRadius { get; init; } = UIBorderRadii.Square;
    public MenuButtonTokens MenuButton { get; init; } = MenuButtonTokens.Default;
    public ScreenLayoutTokens Layout { get; init; } = new();
    public ResourceHudTokens ResourceHud { get; init; } = new();

    // Monochrome terminal surfaces
    public required Color DeepBlack
    {
        get; init;
    }
    public required Color Background
    {
        get; init;
    }
    public required Color DialogScrim
    {
        get; init;
    }
    public required Color RaisedSurface
    {
        get; init;
    }
    public required Color ControlSurface
    {
        get; init;
    }
    public required Color Border
    {
        get; init;
    }

    // Text and inactive state
    public required Color Disabled
    {
        get; init;
    }
    public required Color SecondaryText
    {
        get; init;
    }
    public required Color PrimaryText
    {
        get; init;
    }

    // Orange is reserved for selection, focus, action, and immediate attention.
    public required Color Selection
    {
        get; init;
    }
    public required Color SelectionHighlight
    {
        get; init;
    }

    // Green is reserved for successful, completed, purchased, valid, or confirmed state.
    public required Color Success
    {
        get; init;
    }

    // Explicit cancel/destructive actions; not ordinary navigation or selection.
    public required Color Danger
    {
        get; init;
    }
    public required Color DangerHighlight
    {
        get; init;
    }
}
