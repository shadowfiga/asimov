using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI.Styles;
using Graphite.Engine.UI.Theming;

namespace Graphite.Game.UI.Theming;

public static class MyraTheme
{
    // Apply before constructing screens; use Abel with Myra's control glyphs.
    public static void Apply(GameTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var styles = ThemeAssets.LoadStylesheet();
        styles.DesktopStyle = new DesktopStyle { Background = Brush(theme.Background) };
        styles.PanelStyle = new WidgetStyle();
        Text(styles.LabelStyle, theme);
        Text(styles.TooltipStyle, theme);
        Surface(styles.TooltipStyle, theme.Gray1, theme);
        styles.TooltipStyle.Padding = new Thickness(theme.Spacing.Sm);
        Surface(styles.PanelStyle, theme.Gray1, theme);
        styles.PanelStyles["root"] = new WidgetStyle { Background = Brush(theme.Background) };
        styles.PanelStyles["nested"] = new WidgetStyle { Background = Brush(theme.Gray2) };
        styles.LabelStyles["secondary"] = new LabelStyle
        {
            Font = styles.LabelStyle.Font,
            TextColor = theme.Gray5,
            DisabledTextColor = theme.Gray5
        };

        foreach (var button in styles.ButtonStyles.Values)
        {
            Button(button, theme);
        }
        Input(styles.TextBoxStyle, theme);
        Surface(styles.WindowStyle, theme.Gray1, theme);
        Text(styles.WindowStyle.TitleStyle, theme);
        Button(styles.WindowStyle.CloseButtonStyle, theme);
        Separator(styles.HorizontalSeparatorStyle, theme);
        Separator(styles.VerticalSeparatorStyle, theme);
        Progress(styles.HorizontalProgressBarStyle, theme);
        Progress(styles.VerticalProgressBarStyle, theme);
        Surface(styles.ListBoxStyle, theme.Gray1, theme);
        Button(styles.ListBoxStyle.ListItemStyle, theme);
        Separator(styles.ListBoxStyle.SeparatorStyle, theme);
        Button(styles.ComboBoxStyle, theme);
        Text(styles.ComboBoxStyle.LabelStyle, theme);
        Surface(styles.ComboBoxStyle.ListBoxStyle, theme.Gray1, theme);
        Button(styles.ComboBoxStyle.ListBoxStyle.ListItemStyle, theme);
        Button(styles.TabControlStyle.TabItemStyle, theme);
        Text(styles.TabControlStyle.TabItemStyle.LabelStyle, theme);
        Surface(styles.TabControlStyle.ContentStyle, theme.Gray1, theme);
        Button(styles.TabControlStyle.CloseButtonStyle, theme);
        Stylesheet.Current = styles;
    }

    private static SolidBrush Brush(Color color) => new(color);

    private static void Text(LabelStyle style, GameTheme theme)
    {
        style.TextColor = theme.Foreground;
        style.DisabledTextColor = theme.Gray5;
        style.OverTextColor = style.FocusedTextColor = style.PressedTextColor = theme.Foreground;
    }

    private static void Surface(WidgetStyle style, Color background, GameTheme theme)
    {
        var surface = new RoundedRectangleBrush(background, theme.BorderRadius.Sm, theme.Gray3, 1);
        style.Background = style.DisabledBackground = surface;
        style.OverBackground = style.PressedBackground = surface;
        style.FocusedBackground = new RoundedRectangleBrush(background, theme.BorderRadius.Sm, theme.Foreground, 1);
        style.Border = style.OverBorder = style.DisabledBorder = style.PressedBorder = style.FocusedBorder = null;
        style.BorderThickness = new Thickness(theme.BorderRadius.Zero);
    }

    private static void Button(ButtonStyle style, GameTheme theme)
    {
        Surface(style, theme.Gray2, theme);
        style.OverBackground = new RoundedRectangleBrush(theme.Gray3, theme.BorderRadius.Sm, theme.Gray4, 1);
        style.PressedBackground = new RoundedRectangleBrush(theme.Gray1, theme.BorderRadius.Sm, theme.Gray3, 1);
    }

    private static void Input(TextBoxStyle style, GameTheme theme)
    {
        Surface(style, theme.Gray2, theme);
        style.TextColor = theme.Foreground;
        style.DisabledTextColor = theme.Gray5;
        style.OverTextColor = style.FocusedTextColor = style.PressedTextColor = theme.Foreground;
        style.Selection = Brush(theme.Gray3);
    }

    private static void Separator(SeparatorStyle style, GameTheme theme)
    {
        // Clear the default textured image so the palette brush supplies the line.
        style.Image = style.OverImage = style.DisabledImage = style.FocusedImage = style.PressedImage = null;
        style.Background = Brush(theme.Gray3);
        style.Thickness = 1;
    }

    private static void Progress(ProgressBarStyle style, GameTheme theme)
    {
        Surface(style, theme.Gray2, theme);
        style.Filler = Brush(theme.Gray3);
    }
}
