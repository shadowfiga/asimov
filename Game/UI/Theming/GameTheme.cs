using Microsoft.Xna.Framework;
using Graphite.Engine.UI.Theming;

namespace Graphite.Game.UI.Theming;

public sealed class GameTheme
{
    public UISpacing Spacing { get; init; } = UISpacing.Default;
    public UIBorderRadii BorderRadius { get; init; } = UIBorderRadii.Default;
    public MenuButtonTokens MenuButton { get; init; } = MenuButtonTokens.Default;

    // Monochrome terminal surfaces
    public required Color DeepBlack { get; init; }
    public required Color Background { get; init; }
    public required Color RaisedSurface { get; init; }
    public required Color ControlSurface { get; init; }
    public required Color Border { get; init; }

    // Text and inactive state
    public required Color Disabled { get; init; }
    public required Color SecondaryText { get; init; }
    public required Color PrimaryText { get; init; }

    // Orange is reserved for selection, focus, action, and immediate attention.
    public required Color Selection { get; init; }
    public required Color SelectionHighlight { get; init; }

    // Green is reserved for successful, completed, purchased, valid, or confirmed state.
    public required Color Success { get; init; }
}
