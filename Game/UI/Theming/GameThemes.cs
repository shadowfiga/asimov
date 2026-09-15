using System.Globalization;
using Microsoft.Xna.Framework;
using Graphite.Engine.UI.Theming;

namespace Graphite.Game.UI.Theming;

public static class GameThemes
{
    public static readonly GameTheme DeepDrive = new()
    {
        Spacing = UISpacing.Default,
        BorderRadius = UIBorderRadii.Square,
        MenuButton = MenuButtonTokens.Default,
        DeepBlack = Hex("#050505"),
        Background = Hex("#090A09"),
        DialogScrim = WithAlpha(Hex("#050505"), 184),
        RaisedSurface = Hex("#111211"),
        ControlSurface = Hex("#181918"),
        Border = Hex("#393B39"),
        Disabled = Hex("#70736F"),
        SecondaryText = Hex("#B7BAB6"),
        PrimaryText = Hex("#E8EAE7"),
        Selection = Hex("#E9943A"),
        SelectionHighlight = Hex("#FFA143"),
        Success = Hex("#79C98B"),
        Danger = Hex("#D96A66"),
        DangerHighlight = Hex("#FF8B85")
    };

    private static Color Hex(string value)
    {
        if (value.Length != 7 || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new ArgumentException("Expected a color in #RRGGBB format.", nameof(value));
        }

        return new Color((int)(rgb >> 16), (int)((rgb >> 8) & 255), (int)(rgb & 255));
    }

    private static Color WithAlpha(Color color, byte alpha) => new(color.R, color.G, color.B, alpha);
}
