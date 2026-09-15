using Graphite.Engine.UI.Theming;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

internal sealed class MenuButton : Button
{
    private readonly Label _label;
    private readonly Image? _icon;
    private readonly Image? _arrow;
    private readonly Grid _layout;
    private readonly Proportion? _iconTextSpacing;
    private readonly Proportion? _textTrailingIconSpacing;
    private readonly MenuButtonTokens _tokens;
    private readonly int _fontSize;
    private readonly int _minimumFontSize;
    private readonly int _iconSize;
    private readonly int _trailingIconSize;
    private readonly Color _normalTextColor;
    private readonly Color _highlightColor;
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled)? _iconImages;
    private readonly (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled)? _arrowImages;

    internal bool IsContentHighlighted
        => _label.TextColor == _highlightColor &&
           (_icon is null || ReferenceEquals(_icon.Renderable, _iconImages!.Value.Highlight)) &&
           (_arrow is null || ReferenceEquals(_arrow.Renderable, _arrowImages!.Value.Highlight));

    internal MenuButton(
        MenuAssets assets,
        string text,
        string? icon = null,
        bool primary = false,
        bool? arrow = null,
        MenuButtonTextVariant textVariant = MenuButtonTextVariant.Default,
        int? iconSize = null,
        int? trailingIconSize = null,
        MenuButtonTone tone = MenuButtonTone.Default)
    {
        var theme = GameThemes.DeepDrive;
        if (!Enum.IsDefined(tone))
        {
            throw new ArgumentOutOfRangeException(nameof(tone));
        }
        var danger = tone == MenuButtonTone.Danger;
        var accent = danger ? theme.Danger : theme.Selection;
        _normalTextColor = danger ? theme.Danger : theme.SecondaryText;
        _highlightColor = danger ? theme.DangerHighlight : theme.SelectionHighlight;
        var showArrow = arrow ?? icon is not null;
        _tokens = theme.MenuButton;
        _fontSize = _tokens.FontSize(textVariant);
        _minimumFontSize = _tokens.MinimumFontSize(textVariant);
        _iconSize = SizeOverride(iconSize, _tokens.MinimumIconSize, nameof(iconSize)) ?? _tokens.IconSize;
        _trailingIconSize = SizeOverride(
            trailingIconSize,
            _tokens.MinimumTrailingIconSize,
            nameof(trailingIconSize)) ?? _tokens.TrailingIconSize;
        var radius = theme.BorderRadius.Xs;
        var fill = primary || danger ? Color.Lerp(theme.ControlSurface, accent, .09f) : theme.DeepBlack;
        var border = primary || danger ? accent : theme.Border;
        Background = new RoundedRectangleBrush(fill, radius, border, 1);
        DisabledBackground = new RoundedRectangleBrush(theme.DeepBlack, radius, theme.Border, 1);
        OverBackground = new RoundedRectangleBrush(
            Color.Lerp(theme.ControlSurface, accent, .16f), radius, _highlightColor, 2);
        FocusedBackground = new RoundedRectangleBrush(
            Color.Lerp(theme.ControlSurface, accent, .1f), radius, _highlightColor, 2);
        PressedBackground = new RoundedRectangleBrush(accent, radius, _highlightColor, 1);
        Border = OverBorder = FocusedBorder = PressedBorder = DisabledBorder = null;
        BorderThickness = new Thickness(theme.BorderRadius.Zero);

        _layout = new Grid
        {
            ColumnSpacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        if (icon is not null)
        {
            _layout.ColumnsProportions.Add(Proportion.Auto);
            _iconTextSpacing = new Proportion(ProportionType.Pixels, _tokens.IconTextSpacing);
            _layout.ColumnsProportions.Add(_iconTextSpacing);
            _icon = assets.Icon(icon, _iconSize, _normalTextColor, _highlightColor);
            _iconImages = IconImages(_icon);
            _layout.Widgets.Add(_icon);
        }
        _layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _label = new Label
        {
            Text = text,
            Font = ThemeAssets.Font(_fontSize),
            TextColor = _normalTextColor,
            DisabledTextColor = theme.Disabled,
            OverTextColor = _highlightColor,
            FocusedTextColor = _highlightColor,
            PressedTextColor = theme.DeepBlack,
            HorizontalAlignment = icon is null && !showArrow ? HorizontalAlignment.Center : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_label, _layout.ColumnsProportions.Count - 1);
        _layout.Widgets.Add(_label);
        if (showArrow)
        {
            _textTrailingIconSpacing = new Proportion(ProportionType.Pixels, _tokens.TextTrailingIconSpacing);
            _layout.ColumnsProportions.Add(_textTrailingIconSpacing);
            _layout.ColumnsProportions.Add(Proportion.Auto);
            _arrow = assets.Icon("chevron-right", _trailingIconSize, danger ? theme.Danger : theme.Disabled, _highlightColor);
            _arrowImages = IconImages(_arrow);
            Grid.SetColumn(_arrow, _layout.ColumnsProportions.Count - 1);
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
        if (_iconTextSpacing is not null)
        {
            _iconTextSpacing.Value = ScaleSpacing(_tokens.IconTextSpacing, scale);
        }
        _label.Font = ThemeAssets.Font(ScaleDimension(_fontSize, _minimumFontSize, scale));
        if (_icon is not null)
        {
            _icon.Width = _icon.Height = ScaleDimension(_iconSize, _tokens.MinimumIconSize, scale);
        }
        if (_arrow is not null)
        {
            _textTrailingIconSpacing!.Value = ScaleSpacing(_tokens.TextTrailingIconSpacing, scale);
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
                    ? _highlightColor
                    : _normalTextColor;
        if (_icon is not null && _iconImages is { } iconImages)
        {
            _icon.Renderable = Select(iconImages, highlighted);
        }
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
