using Graphite.Engine.UI.Theming;
using Graphite.Game.UI.Theming;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal static class DialogLayout
{
    internal static Label Title(string text, int size = 32) => new()
    {
        Text = text,
        Font = ThemeAssets.Font(size),
        TextColor = GameThemes.DeepDrive.PrimaryText,
        HorizontalAlignment = HorizontalAlignment.Left
    };

    internal static RoundedRectangleBrush Surface()
    {
        var theme = GameThemes.DeepDrive;
        return new(theme.RaisedSurface, theme.BorderRadius.Xs, theme.Border, 1);
    }

    internal static VerticalStackPanel Content(string title, int width)
    {
        var content = new VerticalStackPanel
        {
            Width = width,
            Spacing = GameThemes.DeepDrive.Spacing.Md,
            Padding = new Thickness(GameThemes.DeepDrive.Spacing.Xl),
            Background = Surface(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(Title(title));
        return content;
    }
}
