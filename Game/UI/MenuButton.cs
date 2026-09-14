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
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled) _iconImages;
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled)? _arrowImages;

    internal bool IsContentHighlighted
        => _label.TextColor == GameThemes.DeepDrive.SelectionHighlight &&
           ReferenceEquals(_icon.Renderable, _iconImages.Highlight) &&
           (_arrow is null || ReferenceEquals(_arrow.Renderable, _arrowImages!.Value.Highlight));

    internal MenuButton(MenuAssets assets, string text, string icon, bool primary = false, bool arrow = true)
    {
        var theme = GameThemes.DeepDrive;
        var radius = theme.BorderRadius.Xs;
        var fill = primary ? Color.Lerp(theme.ControlSurface, theme.Selection, .09f) : theme.DeepBlack;
        var border = primary ? theme.Selection : theme.Border;
        Background = new RoundedRectangleBrush(fill, radius, border, 1);
        DisabledBackground = new RoundedRectangleBrush(theme.DeepBlack, radius, theme.Border, 1);
        OverBackground = new RoundedRectangleBrush(
            Color.Lerp(theme.ControlSurface, theme.Selection, .16f), radius, theme.SelectionHighlight, 2);
        FocusedBackground = new RoundedRectangleBrush(
            Color.Lerp(theme.ControlSurface, theme.Selection, .1f), radius, theme.SelectionHighlight, 2);
        PressedBackground = new RoundedRectangleBrush(theme.Selection, radius, theme.SelectionHighlight, 1);
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
        _icon = assets.Icon(icon, 28, theme.SecondaryText);
        _iconImages = IconImages(_icon);
        _layout.Widgets.Add(_icon);
        _label = new Label
        {
            Text = text,
            Font = ThemeAssets.Font(24),
            TextColor = theme.SecondaryText,
            DisabledTextColor = theme.Disabled,
            OverTextColor = theme.SelectionHighlight,
            FocusedTextColor = theme.SelectionHighlight,
            PressedTextColor = theme.DeepBlack,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_label, 1);
        _layout.Widgets.Add(_label);
        if (arrow)
        {
            _arrow = assets.Icon("chevron-right", 18, theme.Disabled);
            _arrowImages = IconImages(_arrow);
            Grid.SetColumn(_arrow, 2);
            _layout.Widgets.Add(_arrow);
        }

        Content = _layout;
        MouseEntered += RefreshContentState;
        MouseLeft += RefreshContentState;
        PressedChanged += RefreshContentState;
        KeyboardFocusChanged += RefreshContentState;
        EnabledChanged += RefreshContentState;
        RefreshContentState();
    }

    internal void Resize(float scale, int fontSize = 34)
    {
        var spacing = GameThemes.DeepDrive.Spacing;
        _layout.Padding = new Thickness((int)(spacing.Lg * scale), 0);
        _layout.ColumnSpacing = (int)(spacing.Lg * scale);
        _label.Font = ThemeAssets.Font(Math.Max(17, (int)(fontSize * scale)));
        _icon.Width = _icon.Height = Math.Max(22, (int)(34 * scale));
        if (_arrow is not null)
        {
            _arrow.Width = _arrow.Height = Math.Max(14, (int)(20 * scale));
        }
    }

    internal void SetText(string text) => _label.Text = text;

    private static (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled) IconImages(Image image)
        => (
            image.Renderable ?? throw new InvalidOperationException("Menu icon needs a normal renderable."),
            image.OverRenderable ?? throw new InvalidOperationException("Menu icon needs a hover renderable."),
            image.PressedRenderable ?? throw new InvalidOperationException("Menu icon needs a pressed renderable."),
            image.DisabledRenderable ?? throw new InvalidOperationException("Menu icon needs a disabled renderable."));

    private void RefreshContentState(object sender, Myra.Events.MyraEventArgs args)
        => RefreshContentState();

    private void RefreshContentState()
    {
        var theme = GameThemes.DeepDrive;
        var highlighted = IsMouseInside || IsKeyboardFocused;
        _label.TextColor = !Enabled
            ? theme.Disabled
            : IsPressed
                ? theme.DeepBlack
                : highlighted
                    ? theme.SelectionHighlight
                    : theme.SecondaryText;
        _icon.Renderable = Select(_iconImages, highlighted);
        if (_arrow is not null && _arrowImages is { } arrowImages)
        {
            _arrow.Renderable = Select(arrowImages, highlighted);
        }
    }

    private IImage Select(
        (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled) images,
        bool highlighted)
        => !Enabled ? images.Disabled : IsPressed ? images.Pressed : highlighted ? images.Highlight : images.Normal;
}
