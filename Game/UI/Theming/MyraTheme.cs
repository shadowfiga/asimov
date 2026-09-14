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
        Surface(styles.TooltipStyle, theme.RaisedSurface, theme);
        styles.TooltipStyle.Padding = new Thickness(theme.Spacing.Sm);
        Surface(styles.PanelStyle, theme.RaisedSurface, theme);
        styles.PanelStyles["root"] = new WidgetStyle { Background = Brush(theme.Background) };
        styles.PanelStyles["nested"] = new WidgetStyle { Background = Brush(theme.ControlSurface) };
        styles.LabelStyles["secondary"] = new LabelStyle
        {
            Font = styles.LabelStyle.Font,
            TextColor = theme.SecondaryText,
            DisabledTextColor = theme.Disabled
        };

        Button(styles.ButtonStyle, theme);
        Button(styles.CheckBoxStyle, theme);
        foreach (var button in styles.ButtonStyles.Values)
        {
            Button(button, theme);
        }
        Input(styles.TextBoxStyle, theme);
        Surface(styles.WindowStyle, theme.RaisedSurface, theme);
        Text(styles.WindowStyle.TitleStyle, theme);
        Button(styles.WindowStyle.CloseButtonStyle, theme);
        Separator(styles.HorizontalSeparatorStyle, theme);
        Separator(styles.VerticalSeparatorStyle, theme);
        Progress(styles.HorizontalProgressBarStyle, theme);
        Progress(styles.VerticalProgressBarStyle, theme);
        Slider(styles.HorizontalSliderStyle, theme);
        Surface(styles.ListBoxStyle, theme.RaisedSurface, theme);
        Button(styles.ListBoxStyle.ListItemStyle, theme);
        Separator(styles.ListBoxStyle.SeparatorStyle, theme);
        Button(styles.ComboBoxStyle, theme);
        Text(styles.ComboBoxStyle.LabelStyle, theme);
        Surface(styles.ComboBoxStyle.ListBoxStyle, theme.RaisedSurface, theme);
        Button(styles.ComboBoxStyle.ListBoxStyle.ListItemStyle, theme);
        Button(styles.TabControlStyle.TabItemStyle, theme);
        Text(styles.TabControlStyle.TabItemStyle.LabelStyle, theme);
        Surface(styles.TabControlStyle.ContentStyle, theme.RaisedSurface, theme);
        Button(styles.TabControlStyle.CloseButtonStyle, theme);
        Stylesheet.Current = styles;
    }

    private static SolidBrush Brush(Color color) => new(color);

    private static void Text(LabelStyle style, GameTheme theme)
    {
        style.TextColor = theme.PrimaryText;
        style.DisabledTextColor = theme.Disabled;
        style.OverTextColor = style.FocusedTextColor = theme.SelectionHighlight;
        style.PressedTextColor = theme.DeepBlack;
    }

    private static void Surface(WidgetStyle style, Color background, GameTheme theme)
    {
        var surface = new RoundedRectangleBrush(background, theme.BorderRadius.Sm, theme.Border, 1);
        style.Background = style.DisabledBackground = surface;
        style.OverBackground = style.PressedBackground = surface;
        style.FocusedBackground = new RoundedRectangleBrush(background, theme.BorderRadius.Sm, theme.SelectionHighlight, 1);
        style.Border = style.OverBorder = style.DisabledBorder = style.PressedBorder = style.FocusedBorder = null;
        style.BorderThickness = new Thickness(theme.BorderRadius.Zero);
    }

    private static void Button(ButtonStyle style, GameTheme theme)
    {
        Surface(style, theme.ControlSurface, theme);
        style.OverBackground = new RoundedRectangleBrush(
            Color.Lerp(theme.ControlSurface, theme.Selection, .16f), theme.BorderRadius.Sm, theme.SelectionHighlight, 1);
        style.PressedBackground = new RoundedRectangleBrush(theme.Selection, theme.BorderRadius.Sm, theme.SelectionHighlight, 1);
    }

    private static void Input(TextBoxStyle style, GameTheme theme)
    {
        Surface(style, theme.ControlSurface, theme);
        style.TextColor = theme.PrimaryText;
        style.DisabledTextColor = theme.Disabled;
        style.OverTextColor = style.FocusedTextColor = theme.SelectionHighlight;
        style.PressedTextColor = theme.DeepBlack;
        style.Selection = Brush(Color.Lerp(theme.ControlSurface, theme.Selection, .5f));
    }

    private static void Separator(SeparatorStyle style, GameTheme theme)
    {
        // Clear the default textured image so the palette brush supplies the line.
        style.Image = style.OverImage = style.DisabledImage = style.FocusedImage = style.PressedImage = null;
        style.Background = Brush(theme.Border);
        style.Thickness = 1;
    }

    private static void Progress(ProgressBarStyle style, GameTheme theme)
    {
        Surface(style, theme.ControlSurface, theme);
        style.Filler = Brush(theme.Selection);
    }

    private static void Slider(SliderStyle style, GameTheme theme)
    {
        var transparent = Brush(Color.Transparent);
        style.Background = style.DisabledBackground = transparent;
        style.OverBackground = style.FocusedBackground = style.PressedBackground = transparent;
        style.Border = style.DisabledBorder = style.OverBorder = style.PressedBorder = Brush(theme.Border);
        style.FocusedBorder = Brush(theme.SelectionHighlight);
        style.BorderThickness = new Thickness(1);
        style.Height = 20;
        Button(style.KnobStyle, theme);
        style.KnobStyle.Background = Brush(theme.SecondaryText);
        style.KnobStyle.OverBackground = style.KnobStyle.FocusedBackground = Brush(theme.PrimaryText);
        style.KnobStyle.PressedBackground = Brush(theme.SelectionHighlight);
        style.KnobStyle.Width = 10;
        style.KnobStyle.Height = 24;
        var knobImage = style.KnobStyle.ImageStyle;
        knobImage.Image = knobImage.DisabledImage = knobImage.OverImage = null;
        knobImage.FocusedImage = knobImage.PressedImage = null;
    }
}
