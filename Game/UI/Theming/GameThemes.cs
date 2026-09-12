using System.Globalization;
using Microsoft.Xna.Framework;

namespace Graphite.Game.UI.Theming;

public static class GameThemes
{
    public static readonly GameTheme Aftergreen = new()
    {
        BackgroundDark = Hex("#081217"),
        Background = Hex("#0E1B1F"),
        BackgroundLight = Hex("#1B2C31"),
        Foreground = Hex("#F2F7F4"),

        Gray1 = Hex("#253A41"),
        Gray2 = Hex("#3F5B63"),
        Gray3 = Hex("#608089"),
        Gray4 = Hex("#82939A"),
        Gray5 = Hex("#C5D4D9"),

        Color1 = Hex("#2E7D32"),
        Color2 = Hex("#7ED957"),
        Color3 = Hex("#A3C957"),
        Color4 = Hex("#19A7A5"),
        Color5 = Hex("#8B6F4E"),
        Color6 = Hex("#3FA9F5"),

        Success = Hex("#4CD964"),
        Warning = Hex("#FFB020"),
        Destructive = Hex("#B5523C"),
        Info = Hex("#2EC4E6")
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
}
