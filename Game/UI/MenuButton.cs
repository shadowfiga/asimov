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
    private readonly MenuButtonTokens _tokens;
    private readonly int _fontSize;
    private readonly int _minimumFontSize;
    private readonly int _iconSize;
    private readonly int _trailingIconSize;
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled) _iconImages;
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled)? _arrowImages;

    internal bool IsContentHighlighted
        => _label.TextColor == GameThemes.DeepDrive.SelectionHighlight &&
           ReferenceEquals(_icon.Renderable, _iconImages.Highlight) &&
           (_arrow is null || ReferenceEquals(_arrow.Renderable, _arrowImages!.Value.Highlight));

    internal MenuButton(
        MenuAssets assets,
        string text,
        string icon,
        bool primary = false,
        bool arrow = true,
        MenuButtonTextVariant textVariant = MenuButtonTextVariant.Default,
        int? iconSize = null,
        int? trailingIconSize = null)
    {
        var theme = GameThemes.DeepDrive;
        _tokens = theme.MenuButton;
        _fontSize = _tokens.FontSize(textVariant);
        _minimumFontSize = _tokens.MinimumFontSize(textVariant);
        _iconSize = SizeOverride(iconSize, _tokens.MinimumIconSize, nameof(iconSize)) ?? _tokens.IconSize;
        _trailingIconSize = SizeOverride(
            trailingIconSize,
            _tokens.MinimumTrailingIconSize,
            nameof(trailingIconSize)) ?? _tokens.TrailingIconSize;
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
            ColumnSpacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _layout.ColumnsProportions.Add(Proportion.Auto);
        _layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _icon = assets.Icon(icon, _iconSize, theme.SecondaryText);
        _iconImages = IconImages(_icon);
        _layout.Widgets.Add(_icon);
        _label = new Label
        {
            Text = text,
            Font = ThemeAssets.Font(_fontSize),
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
            _layout.ColumnsProportions.Add(Proportion.Auto);
            _arrow = assets.Icon("chevron-right", _trailingIconSize, theme.Disabled);
            _arrowImages = IconImages(_arrow);
            Grid.SetColumn(_arrow, 2);
            _layout.Widgets.Add(_arrow);
        }

        Content = _layout;
        Resize(1);
        MouseEntered += RefreshContentState;
        MouseLeft += RefreshContentState;
        PressedChanged += RefreshContentState;
        KeyboardFocusChanged += RefreshContentState;
        EnabledChanged += RefreshContentState;
        RefreshContentState();
    }

    internal void Resize(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Menu-button scale must be positive and finite.");
        }

        var horizontalPadding = ScaleSpacing(_tokens.HorizontalPadding, scale);
        var verticalPadding = ScaleSpacing(_tokens.VerticalPadding, scale);
        _layout.Padding = new Thickness(horizontalPadding, verticalPadding);
        _icon.Margin = new Thickness(0, 0, ScaleSpacing(_tokens.IconTextSpacing, scale), 0);
        _label.Font = ThemeAssets.Font(ScaleDimension(_fontSize, _minimumFontSize, scale));
        _icon.Width = _icon.Height = ScaleDimension(_iconSize, _tokens.MinimumIconSize, scale);
        if (_arrow is not null)
        {
            _arrow.Margin = new Thickness(ScaleSpacing(_tokens.TextTrailingIconSpacing, scale), 0, 0, 0);
            _arrow.Width = _arrow.Height = ScaleDimension(_trailingIconSize, _tokens.MinimumTrailingIconSize, scale);
        }
    }

    internal void SetText(string text) => _label.Text = text;

    private static int? SizeOverride(int? value, int minimum, string parameter)
    {
        if (value is not null && value < minimum)
        {
            throw new ArgumentOutOfRangeException(
                parameter,
                value,
                $"Menu-button sizes must be at least the themed minimum of {minimum}.");
        }

        return value;
    }

    private static int ScaleDimension(int value, int minimum, float scale)
        => Math.Max(minimum, (int)(value * scale));

    private static int ScaleSpacing(int value, float scale)
        => Math.Max(0, (int)(value * scale));

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
