using Graphite.Engine.UI.Theming;
using Graphite.Engine.UI.Audio;
using Graphite.Game.Audio;
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
        MenuButtonTone tone = MenuButtonTone.Default,
        MenuButtonSize size = MenuButtonSize.Standard)
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
        var tokens = theme.MenuButton.Size(size);
        var fontSize = tokens.FontSize(textVariant);
        var leadingSize = SizeOverride(iconSize, tokens.MinimumIconSize, nameof(iconSize)) ?? tokens.IconSize;
        var trailingSize = SizeOverride(
            trailingIconSize,
            tokens.MinimumTrailingIconSize,
            nameof(trailingIconSize)) ?? tokens.TrailingIconSize;
        Width = tokens.Width;
        Height = tokens.Height;
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

        var layout = new Grid
        {
            Padding = new Thickness(tokens.HorizontalPadding, tokens.VerticalPadding),
            ColumnSpacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        if (icon is not null)
        {
            layout.ColumnsProportions.Add(Proportion.Auto);
            layout.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, tokens.IconTextSpacing));
            _icon = assets.Icon(icon, leadingSize, _normalTextColor, _highlightColor);
            _iconImages = IconImages(_icon);
            layout.Widgets.Add(_icon);
        }
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _label = new Label
        {
            Text = text,
            Font = ThemeAssets.Font(fontSize),
            TextColor = _normalTextColor,
            DisabledTextColor = theme.Disabled,
            OverTextColor = _highlightColor,
            FocusedTextColor = _highlightColor,
            PressedTextColor = theme.DeepBlack,
            HorizontalAlignment = icon is null && !showArrow ? HorizontalAlignment.Center : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_label, layout.ColumnsProportions.Count - 1);
        layout.Widgets.Add(_label);
        if (showArrow)
        {
            layout.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, tokens.TextTrailingIconSpacing));
            layout.ColumnsProportions.Add(Proportion.Auto);
            _arrow = assets.Icon("chevron-right", trailingSize, danger ? theme.Danger : theme.Disabled, _highlightColor);
            _arrowImages = IconImages(_arrow);
            Grid.SetColumn(_arrow, layout.ColumnsProportions.Count - 1);
            layout.Widgets.Add(_arrow);
        }

        Content = layout;
        MouseEntered += RefreshContentState;
        MouseLeft += RefreshContentState;
        PressedChanged += RefreshContentState;
        KeyboardFocusChanged += RefreshContentState;
        EnabledChanged += RefreshContentState;
        RefreshContentState();
        _ = new UIAudioFeedback(this, GameAudio.Hover, GameAudio.Click);
    }

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
        if (_icon is not null)
        {
            _icon.Renderable = Select(_iconImages!.Value, highlighted);
        }
        if (_arrow is not null)
        {
            _arrow.Renderable = Select(_arrowImages!.Value, highlighted);
        }
    }

    private IImage Select(
        (IImage Normal, IImage Highlight, IImage Pressed, IImage Disabled) images,
        bool highlighted)
        => !Enabled ? images.Disabled : IsPressed ? images.Pressed : highlighted ? images.Highlight : images.Normal;
}
