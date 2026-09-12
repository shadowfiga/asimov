using Graphite.Engine.UI.Theming;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal sealed class MenuButton : Button
{
    private readonly Label _label;
    private readonly Image _icon;
    private readonly Image? _arrow;
    private readonly Grid _layout;

    internal MenuButton(MenuAssets assets, string text, string icon, bool primary = false, bool arrow = true)
    {
        var theme = GameThemes.Aftergreen;
        var radius = theme.BorderRadius.Md;
        var fill = primary ? Color.Lerp(theme.Background, theme.Color4, .22f) * .88f : theme.BackgroundDark * .78f;
        var border = primary ? theme.Color4 * .85f : theme.Gray3 * .6f;
        Background = DisabledBackground = new RoundedRectangleBrush(fill, radius, border, 1);
        OverBackground = new RoundedRectangleBrush(theme.Gray1 * .94f, radius, theme.Gray5, 1);
        FocusedBackground = new RoundedRectangleBrush(theme.Gray1 * .94f, radius, theme.Foreground, 1);
        PressedBackground = new RoundedRectangleBrush(theme.Background * .98f, radius, theme.Color4, 1);
        Border = OverBorder = FocusedBorder = PressedBorder = DisabledBorder = null;
        BorderThickness = new Thickness(theme.BorderRadius.Zero);

        _layout = new Grid
        {
            ColumnSpacing = theme.Spacing.Lg,
            Padding = new Thickness(theme.Spacing.Lg, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _layout.ColumnsProportions.Add(Proportion.Auto);
        _layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _layout.ColumnsProportions.Add(Proportion.Auto);
        _layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _icon = assets.Icon(icon, 28, theme.Gray5);
        _layout.Widgets.Add(_icon);
        _label = new Label
        {
            Text = text,
            Font = ThemeAssets.Font(24),
            TextColor = theme.Gray5,
            DisabledTextColor = theme.Gray4,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_label, 1);
        _layout.Widgets.Add(_label);
        if (arrow)
        {
            _arrow = assets.Icon("chevron-right", 18, theme.Gray4);
            Grid.SetColumn(_arrow, 2);
            _layout.Widgets.Add(_arrow);
        }

        Content = _layout;
    }

    internal void Resize(float scale, int fontSize = 34)
    {
        var spacing = GameThemes.Aftergreen.Spacing;
        _layout.Padding = new Thickness((int)(spacing.Lg * scale), 0);
        _layout.ColumnSpacing = (int)(spacing.Lg * scale);
        _label.Font = ThemeAssets.Font(Math.Max(17, (int)(fontSize * scale)));
        _icon.Width = _icon.Height = Math.Max(22, (int)(34 * scale));
        if (_arrow is not null)
        {
            _arrow.Width = _arrow.Height = Math.Max(14, (int)(20 * scale));
        }
    }
}
