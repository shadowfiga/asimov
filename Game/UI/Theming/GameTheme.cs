using Microsoft.Xna.Framework;
using Graphite.Engine.UI.Theming;

namespace Graphite.Game.UI.Theming;

public sealed class GameTheme
{
    public UISpacing Spacing { get; init; } = UISpacing.Default;
    public UIBorderRadii BorderRadius { get; init; } = UIBorderRadii.Default;

    // Global surfaces
    public required Color BackgroundDark { get; init; }
    public required Color Background { get; init; }
    public required Color BackgroundLight { get; init; }
    public required Color Foreground { get; init; }

    // Neutral UI scale
    public required Color Gray1 { get; init; }
    public required Color Gray2 { get; init; }
    public required Color Gray3 { get; init; }
    public required Color Gray4 { get; init; }
    public required Color Gray5 { get; init; }

    // Game-specific colors
    public required Color Color1 { get; init; }
    public required Color Color2 { get; init; }
    public required Color Color3 { get; init; }
    public required Color Color4 { get; init; }
    public required Color Color5 { get; init; }
    public required Color Color6 { get; init; }

    // Status
    public required Color Success { get; init; }
    public required Color Warning { get; init; }
    public required Color Destructive { get; init; }
    public required Color Info { get; init; }
}
